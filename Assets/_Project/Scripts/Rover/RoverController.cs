using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.Rover
{
    /// <summary>
    /// The player rover: a hidden, rotation-locked physics sphere pushed by accelerations along a steered heading, with
    /// the visual model following the interpolated sphere. Registers itself as <see cref="IRoverState"/> and
    /// <see cref="IRoverRig"/> (interaction points and gaze requests for gameplay), <see cref="IRoverStillness"/>
    /// (how long 07 has rested, stepped every rendered frame) and <see cref="IRoverPlacement"/> (set 07 down elsewhere
    /// in one step, publishing <see cref="RoverPlaced"/>) and <see cref="IRoverCargoSeat"/> (the cargo hatch's
    /// socket), publishes <see cref="RoverLanded"/> and <see cref="RoverBoostChanged"/>, and ticks its visual rig,
    /// road light, wheel effects and lamp motes in a fixed order every frame. The Boost Coils raise the top speed
    /// gently while held at cruise on open, flat-ish ground (<see cref="BoostDrive"/>). Docked at home
    /// (<see cref="RoverDockChanged"/>), 07 eases onto the dock's anchor and rests there, its lamp dimmed, while its
    /// drive input is still read so a touch shows at once and gameplay can undock it; undocked, it is free at once.
    /// Needs the World's <see cref="ITerrainQuery"/> (spawn height, stuck recovery), so it initialises after the World
    /// systems. If 07 is trying to drive but stuck for a few seconds, it is lifted gently to a nearby open spot.
    /// The maths lives in plain classes (<see cref="LongitudinalDrive"/>, <see cref="SteeringModel"/>,
    /// <see cref="GroundModel"/>, <see cref="ObstacleProbe"/>, <see cref="ContactResponse"/>,
    /// <see cref="LandingDetector"/>); this component only wires them to physics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverController : MonoBehaviour, IGameSystem, IRoverState, IRoverRig, IRoverAbilities,
        IRoverPlacement, IRoverFaceScreen, IRoverCargoSeat
    {
        /// <summary>The probe starts this fraction of the radius above the centre, so slight sinking hits.</summary>
        private const float ProbeLiftFraction = 0.5f;
        private const float ScreenRaycastMaxDistance = 12f;

        [Tooltip("Drive, steering, ground and landing tuning (Assets/_Project/Data/Tuning/RoverTuning.asset).")]
        [SerializeField] private RoverTuning _tuning;

        [Tooltip("Rigidbody of the hidden physics sphere (child 'PhysicsSphere', layer Rover).")]
        [SerializeField] private Rigidbody _body;

        [Tooltip("Sphere collider on the physics sphere; its radius is set from the tuning.")]
        [SerializeField] private SphereCollider _sphere;

        [Tooltip("Drives the RoverModel nodes (body alignment, suspension, wheels, jelly lean).")]
        [SerializeField] private RoverVisualRig _visualRig;

        [Tooltip("Tire tracks and dust.")]
        [SerializeField] private RoverWheelFx _wheelFx;

        [Tooltip("Dust motes hanging in the headlamp's beam.")]
        [SerializeField] private RoverLampMotes _lampMotes;

        [Tooltip("07's road light.")]
        [SerializeField] private RoverHeadlamp _headlamp;

        [Tooltip("The amber flank lamps along the stripe: 07's battery (docs/features/M3-20).")]
        [SerializeField] private RoverFlankLamps _flankLamps;

        [Tooltip("The Tab \"07\" page's live portrait of 07 on the Visual root (0.5.5 §3.5).")]
        [SerializeField] private RoverPortrait _portrait;

        [Tooltip("07's expressive body language and CRT face director driver on the Rover root.")]
        [SerializeField] private RoverBodyLanguage _bodyLanguage;

        [Tooltip("MeshCollider on RoverModel/Neck/NeckLift/Head/Screen for diegetic UI pointer raycasting.")]
        [SerializeField] private MeshCollider _screenCollider;

        [Tooltip("The charge-up's ConduitGlow renderers: the deck pair and the neck's (0.5.5 §5.4).")]
        [SerializeField] private Renderer[] _conduitGlows = new Renderer[0];

        [Tooltip("The stage cells' StageGlow sleeves, two per stage (left, right), stage 1 first.")]
        [SerializeField] private Renderer[] _stageSleeves = new Renderer[0];

        [Tooltip("The lock ring's LockGlow on the eye, lit at the full lock.")]
        [SerializeField] private Renderer _lockGlow;

        [Tooltip("Art's Focus Lens on Neck/Head/Eye/LensSocket, shown from its cutter tier on (M3-26).")]
        [SerializeField] private GameObject _focusLens;

        [Tooltip("The Focus Lens's Glass child, glowing with the lock ring.")]
        [SerializeField] private Renderer _focusLensGlass;

        [Tooltip("RoverModel's CargoHatch: the rear leaf on its top hinge, opening about local +X (M3-25).")]
        [SerializeField] private Transform _cargoHatch;

        [Tooltip("The hatch window's HatchCrate_0.. nodes, one per hold slot (top row L to R, then bottom).")]
        [SerializeField] private GameObject[] _hatchCrates = new GameObject[0];

        [Tooltip("RoverModel 'TetherOrigin' (lens centre of 07's eye, +Z = gaze).")]
        [SerializeField] private Transform _tetherOrigin;

        [Tooltip("RoverModel 'CutterOrigin' (chest cutter turret nozzle, +Z = beam).")]
        [SerializeField] private Transform _cutterOrigin;

        [Tooltip("RoverModel 'EyeOrigin' (cheek scan/camera lens centre, +Z = gaze).")]
        [SerializeField] private Transform _scanOrigin;

        [Tooltip("RoverModel 'WinchOrigin' (chest winch tow-hook origin, +Z = forward).")]
        [SerializeField] private Transform _winchOrigin;

        [Tooltip("RoverModel 'ScoopOrigin' (front belly dig scoop tip, +Z = forward).")]
        [SerializeField] private Transform _scoopOrigin;

        [Tooltip("RoverModel 'CargoSocket'.")]
        [SerializeField] private Transform _cargoSocket;

        private InputReader _input;
        private IRoverDriveSource _driveSource;
        private readonly HoldRequests _holds = new HoldRequests();
        private readonly object _faceScreenHoldOwner = new object();
        private RoverCameraRig _cameraRig;
        private bool _faceScreenOpen;
        private bool _facePickingEnabled;
        private EventBus _events;
        private LandingDetector _landing;
        private StuckDetector _stuck;
        private RoverStillness _stillness;
        private BoostDrive _boost;
        private BatteryLink _battery;
        private ObstacleProbe _obstacleProbe;
        private ContactResponse _contacts;
        private IDisposable _dockChanges;
        private RoverFeel _feel;
        private ChargeCellHardware _chargeCells;
        private CargoHatch _hatch;
        private GameContext _context;
        private bool _docked;
        private float _dockElapsed;
        private Vector3 _dockFrom;
        private Vector3 _dockTo;
        private float _dockFromHeading;
        private float _dockToHeading;
        private int _abilities;
        private ITerrainQuery _terrain;
        private bool _initialized;
        private bool _recovering;
        private float _recoveryElapsed;
        private Vector3 _recoveryFrom;
        private Vector3 _recoveryTo;

        private float _throttle;
        private float _steer;
        private float _steerDirection = 1f;
        private float _heading;
        private float _previousHeading;
        private float _visualHeading;
        private float _yawRate;
        private float _scanTurnRate;
        private float _scanTurnScrub;
        private float _forwardSpeed;
        private bool _hasContact;
        private Vector3 _contactNormal = Vector3.up;
        private Vector3 _groundNormal = Vector3.up;
        private Vector3 _lastVelocity;
        private Vector3 _localAcceleration;
        private Vector3 _spawnNormal = Vector3.up;

        public RoverTuning Tuning => _tuning;

        /// <summary>Gaze requests made via <see cref="IRoverRig"/>; <see cref="RoverBodyLanguage"/> follows.</summary>
        public GazeRequests Gaze { get; } = new GazeRequests();

        /// <summary>Normalized [-1..1] wheel scrub while 07 turns its body in place toward a scan target.</summary>
        public float ScanTurnScrub => _scanTurnScrub;

        /// <summary>Interpolated centre of the physics sphere (render-frame accurate).</summary>
        public Vector3 SpherePosition => _body.transform.position;

        public float SphereRadius => _tuning.Ground.SphereRadius;

        /// <summary>Interpolated heading as a yaw-only rotation (world up).</summary>
        public Quaternion HeadingRotation => Quaternion.Euler(0f, _visualHeading, 0f);

        /// <summary>Interpolated heading in degrees.</summary>
        public float Heading => _visualHeading;

        /// <summary>Signed speed along the heading (m/s, negative when reversing).</summary>
        public float ForwardSpeed => _forwardSpeed;

        /// <summary>Current yaw rate (deg/s, + = turning right).</summary>
        public float YawRate => _yawRate;

        /// <summary>Eased steering input, -1..1 (also used to angle the front wheels).</summary>
        public float SteerInput => _steer;

        /// <summary>Velocity change over the last physics step divided by its duration, in the heading frame.</summary>
        public Vector3 LocalAcceleration => _localAcceleration;

        /// <summary>True when the probe touched driveable ground this physics step (no coyote time).</summary>
        public bool HasGroundContact => _hasContact;

        // IRoverState
        public Vector3 Position => _visualRig.RootPosition;

        public Quaternion Rotation => _visualRig.RootRotation;

        public Vector3 Velocity => _body.linearVelocity;

        public float Speed
        {
            get
            {
                Vector3 v = _body.linearVelocity;
                return Mathf.Sqrt(v.x * v.x + v.z * v.z);
            }
        }

        public float NormalizedSpeed
        {
            get
            {
                float top = _forwardSpeed >= 0f ? _tuning.Drive.TopSpeed : _tuning.Drive.ReverseTopSpeed;
                return Mathf.Clamp01(Speed / top);
            }
        }

        public Vector2 DriveInput => new Vector2(_steer, _throttle);

        /// <summary>The drive asked for (the player's, or a test's drive source), before holds and easing.</summary>
        public Vector2 RequestedDrive => _driveSource != null ? _driveSource.Drive : _input.Drive;

        public bool IsGrounded => _landing.IsGrounded;

        public float AirTime => _landing.AirTime;

        public Vector3 GroundNormal => _groundNormal;

        // IRoverRig
        public Transform TetherOrigin => _tetherOrigin;

        public Transform CutterOrigin => _cutterOrigin;

        public Transform ScanOrigin => _scanOrigin;

        public Transform WinchOrigin => _winchOrigin;

        public Transform ScoopOrigin => _scoopOrigin;

        public Transform CargoSocket => _cargoSocket;

        public Rigidbody PhysicsBody => _body;

        public void SetGazeTarget(object owner, Vector3 worldPosition, int priority)
        {
            Gaze.Set(owner, worldPosition, priority);
        }

        public void ClearGazeTarget(object owner)
        {
            Gaze.Clear(owner);
        }

        public void SetHoldStill(object owner, bool hold)
        {
            _holds.Set(owner, hold);
        }

        /// <summary>True while any owner asks 07 to stay parked.</summary>
        public bool IsHeldStill => _holds.IsHeld;

        // IRoverFaceScreen
        public float Curvature => _bodyLanguage != null ? _bodyLanguage.Curvature : 0f;

        public bool IsOpen => _faceScreenOpen;

        public void SetScreenOpen(bool open, Texture contentTexture)
        {
            if (this == null)
            {
                return;
            }

            _faceScreenOpen = open;
            SetHoldStill(_faceScreenHoldOwner, open);
            if (_bodyLanguage != null)
            {
                _bodyLanguage.SetScreenInspect(open, 0f, 10f, contentTexture);
            }

            if (_cameraRig == null && _context != null && _context.TryGet(out IViewCamera viewCamera))
            {
                _cameraRig = viewCamera as RoverCameraRig;
            }

            if (_cameraRig != null)
            {
                _cameraRig.SetFaceInspect(open, _bodyLanguage);
            }
        }

        public bool TryRaycastScreenUv(Ray ray, out Vector2 meshUv)
        {
            if (_facePickingEnabled && _screenCollider != null
                && _screenCollider.Raycast(ray, out RaycastHit hit, ScreenRaycastMaxDistance))
            {
                meshUv = hit.textureCoord;
                return true;
            }

            meshUv = default;
            return false;
        }

        /// <summary>The battery's flank lamps (where 07 glances when a leap is refused).</summary>
        public RoverFlankLamps FlankLamps => _flankLamps;

        /// <summary>How much of the Boost Coils' extra cruise is in, 0..1 (eased; the drums glow with it).</summary>
        public float BoostLevel => _boost.Level;

        /// <summary>True while the Boost Coils are engaged.</summary>
        public bool IsBoosting => _boost.Engaged;

        /// <summary>True while 07 rests on the charging dock (eased onto it, held there until it drives off).</summary>
        public bool IsDocked => _docked;

        /// <summary>Held by the charging pad: kinematic, at rest.</summary>
        private void Freeze()
        {
            _body.linearVelocity = Vector3.zero;
            _body.isKinematic = true;
            _lastVelocity = Vector3.zero;
            _localAcceleration = Vector3.zero;
            _yawRate = 0f;
            _forwardSpeed = 0f;
            _contacts.Reset();
        }

        /// <summary>Let go by a machine: on its wheels again, at rest.</summary>
        private void Free()
        {
            _body.isKinematic = false;
            _body.linearVelocity = Vector3.zero;
            _lastVelocity = Vector3.zero;
            _forwardSpeed = 0f;
            _contacts.Reset();
            _landing.Reset();
            _stuck.Reset();
        }

        // IRoverAbilities
        public bool Has(RoverAbility ability)
        {
            return (_abilities & AbilityBit(ability)) != 0;
        }

        public void Grant(RoverAbility ability)
        {
            _abilities |= AbilityBit(ability);
        }

        public void Revoke(RoverAbility ability)
        {
            _abilities &= ~AbilityBit(ability);
        }

        // IRoverCargoSeat: where a relic rides is the hatch's socket on 07's back.
        bool IRoverCargoSeat.IsFitted => Has(RoverAbility.CargoCradle);

        Vector3 IRoverCargoSeat.Position => _cargoSocket.position;

        Quaternion IRoverCargoSeat.Rotation => _cargoSocket.rotation;

        private static int AbilityBit(RoverAbility ability)
        {
            return 1 << (int)ability;
        }

        public void Initialize(GameContext context)
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return;
            }

            _context = context;
            _input = context.Input;
            _events = context.Events;
            _facePickingEnabled = ValidateScreenCollider();
            if (_facePickingEnabled)
            {
                Physics.IgnoreCollision(_sphere, _screenCollider, true);
            }

            _landing = new LandingDetector(_tuning.Landing);
            _stuck = new StuckDetector(_tuning.Recovery);
            _stillness = new RoverStillness(_tuning.Stillness);
            _boost = new BoostDrive(_tuning.Boost);
            _battery = new BatteryLink(context);
            _obstacleProbe = new ObstacleProbe(_tuning.Ground, new PhysicsObstacleQuery(_sphere, _screenCollider));
            _contacts = new ContactResponse(_tuning.Ground, _tuning.Drive);
            _terrain = context.Get<ITerrainQuery>();
            PlaceOnTerrain(_terrain);
            ConfigureBody();
            _heading = transform.eulerAngles.y;
            _previousHeading = _heading;
            _visualHeading = _heading;
            context.Register<IRoverState>(this);
            context.Register<IRoverRig>(this);
            context.Register<IRoverAbilities>(this);
            context.Register<IRoverStillness>(_stillness);
            context.Register<IRoverPlacement>(this);
            context.Register<IRoverCargoSeat>(this);
            context.Register<IRoverFaceScreen>(this);
            var feelPreferences = new FeelPreferences();
            context.Register<IFeelSettings>(feelPreferences);
            _dockChanges = _events.Subscribe<RoverDockChanged>(OnDockChanged);

            bool visualsReady = _visualRig.Initialize(this);
            if (visualsReady)
            {
                RoverRigTuning feel = _visualRig.Tuning;
                _feel = new RoverFeel(_events, feel.CutFeel, feel.DockFeel, feel.MotionFeel, feelPreferences,
                    new GamepadRumbleMotors(), _visualRig);
                _visualRig.SetNudge(_feel.Nudge);
                _visualRig.SetChargeBrace(_feel.Brace);
                _hatch = new CargoHatch(_events, feel.CargoHatch, _cargoHatch, _hatchCrates, _feel.Rumble);
                _chargeCells = new ChargeCellHardware(_events, feel.ChargeCells, _conduitGlows, _stageSleeves, _lockGlow,
                    Cutter, CutterOrigin, _focusLens, _focusLensGlass);
            }

            bool headlampReady = _headlamp.Initialize(context, this);
            bool effectsReady = _wheelFx.Initialize(context, this);
            bool motesReady = _lampMotes.Initialize(this);
            bool lampsReady = _flankLamps.Initialize(context);
            bool portraitReady = _portrait.Initialize();
            if (portraitReady)
            {
                context.Register<IRoverPortrait>(_portrait);
            }

            _initialized = visualsReady && headlampReady && effectsReady && motesReady && lampsReady
                && portraitReady;
            enabled = _initialized;
        }

        /// <summary>
        /// Hands the wheel to <paramref name="source"/> (a cinematic autopilot, a scripted feel session) instead of
        /// the player's input; null gives it back to the player.
        /// </summary>
        public void SetDriveSource(IRoverDriveSource source)
        {
            _driveSource = source;
        }

        private bool ValidateScreenCollider()
        {
            if (_screenCollider == null)
            {
                Debug.LogError(
                    $"{nameof(RoverController)}: Screen MeshCollider is not assigned on '{name}'; face picking is disabled.",
                    this);
                return false;
            }

            Mesh mesh = _screenCollider.sharedMesh;
            if (mesh == null || !mesh.isReadable)
            {
                string meshState = mesh == null ? "null" : $"'{mesh.name}' (isReadable=false)";
                Debug.LogError(
                    $"{nameof(RoverController)}: Screen MeshCollider sharedMesh on '{name}' is {meshState}; face picking is disabled.",
                    this);
                return false;
            }

            return true;
        }

        private bool ValidateWiring()
        {
            bool ok = true;
            ok &= Require(_tuning != null, "RoverTuning is not assigned.");
            ok &= Require(_body != null, "Physics sphere Rigidbody is not assigned.");
            ok &= Require(_sphere != null, "Physics sphere SphereCollider is not assigned.");
            ok &= Require(_visualRig != null, "RoverVisualRig is not assigned.");
            ok &= Require(_wheelFx != null, "RoverWheelFx is not assigned.");
            ok &= Require(_headlamp != null, "RoverHeadlamp is not assigned.");
            ok &= Require(_lampMotes != null, "RoverLampMotes is not assigned.");
            ok &= Require(_flankLamps != null, "RoverFlankLamps is not assigned.");
            ok &= Require(_portrait != null, "RoverPortrait is not assigned.");
            ok &= Require(_cargoHatch != null && _hatchCrates.Length > 0 && Array.TrueForAll(_hatchCrates, c => c != null),
                "The CargoHatch or a HatchCrate is not assigned (M3-25 hatch contract).");
            ok &= Require(_focusLens != null && _focusLensGlass != null,
                "The Focus Lens or its Glass is not assigned (art's cutter kit).");
            ok &= Require(_conduitGlows.Length > 0 && Array.TrueForAll(_conduitGlows, r => r != null)
                && _stageSleeves.Length == 2 * RoverModelNodes.ChargeStages
                && Array.TrueForAll(_stageSleeves, r => r != null) && _lockGlow != null,
                "The charge-up's ConduitGlow, StageGlow sleeves or LockGlow are not assigned (0.5.5 Cells contract).");

            ok &= Require(_tetherOrigin != null && _cutterOrigin != null && _scanOrigin != null
                && _winchOrigin != null && _scoopOrigin != null && _cargoSocket != null,
                "TetherOrigin/CutterOrigin/ScanOrigin/WinchOrigin/ScoopOrigin/CargoSocket are not assigned.");
            if (_body != null)
            {
                ok &= Require(_body.gameObject.layer == Layers.Rover, "Physics sphere must be on the Rover layer.");
            }

            if (_sphere != null)
            {
                ok &= Require(_sphere.sharedMaterial != null, "Physics sphere needs its frictionless material.");
            }

            return ok;
        }

        private bool Require(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"{nameof(RoverController)}: {message}", this);
            }

            return condition;
        }

        /// <summary>
        /// The scene places 07 on the base pad in x/z; its height comes from the world's terrain query so it spawns
        /// resting on the ground (no frame-one drop and landing) whatever height the pad ends up at.
        /// </summary>
        private void PlaceOnTerrain(ITerrainQuery terrain)
        {
            Vector3 spawn = transform.position;
            spawn.y = terrain.SampleHeight(spawn.x, spawn.z);
            transform.position = spawn;
            _spawnNormal = terrain.SampleNormal(spawn.x, spawn.z);
        }

        private void ConfigureBody()
        {
            GroundSettings ground = _tuning.Ground;
            _sphere.radius = ground.SphereRadius;
            _sphere.center = Vector3.zero;
            _body.mass = ground.Mass;
            _body.linearDamping = 0f;
            _body.angularDamping = 0f;
            _body.useGravity = true;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _body.constraints = RigidbodyConstraints.FreezeRotation;
            Vector3 spawn = transform.position + _spawnNormal * ground.SphereRadius;
            _body.transform.SetPositionAndRotation(spawn, Quaternion.identity);
            _body.position = spawn;
            _body.rotation = Quaternion.identity;
            _body.linearVelocity = Vector3.zero;
            _lastVelocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (!_initialized)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            if (_docked)
            {
                ReadInput(dt);
                StepDock(dt);
                return;
            }

            if (_recovering)
            {
                ReadInput(dt);
                StepRecovery(dt);
                return;
            }

            Vector3 velocity = _body.linearVelocity;
            Quaternion yaw = Quaternion.Euler(0f, _heading, 0f);
            _localAcceleration = Quaternion.Inverse(yaw) * ((velocity - _lastVelocity) / dt);

            ReadInput(dt);
            bool groundedBeforeProbe = _hasContact || _landing.IsGrounded;
            _obstacleProbe.ProbeObstacle(_body.position, velocity, _heading, _throttle, dt, groundedBeforeProbe,
                out Vector3 obstacleNormal, out Vector3 obstaclePoint, out float obstacleGap);
            velocity = _contacts.StepContact(velocity, obstacleNormal, _obstacleProbe.SecondaryNormal, obstacleGap,
                groundedBeforeProbe, dt, out Vector3 pushOut, out float impactSpeed, out bool publishCollision);
            if (_contacts.HasObstacle)
            {
                if (publishCollision)
                {
                    _events.Publish(new RoverCollided(obstaclePoint, obstacleNormal, impactSpeed));
                }

                if (pushOut.sqrMagnitude > 0f)
                {
                    _body.position += pushOut;
                    _body.transform.position = _body.position;
                }

                _body.linearVelocity = velocity;
            }

            ProbeGround(out float speedIntoGround, velocity);
            if (_landing.Step(_hasContact, speedIntoGround, dt))
            {
                Vector3 contact = _body.position - _contactNormal * _tuning.Ground.SphereRadius;
                _events.Publish(new RoverLanded(contact, _landing.LastImpactSpeed, _landing.LastAirTime));
            }

            StepBoost(dt);

            Vector3 normal = _hasContact ? _contactNormal : Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(yaw * Vector3.forward, normal).normalized;
            _forwardSpeed = Vector3.Dot(velocity, forward);

            Steer(dt);
            Drive(velocity, normal, forward, dt);
            if (_contacts.IsRebounding(velocity))
            {
                _stuck.Reset();
            }
            else if (_stuck.Step(_throttle, _body.position, dt))
            {
                TryStartRecovery();
            }

            float smoothing = Smoothing.Factor(_tuning.Ground.GroundNormalHalfLife, dt);
            Vector3 targetNormal = _landing.IsGrounded ? normal : Vector3.up;
            _groundNormal = Vector3.Slerp(_groundNormal, targetNormal, smoothing);
            _lastVelocity = velocity;
        }

        /// <summary>True while 07 is being lifted out of a stuck spot.</summary>
        public bool IsRecovering => _recovering;

        /// <summary>
        /// Sets 07 down at rest on the surface under <paramref name="position"/> (the terrain's height and slope),
        /// facing <paramref name="rotation"/>'s yaw, in one step: the body, heading, visual rig, tire tracks and lamp
        /// motes jump; velocity, eased input, a lift, a jump charge and the stuck/landing detectors start over;
        /// stillness goes back to 0; then <see cref="RoverPlaced"/> lets the camera snap too. Only for moments the
        /// player cannot see.
        /// </summary>
        public void PlaceAt(Vector3 position, Quaternion rotation)
        {
            if (!_initialized)
            {
                Debug.LogError($"{nameof(RoverController)}: PlaceAt before initialisation.", this);
                return;
            }

            Transform lamp = _lampMotes.Lamp;
            Vector3 lens = lamp.position;
            Quaternion facing = lamp.rotation;
            Vector3 ground = RoverPlacementMath.Ground(_terrain, position, out Vector3 normal);
            Vector3 centre = RoverPlacementMath.RestingCentre(ground, normal, _tuning.Ground.SphereRadius);
            float yaw = RoverPlacementMath.Yaw(rotation);

            _recovering = false;
            _recoveryElapsed = 0f;
            _docked = false;
            _headlamp.SetDocked(false);
            _body.isKinematic = false;
            _body.transform.SetPositionAndRotation(centre, Quaternion.identity);
            _body.position = centre;
            _body.rotation = Quaternion.identity;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _lastVelocity = Vector3.zero;
            _localAcceleration = Vector3.zero;

            _heading = yaw;
            _previousHeading = yaw;
            _visualHeading = yaw;
            _yawRate = 0f;
            _scanTurnRate = 0f;
            _scanTurnScrub = 0f;
            _forwardSpeed = 0f;
            _throttle = 0f;
            _steer = 0f;
            _steerDirection = 1f;
            _contactNormal = normal;
            _groundNormal = normal;
            _contacts.Reset();
            _landing.Reset();
            _stuck.Reset();
            _stillness.Reset();

            _feel?.Nudge.Reset();
            _feel?.Brace.Reset();
            _hatch?.Reset();
            _chargeCells?.Reset();
            _visualRig.Snap(normal);
            _wheelFx.Break();
            _lampMotes.Carry(lens, facing);
            _events.Publish(new RoverPlaced(ground, Quaternion.Euler(0f, yaw, 0f)));
        }

        /// <summary>
        /// Looks for the nearest comfortable spot (drivable, gentle, clear of rocks) and starts the lift there. If none
        /// is in reach nothing happens; the detector re-arms and tries again later.
        /// </summary>
        private void TryStartRecovery()
        {
            RecoverySettings recovery = _tuning.Recovery;
            float radius = _tuning.Ground.SphereRadius;
            Vector3 from = _body.position;
            for (int i = 0; i < recovery.SearchCandidates; i++)
            {
                Vector2 offset = RecoveryPlanner.CandidateOffset(recovery, _heading, i);
                float x = from.x + offset.x;
                float z = from.z + offset.y;
                if (!_terrain.IsDrivable(x, z))
                {
                    continue;
                }

                Vector3 normal = _terrain.SampleNormal(x, z);
                if (Vector3.Angle(normal, Vector3.up) > recovery.MaxSetDownSlope)
                {
                    continue;
                }

                Vector3 rest = new Vector3(x, _terrain.SampleHeight(x, z), z) + normal * radius;
                if (Physics.CheckSphere(rest + Vector3.up * recovery.Clearance, recovery.Clearance, Layers.PropMask,
                        QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                StartRecovery(from, rest);
                return;
            }
        }

        /// <summary>
        /// Docked: 07 eases from where it stopped onto the dock's anchor (07's pivot, facing the lander), held there by
        /// the dock rather than its wheels. Undocked: free at once, at rest.
        /// </summary>
        private void OnDockChanged(RoverDockChanged dock)
        {
            if (!_initialized)
            {
                return;
            }

            _headlamp.SetDocked(dock.Docked);
            if (!dock.Docked)
            {
                if (_docked)
                {
                    _docked = false;
                    Free();
                }

                return;
            }

            _docked = true;
            _recovering = false;
            _dockElapsed = 0f;
            _dockFrom = _body.position;
            _dockTo = dock.Position + dock.Rotation * Vector3.up * _tuning.Ground.SphereRadius;
            _dockFromHeading = _heading;
            _dockToHeading = RoverPlacementMath.Yaw(dock.Rotation);
            Freeze();
        }

        private void StepDock(float dt)
        {
            _dockElapsed += dt;
            float settle = Smoothing.SmoothStep(0f, 1f, _dockElapsed / _tuning.Dock.SettleSeconds);
            _body.MovePosition(Vector3.Lerp(_dockFrom, _dockTo, settle));
            _previousHeading = _heading;
            _heading = Mathf.Repeat(Mathf.LerpAngle(_dockFromHeading, _dockToHeading, settle), 360f);
        }

        private void StartRecovery(Vector3 from, Vector3 to)
        {
            _recovering = true;
            _recoveryElapsed = 0f;
            _recoveryFrom = from;
            _recoveryTo = to;
            _body.linearVelocity = Vector3.zero;
            _body.isKinematic = true;
            _localAcceleration = Vector3.zero;
            _yawRate = 0f;
            _events.Publish(new RoverRecovering(from, to, _tuning.Recovery.LiftDuration));
        }

        private void StepRecovery(float dt)
        {
            RecoverySettings recovery = _tuning.Recovery;
            _recoveryElapsed += dt;
            float t = _recoveryElapsed / recovery.LiftDuration;
            _body.MovePosition(RecoveryPlanner.LiftPosition(_recoveryFrom, _recoveryTo, recovery.LiftHeight, t));
            _previousHeading = _heading;
            if (t < 1f)
            {
                return;
            }

            _recovering = false;
            _body.isKinematic = false;
            _body.linearVelocity = Vector3.zero;
            _lastVelocity = Vector3.zero;
            _forwardSpeed = 0f;
            _landing.Reset();
            _stuck.Reset();
        }

        private void ReadInput(float dt)
        {
            Vector2 held = _driveSource != null ? _driveSource.Drive : _input.Drive;
            Vector2 raw = DriveInputShaping.CircleToSquare(held);
            if (_holds.IsHeld)
            {
                // Ignored, not queued: the eased throttle settles to zero, so releasing eases in from rest.
                raw.y = 0f;
            }

            DriveSettings drive = _tuning.Drive;
            _throttle = Ease(_throttle, raw.y, drive.ThrottleRiseHalfLife, drive.ThrottleFallHalfLife, dt);
            _steer = Ease(_steer, raw.x, _tuning.Steering.SteerRiseHalfLife, _tuning.Steering.SteerReturnHalfLife, dt);
        }

        /// <summary>
        /// The Boost Coils (M3-11): engaged while 07 owns them and cruises on open, flat-ish ground with Drive held,
        /// free of holds, leaps and lifts; the extra top speed eases in and out and <see cref="RoverBoostChanged"/>
        /// marks each change. They sleep while 07 crawls on the reserve (docs/features/M3-20).
        /// </summary>
        private void StepBoost(float dt)
        {
            bool free = _hasContact && _landing.IsGrounded && !_holds.IsHeld;
            float slope = Vector3.Angle(_contactNormal, Vector3.up);
            var sample = new BoostSample(Has(RoverAbility.BoostCoils) && !_battery.IsLowPower, _throttle, _steer,
                _forwardSpeed, _tuning.Drive.TopSpeed, slope, free);
            if (_boost.Step(sample, dt))
            {
                _events.Publish(new RoverBoostChanged(_boost.Engaged));
            }
        }

        /// <summary>Eases toward a held input with one half-life and back toward zero with another.</summary>
        private static float Ease(float current, float target, float riseHalfLife, float fallHalfLife, float dt)
        {
            bool rising = Mathf.Abs(target) > Mathf.Abs(current) || target * current < 0f;
            return Smoothing.Damp(current, target, rising ? riseHalfLife : fallHalfLife, dt);
        }

        private void ProbeGround(out float speedIntoGround, Vector3 velocity)
        {
            GroundSettings ground = _tuning.Ground;
            float radius = ground.SphereRadius;
            float probeRadius = radius * ground.ProbeRadiusFactor;
            float lift = radius * ProbeLiftFraction;
            Vector3 origin = _body.position + Vector3.up * lift;
            float distance = lift + (radius - probeRadius) + ground.GroundSnapDistance;

            _hasContact = Physics.SphereCast(origin, probeRadius, Vector3.down, out RaycastHit hit, distance, Layers.DriveableMask,
                    QueryTriggerInteraction.Ignore)
                && Vector3.Angle(hit.normal, Vector3.up) <= ground.MaxGroundAngle;
            _contactNormal = _hasContact ? hit.normal : Vector3.up;
            speedIntoGround = -Vector3.Dot(velocity, _contactNormal);
        }

        private void Steer(float dt)
        {
            SteeringSettings steering = _tuning.Steering;
            float direction = SteeringModel.SteerDirection(steering, _forwardSpeed, _throttle,
                _tuning.Drive.InputDeadZone);
            _steerDirection = Smoothing.Damp(_steerDirection, direction, steering.SteerDirectionHalfLife, dt);
            float airSteer = steering.AirTurnFactor;
            float playerYawRate = SteeringModel.YawRate(steering, _steer, _steerDirection, _forwardSpeed,
                _tuning.Drive.TopSpeed, true) * (_hasContact ? 1f : airSteer);

            if (Mathf.Abs(_steer) <= _tuning.Drive.InputDeadZone && _bodyLanguage != null
                && _bodyLanguage.TryGetScanBodyTurn(out float bodyYawDelta, out float maxRate, out float halfLife))
            {
                float desiredRate = Mathf.Clamp(bodyYawDelta * 3.2f, -maxRate, maxRate);
                _scanTurnRate = Smoothing.Damp(_scanTurnRate, desiredRate, halfLife, dt);
                _scanTurnScrub = maxRate > 1e-4f ? Mathf.Clamp(_scanTurnRate / maxRate, -1f, 1f) : 0f;
            }
            else
            {
                _scanTurnRate = Smoothing.Damp(_scanTurnRate, 0f, 0.08f, dt);
                _scanTurnScrub = Smoothing.Damp(_scanTurnScrub, 0f, 0.08f, dt);
            }

            _yawRate = playerYawRate + _scanTurnRate * (_hasContact ? 1f : airSteer);
            float crossSlopeYaw = 0f;
            if (_hasContact && !_contacts.HasObstacle)
            {
                Vector3 headingForward = Quaternion.Euler(0f, _heading, 0f) * Vector3.forward;
                crossSlopeYaw = GroundModel.CrossSlopeYawRate(_tuning.Ground, Physics.gravity, _contactNormal,
                    headingForward, _forwardSpeed);
            }

            _previousHeading = _heading;
            _heading = Mathf.Repeat(_heading + (_yawRate + crossSlopeYaw) * dt, 360f);
            _heading = _contacts.AdjustHeading(_heading, _throttle, _steer, _hasContact, dt);
        }

        private void Drive(Vector3 velocity, Vector3 normal, Vector3 forward, float dt)
        {
            GroundSettings ground = _tuning.Ground;
            float effectiveThrottle = _contacts.FilterThrottle(_throttle, forward);
            float drive = _holds.IsHeld
                ? LongitudinalDrive.HoldAcceleration(_tuning.Drive, _forwardSpeed, dt)
                : LongitudinalDrive.Acceleration(_tuning.Drive, _forwardSpeed, effectiveThrottle, _boost.ExtraSpeed,
                    _battery.DriveCap01, dt);

            if (_hasContact)
            {
                Vector3 driveForward = _contacts.RedirectDrive(forward, normal, effectiveThrottle, ref drive);
                Vector3 acceleration = driveForward * drive
                    + GroundModel.SlopeAcceleration(ground, Physics.gravity, normal, driveForward, _forwardSpeed)
                    - normal * ground.Downforce;
                acceleration = _contacts.ClampAcceleration(acceleration);
                _body.AddForce(acceleration, ForceMode.Acceleration);
                _body.AddForce(GroundModel.GripVelocityChange(velocity, normal, driveForward, ground.GripRate, dt),
                    ForceMode.VelocityChange);
            }
            else
            {
                float extraGravity = GroundModel.ExtraAirGravity(ground, velocity.y, Physics.gravity.y);
                Vector3 acceleration = forward * (drive * ground.AirThrottleFactor) + Vector3.down * extraGravity;
                _body.AddForce(acceleration, ForceMode.Acceleration);
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            // Same blend factor Unity uses for rigidbody interpolation, so yaw and position stay in lockstep.
            float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
            _visualHeading = Mathf.LerpAngle(_previousHeading, _heading, alpha);

            float deltaTime = Time.deltaTime;
            _stillness.Step(SampleStillness(), deltaTime);
            _feel.Tick(deltaTime, Time.time, Speed, _visualRig.RootPosition);
            _chargeCells?.Tick(deltaTime);
            _hatch.Tick(deltaTime);
            _visualRig.Tick(deltaTime);
            _headlamp.Tick(deltaTime);
            _wheelFx.Tick();
            _lampMotes.Tick(deltaTime);
            _flankLamps.Tick(deltaTime);
        }

        /// <summary>This frame's motion and the player's hands, as <see cref="RoverStillness"/> reads them.</summary>
        private StillnessSample SampleStillness()
        {
            Vector2 drive = _driveSource != null ? _driveSource.Drive : _input.Drive;
            bool engaged = _recovering || _holds.IsHeld;
            return new StillnessSample(_landing.IsGrounded, Speed, drive, _input.LookDelta, _input.LookRate, engaged);
        }

        /// <summary>The cutter now (ICutter): its brace, warm window and the bay's test cut's charge.</summary>
        private ICutter Cutter()
        {
            return _context.Get<ICutter>();
        }

        /// <summary>Rumble never runs while the game window is away.</summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            _feel?.SetFocused(hasFocus);
        }

        private void OnApplicationQuit()
        {
            _feel?.Silence();
        }

        private void OnDestroy()
        {
            _dockChanges?.Dispose();
            _feel?.Dispose();
            _chargeCells?.Dispose();
            _hatch?.Dispose();
        }
    }
}
