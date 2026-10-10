using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// The crew's lift on the lander (docs/features/M3-16 step 8 and "Each place"), driving <see cref="LiftCycle"/>:
    /// <list type="bullet">
    /// <item>Dormant and jammed until the base power reaches the lift stage. Then 07 rests on the feed pad, its beam
    /// feeds the winch hopper (<see cref="HopperFeed"/>, a little metal and wiring) and stitches the winch: the
    /// platform eases level and down to the bottom stop, the taut cable replaces the snapped one and the winch lamp
    /// lights. Restored is saved.</item>
    /// <item>The ride: 07 rests on the platform and is carried up (a kinematic platform it stands on, 07 held still)
    /// to the deck, where <see cref="LiftRideChanged"/> opens the lookout; any input brings it back down.</item>
    /// <item>The ladder look: the first time near the crew's ladder, 07 looks up it, then at the lift.</item>
    /// <item>Audio's beats (<see cref="LiftCued"/>): the powered, jammed winch creaks as 07 comes near; the platform
    /// starts moving and reaches each stop.</item>
    /// </list>
    /// </summary>
    public sealed class LiftStation : MonoBehaviour
    {
        private const string FeedId = "lift";
        private const float InputEpsilon = 1e-4f;

        [SerializeField] private LiftTuning _tuning;
        [SerializeField] private RadioTowerTuning _looks;
        [SerializeField] private Transform _lander;
        [SerializeField] private Transform _platform;
        [SerializeField] private Rigidbody _platformBody;
        [SerializeField] private Transform _roverSpot;
        [SerializeField] private Transform _bottom;
        [SerializeField] private Transform _top;
        [SerializeField] private Transform _cableLeft;
        [SerializeField] private Transform _cableRight;
        [SerializeField] private Transform _cableSnapped;
        [SerializeField] private Renderer _winchLamp;
        [SerializeField] private Transform _hopperMouth;
        [SerializeField] private Transform _dock;
        [SerializeField] private float _cableBuiltLength = 1f;

        private readonly ApproachLatch _creak = new ApproachLatch();
        private LiftCycle _cycle;
        private BasePower _power;
        private MaterialStock _materials;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IRoverStillness _stillness;
        private InputReader _input;
        private EventBus _events;
        private HopperFeed _feed;
        private RepairBeam _stitch;
        private StationPad _feedPad;
        private StationPad _ridePad;
        private EmissionGlow _lamp;
        private IDisposable _approaches;
        private Vector3 _jammedPosition;
        private Quaternion _jammedRotation;
        private Vector3 _posePosition;
        private Quaternion _poseRotation;
        private Vector3 _cableLeftScale;
        private Vector3 _cableRightScale;
        private float _lampLevel;
        private float _ladderLookStarted = float.NegativeInfinity;
        private bool _gazing;
        private bool _holding;
        private bool _initialized;

        /// <summary>The lift's phase (tests and capture sessions).</summary>
        public LiftPhase Phase => _cycle != null ? _cycle.Phase : LiftPhase.Dormant;

        /// <summary>Where 07 rests to feed the winch.</summary>
        public Vector3 FeedPadCentre => _feedPad.Centre;

        /// <summary>Where 07 rests on the platform to ride it, at the bottom stop.</summary>
        public Vector3 RideSpot => _ridePad.Centre;

        /// <summary>Where the rover spot stands with the platform at the top stop, by the deck.</summary>
        public Vector3 TopSpot => _lander.TransformPoint(_top.localPosition + _roverSpot.localPosition);

        internal void Wire(LiftTuning tuning, RadioTowerTuning looks, Transform lander, Transform platform,
            Rigidbody platformBody, Transform roverSpot, Transform bottom, Transform top, Transform cableLeft,
            Transform cableRight, Transform cableSnapped, Renderer winchLamp, Transform hopperMouth, Transform dock,
            float cableBuiltLength)
        {
            _tuning = tuning;
            _looks = looks;
            _lander = lander;
            _platform = platform;
            _platformBody = platformBody;
            _roverSpot = roverSpot;
            _bottom = bottom;
            _top = top;
            _cableLeft = cableLeft;
            _cableRight = cableRight;
            _cableSnapped = cableSnapped;
            _winchLamp = winchLamp;
            _hopperMouth = hopperMouth;
            _dock = dock;
            _cableBuiltLength = cableBuiltLength;
        }

        internal bool Initialize(GameplayServices services, BasePower power, SalvageCatalog bundles,
            IRoverStillness stillness)
        {
            string problem = WiringProblem(power, bundles, stillness);
            if (problem != null)
            {
                Debug.LogError($"{nameof(LiftStation)}: {problem}", this);
                enabled = false;
                return false;
            }

            _power = power;
            _materials = services.Materials;
            _rover = services.Rover;
            _rig = services.Rig;
            _stillness = stillness;
            _input = services.Input;
            _events = services.Events;
            _cycle = new LiftCycle(_tuning);
            _jammedPosition = _platform.localPosition;
            _jammedRotation = _platform.localRotation;
            _cableLeftScale = _cableLeft.localScale;
            _cableRightScale = _cableRight.localScale;
            _feed = new HopperFeed("LiftFeed", transform, services.Visuals.TetherBeam, bundles, _looks.FeedLook,
                _events);
            _stitch = new RepairBeam("LiftStitch", transform, services.Visuals.TetherBeam, _looks.StitchRate,
                _looks.StitchSpread);
            Vector3 mouth = _hopperMouth.position;
            Vector3 outward = Vector3.ProjectOnPlane(_hopperMouth.forward, Vector3.up).normalized;
            _feedPad = new StationPad("LiftFeedPad", transform, services.Terrain,
                mouth + outward * _tuning.FeedPadDistance, _looks.PadLook, services.Visuals.WarmRing);
            _ridePad = StationPad.OnDeck("LiftRidePad", transform, BottomSpot(), _looks.PadLook,
                services.Visuals.WarmRing);
            _lamp = new EmissionGlow(_winchLamp);
            _approaches = _events.Subscribe<PlaceFirstApproached>(OnPlaceFirstApproached);
            _cycle.Restore(false);
            ApplyPose(true);
            _initialized = true;
            return true;
        }

        internal LiftSaveData Capture()
        {
            return new LiftSaveData { restored = _cycle.IsRestored };
        }

        internal void Restore(LiftSaveData data)
        {
            _cycle.Restore(data != null && data.restored);
            _lampLevel = _cycle.IsRestored ? _tuning.LampGlow : 0f;
            ApplyPose(true);
        }

        private string WiringProblem(BasePower power, SalvageCatalog bundles, IRoverStillness stillness)
        {
            if (_tuning == null || _looks == null)
            {
                return "LiftTuning or the tower's looks are not assigned.";
            }

            if (_lander == null || _platform == null || _platformBody == null || _roverSpot == null
                || _bottom == null || _top == null || _cableLeft == null || _cableRight == null
                || _cableSnapped == null || _winchLamp == null || _hopperMouth == null || _dock == null)
            {
                return "a lift node of the lander is not assigned (LiftPlatform, its RoverSpot and body, LiftBottom, "
                    + "LiftTop, the cables, WinchLamp, LiftHopperMouth, DockAnchor).";
            }

            if (_cableBuiltLength <= 0f)
            {
                return "the cables' built length is not set.";
            }

            return power == null || bundles == null || stillness == null
                ? "the base power, salvage catalog or 07's stillness is missing."
                : null;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (!_initialized || deltaTime <= 0f)
            {
                return;
            }

            float now = Time.time;
            Vector3 eye = _rig.TetherOrigin.position;
            bool feedDone = _cycle.Phase == LiftPhase.Feeding
                && _feed.Step(eye, _rig.CargoSocket.position, _hopperMouth.position, now, deltaTime);
            var input = new LiftInput(_power.HasReached(BasePowerStage.Lift), _materials.Has(_tuning.FeedRecipe),
                _feedPad.Occupied, OnPlatform(), _stillness.StillSeconds, feedDone, PlayerInput());
            Play(_cycle.Step(input, deltaTime), feedDone, now);
            StepCreak();

            bool stitching = _cycle.Phase == LiftPhase.Stitching;
            _stitch.Step(stitching, true, eye, _platform.position, now, deltaTime);
            if (stitching)
            {
                _rig.SetGazeTarget(this, _platform.position, _tuning.GazePriority);
            }

            StepPads(now, deltaTime);
            StepLadderLook(now);
            Hold(_cycle.Phase == LiftPhase.Feeding || stitching || _cycle.Holds07);
            _lampLevel = Damp.Toward(_lampLevel, _cycle.IsRestored ? _tuning.LampGlow : 0f, _tuning.SettleSeconds,
                deltaTime);
            _lamp.Apply(_lampLevel);
            ApplyPose(false);
        }

        private void FixedUpdate()
        {
            if (!_initialized)
            {
                return;
            }

            _platformBody.MovePosition(_posePosition);
            _platformBody.MoveRotation(_poseRotation);
        }

        private void Play(LiftStep step, bool fed, float now)
        {
            if (fed)
            {
                _events.Publish(new StationCued(StationCue.Fed, FeedId, _hopperMouth.position));
                _events.Publish(new StationCued(StationCue.StitchStarted, FeedId, _hopperMouth.position));
            }

            switch (step)
            {
                case LiftStep.FeedStarted:
                    if (!_materials.TrySpend(_tuning.FeedRecipe))
                    {
                        _cycle.Restore(false);
                        return;
                    }

                    _feed.Begin(_tuning.FeedRecipe, FeedId, now);
                    _rig.SetGazeTarget(this, _hopperMouth.position, _tuning.GazePriority);
                    _gazing = true;
                    _events.Publish(new StationCued(StationCue.FeedStarted, FeedId, _hopperMouth.position));
                    break;
                case LiftStep.Restored:
                    ClearGaze();
                    _events.Publish(new StationCued(StationCue.HatchClosed, FeedId, _hopperMouth.position));
                    break;
                case LiftStep.RideStarted:
                case LiftStep.Descending:
                    _events.Publish(new LiftCued(LiftCue.RideStarted, _platform.position));
                    _events.Publish(new LiftRideChanged(true, false, _platform.position));
                    break;
                case LiftStep.Arrived:
                    _events.Publish(new LiftCued(LiftCue.RideStopped, _platform.position));
                    break;
                case LiftStep.ReachedTop:
                    _events.Publish(new LiftRideChanged(false, true, _platform.position));
                    break;
                case LiftStep.ReachedBottom:
                    _events.Publish(new LiftRideChanged(false, false, _platform.position));
                    break;
            }
        }

        /// <summary>
        /// The powered, jammed winch creaks once each time 07 comes near it (Audio's <see cref="LiftCue.WinchCreaked"/>),
        /// a nudge toward the lift before it is restored.
        /// </summary>
        private void StepCreak()
        {
            Vector3 winch = _winchLamp.transform.position;
            Vector3 rover = _rover.Position;
            float distance = Vector2.Distance(new Vector2(winch.x, winch.z), new Vector2(rover.x, rover.z));
            bool jammed = _cycle.Phase == LiftPhase.Jammed;
            if (_creak.Step(jammed ? distance : float.PositiveInfinity, _tuning.CreakRadius, _tuning.CreakRearm))
            {
                _events.Publish(new LiftCued(LiftCue.WinchCreaked, winch));
            }
        }

        private void StepPads(float now, float deltaTime)
        {
            bool jammed = _cycle.Phase == LiftPhase.Jammed;
            if (jammed || _cycle.Phase == LiftPhase.Feeding)
            {
                _feedPad.Tick(_rover.Position, false, _materials.Has(_tuning.FeedRecipe), now, deltaTime);
            }
            else
            {
                _feedPad.Rest(deltaTime);
            }

            if (_cycle.Phase == LiftPhase.Ready)
            {
                _ridePad.Tick(_rover.Position, false, true, now, deltaTime);
            }
            else
            {
                _ridePad.Rest(deltaTime);
            }
        }

        private bool OnPlatform()
        {
            Vector3 spot = _roverSpot.position;
            Vector3 rover = _rover.Position;
            float dx = rover.x - spot.x;
            float dz = rover.z - spot.z;
            return dx * dx + dz * dz <= _tuning.PlatformRadius * _tuning.PlatformRadius;
        }

        private bool PlayerInput()
        {
            return _input.Drive.sqrMagnitude > InputEpsilon || _input.LookDelta.sqrMagnitude > InputEpsilon
                || _input.LookRate.sqrMagnitude > InputEpsilon;
        }

        /// <summary>
        /// The platform's pose between jammed, level at the bottom stop and the top stop (moved by its kinematic body
        /// in <see cref="FixedUpdate"/> so 07 rides on it; set at once when <paramref name="instant"/>); the cables
        /// stretch to it and swap once restored.
        /// </summary>
        private void ApplyPose(bool instant)
        {
            float level = _cycle.Level;
            Vector3 travel = Vector3.Lerp(_bottom.localPosition, _top.localPosition, _cycle.Height);
            Vector3 local = Vector3.Lerp(_jammedPosition, travel, level);
            _posePosition = _lander.TransformPoint(local);
            _poseRotation = _lander.rotation * Quaternion.Slerp(_jammedRotation, Quaternion.identity, level);
            if (instant)
            {
                _platform.SetPositionAndRotation(_posePosition, _poseRotation);
                _platformBody.position = _posePosition;
                _platformBody.rotation = _poseRotation;
            }

            float extra = _jammedPosition.y - local.y;
            float stretch = (_cableBuiltLength + extra) / _cableBuiltLength;
            _cableLeft.localScale = new Vector3(_cableLeftScale.x, _cableLeftScale.y * stretch, _cableLeftScale.z);
            _cableRight.localScale = new Vector3(_cableRightScale.x, _cableRightScale.y * stretch,
                _cableRightScale.z);
            bool restored = _cycle.IsRestored;
            if (_cableRight.gameObject.activeSelf != restored)
            {
                _cableRight.gameObject.SetActive(restored);
                _cableSnapped.gameObject.SetActive(!restored);
            }
        }

        /// <summary>The rover spot's place when the platform rests level at the bottom stop.</summary>
        private Vector3 BottomSpot()
        {
            Vector3 local = _bottom.localPosition + _roverSpot.localPosition;
            return _lander.TransformPoint(local);
        }

        /// <summary>
        /// The first time near the crew's ladder (docs/features/M3-16 "Each place"), 07 looks up it (the dock stands at
        /// its foot; the deck it climbs to is the lift's top stop), then at the lift.
        /// </summary>
        private void OnPlaceFirstApproached(PlaceFirstApproached approached)
        {
            if (approached.PlaceId == ProgressionKeys.PlaceLadder)
            {
                _ladderLookStarted = Time.time;
            }
        }

        private void StepLadderLook(float now)
        {
            float looked = now - _ladderLookStarted;
            if (looked < 0f || float.IsInfinity(looked) || _cycle.Phase == LiftPhase.Feeding
                || _cycle.Phase == LiftPhase.Stitching)
            {
                return;
            }

            if (looked < _tuning.LadderLookSeconds)
            {
                Vector3 dock = _dock.position;
                _rig.SetGazeTarget(this, new Vector3(dock.x, _top.position.y, dock.z), _tuning.GazePriority);
                _gazing = true;
            }
            else if (looked < _tuning.LadderLookSeconds + _tuning.LiftLookSeconds)
            {
                _rig.SetGazeTarget(this, _platform.position, _tuning.GazePriority);
                _gazing = true;
            }
            else
            {
                _ladderLookStarted = float.NegativeInfinity;
                ClearGaze();
            }
        }

        private void ClearGaze()
        {
            if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        private void Hold(bool hold)
        {
            if (_holding == hold)
            {
                return;
            }

            _holding = hold;
            _rig.SetHoldStill(this, hold);
        }

        private void OnDisable()
        {
            if (!_initialized)
            {
                return;
            }

            Hold(false);
            ClearGaze();
        }

        private void OnDestroy()
        {
            _approaches?.Dispose();
            _feedPad?.Dispose();
            _ridePad?.Dispose();
        }
    }
}
