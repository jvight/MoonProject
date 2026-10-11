using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// The crew's belly lift and the lab it carries 07 into (docs/features/M3-19 §4, §5), driving
    /// <see cref="LiftCycle"/>, the <see cref="Turntable"/> and the <see cref="LabInterior"/> on Art's nodes
    /// (docs/ARCHITECTURE.md "Contract: the lab"):
    /// <list type="bullet">
    /// <item>Dormant until home has power, then jammed (built in its tilt, one side low) and silent. The first time 07
    /// comes home carrying something for the lab (a relic or a blueprint tube) the platform creaks and its pad ring
    /// lights; 07 rests on it and stitches the cable (no feeding): the platform eases level, the jam and the dust drift
    /// go and the second cable hangs taut. Saved.</item>
    /// <item>The ride: 07 rests on the platform and after a breath it rises by itself (ruling 14: no button). The hatch
    /// leaves slide open, the platform rises eased at both ends along the shaft and a relic 07 was towing rides up
    /// beside it. Rover holds 07 on the platform (<see cref="LiftRideChanged"/>) and flies its camera up through the
    /// hatch. The lab is shown only while 07 rides or is up in it.</item>
    /// <item>Inside, the platform is the turntable: it turns by itself to the station that wants what 07 carries (a
    /// tube to the reader, a relic to the keepsake shelf; otherwise 07 arrives facing the survey chart), left and right
    /// turn it softly to the next station, forward looks at the station and back comes away from a look or brings the
    /// platform down, turning so 07 drives out forward toward the dock. The reader (<see cref="LabReader"/>) runs here
    /// too; the keepsake shelf is <see cref="HomeBase"/>'s, the chart's map the map system's.</item>
    /// <item>The ladder look: the first time near the crew's ladder, 07 looks up it, then down at the belly
    /// lift.</item>
    /// </list>
    /// Nothing allocates per frame.
    /// </summary>
    public sealed class LiftStation : MonoBehaviour
    {
        private const string StitchId = "lift";

        private enum LadderLook
        {
            None,
            Ladder,
            Lift,
        }

        [SerializeField] private LiftTuning _tuning;
        [SerializeField] private RadioTowerTuning _looks;

        [Tooltip("BellyLift/Platform: pivot at the middle of its top, built at PlatformDown in its jammed tilt.")]
        [SerializeField] private Transform _platform;

        [SerializeField] private Rigidbody _platformBody;

        [Tooltip("BellyLift/PlatformDown and PlatformUp: the platform's top on the ground, and flush with the cabin " +
                 "floor.")]
        [SerializeField] private Transform _down;

        [SerializeField] private Transform _up;

        [Tooltip("The platform's level rotation in BellyLift's space (Art's BellyPlatformLevel).")]
        [SerializeField] private Quaternion _levelRotation = Quaternion.identity;

        [SerializeField] private Transform _roverSpot;
        [SerializeField] private Renderer _padGlow;

        [Tooltip("Platform/Weather_Dust: the dust drift, gone once restored.")]
        [SerializeField] private Transform _dust;

        [SerializeField] private Transform _hatchLeft;
        [SerializeField] private Transform _hatchRight;

        [Tooltip("Metres each hatch leaf slides out to open (Art's HatchLeafTravel).")]
        [Min(0f)] [SerializeField] private float _hatchTravel = 1f;

        [SerializeField] private Transform _cableLeft;
        [SerializeField] private Transform _cableRight;

        [Tooltip("The cables' modelled length (Art's BellyCableBuiltLength).")]
        [Min(0.01f)] [SerializeField] private float _cableBuiltLength = 1f;

        [SerializeField] private Transform _cableJam;
        [SerializeField] private Transform _stitchPoint;

        [Tooltip("Lab: the cabin interior, shown while 07 rides or is up in it.")]
        [SerializeField] private Transform _labRoot;

        [Tooltip("The lab's stations in LabStation order: the survey chart, the reader, the tech board, the " +
                 "keepsakes.")]
        [SerializeField] private Transform[] _stations = Array.Empty<Transform>();

        [SerializeField] private Transform _readerSlot;
        [SerializeField] private Transform _readerReel;
        [SerializeField] private Transform _readerTape;
        [SerializeField] private Transform _dock;

        private readonly LabCommandLatch _commands = new LabCommandLatch();
        private readonly float[] _stationYaw = new float[LabInterior.StationCount];
        private LiftCycle _cycle;
        private Turntable _turntable;
        private LabInterior _lab;
        private LabReader _reader;
        private ISurveyChart _chart;
        private BasePower _power;
        private Blueprints _blueprints;
        private TetherSystem _tether;
        private CargoCradle _cradle;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IRoverStillness _stillness;
        private InputReader _input;
        private EventBus _events;
        private ISaveService _save;
        private RepairBeam _stitch;
        private EmissionGlow _pad;
        private IDisposable _approaches;
        private Transform _frame;
        private Vector3 _jammedPosition;
        private Quaternion _jammedRotation;
        private Vector3 _hatchLeftRest;
        private Vector3 _hatchRightRest;
        private Vector3 _cableLeftScale;
        private Vector3 _cableRightScale;
        private Vector3 _posePosition;
        private Quaternion _poseRotation;
        private float _padLevel;
        private float _hatch;
        private float _hatchTarget;
        private float _exitYaw;
        private float _yawOffset;
        private float _descentFrom;
        private float _descentDelta;
        private int _queuedTurn;
        private bool _playerTurned;
        private float _autoTurnAt = float.PositiveInfinity;
        private float _chartAt = float.PositiveInfinity;
        private Relic _rider;
        private Vector3 _riderFrom;
        private Quaternion _riderFromRotation;
        private float _riderTime;
        private float _ladderLookStarted = float.NegativeInfinity;
        private LadderLook _ladderLook;
        private bool _gazing;
        private bool _holding;
        private bool _initialized;

        /// <summary>The lift's phase (tests and capture sessions).</summary>
        public LiftPhase Phase => _cycle != null ? _cycle.Phase : LiftPhase.Dormant;

        /// <summary>The lab as 07 sees it (registered as <see cref="ILabInterior"/>).</summary>
        public LabInterior Lab => _lab;

        /// <summary>The turntable is turning between stations.</summary>
        public bool Turning => _turntable != null && _turntable.Turning;

        /// <summary>The reader's phase (tests and capture sessions).</summary>
        public ReaderPhase ReaderPhase => _reader != null ? _reader.Phase : ReaderPhase.Idle;

        /// <summary>The relic that rode up beside 07 and waits to be placed, or null.</summary>
        public Relic Rider => _rider;

        public LiftTuning Tuning => _tuning;

        /// <summary>Where 07 rests on the platform, to stitch it and to ride it, at the bottom stop.</summary>
        public Vector3 RideSpot => _frame.TransformPoint(_down.localPosition);

        /// <summary>Where the platform's top rests up in the lab, flush with the cabin floor.</summary>
        public Vector3 InsideSpot => _frame.TransformPoint(_up.localPosition);

        /// <summary>The way 07 drives onto the platform: from the dock, in under the belly.</summary>
        public Vector3 RideHeading
        {
            get
            {
                Vector3 inward = RideSpot - _dock.position;
                inward.y = 0f;
                return inward.normalized;
            }
        }

        /// <summary>Where <paramref name="station"/> stands in the lab (its face, at 07's lens height).</summary>
        public Vector3 StationPosition(LabStation station)
        {
            return _stations[(int)station].position;
        }

        /// <summary>07 is in the lab with the turntable at rest facing <paramref name="station"/>.</summary>
        public bool Faces(LabStation station)
        {
            return _initialized && _cycle.Phase == LiftPhase.Inside && _lab.Facing == station && !_turntable.Turning;
        }

        internal void Wire(LiftTuning tuning, RadioTowerTuning looks, Transform platform, Rigidbody platformBody,
            Transform down, Transform up, Quaternion levelRotation, Transform roverSpot, Renderer padGlow,
            Transform dust, Transform hatchLeft, Transform hatchRight, float hatchTravel, Transform cableLeft,
            Transform cableRight, float cableBuiltLength, Transform cableJam, Transform stitchPoint, Transform labRoot,
            Transform[] stations, Transform readerSlot, Transform readerReel, Transform readerTape, Transform dock)
        {
            _tuning = tuning;
            _looks = looks;
            _platform = platform;
            _platformBody = platformBody;
            _down = down;
            _up = up;
            _levelRotation = levelRotation;
            _roverSpot = roverSpot;
            _padGlow = padGlow;
            _dust = dust;
            _hatchLeft = hatchLeft;
            _hatchRight = hatchRight;
            _hatchTravel = hatchTravel;
            _cableLeft = cableLeft;
            _cableRight = cableRight;
            _cableBuiltLength = cableBuiltLength;
            _cableJam = cableJam;
            _stitchPoint = stitchPoint;
            _labRoot = labRoot;
            _stations = stations;
            _readerSlot = readerSlot;
            _readerReel = readerReel;
            _readerTape = readerTape;
            _dock = dock;
        }

        /// <summary>Wires the lift once home stands in place (after <see cref="HomeBase"/>).</summary>
        internal bool Initialize(GameplayServices services, BasePower power, IRoverStillness stillness,
            TetherSystem tether, CargoCradle cradle, Blueprints blueprints, ProgressionTuning progression)
        {
            string problem = WiringProblem(power, stillness, tether, cradle, blueprints, progression);
            if (problem != null)
            {
                Debug.LogError($"{nameof(LiftStation)}: {problem}", this);
                enabled = false;
                return false;
            }

            _power = power;
            _blueprints = blueprints;
            _tether = tether;
            _cradle = cradle;
            _rover = services.Rover;
            _rig = services.Rig;
            _stillness = stillness;
            _input = services.Input;
            _events = services.Events;
            _save = services.Save;
            _frame = _platform.parent;
            _cycle = new LiftCycle(_tuning);
            _turntable = new Turntable(_tuning.TurnSecondsPerQuarter, _tuning.TurnMinSeconds);
            _lab = new LabInterior(_events);
            _reader = new LabReader(transform, _readerSlot, _readerReel, _readerTape, services.Visuals.TetherBeam,
                blueprints, progression, _events, _save);
            _jammedPosition = _platform.localPosition;
            _jammedRotation = _platform.localRotation;
            _hatchLeftRest = _hatchLeft.localPosition;
            _hatchRightRest = _hatchRight.localPosition;
            _cableLeftScale = _cableLeft.localScale;
            _cableRightScale = _cableRight.localScale;
            _stitch = new RepairBeam("LiftStitch", transform, services.Visuals.TetherBeam, _looks.StitchRate,
                _looks.StitchSpread);
            _pad = new EmissionGlow(_padGlow);
            Vector3 axis = _frame.TransformPoint(_up.localPosition);
            for (int i = 0; i < _stationYaw.Length; i++)
            {
                _stationYaw[i] = FrameYaw(_stations[i].position - axis);
            }

            _exitYaw = FrameYaw(_dock.position - RideSpot);
            _approaches = _events.Subscribe<PlaceFirstApproached>(OnPlaceFirstApproached);
            Restore(null);
            _initialized = true;
            return true;
        }

        /// <summary>The map system connects the survey chart once it is up.</summary>
        internal void ConnectChart(ISurveyChart chart)
        {
            _chart = chart ?? throw new ArgumentNullException(nameof(chart));
        }

        internal LiftSaveData Capture()
        {
            return new LiftSaveData { restored = _cycle.IsRestored };
        }

        internal void Restore(LiftSaveData data)
        {
            bool restored = data != null && data.restored;
            _cycle.Restore(restored);
            _turntable.Snap(0f);
            _cableJam.gameObject.SetActive(!restored);
            _dust.gameObject.SetActive(!restored);
            _labRoot.gameObject.SetActive(false);
            _padLevel = restored ? _tuning.PadGlow : 0f;
            _pad.Apply(_padLevel);
            _hatch = 0f;
            _hatchTarget = 0f;
            ApplyPose(true);
            ApplyHatch();
        }

        /// <summary>
        /// A loaded game: a tube the reader had taken decodes again (after the blueprints are restored).
        /// </summary>
        internal void RestoreReader()
        {
            _reader.Restore();
        }

        /// <summary>The keepsake shelf takes the relic that rode up.</summary>
        internal void LetRiderGo(Relic relic)
        {
            if (relic != null && relic == _rider)
            {
                _rider = null;
            }
        }

        private string WiringProblem(BasePower power, IRoverStillness stillness, TetherSystem tether,
            CargoCradle cradle, Blueprints blueprints, ProgressionTuning progression)
        {
            if (_tuning == null || _looks == null)
            {
                return "LiftTuning or the tower's looks are not assigned.";
            }

            if (_platform == null || _platform.parent == null || _platformBody == null || _down == null || _up == null
                || _roverSpot == null || _padGlow == null || _dust == null || _hatchLeft == null || _hatchRight == null
                || _cableLeft == null || _cableRight == null || _cableJam == null || _stitchPoint == null
                || _labRoot == null || _readerSlot == null || _readerReel == null || _readerTape == null
                || _dock == null)
            {
                return "a belly lift or lab node is not assigned (see docs/ARCHITECTURE.md \"Contract: the lab\").";
            }

            if (_stations == null || _stations.Length != LabInterior.StationCount || Array.IndexOf(_stations, null) >= 0)
            {
                return $"the lab needs its {LabInterior.StationCount} stations (Lab/Station_Chart, Station_Reader, "
                    + "Station_Board, Station_Keepsakes).";
            }

            return power == null || stillness == null || tether == null || cradle == null || blueprints == null
                   || progression == null
                ? "the base power, 07's stillness, the tether, the cradle, the blueprints or ProgressionTuning is "
                  + "missing."
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
            LabCommand command = _cycle.Phase == LiftPhase.Inside
                ? _commands.Step(_input.Drive, _tuning.CommandPress, _tuning.CommandRelease)
                : LabCommand.None;
            bool down = Command(command, now);
            var input = new LiftInput(_power.HasReached(BasePowerStage.Home), Carrying() && NearHome(), OnPlatform(),
                _stillness.StillSeconds, down);
            Play(_cycle.Step(input, deltaTime), now);

            bool stitching = _cycle.Phase == LiftPhase.Stitching;
            _stitch.Step(stitching, true, eye, _stitchPoint.position, now, deltaTime);
            StepLab(now, deltaTime);
            _reader.Step(Faces(LabStation.Reader), eye, now, deltaTime);
            StepLadderLook(now);
            Hold(stitching || _cycle.Holds07);
            bool lit = _cycle.Phase == LiftPhase.Called || _cycle.Phase == LiftPhase.Ready;
            _padLevel = Damp.Toward(_padLevel, lit ? _tuning.PadGlow : 0f, _tuning.PadEase, deltaTime);
            _pad.Apply(_padLevel);
            _hatch = Mathf.MoveTowards(_hatch, _hatchTarget, deltaTime / _tuning.HatchSeconds);
            ApplyHatch();
            ApplyPose(false);
            StepRider(deltaTime);
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

        /// <summary>Acts on the player's command in the lab; true when it asks to go down.</summary>
        private bool Command(LabCommand command, float now)
        {
            switch (command)
            {
                case LabCommand.TurnLeft:
                case LabCommand.TurnRight:
                    int direction = command == LabCommand.TurnRight ? 1 : -1;
                    _playerTurned = true;
                    if (_turntable.Turning)
                    {
                        _queuedTurn = direction;
                    }
                    else
                    {
                        TurnTo(LabInterior.Next(_lab.Facing, direction), direction, now);
                    }

                    return false;
                case LabCommand.Look:
                    if (!_turntable.Turning && !_lab.IsLooking)
                    {
                        LookAtFacing();
                    }

                    return false;
                case LabCommand.Down:
                    if (_lab.IsLooking)
                    {
                        _lab.Look(false);
                        return false;
                    }

                    return !_reader.Feeding;
                default:
                    return false;
            }
        }

        private void LookAtFacing()
        {
            LabStation facing = _lab.Facing;
            if (facing == LabStation.Chart && !_power.HasReached(BasePowerStage.Chart))
            {
                _events.Publish(new StationNotYet(ProgressionKeys.NotYetChart, StationPosition(facing), 0f));
            }

            _lab.Look(true);
        }

        private void Play(LiftStep step, float now)
        {
            switch (step)
            {
                case LiftStep.Called:
                    _events.Publish(new LiftCued(LiftCue.WinchCreaked, _stitchPoint.position));
                    break;
                case LiftStep.StitchStarted:
                    _rig.SetGazeTarget(this, _stitchPoint.position, _tuning.GazePriority);
                    _gazing = true;
                    _events.Publish(new StationCued(StationCue.StitchStarted, StitchId, _stitchPoint.position));
                    break;
                case LiftStep.Restored:
                    ClearGaze();
                    _cableJam.gameObject.SetActive(false);
                    _dust.gameObject.SetActive(false);
                    _events.Publish(new StationCued(StationCue.HatchClosed, StitchId, _stitchPoint.position));
                    _save.SaveNow();
                    break;
                case LiftStep.RideStarted:
                    ClearGaze();
                    _yawOffset = Mathf.DeltaAngle(_turntable.Yaw, FrameYaw(_rover.Rotation * Vector3.forward));
                    Board();
                    _labRoot.gameObject.SetActive(true);
                    _hatchTarget = 1f;
                    _events.Publish(new LiftCued(LiftCue.HatchOpened, HatchCentre()));
                    _events.Publish(new LiftCued(LiftCue.RideStarted, _platform.position));
                    _events.Publish(new LiftRideChanged(true, false, _platform.position));
                    break;
                case LiftStep.Arrived:
                    _events.Publish(new LiftCued(LiftCue.RideStopped, _platform.position));
                    break;
                case LiftStep.ReachedInside:
                    _events.Publish(new LiftRideChanged(false, true, _platform.position));
                    Enter(now);
                    break;
                case LiftStep.Descending:
                    _lab.Leave();
                    ClearGaze();
                    _descentFrom = _turntable.Yaw;
                    _descentDelta = Mathf.DeltaAngle(_descentFrom, _exitYaw - _yawOffset);
                    _queuedTurn = 0;
                    _events.Publish(new LiftCued(LiftCue.RideStarted, _platform.position));
                    _events.Publish(new LiftRideChanged(true, false, _platform.position));
                    break;
                case LiftStep.ReachedBottom:
                    _turntable.Snap(_descentFrom + _descentDelta);
                    _hatchTarget = 0f;
                    _labRoot.gameObject.SetActive(false);
                    _events.Publish(new LiftCued(LiftCue.HatchClosed, HatchCentre()));
                    _events.Publish(new LiftRideChanged(false, false, _platform.position));
                    SetRiderDown();
                    break;
            }
        }

        /// <summary>
        /// The platform settled in the lab: it faces 07 toward the station that wants what it carries.
        /// </summary>
        private void Enter(float now)
        {
            LabStation wanted = Wanted();
            _commands.Disarm();
            _playerTurned = false;
            _queuedTurn = 0;
            _lab.Enter(wanted);
            TurnTo(wanted, 0, now);
        }

        private void StepLab(float now, float deltaTime)
        {
            if (_cycle.Phase != LiftPhase.Inside)
            {
                return;
            }

            if (_turntable.Step(deltaTime))
            {
                Faced(now);
            }

            bool resting = !_turntable.Turning && !_lab.IsLooking;
            if (resting && !_playerTurned && now >= _autoTurnAt)
            {
                LabStation wanted = Wanted();
                if (wanted != LabStation.Chart && wanted != _lab.Facing)
                {
                    TurnTo(wanted, 0, now);
                }
            }

            if (resting && now >= _chartAt && _lab.Facing == LabStation.Chart)
            {
                _chartAt = float.PositiveInfinity;
                LookAtFacing();
            }

            _rig.SetGazeTarget(this, StationPosition(_lab.Facing), _tuning.GazePriority);
            _gazing = true;
        }

        private void TurnTo(LabStation station, int direction, float now)
        {
            _lab.TurnTo(station);
            _autoTurnAt = float.PositiveInfinity;
            _chartAt = float.PositiveInfinity;
            if (!_turntable.TurnTo(_stationYaw[(int)station] - _yawOffset, direction))
            {
                Faced(now);
            }
        }

        /// <summary>
        /// The turntable came to rest: announce it, then take a waiting turn or plan what comes next.
        /// </summary>
        private void Faced(float now)
        {
            _lab.Faced();
            if (_queuedTurn != 0)
            {
                int direction = _queuedTurn;
                _queuedTurn = 0;
                TurnTo(LabInterior.Next(_lab.Facing, direction), direction, now);
                return;
            }

            _autoTurnAt = now + _tuning.AutoTurnDelay;
            bool news = _chart != null && _chart.HasNews && _power.HasReached(BasePowerStage.Chart);
            _chartAt = _lab.Facing == LabStation.Chart && news ? now + _tuning.ChartBreath : float.PositiveInfinity;
        }

        /// <summary>
        /// The station that wants what 07 carries: a tube the reader (until the beam has fed it in), a relic the
        /// shelf; else the chart.
        /// </summary>
        private LabStation Wanted()
        {
            if (_reader.Wants || _reader.Feeding)
            {
                return LabStation.Reader;
            }

            return _rider != null || _cradle.Carried != null ? LabStation.Keepsakes : LabStation.Chart;
        }

        /// <summary>07 carries something for the lab: a blueprint tube, a towed relic or one in the cradle.</summary>
        private bool Carrying()
        {
            return _blueprints.IsCarrying || _tether.Towed != null || _cradle.Carried != null;
        }

        private bool NearHome()
        {
            return SurfaceRules.HorizontalDistance(_rover.Position, RideSpot) <= _tuning.CallRadius;
        }

        /// <summary>The towed relic comes aboard and floats to its place beside 07.</summary>
        private void Board()
        {
            Relic relic = _tether.HandOver();
            if (relic == null)
            {
                return;
            }

            relic.BeginCarry(RelicState.Riding, -1);
            _rider = relic;
            _riderFrom = relic.transform.position;
            _riderFromRotation = relic.transform.rotation;
            _riderTime = 0f;
        }

        private void StepRider(float deltaTime)
        {
            if (_rider == null)
            {
                return;
            }

            if (_rider.State != RelicState.Riding)
            {
                _rider = null;
                return;
            }

            _riderTime += deltaTime;
            float t = Mathf.Clamp01(_riderTime / _tuning.RiderBoardSeconds);
            float eased = t * t * (3f - 2f * t);
            Vector3 seat = _platform.TransformPoint(_tuning.RiderOffset);
            _rider.SetCarryPose(Vector3.Lerp(_riderFrom, seat, eased),
                Quaternion.Slerp(_riderFromRotation, _platform.rotation, eased));
        }

        /// <summary>Down again with the relic still beside 07: it is set down loose on the platform.</summary>
        private void SetRiderDown()
        {
            if (_rider == null)
            {
                return;
            }

            _rider.Release();
            _rider = null;
        }

        private bool OnPlatform()
        {
            Vector3 spot = _roverSpot.position;
            Vector3 rover = _rover.Position;
            float dx = rover.x - spot.x;
            float dz = rover.z - spot.z;
            return dx * dx + dz * dz <= _tuning.PlatformRadius * _tuning.PlatformRadius;
        }

        /// <summary>
        /// The platform's pose in BellyLift's space between its jammed tilt, level at the bottom stop and flush with
        /// the cabin floor (moved by its kinematic body in <see cref="FixedUpdate"/> so 07 can drive onto it; set at
        /// once when <paramref name="instant"/>). It turns about its own Y: the turntable's yaw, or on the way down
        /// the turn toward the dock. The cables stretch to it, the second one taut once restored.
        /// </summary>
        private void ApplyPose(bool instant)
        {
            float level = _cycle.Level;
            Vector3 travel = Vector3.Lerp(_down.localPosition, _up.localPosition, _cycle.Height);
            float yaw = _cycle.Phase == LiftPhase.Lowering
                ? _descentFrom + _descentDelta * _cycle.Descent
                : _turntable.Yaw;
            Quaternion levelRotation = _levelRotation * Quaternion.Euler(0f, yaw, 0f);
            Vector3 local = Vector3.Lerp(_jammedPosition, travel, level);
            Quaternion localRotation = Quaternion.Slerp(_jammedRotation, levelRotation, level);
            _posePosition = _frame.TransformPoint(local);
            _poseRotation = _frame.rotation * localRotation;
            if (instant)
            {
                _platform.SetPositionAndRotation(_posePosition, _poseRotation);
                _platformBody.position = _posePosition;
                _platformBody.rotation = _poseRotation;
            }

            Stretch(_cableLeft, _cableLeftScale);
            bool restored = _cycle.IsRestored;
            if (_cableRight.gameObject.activeSelf != restored)
            {
                _cableRight.gameObject.SetActive(restored);
            }

            Stretch(_cableRight, _cableRightScale);
        }

        /// <summary>A cable hangs down its local Y from its drum to the platform's top.</summary>
        private void Stretch(Transform cable, Vector3 builtScale)
        {
            float length = Mathf.Max(0f, cable.position.y - _posePosition.y);
            cable.localScale = new Vector3(builtScale.x, builtScale.y * length / _cableBuiltLength, builtScale.z);
        }

        /// <summary>The hatch leaves slide out along their local X (the left one toward -X), eased.</summary>
        private void ApplyHatch()
        {
            float open = _hatch * _hatch * (3f - 2f * _hatch) * _hatchTravel;
            _hatchLeft.localPosition = _hatchLeftRest + _hatchLeft.localRotation * Vector3.left * open;
            _hatchRight.localPosition = _hatchRightRest + _hatchRight.localRotation * Vector3.right * open;
        }

        private Vector3 HatchCentre()
        {
            return _hatchLeft.parent.TransformPoint(Vector3.Lerp(_hatchLeftRest, _hatchRightRest, 0.5f));
        }

        /// <summary>
        /// The first time near the crew's ladder (docs/features/M3-16 "Each place", M3-19 §3), 07 looks up it toward
        /// the roof hatch it leads to, then down at the belly lift.
        /// </summary>
        private void OnPlaceFirstApproached(PlaceFirstApproached approached)
        {
            if (approached.PlaceId == ProgressionKeys.PlaceLadder)
            {
                _ladderLookStarted = Time.time;
            }
        }

        /// <summary>
        /// Up the ladder, then at the lift, each look published as it begins (Rover frames it) and once more as the
        /// look ends; the stitch or a ride cuts it short (their gaze takes over).
        /// </summary>
        private void StepLadderLook(float now)
        {
            float looked = now - _ladderLookStarted;
            bool busy = _cycle.Phase == LiftPhase.Stitching || _cycle.Holds07;
            LadderLook look = LadderLook.None;
            if (!busy && looked >= 0f)
            {
                look = looked < _tuning.LadderLookSeconds ? LadderLook.Ladder
                    : looked < _tuning.LadderLookSeconds + _tuning.LiftLookSeconds ? LadderLook.Lift
                    : LadderLook.None;
            }

            if (look == LadderLook.None)
            {
                if (_ladderLook != LadderLook.None)
                {
                    _ladderLookStarted = float.NegativeInfinity;
                    if (!busy)
                    {
                        ClearGaze();
                    }

                    _events.Publish(new LadderLookChanged(false, _platform.position));
                }

                _ladderLook = LadderLook.None;
                return;
            }

            Vector3 subject = look == LadderLook.Ladder
                ? _dock.position + Vector3.up * _tuning.LadderLookHeight
                : _platform.position;
            _rig.SetGazeTarget(this, subject, _tuning.GazePriority);
            _gazing = true;
            if (look != _ladderLook)
            {
                _ladderLook = look;
                _events.Publish(new LadderLookChanged(true, subject));
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

        /// <summary>The yaw (deg) of a world direction in BellyLift's space, about its Y.</summary>
        private float FrameYaw(Vector3 direction)
        {
            Vector3 local = _frame.InverseTransformDirection(direction);
            return Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
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
        }
    }
}
