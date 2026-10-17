using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Runtime coordinator for 07's freeform base building (0.5.7 slice 6, BUILD-MODE). Bridges pure
    /// <see cref="PlacedBase"/>, <see cref="KnownStructures"/>, and <see cref="BuildModeController"/> to the world:
    /// spawns and updates <see cref="StructureView"/> instances, drives the smooth-gliding placement ghost and static
    /// <see cref="Layers.PropMask"/> obstacle checks, runs 07's welding beam + weld sparks + <see cref="FeelKind.Build"/>
    /// beats, and handles hold-to-deconstruct. Allocation-free per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildField : MonoBehaviour
    {
        private const float GhostGlideHalfLife = 0.045f;
        private const float GhostYawHalfLife = 0.04f;
        private const float WinchRepeatCooldown = 0.14f;
        private const float HoverConeDegrees = 55f;
        private const float HoverDistanceWeight = 0.25f;
        private const int OverlapBufferSize = 24;

        [Tooltip("Building tuning (Assets/_Project/Data/Tuning/Gameplay/BuildingTuning.asset).")]
        [SerializeField] private BuildingTuning _tuning;

        [Tooltip("All buildable base structures (Assets/_Project/Data/Content/StructureCatalog.asset).")]
        [SerializeField] private StructureCatalog _catalog;

        private readonly Dictionary<int, StructureView> _viewsById = new Dictionary<int, StructureView>(16);
        private readonly Dictionary<string, StructureView> _ghostsById =
            new Dictionary<string, StructureView>(StructureIds.Count, StringComparer.Ordinal);
        private readonly HashSet<Collider> _managedColliders = new HashSet<Collider>();
        private readonly Collider[] _overlapBuffer = new Collider[OverlapBufferSize];
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>(6);

        private EventBus _events;
        private InputReader _input;
        private ITerrainQuery _terrain;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IViewCamera _view;
        private ISaveService _save;
        private MaterialStock _stock;
        private GameplayVisuals _visuals;
        private BeamWork _beamWork;

        private KnownStructures _known;
        private PlacedBase _placedBase;
        private BuildModeController _controller;

        private Transform _structuresRoot;
        private Transform _ghostsRoot;
        private Transform _sparkSocket;
        private RepairBeam _beam;
        private WeldSparks _sparks;

        private StructureView _activeGhost;
        private string _activeGhostId;
        private Vector3 _ghostPosition;
        private float _ghostYaw;
        private bool _ghostPoseSeeded;

        private Vector3? _aimPointOverride;
        private int _lastNavSign;
        private float _lastWinchSign;
        private float _nextWinchTime;
        private bool _waitExcavateRelease;
        private int _weldingInstanceId;
        private bool _weldLoopActive;
        private Vector3 _lastWeldLoopPoint;
        private bool _holding;
        private bool _gazing;
        private bool _initialized;

        public BuildingTuning Tuning => _tuning;

        public StructureCatalog Catalog => _catalog;

        public KnownStructures Known => _known;

        public PlacedBase PlacedBase => _placedBase;

        public BuildModeController Controller => _controller;

        public BuildModeState State => _controller != null ? _controller.State : BuildModeState.Closed;

        /// <summary>True while 07 is actively in the welding state on a placed structure.</summary>
        public bool IsWelding => _initialized && _controller.State == BuildModeState.Welding && _controller.ActiveWeldInstanceId > 0;

        /// <summary>Currently active placement ghost view (non-null only during <see cref="BuildModeState.Placing"/>).</summary>
        public StructureView GhostView => _activeGhost;

        /// <summary>Instance id of the placed structure 07 is looking at within <see cref="BuildingTuning.WeldRange"/>, or 0.</summary>
        public int HoveredInstanceId { get; private set; }

        /// <summary>World-space prompt anchor of the hovered structure.</summary>
        public Vector3 HoveredPosition { get; private set; }

        /// <summary>Current weld beam level (for tests and inspection).</summary>
        public float WeldBeamLevel => _beam != null ? _beam.Level : 0f;

        /// <summary>World-space origin point of the weld beam (for tests and inspection).</summary>
        public Vector3 WeldBeamStart => _beam != null ? _beam.Start : Vector3.zero;

        /// <summary>Live weld spark particle count (for tests and inspection).</summary>
        public int SparkCount => _sparks.ParticleCount;

        internal void Wire(BuildingTuning tuning, StructureCatalog catalog)
        {
            _tuning = tuning;
            _catalog = catalog;
        }

        internal void Wire(StructureCatalog catalog, BuildingTuning tuning)
        {
            _catalog = catalog;
            _tuning = tuning;
        }

        internal bool Initialize(GameplayServices services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            string problem = _tuning == null ? "BuildingTuning is not assigned (run Gameplay/Tuning)."
                : _catalog == null ? "StructureCatalog is not assigned (run Gameplay/Content)."
                : services.Rig == null || services.Rig.CutterOrigin == null ? "IRoverRig.CutterOrigin is not assigned."
                : _catalog.Validate();
            if (problem != null)
            {
                Debug.LogError($"{nameof(BuildField)}: {problem}", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _input = services.Input;
            _terrain = services.Terrain;
            _rover = services.Rover;
            _rig = services.Rig;
            _view = services.View;
            _save = services.Save;
            _stock = services.Materials;
            _visuals = services.Visuals;
            _beamWork = services.Beam;

            _known = new KnownStructures(_catalog, _events);
            _placedBase = new PlacedBase(_catalog, _tuning, _events, _known);
            _controller = new BuildModeController(_catalog, _known, _placedBase, _stock, _tuning);
            _controller.RefreshAvailable();

            _structuresRoot = new GameObject("PlacedStructures").transform;
            _structuresRoot.SetParent(transform, false);

            _ghostsRoot = new GameObject("PlacementGhosts").transform;
            _ghostsRoot.SetParent(transform, false);

            _sparkSocket = new GameObject("WeldSparkSocket").transform;
            _sparkSocket.SetParent(transform, false);

            _beam = new RepairBeam("BuildWeldBeam", transform, _visuals.WeldBeam, _tuning.WeldStitchRate,
                _tuning.WeldStitchSpread);
            _sparks = new WeldSparks(_sparkSocket, _tuning, _visuals.Spark);

            PrewarmGhosts();

            _subscriptions.Add(_events.Subscribe<StructurePlaced>(OnStructurePlaced));
            _subscriptions.Add(_events.Subscribe<StructureBuilt>(OnStructureBuilt));
            _subscriptions.Add(_events.Subscribe<StructureRemoved>(OnStructureRemoved));
            _subscriptions.Add(_events.Subscribe<StructureLearned>(_ => _controller.RefreshAvailable()));

            _initialized = true;
            return true;
        }

        internal IDisposable RegisterSave(ISaveService save)
        {
            return save.Register(new SaveSection<BuildingSaveData>(
                GameplaySaveKeys.Building,
                GameplaySaveKeys.BuildingVersion,
                Capture,
                Restore));
        }

        /// <summary>Looks up the spawned <see cref="StructureView"/> for a placed structure instance id.</summary>
        public bool TryGetView(int instanceId, out StructureView view)
        {
            return _viewsById.TryGetValue(instanceId, out view);
        }

        /// <summary>Sets or clears a world-space aim point override for deterministic placement testing.</summary>
        public void SetAimPointOverride(Vector3? aimPoint)
        {
            _aimPointOverride = aimPoint;
        }

        /// <summary>
        /// True when 07 is actively welding or in <see cref="BuildModeState.Closed"/> looking at an unfinished
        /// structure within <see cref="BuildingTuning.WeldRange"/> that can be welded with <c>Excavate</c>.
        /// </summary>
        public bool TryGetWeldCandidate(out Vector3 position)
        {
            if (IsWelding)
            {
                position = WeldPointOf(_controller.ActiveWeldInstanceId);
                return true;
            }

            return TryGetResumeWeldHint(out position);
        }

        /// <summary>
        /// True when 07 is in <see cref="BuildModeState.Closed"/> and looking at any placed structure within
        /// <see cref="BuildingTuning.WeldRange"/> that can be deconstructed by holding <c>Deconstruct</c>.
        /// </summary>
        public bool TryGetDeconstructCandidate(out Vector3 position)
        {
            return TryGetDeconstructHint(out position);
        }

        /// <summary>
        /// True when 07 is in <see cref="BuildModeState.Closed"/> and looking at an unfinished structure within
        /// <see cref="BuildingTuning.WeldRange"/> that can be resumed with <c>Excavate</c>.
        /// </summary>
        public bool TryGetResumeWeldHint(out Vector3 position)
        {
            if (_initialized &&
                _controller.State == BuildModeState.Closed &&
                HoveredInstanceId > 0 &&
                _placedBase.TryGetStructure(HoveredInstanceId, out PlacedStructure structure) &&
                !structure.IsBuilt)
            {
                position = HoveredPosition;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        /// <summary>
        /// True when 07 is in <see cref="BuildModeState.Closed"/> and looking at any placed structure within
        /// <see cref="BuildingTuning.WeldRange"/> that can be deconstructed by holding <c>Deconstruct</c>.
        /// </summary>
        public bool TryGetDeconstructHint(out Vector3 position)
        {
            if (_initialized &&
                _controller.State == BuildModeState.Closed &&
                HoveredInstanceId > 0 &&
                _placedBase.TryGetStructure(HoveredInstanceId, out _))
            {
                position = HoveredPosition;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Applies any structure IDs configured in <see cref="BuildingTuning.PlaytestUnlockedIds"/> to
        /// <see cref="KnownStructures"/>, refreshes <see cref="BuildModeController.AvailableStructures"/>, and returns
        /// the number of newly learned structures.
        /// </summary>
        public int ApplyPlaytestUnlocks(bool restored = false)
        {
            if (!_initialized)
            {
                return 0;
            }

            int added = _placedBase.ApplyPlaytestUnlocks(restored);
            _controller.RefreshAvailable();
            return added;
        }

        internal BuildingSaveData Capture()
        {
            return _placedBase.Capture();
        }

        internal void Restore(BuildingSaveData data)
        {
            if (!_initialized)
            {
                return;
            }

            _controller.Close();
            HideAllGhosts();
            ClearSpawnedViews();
            _placedBase.Restore(data);
            _controller.RefreshAvailable();
        }

        private void Update()
        {
            Step(Time.time, Time.deltaTime);
        }

        public void Step(float deltaTime)
        {
            Step(Time.time, deltaTime);
        }

        public void Step(float now, float deltaTime)
        {
            if (!_initialized)
            {
                return;
            }

            StepInput(now, deltaTime);

            bool weldingNow = _controller.State == BuildModeState.Welding && _controller.ActiveWeldInstanceId > 0;
            Vector3 weldPoint = weldingNow ? WeldPointOf(_controller.ActiveWeldInstanceId) : Vector3.zero;
            _beam.Step(weldingNow, weldingNow, _rig.CutterOrigin.position, weldPoint, now, deltaTime);
            StepWeldLoopAudio(weldingNow, weldPoint);
            SetHold(weldingNow);
            StepGaze(weldingNow, weldPoint);

            _placedBase.StepPower(deltaTime, 1f);
            StepPlacedViews(deltaTime);
        }

        private void StepInput(float now, float deltaTime)
        {
            bool buildPressed = _input.BuildPressed;
            bool cancelPressed = _input.Menu.CancelPressed;
            Vector2 nav = _input.Menu.Navigate;
            bool excavatePressed = _input.ExcavatePressed;
            bool excavateHeld = _input.ExcavateHeld;
            float winch = _input.Winch;
            bool deconstructHeld = _input.DeconstructHeld;

            if (!excavateHeld)
            {
                _waitExcavateRelease = false;
            }

            if (buildPressed)
            {
                if (_controller.State == BuildModeState.Welding)
                {
                    PublishCancelWeld(_controller.ActiveWeldInstanceId, _controller.ActiveWeldProgress01);
                    _weldingInstanceId = 0;
                }

                _controller.ToggleBuildMode();
                _waitExcavateRelease = excavateHeld;
            }
            else if (cancelPressed && _controller.IsOpen)
            {
                if (_controller.State == BuildModeState.Welding)
                {
                    PublishCancelWeld(_controller.ActiveWeldInstanceId, _controller.ActiveWeldProgress01);
                    _weldingInstanceId = 0;
                }

                _controller.Close();
            }

            switch (_controller.State)
            {
                case BuildModeState.Menu:
                    HideAllGhosts();
                    HoveredInstanceId = 0;
                    StepMenuState(nav, winch, excavatePressed, now);
                    break;

                case BuildModeState.Placing:
                    HoveredInstanceId = 0;
                    StepPlacingState(winch, excavatePressed, excavateHeld, now, deltaTime);
                    break;

                case BuildModeState.Welding:
                    HideAllGhosts();
                    HoveredInstanceId = 0;
                    StepWeldingState(excavateHeld, deltaTime);
                    break;

                case BuildModeState.Closed:
                default:
                    HideAllGhosts();
                    StepClosedState(excavateHeld, deconstructHeld, deltaTime);
                    break;
            }
        }

        private bool _wasSnapped;
        private bool _wasValid = true;

        private void StepMenuState(Vector2 nav, float winch, bool excavatePressed, float now)
        {
            int prevIndex = _controller.SelectedIndex;
            int navSign = Mathf.Abs(nav.x) > 0.5f && Mathf.Abs(nav.y) < 0.45f
                ? (nav.x > 0f ? 1 : -1)
                : 0;
            if (navSign != 0 && navSign != _lastNavSign)
            {
                _controller.MoveSelection(navSign);
            }

            _lastNavSign = navSign;

            if (Mathf.Abs(winch) > 0.05f && now >= _nextWinchTime)
            {
                _controller.MoveSelection(winch > 0f ? -1 : 1);
                _nextWinchTime = now + WinchRepeatCooldown;
            }

            if (_controller.SelectedIndex != prevIndex)
            {
                _events.Publish(new UiCue(UiCueKind.FocusMove));
            }

            if (excavatePressed && !_waitExcavateRelease)
            {
                if (_controller.PickSelected())
                {
                    _events.Publish(new UiCue(UiCueKind.Confirm));
                }

                _waitExcavateRelease = true;
                _ghostPoseSeeded = false;
                _wasSnapped = false;
                _wasValid = true;
            }
        }

        private void StepPlacingState(
            float winch,
            bool excavatePressed,
            bool excavateHeld,
            float now,
            float deltaTime)
        {
            _lastNavSign = 0;
            if (Mathf.Abs(winch) > 0.05f && (now >= _nextWinchTime || Mathf.Sign(winch) != _lastWinchSign))
            {
                _controller.RotateGhost(winch);
                _events.Publish(new UiCue(UiCueKind.SliderStep));
                _nextWinchTime = now + WinchRepeatCooldown;
                _lastWinchSign = Mathf.Sign(winch);
            }
            else if (Mathf.Abs(winch) <= 0.05f)
            {
                _lastWinchSign = 0f;
            }

            StructureDefinition selected = _controller.SelectedStructure;
            if (selected == null)
            {
                HideAllGhosts();
                return;
            }

            Vector3 roverPosition = _rover.Position;
            Vector3 groundPoint = SampleAimPoint(roverPosition);
            var freePose = new StructurePose(groundPoint, _controller.GhostYaw);
            StructurePose snappedPose = _placedBase.SolveSnap(selected, freePose, true);
            float snappedY = _terrain.SampleHeight(snappedPose.Position.x, snappedPose.Position.z);
            snappedPose = new StructurePose(
                new Vector3(snappedPose.Position.x, snappedY, snappedPose.Position.z),
                snappedPose.Yaw);

            bool propBlocked = IsPropBlocked(selected, snappedPose);
            _controller.UpdateCandidate(groundPoint, roverPosition, _terrain, propBlocked, snapEnabled: true);

            bool snappedNow = _controller.CandidateSnapped;
            if (snappedNow && !_wasSnapped)
            {
                _events.Publish(new UiCue(UiCueKind.PartDrop));
                _events.Publish(new BuildCue(BuildCueKind.Snap, _controller.CandidatePose.Position));
            }

            _wasSnapped = snappedNow;

            bool validNow = _controller.CandidateReason == PlacementReason.Valid && _controller.CanAffordSelected;
            if (!validNow && _wasValid)
            {
                _events.Publish(new UiCue(UiCueKind.PartRefuse));
                _events.Publish(new BuildCue(BuildCueKind.Invalid, _controller.CandidatePose.Position));
            }

            _wasValid = validNow;

            StepGhost(selected, _controller.CandidatePose, deltaTime);

            if (excavatePressed && !_waitExcavateRelease)
            {
                if (_controller.TryConfirmPlacement(_terrain, roverPosition, out int placedId, out _))
                {
                    HideAllGhosts();
                    BeginWeldPresentation(placedId);
                    _waitExcavateRelease = !excavateHeld;
                }
                else
                {
                    _events.Publish(new UiCue(UiCueKind.PartRefuse));
                    _events.Publish(new BuildCue(BuildCueKind.Invalid, _controller.CandidatePose.Position));
                }
            }
        }

        private void StepWeldingState(bool excavateHeld, float deltaTime)
        {
            _lastNavSign = 0;
            int activeId = _controller.ActiveWeldInstanceId;
            if (activeId <= 0)
            {
                _controller.Close();
                return;
            }

            if (_weldingInstanceId != activeId)
            {
                BeginWeldPresentation(activeId);
            }

            bool canWeld = excavateHeld && _beamWork.MayStart;
            _controller.StepWelding(deltaTime, _rover.Position, canWeld, out bool completedNow, out bool cancelledNow);

            if (cancelledNow)
            {
                PublishCancelWeld(activeId, _controller.ActiveWeldProgress01);
                _weldingInstanceId = 0;
            }
            else if (completedNow)
            {
                _weldingInstanceId = 0;
                _waitExcavateRelease = excavateHeld;
            }
            else if (canWeld)
            {
                _beamWork.Weld(deltaTime);
            }
        }

        private void StepClosedState(bool excavateHeld, bool deconstructHeld, float deltaTime)
        {
            _lastNavSign = 0;
            _weldingInstanceId = 0;
            UpdateHovered();

            if (HoveredInstanceId > 0 &&
                excavateHeld &&
                !_waitExcavateRelease &&
                _beamWork.MayStart &&
                _placedBase.TryGetStructure(HoveredInstanceId, out PlacedStructure hovered) &&
                !hovered.IsBuilt)
            {
                if (_controller.TryBeginResumeWeld(HoveredInstanceId, _rover.Position))
                {
                    BeginWeldPresentation(HoveredInstanceId);
                    return;
                }
            }

            int previousTarget = _controller.DeconstructTargetId;
            _controller.StepDeconstruct(
                deltaTime,
                HoveredInstanceId,
                _rover.Position,
                deconstructHeld,
                out _,
                out _);

            int currentTarget = _controller.DeconstructTargetId;
            if (currentTarget > 0 && _viewsById.TryGetValue(currentTarget, out StructureView activeView))
            {
                activeView.SetDeconstructProgress(_controller.DeconstructProgress01);
            }

            if (previousTarget > 0 &&
                previousTarget != currentTarget &&
                _viewsById.TryGetValue(previousTarget, out StructureView previousView))
            {
                previousView.SetDeconstructProgress(0f);
            }
        }

        private void BeginWeldPresentation(int instanceId)
        {
            _weldingInstanceId = instanceId;
            if (!_placedBase.TryGetStructure(instanceId, out PlacedStructure placed))
            {
                return;
            }

            Vector3 weldPoint = WeldPointOf(instanceId);
            float remainingSeconds = placed.Definition.BuildSeconds * (1f - placed.BuildProgress01);
            _events.Publish(new FeelBeat(
                FeelKind.Build,
                FeelPhase.Anticipate,
                weldPoint,
                Vector3.up,
                placed.BuildProgress01,
                remainingSeconds));
        }

        private void PublishCancelWeld(int instanceId, float progress01)
        {
            if (instanceId <= 0)
            {
                return;
            }

            Vector3 point = WeldPointOf(instanceId);
            _events.Publish(new FeelBeat(FeelKind.Build, FeelPhase.Cancel, point, Vector3.up, progress01, 0f));
        }

        private Vector3 SampleAimPoint(Vector3 roverPosition)
        {
            if (_aimPointOverride.HasValue)
            {
                Vector3 overridePt = _aimPointOverride.Value;
                return SurfaceRules.OnSurface(_terrain, overridePt.x, overridePt.z);
            }

            float minDist = _tuning.MinPlacementDistance + 0.35f;
            float maxDist = Mathf.Max(minDist, _tuning.MaxPlacementDistance - 0.35f);
            float defaultDist = Mathf.Lerp(minDist, maxDist, 0.45f);

            Camera cam = _view != null ? _view.Camera : null;
            if (cam == null)
            {
                Vector3 ahead = roverPosition + Vector3.forward * defaultDist;
                return SurfaceRules.OnSurface(_terrain, ahead.x, ahead.z);
            }

            Transform camTransform = cam.transform;
            Vector3 camPos = camTransform.position;
            Vector3 camForward = camTransform.forward;

            Vector3 flatForward = new Vector3(camForward.x, 0f, camForward.z);
            flatForward = flatForward.sqrMagnitude > 1e-6f ? flatForward.normalized : Vector3.forward;

            Vector3 rawPoint = roverPosition + flatForward * defaultDist;
            if (camForward.y < -0.05f)
            {
                float maxRay = _tuning.MaxPlacementDistance + 25f;
                if (Physics.Raycast(
                        camPos,
                        camForward,
                        out RaycastHit hit,
                        maxRay,
                        Layers.GroundMask,
                        QueryTriggerInteraction.Ignore))
                {
                    rawPoint = hit.point;
                }
                else
                {
                    float heightAboveRover = camPos.y - roverPosition.y;
                    if (heightAboveRover > 0.1f)
                    {
                        float t = Mathf.Clamp(heightAboveRover / -camForward.y, 0.5f, maxRay);
                        rawPoint = camPos + camForward * t;
                    }
                }
            }
            else
            {
                float pitch01 = Mathf.InverseLerp(-0.05f, 0.20f, camForward.y);
                float distance = Mathf.Lerp(minDist, maxDist, pitch01);
                rawPoint = roverPosition + flatForward * distance;
            }

            Vector3 horizontalOffset = new Vector3(rawPoint.x - roverPosition.x, 0f, rawPoint.z - roverPosition.z);
            float horizontalDist = horizontalOffset.magnitude;
            Vector3 dir = horizontalDist > 1e-4f ? horizontalOffset / horizontalDist : flatForward;
            float clampedDist = Mathf.Clamp(horizontalDist, minDist, maxDist);
            Vector3 clamped = roverPosition + dir * clampedDist;
            return SurfaceRules.OnSurface(_terrain, clamped.x, clamped.z);
        }

        private bool IsPropBlocked(StructureDefinition definition, StructurePose pose)
        {
            Vector3 halfExtents = new Vector3(
                Mathf.Max(0.05f, definition.Width * 0.5f - 0.02f),
                Mathf.Max(0.05f, definition.Height * 0.5f),
                Mathf.Max(0.05f, definition.Depth * 0.5f - 0.02f));
            Vector3 center = pose.Position + Vector3.up * (definition.Height * 0.5f + 0.05f);
            Quaternion orientation = Quaternion.Euler(0f, pose.Yaw, 0f);

            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                _overlapBuffer,
                orientation,
                Layers.PropMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col != null && !_managedColliders.Contains(col))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateHovered()
        {
            HoveredInstanceId = 0;
            HoveredPosition = Vector3.zero;

            int count = _placedBase.Count;
            if (count == 0)
            {
                return;
            }

            Vector3 roverPos = _rover.Position;
            float maxRange = _tuning.WeldRange;
            float maxRangeSq = maxRange * maxRange;

            Camera cam = _view != null ? _view.Camera : null;
            Vector3 eye = cam != null ? cam.transform.position : roverPos + Vector3.up * 1.2f;
            Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;

            float bestScore = float.MaxValue;
            int bestId = 0;
            Vector3 bestPos = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                PlacedStructure placed = _placedBase.StructureAt(i);
                float horizSq = SurfaceRules.HorizontalDistanceSquared(roverPos, placed.Pose.Position);
                if (horizSq > maxRangeSq)
                {
                    continue;
                }

                Vector3 targetPoint = placed.Pose.Position + Vector3.up * (placed.Definition.Height * 0.45f);
                if (AimScore.TryScore(
                        eye,
                        forward,
                        targetPoint,
                        HoverConeDegrees,
                        float.MaxValue,
                        HoverDistanceWeight,
                        out float score))
                {
                    // Prefer unfinished structures when both are under the reticle so Resume Weld wins cleanly.
                    if (!placed.IsBuilt)
                    {
                        score -= 0.25f;
                    }

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestId = placed.InstanceId;
                        bestPos = targetPoint;
                    }
                }
            }

            HoveredInstanceId = bestId;
            HoveredPosition = bestPos;
        }

        private void PrewarmGhosts()
        {
            Material validMat = _visuals.HologramValid;
            Material invalidMat = _visuals.HologramInvalid;
            if (validMat == null || invalidMat == null)
            {
                return;
            }

            int count = _catalog.Count;
            for (int i = 0; i < count; i++)
            {
                StructureDefinition def = _catalog.StructureAt(i);
                if (def == null || def.ArtPending || def.Prefab == null)
                {
                    continue;
                }

                GameObject instance = Instantiate(def.Prefab, _ghostsRoot);
                instance.name = "Ghost_" + def.Id;
                if (!instance.TryGetComponent(out StructureView view))
                {
                    Debug.LogError(
                        $"{nameof(BuildField)}: structure prefab for '{def.Id}' has no {nameof(StructureView)} (run Gameplay/Content).",
                        this);
                    Destroy(instance);
                    continue;
                }

                if (!view.Initialize(validMat, invalidMat))
                {
                    Destroy(instance);
                    continue;
                }

                view.SetGhostMode(true);
                RegisterManagedColliders(view, true);
                instance.SetActive(false);
                _ghostsById[def.Id] = view;
            }
        }

        private void StepGhost(StructureDefinition selected, StructurePose targetPose, float deltaTime)
        {
            if (!_ghostsById.TryGetValue(selected.Id, out StructureView ghost) || ghost == null)
            {
                HideAllGhosts();
                return;
            }

            if (_activeGhost != ghost)
            {
                if (_activeGhost != null)
                {
                    _activeGhost.gameObject.SetActive(false);
                }

                _activeGhost = ghost;
                _activeGhostId = selected.Id;
                _activeGhost.gameObject.SetActive(true);
                _ghostPoseSeeded = false;
                _events.Publish(new BuildCue(BuildCueKind.Ghost, targetPose.Position));
            }

            if (!_ghostPoseSeeded || deltaTime <= 0f)
            {
                _ghostPosition = targetPose.Position;
                _ghostYaw = targetPose.Yaw;
                _ghostPoseSeeded = true;
            }
            else
            {
                _ghostPosition = Damp.Toward(_ghostPosition, targetPose.Position, GhostGlideHalfLife, deltaTime);
                float yawDelta = Mathf.DeltaAngle(_ghostYaw, targetPose.Yaw);
                _ghostYaw = StructurePose.NormalizeYaw(
                    Damp.Toward(_ghostYaw, _ghostYaw + yawDelta, GhostYawHalfLife, deltaTime));
            }

            _activeGhost.transform.SetPositionAndRotation(_ghostPosition, Quaternion.Euler(0f, _ghostYaw, 0f));
            bool valid = _controller.CandidateReason == PlacementReason.Valid && _controller.CanAffordSelected;
            _activeGhost.SetGhostMode(valid);
        }

        private void HideAllGhosts()
        {
            if (_activeGhost != null)
            {
                _activeGhost.gameObject.SetActive(false);
                _activeGhost = null;
                _activeGhostId = null;
            }

            _ghostPoseSeeded = false;
        }

        private void StepPlacedViews(float deltaTime)
        {
            int count = _placedBase.Count;
            for (int i = 0; i < count; i++)
            {
                PlacedStructure placed = _placedBase.StructureAt(i);
                if (!_viewsById.TryGetValue(placed.InstanceId, out StructureView view) || view == null)
                {
                    continue;
                }

                int newlySolid = view.SetPlacedState(placed.BuildProgress01, instant: false);
                if (newlySolid > 0)
                {
                    Vector3 weldPoint = view.GetActiveWeldPoint();
                    BurstSparksAt(weldPoint);
                    _events.Publish(new BuildCue(BuildCueKind.StageComplete, weldPoint));
                    _events.Publish(new FeelBeat(
                        FeelKind.Build,
                        FeelPhase.Act,
                        weldPoint,
                        Vector3.up,
                        placed.BuildProgress01,
                        0f));
                }

                view.StepVisuals(deltaTime);
            }
        }

        private void OnStructurePlaced(StructurePlaced e)
        {
            if (!_catalog.TryGet(e.StructureId, out StructureDefinition def) || def.Prefab == null)
            {
                return;
            }

            if (_viewsById.TryGetValue(e.InstanceId, out StructureView existing) && existing != null)
            {
                RegisterManagedColliders(existing, false);
                Destroy(existing.gameObject);
                _viewsById.Remove(e.InstanceId);
            }

            Quaternion rotation = Quaternion.Euler(0f, e.Yaw, 0f);
            GameObject instance = Instantiate(def.Prefab, e.Position, rotation, _structuresRoot);
            instance.name = $"{def.Id}_{e.InstanceId}";
            if (!instance.TryGetComponent(out StructureView view))
            {
                Debug.LogError(
                    $"{nameof(BuildField)}: structure prefab for '{def.Id}' has no {nameof(StructureView)} (run Gameplay/Content).",
                    this);
                Destroy(instance);
                return;
            }

            if (!view.Initialize(_visuals.HologramValid, _visuals.HologramInvalid))
            {
                Destroy(instance);
                return;
            }
            float initialProgress = _placedBase.TryGetStructure(e.InstanceId, out PlacedStructure placed)
                ? placed.BuildProgress01
                : 0f;
            view.SetPlacedState(initialProgress, instant: true);
            RegisterManagedColliders(view, true);
            _viewsById[e.InstanceId] = view;

            if (!e.Restored)
            {
                _save?.SaveNow();
            }
        }

        private void OnStructureBuilt(StructureBuilt e)
        {
            Vector3 effectPoint = e.Position + Vector3.up * 0.5f;
            if (_viewsById.TryGetValue(e.InstanceId, out StructureView view) && view != null)
            {
                effectPoint = view.GetActiveWeldPoint();
                view.SetPlacedState(1f, instant: e.Restored);
            }

            if (!e.Restored)
            {
                BurstSparksAt(effectPoint);
                _events.Publish(new FeelBeat(FeelKind.Build, FeelPhase.Impact, effectPoint, Vector3.up, 1f, 0f));
                _events.Publish(new FeelBeat(FeelKind.Build, FeelPhase.Reward, effectPoint, Vector3.up, 1f, 0f));
                _events.Publish(new RoverChirpRequested(ChirpMood.Happy, 1f));
                _save?.SaveNow();
            }
        }

        private void OnStructureRemoved(StructureRemoved e)
        {
            if (_viewsById.TryGetValue(e.InstanceId, out StructureView view))
            {
                if (view != null)
                {
                    RegisterManagedColliders(view, false);
                    Destroy(view.gameObject);
                }

                _viewsById.Remove(e.InstanceId);
            }

            _events.Publish(new FeelBeat(FeelKind.Build, FeelPhase.Cancel, e.Position, Vector3.up, 1f, 0f));
            _save?.SaveNow();
        }

        private Vector3 WeldPointOf(int instanceId)
        {
            if (_viewsById.TryGetValue(instanceId, out StructureView view) && view != null)
            {
                return view.GetActiveWeldPoint();
            }

            if (_placedBase.TryGetStructure(instanceId, out PlacedStructure placed))
            {
                return placed.Pose.Position + Vector3.up * (placed.Definition.Height * 0.5f);
            }

            return _rover.Position + Vector3.up * 0.5f;
        }

        private void BurstSparksAt(Vector3 worldPoint)
        {
            _sparkSocket.position = worldPoint;
            _sparks.Burst();
        }

        private void RegisterManagedColliders(StructureView view, bool add)
        {
            IReadOnlyList<Collider> colliders = view.Colliders;
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Count; i++)
            {
                Collider col = colliders[i];
                if (col == null)
                {
                    continue;
                }

                if (add)
                {
                    _managedColliders.Add(col);
                }
                else
                {
                    _managedColliders.Remove(col);
                }
            }
        }

        private void ClearSpawnedViews()
        {
            foreach (KeyValuePair<int, StructureView> pair in _viewsById)
            {
                if (pair.Value != null)
                {
                    RegisterManagedColliders(pair.Value, false);
                    Destroy(pair.Value.gameObject);
                }
            }

            _viewsById.Clear();
        }

        private void StepWeldLoopAudio(bool welding, Vector3 weldPoint)
        {
            if (welding)
            {
                if (!_weldLoopActive || (weldPoint - _lastWeldLoopPoint).sqrMagnitude > 1e-4f)
                {
                    _weldLoopActive = true;
                    _lastWeldLoopPoint = weldPoint;
                    _events.Publish(new BuildWeldStateChanged(true, weldPoint, _controller.ActiveWeldProgress01));
                }
            }
            else if (_weldLoopActive)
            {
                _weldLoopActive = false;
                _lastWeldLoopPoint = Vector3.zero;
                _events.Publish(new BuildWeldStateChanged(false, Vector3.zero, 0f));
            }
        }

        private void SetHold(bool hold)
        {
            if (_holding == hold || _rig == null)
            {
                return;
            }

            _holding = hold;
            _rig.SetHoldStill(this, hold);
        }

        private void StepGaze(bool welding, Vector3 weldPoint)
        {
            if (_rig == null)
            {
                return;
            }

            if (welding)
            {
                _rig.SetGazeTarget(this, weldPoint, GazePriorities.Focus);
                _gazing = true;
            }
            else if (_controller.State == BuildModeState.Placing && _controller.HasCandidatePoint)
            {
                _rig.SetGazeTarget(this, _controller.CandidatePose.Position, GazePriorities.Interest);
                _gazing = true;
            }
            else if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        private void OnDisable()
        {
            if (!_initialized)
            {
                return;
            }

            StepWeldLoopAudio(false, Vector3.zero);
            SetHold(false);
            if (_gazing && _rig != null)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isUpdating)
            {
                return;
            }
#endif
            if (_catalog != null)
            {
                string problem = _catalog.Validate();
                if (problem != null)
                {
                    Debug.LogError($"{nameof(BuildField)}: {problem}", this);
                }
            }
        }
    }
}
