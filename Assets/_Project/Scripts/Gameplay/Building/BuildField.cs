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
    public sealed class BuildField : MonoBehaviour, ICutterAimOwner
    {
        private const float GhostGlideHalfLife = 0.045f;
        private const float GhostYawHalfLife = 0.04f;

        /// <summary>Squared metres within which an aim point counts as the apron bay's centre.</summary>
        private const float ApronBayEpsilonSq = 0.01f;
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

        private const string TintProperty = "_Color";

        /// <summary>Metres above 07's origin the taken-apart pieces float to: about its body.</summary>
        private const float TakeApartBodyHeight = 0.8f;

        private const int SeamSlotCount = 4;

        private Transform _structuresRoot;
        private Transform _ghostsRoot;
        private CutBeam _weldBeam;
        private LightPulse _weldLight;
        private VacuumDebris _weldDebris;
        private Action<Vector3, int, float> _dustPuff;
        private CutBeam _takeApartBeam;
        private TakeApart _takeApart;
        private Material _takeApartCore;
        private Material _takeApartSheath;
        private readonly HeatSeam[] _weldSeams = new HeatSeam[SeamSlotCount];
        private GridLinkLine _gridLink;
        private readonly Vector3[] _seamLine = new Vector3[3];
        private int _seamInstanceId;
        private int _seamStageIndex = -1;
        private int _activeSeamSlot;

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
        private readonly BlockNotice _weldBlocked = new BlockNotice();
        private int _weldingInstanceId;
        private bool _weldLoopActive;
        private Vector3 _lastWeldLoopPoint;
        private bool _holding;
        private bool _gazing;
        private bool _initialized;
        private Vector3? _apronCentre;

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

        /// <summary>Current weld beam reach level (for tests and inspection).</summary>
        public float WeldBeamLevel => _weldBeam != null ? _weldBeam.Reach : 0f;

        /// <summary>World-space origin point of the weld beam (for tests and inspection).</summary>
        public Vector3 WeldBeamStart => _weldBeam != null ? _weldBeam.Start : Vector3.zero;

        /// <summary>Where the weld beam ends this frame (on the structure's surface once fully reached).</summary>
        public Vector3 WeldBeamEnd => _weldBeam != null ? _weldBeam.End : Vector3.zero;

        /// <summary>Number of rendered layers in the weld beam (core + sheath + halo).</summary>
        public int WeldBeamLayerCount => _weldBeam != null ? _weldBeam.LayerCount : 0;

        /// <summary>Current intensity of the warm flickering weld light.</summary>
        public float WeldLightIntensity => _weldLight != null ? _weldLight.Intensity : 0f;

        /// <summary>Current heat level on the welded structure's active stage seam.</summary>
        public float WeldSeamHeat
        {
            get
            {
                HeatSeam active = _weldSeams[_activeSeamSlot];
                if (active != null && active.Piece != null)
                {
                    return active.HeatAt(0);
                }

                float maxHeat = 0f;
                for (int i = 0; i < SeamSlotCount; i++)
                {
                    HeatSeam s = _weldSeams[i];
                    if (s != null && s.Piece != null)
                    {
                        maxHeat = Mathf.Max(maxHeat, s.HeatAt(0));
                    }
                }

                return maxHeat;
            }
        }

        /// <summary>Live weld spark particle count (for tests and inspection).</summary>
        public int SparkCount => _weldDebris != null ? _weldDebris.ParticleCount : 0;

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

            var vfxRoot = new GameObject("BuildWeldVfx").transform;
            vfxRoot.SetParent(transform, false);
            FeelVfxTuning feelTuning = _visuals.FeelTuning;
            _weldBeam = new CutBeam(vfxRoot, feelTuning, _visuals.BeamCore, _visuals.BeamSheath);
            _weldLight = new LightPulse("BuildWeldLight", vfxRoot, feelTuning.CutLightColor, feelTuning.CutRange);
            _weldDebris = new VacuumDebris(vfxRoot, feelTuning, _visuals.FeelSprite);
            _dustPuff = _weldDebris.Ring;
            _takeApartCore = Tinted(_visuals.BeamCore, _tuning.TakeApartBeamColor);
            _takeApartSheath = Tinted(_visuals.BeamSheath, _tuning.TakeApartBeamColor);
            _takeApartBeam = new CutBeam(vfxRoot, feelTuning, _takeApartCore, _takeApartSheath);
            for (int i = 0; i < SeamSlotCount; i++)
            {
                _weldSeams[i] = new HeatSeam(vfxRoot, feelTuning, _visuals.HeatSeam);
            }

            _gridLink = new GridLinkLine(vfxRoot, _visuals.BeamCore, _tuning);

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
        /// True while the build menu is open (07's builder is out) and 07 looks at a placed structure within
        /// <see cref="BuildingTuning.WeldRange"/> that can be deconstructed by holding <c>Deconstruct</c>: the only time
        /// its prompt shows, never while 07 merely rests by its pad.
        /// </summary>
        public bool TryGetDeconstructCandidate(out Vector3 position)
        {
            return TryGetDeconstructHint(out position);
        }

        /// <summary>
        /// The centre of the apron's marked bay (World's apron markings), where the charging pad's ghost settles as 07
        /// drives it near.
        /// </summary>
        public void MarkApron(Vector3 centre)
        {
            _apronCentre = centre;
        }

        /// <summary>The apron bay's centre the charging pad's ghost settles on, or null before it is marked.</summary>
        public Vector3? ApronCentre => _apronCentre;

        /// <summary>True while a taken-down piece is still coming apart on screen.</summary>
        public bool TakeApartRunning => _takeApart != null;

        /// <summary>
        /// Opens the build menu as the Build key would, for a screen that closes and hands over to build mode (the
        /// face screen's Build); false when build mode is already open or 07 knows no structure it can build yet.
        /// </summary>
        public bool TryOpen()
        {
            if (!_initialized || _controller.IsOpen || !_controller.TryOpenMenu())
            {
                return false;
            }

            _input.SetModeOwner(this, true);
            return true;
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
        /// True while the build menu is open and 07 looks at a placed structure within
        /// <see cref="BuildingTuning.WeldRange"/> (see <see cref="TryGetDeconstructCandidate"/>).
        /// </summary>
        public bool TryGetDeconstructHint(out Vector3 position)
        {
            if (_initialized &&
                _controller.State == BuildModeState.Menu &&
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

            bool wasOpen = _controller.IsOpen || _waitExcavateRelease;
            if (wasOpen)
            {
                _input.SetModeOwner(this, true);
            }

            StepInput(now, deltaTime, out bool excavateHeld);

            bool ownsInput = _controller.IsOpen || _waitExcavateRelease;
            _input.SetModeOwner(this, ownsInput);
            bool placing = _controller.State == BuildModeState.Placing;
            _input.ShareDrive(this, placing);

            // The structures stand as they do this frame before the weld beam aims at one: its end meets their surface.
            _placedBase.StepPower(deltaTime, 1f);
            StepPlacedViews(deltaTime);

            bool weldingNow = _controller.State == BuildModeState.Welding && _controller.ActiveWeldInstanceId > 0;
            Vector3 weldPoint = weldingNow ? WeldPointOf(_controller.ActiveWeldInstanceId) : Vector3.zero;
            SetHold((_controller.IsOpen && !placing) || weldingNow);
            StepGaze(weldingNow, weldPoint);
            bool activelyWelding = weldingNow && excavateHeld && _beamWork.MayStart &&
                                   (_rig == null || _rig.HasCutterLineOfSight(weldPoint));
            StepWeldVfx(weldingNow, activelyWelding, weldPoint, deltaTime);
            StepWeldLoopAudio(activelyWelding, weldPoint);
            StepTakeApart(deltaTime);
        }

        /// <summary>A copy of a beam material in the take-apart's cool, desaturated colour.</summary>
        private static Material Tinted(Material source, Color color)
        {
            var copy = new Material(source) { name = source.name + "_TakeApart" };
            copy.SetColor(TintProperty, color);
            return copy;
        }

        /// <summary>
        /// Runs the take-apart picture of a piece 07 has taken down: a soft cool beam reaches it while it eases down and
        /// its pieces float to 07; the beam thins out and the last of it goes in a small puff of dust.
        /// </summary>
        private void StepTakeApart(float deltaTime)
        {
            Vector3 emitter = _rig != null ? _rig.CutterOrigin.position : _rover.Position;
            if (_takeApart == null)
            {
                _takeApartBeam.Retract();
                _takeApartBeam.Step(deltaTime, emitter, emitter);
                return;
            }

            if (_takeApart.BeamOut)
            {
                _takeApartBeam.Extend();
            }
            else
            {
                _takeApartBeam.Retract();
            }

            _takeApartBeam.Step(deltaTime, emitter, _takeApart.Centre);
            if (_takeApart.Step(deltaTime, _dustPuff))
            {
                _takeApart = null;
            }
        }

        /// <summary>07's body, where taken-apart pieces go.</summary>
        private Vector3 TakeApartDestination()
        {
            return _rover.Position + Vector3.up * TakeApartBodyHeight;
        }

        private void StepWeldVfx(bool weldingNow, bool activelyWelding, Vector3 weldPoint, float deltaTime)
        {
            StructureView view = null;
            if (weldingNow)
            {
                int activeId = _controller.ActiveWeldInstanceId;
                if (_viewsById.TryGetValue(activeId, out view) && view != null)
                {
                    if (_seamInstanceId != activeId)
                    {
                        for (int i = 0; i < SeamSlotCount; i++)
                        {
                            _weldSeams[i].Clear();
                        }

                        _seamInstanceId = activeId;
                        _seamStageIndex = -1;
                    }

                    int stageIndex = Mathf.Clamp(view.SolidStageCount, 0, Mathf.Max(0, view.StageCount - 1));
                    int targetSlot = stageIndex % SeamSlotCount;
                    if (_seamStageIndex != stageIndex || _weldSeams[targetSlot].Piece != view.transform)
                    {
                        if (_seamStageIndex >= 0 && _weldSeams[_activeSeamSlot].Piece == view.transform)
                        {
                            _weldSeams[_activeSeamSlot].Cut(1f);
                        }

                        _activeSeamSlot = targetSlot;
                        view.GetStageWeldSeam(stageIndex, _seamLine, out Vector3 seamNormal);
                        _weldSeams[_activeSeamSlot].Begin(view.transform, _seamLine, seamNormal);
                        _seamStageIndex = stageIndex;
                    }
                }
            }

            FeelVfxTuning feelTuning = _visuals.FeelTuning;
            float vfxDt = activelyWelding ? Mathf.Max(deltaTime, 1e-4f) : deltaTime;
            HeatSeam activeSeam = _weldSeams[_activeSeamSlot];
            if (activelyWelding)
            {
                float stageProgress = view != null
                    ? view.GetActiveStageProgress01(_controller.ActiveWeldProgress01)
                    : _controller.ActiveWeldProgress01;
                if (activeSeam.Piece != null)
                {
                    activeSeam.Cut(Mathf.Max(0.05f, stageProgress));
                }

                _weldBeam.Extend();
                _weldLight.MoveTo(weldPoint);
                _weldLight.Flicker(feelTuning.CutFlicker, feelTuning.CutFlickerRate);
                _weldLight.Ramp(feelTuning.CutLight, feelTuning.ExtendSeconds, false);
                _weldDebris.Spray(weldPoint, Vector3.up, stageProgress);
            }
            else
            {
                _weldBeam.Retract();
                _weldLight.Ramp(0f, feelTuning.CutFadeSeconds, false);
                _weldDebris.StopSpray();
            }

            for (int i = 0; i < SeamSlotCount; i++)
            {
                _weldSeams[i].Step(vfxDt);
            }

            _weldBeam.Step(vfxDt, _rig.CutterOrigin.position, weldPoint);
            _weldLight.Step(vfxDt);
            _weldDebris.Step(vfxDt);
        }

        private void StepInput(float now, float deltaTime, out bool excavateHeld)
        {
            bool buildPressed = _input.BuildPressed;
            bool cancelPressed = _input.Menu.CancelPressed;
            bool modeOwned = _input.HasModeOwner;
            Vector2 drive = modeOwned ? _input.ModeDrive(this) : _input.Drive;
            Vector2 nav = _input.Menu.Navigate;
            if (drive.sqrMagnitude > nav.sqrMagnitude)
            {
                nav = drive;
            }

            bool excavatePressed = modeOwned ? _input.ModeExcavatePressed(this) : _input.ExcavatePressed;
            excavateHeld = modeOwned ? _input.ModeExcavateHeld(this) : _input.ExcavateHeld;
            float winch = modeOwned ? _input.ModeWinch(this) : _input.Winch;
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
                if (_controller.IsOpen || _waitExcavateRelease)
                {
                    _input.SetModeOwner(this, true);
                }
            }
            else if (cancelPressed && _controller.IsOpen)
            {
                if (_controller.State == BuildModeState.Welding)
                {
                    PublishCancelWeld(_controller.ActiveWeldInstanceId, _controller.ActiveWeldProgress01);
                    _weldingInstanceId = 0;
                }

                _controller.Close();
                _waitExcavateRelease = excavateHeld;
            }

            _weldBlocked.Report(excavateHeld, false);
            switch (_controller.State)
            {
                case BuildModeState.Menu:
                    HideAllGhosts();
                    HoveredInstanceId = 0;
                    StepMenuState(nav, winch, excavatePressed, now);
                    if (_controller.State == BuildModeState.Menu)
                    {
                        // With the builder out, a built piece in view can be taken down: its prompt shows here only.
                        UpdateHovered();
                        StepDeconstructTarget(deconstructHeld, deltaTime);
                    }

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
                    StepClosedState(excavateHeld, deltaTime);
                    break;
            }
        }

        private bool _wasSnapped;
        private bool _wasOnApron;
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
                PickSelected();
                _waitExcavateRelease = true;
            }
        }

        /// <summary>
        /// Picks the menu's structure <paramref name="index"/> as E on it would (UI's menu, clicked or confirmed): its
        /// focus and confirm cues, then <see cref="BuildModeState.Placing"/>. False outside the menu or for an index
        /// the menu does not hold.
        /// </summary>
        public bool TryPick(int index)
        {
            if (!_initialized || _controller.State != BuildModeState.Menu)
            {
                return false;
            }

            int previous = _controller.SelectedIndex;
            if (!_controller.SelectIndex(index))
            {
                return false;
            }

            if (_controller.SelectedIndex != previous)
            {
                _events.Publish(new UiCue(UiCueKind.FocusMove));
            }

            return PickSelected();
        }

        private bool PickSelected()
        {
            bool picked = _controller.PickSelected();
            if (picked)
            {
                _events.Publish(new UiCue(UiCueKind.Confirm));
            }

            _ghostPoseSeeded = false;
            _wasSnapped = false;
            _wasOnApron = false;
            _wasValid = true;
            return picked;
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
            Vector3 groundPoint = SampleAimPoint(roverPosition, selected);
            bool onApron = selected.Id == StructureIds.ChargingPad && _apronCentre.HasValue &&
                           SurfaceRules.HorizontalDistanceSquared(groundPoint, _apronCentre.Value) < ApronBayEpsilonSq;
            if (onApron && !_wasOnApron)
            {
                _controller.SetGhostYaw(_tuning.ApronPadYaw);
            }

            _wasOnApron = onApron;
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
                    StepGaze(true, WeldPointOf(placedId));
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

            Vector3 weldPoint = WeldPointOf(activeId);
            StepGaze(true, weldPoint);
            bool canWeld = excavateHeld && _beamWork.MayStart;
            bool hasLineOfSight = canWeld && (_rig == null || _rig.HasCutterLineOfSight(weldPoint));
            if (_weldBlocked.Report(excavateHeld, canWeld && !hasLineOfSight))
            {
                _events.Publish(new InteractionBlocked(InteractionKind.Build, weldPoint, BlockReason.NoLineOfSight));
            }

            _controller.StepWelding(
                hasLineOfSight ? deltaTime : 0f,
                _rover.Position,
                canWeld,
                out bool completedNow,
                out bool cancelledNow);

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
            else if (hasLineOfSight)
            {
                _beamWork.Weld(deltaTime);
            }
        }

        private void StepClosedState(bool excavateHeld, float deltaTime)
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

            // Deconstruct needs the builder out: held X does nothing outside build mode, and lets go of any target.
            StepDeconstructTarget(false, deltaTime);
        }

        /// <summary>
        /// Holds <c>Deconstruct</c> on the hovered piece: its progress shows on the view and resets when the target changes.
        /// </summary>
        private void StepDeconstructTarget(bool deconstructHeld, float deltaTime)
        {
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

        /// <summary>
        /// Where the placing ghost stands: <see cref="BuildingTuning.GhostAheadMetres"/> in front of 07, which drives
        /// it to its spot, settling onto the apron's bay for the charging pad (see <see cref="GhostAim"/>; a test's aim
        /// point wins).
        /// </summary>
        private Vector3 SampleAimPoint(Vector3 roverPosition, StructureDefinition selected)
        {
            Vector3 point;
            if (_aimPointOverride.HasValue)
            {
                point = _aimPointOverride.Value;
            }
            else
            {
                point = GhostAim.Point(
                    roverPosition,
                    _rover.Rotation * Vector3.forward,
                    selected.Id == StructureIds.ChargingPad ? _apronCentre : null,
                    _tuning);
            }

            return SurfaceRules.OnSurface(_terrain, point.x, point.z);
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
            StepGridLink(selected, targetPose.Position);
        }

        /// <summary>
        /// The ghost's cable preview: to the grid hub the structure would join where it would stand, or a loose cable
        /// end when it would stand off the grid.
        /// </summary>
        private void StepGridLink(StructureDefinition selected, Vector3 standsAt)
        {
            if (!GridLinkPreview.JoinsGrid(selected))
            {
                _gridLink.Hide();
                return;
            }

            if (GridLinkPreview.TryFindLink(_placedBase, selected, standsAt, out Vector3 hub))
            {
                _gridLink.ShowLink(_ghostPosition, hub);
            }
            else
            {
                _gridLink.ShowLoose(_ghostPosition, _ghostYaw);
            }
        }

        private void HideAllGhosts()
        {
            _gridLink.Hide();
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
                    if (_weldDebris != null)
                    {
                        _weldDebris.Fall(weldPoint, 6);
                        _weldDebris.Ring(placed.Pose.Position, 6, 0.35f);
                    }

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
            view.ConfigureLiveBuild(!e.Restored, _tuning);
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
                if (!e.Restored && _weldSeams[_activeSeamSlot] != null && _weldSeams[_activeSeamSlot].Piece == view.transform)
                {
                    _weldSeams[_activeSeamSlot].Cut(1f);
                }
            }

            if (!e.Restored)
            {
                BurstSparksAt(effectPoint);
                if (_weldDebris != null)
                {
                    _weldDebris.Ring(e.Position, _tuning.SettleDustRingCount, 0.85f);
                    _weldDebris.Fall(effectPoint, 8);
                }

                _events.Publish(new FeelBeat(FeelKind.Build, FeelPhase.Impact, effectPoint, Vector3.up, 1f, 0f));
                _events.Publish(new FeelBeat(FeelKind.Build, FeelPhase.Reward, effectPoint, Vector3.up, 1f, 0f));
                _events.Publish(new RoverChirpRequested(ChirpMood.Happy, 1f));
                _save?.SaveNow();
            }
        }

        private void OnStructureRemoved(StructureRemoved e)
        {
            if (_seamInstanceId == e.InstanceId)
            {
                for (int i = 0; i < SeamSlotCount; i++)
                {
                    _weldSeams[i].Clear();
                }

                _seamInstanceId = 0;
                _seamStageIndex = -1;
            }

            if (_viewsById.TryGetValue(e.InstanceId, out StructureView view))
            {
                if (view != null)
                {
                    RegisterManagedColliders(view, false);
                    if (_controller.DeconstructTargetId == e.InstanceId)
                    {
                        BeginTakeApart(view);
                    }
                    else
                    {
                        Destroy(view.gameObject);
                    }
                }

                _viewsById.Remove(e.InstanceId);
            }

            _events.Publish(new FeelBeat(FeelKind.Build, FeelPhase.Cancel, e.Position, Vector3.up, 1f, 0f));
            _save?.SaveNow();
        }

        /// <summary>
        /// The removed structure comes apart on screen instead of vanishing: it stands as it was, then eases down and its
        /// pieces float to 07. A take-apart still running is ended first (its picture only; it has been paid already).
        /// </summary>
        private void BeginTakeApart(StructureView view)
        {
            _takeApart?.Finish(_dustPuff);
            view.SetDeconstructProgress(0f);
            _takeApart = new TakeApart(view, _tuning, TakeApartDestination);
        }

        /// <summary>Where 07's weld beam meets structure <paramref name="instanceId"/>: its real surface.</summary>
        private Vector3 WeldPointOf(int instanceId)
        {
            if (_viewsById.TryGetValue(instanceId, out StructureView view) && view != null)
            {
                view.TryWeldContact(_rig.CutterOrigin.position, out Vector3 contact, out _);
                return contact;
            }

            if (_placedBase.TryGetStructure(instanceId, out PlacedStructure placed))
            {
                return placed.Pose.Position + Vector3.up * (placed.Definition.Height * 0.5f);
            }

            return _rover.Position + Vector3.up * 0.5f;
        }

        private void BurstSparksAt(Vector3 worldPoint)
        {
            if (_weldDebris == null || _visuals == null || _visuals.FeelTuning == null)
            {
                return;
            }

            _weldDebris.Vent(worldPoint, Vector3.up, _visuals.FeelTuning.VentSparks);
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

            _input?.SetModeOwner(this, false);
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
            if (_takeApartCore != null)
            {
                Destroy(_takeApartCore);
            }

            if (_takeApartSheath != null)
            {
                Destroy(_takeApartSheath);
            }
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
