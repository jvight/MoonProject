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
    /// in one step, publishing <see cref="RoverPlaced"/>) and <see cref="IRoverCargoSeat"/> (its kit's rack, see
    /// <see cref="RoverKit"/>), publishes <see cref="RoverLanded"/> and <see cref="RoverBoostChanged"/>, and ticks its
    /// visual rig, kit, wheel effects and lamp motes in a fixed order every frame. The Boost Coils raise the top speed
    /// gently while held at cruise on open, flat-ish ground (<see cref="BoostDrive"/>). Docked at home
    /// (<see cref="RoverDockChanged"/>), 07 eases onto the dock's anchor and rests there, its lamp dimmed, while its
    /// drive input is still read so a touch shows at once and gameplay can undock it; undocked, it is free at once.
    /// Needs the World's <see cref="ITerrainQuery"/> (spawn height, stuck recovery), so it initialises after the World
    /// systems. If 07 is trying to drive but stuck for a few seconds, it is lifted gently to a nearby open spot.
    /// The maths lives in plain classes (<see cref="LongitudinalDrive"/>, <see cref="SteeringModel"/>,
    /// <see cref="GroundModel"/>, <see cref="LandingDetector"/>); this component only wires them to physics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverController : MonoBehaviour, IGameSystem, IRoverState, IRoverRig, IRoverAbilities,
        IRoverPlacement
    {
        /// <summary>The probe starts this fraction of the radius above the centre, so slight sinking hits.</summary>
        private const float ProbeLiftFraction = 0.5f;

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

        [Tooltip("07's visible kit and friends' gifts, its road light and the cargo seat.")]
        [SerializeField] private RoverKit _kit;

        [Tooltip("The Hover-Jump coils under the belly (shown only while 07 owns the ability).")]
        [SerializeField] private RoverHoverCoils _hoverCoils;

        [Tooltip("RoverModel 'TetherOrigin' (lens centre of 07's eye, +Z = gaze).")]
        [SerializeField] private Transform _tetherOrigin;

        [Tooltip("RoverModel 'CargoSocket'.")]
        [SerializeField] private Transform _cargoSocket;

        private InputReader _input;
        private IRoverDriveSource _driveSource;
        private readonly HoldRequests _holds = new HoldRequests();
        private EventBus _events;
        private LandingDetector _landing;
        private StuckDetector _stuck;
        private HoverJump _jump;
        private RoverStillness _stillness;
        private BoostDrive _boost;
        private IDisposable _dockChanges;
        private bool _docked;
        private bool _cradled;
        private Vector3 _cradleBody;
        private float _cradleHeading;
        private float _dockElapsed;
        private Vector3 _dockFrom;
        private Vector3 _dockTo;
        private float _dockFromHeading;
        private float _dockToHeading;
        private int _abilities;
        private bool _jumpHeld;
        private bool _leaping;
        private float _liftoff;
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

        public bool IsGrounded => _landing.IsGrounded;

        public float AirTime => _landing.AirTime;

        public Vector3 GroundNormal => _groundNormal;

        // IRoverRig
        public Transform TetherOrigin => _tetherOrigin;

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

        /// <summary>07's visible kit and friends' gifts.</summary>
        public RoverKit Kit => _kit;

        /// <summary>The Hover-Jump coils under 07's belly.</summary>
        public RoverHoverCoils HoverCoils => _hoverCoils;

        /// <summary>How much of the Boost Coils' extra cruise is in, 0..1 (eased; the drums glow with it).</summary>
        public float BoostLevel => _boost.Level;

        /// <summary>True while the Boost Coils are engaged.</summary>
        public bool IsBoosting => _boost.Engaged;

        /// <summary>True while 07 rests on the charging dock (eased onto it, held there until it drives off).</summary>
        public bool IsDocked => _docked;

        /// <summary>True while a machine holds 07 in a pose it sets (the Rover Bay's turntable and guides).</summary>
        public bool IsCradled => _cradled;

        /// <summary>
        /// A machine holds 07 (the Rover Bay, M3-14): its physics sphere at <paramref name="bodyCentre"/>, facing
        /// <paramref name="heading"/> (deg), set every frame by the holder, who eases it. The drive input is still
        /// read; nothing drives 07 until <see cref="ReleasePose"/>.
        /// </summary>
        public void HoldPose(Vector3 bodyCentre, float heading)
        {
            if (!_cradled)
            {
                _cradled = true;
                Freeze();
            }

            _cradleBody = bodyCentre;
            _cradleHeading = heading;
        }

        /// <summary>The machine lets 07 go: free at once, at rest.</summary>
        public void ReleasePose()
        {
            if (!_cradled)
            {
                return;
            }

            _cradled = false;
            Free();
        }

        /// <summary>Held by a machine (the dock, the bay): kinematic, at rest, no jump or leap under way.</summary>
        private void Freeze()
        {
            _body.linearVelocity = Vector3.zero;
            _body.isKinematic = true;
            _lastVelocity = Vector3.zero;
            _localAcceleration = Vector3.zero;
            _yawRate = 0f;
            _forwardSpeed = 0f;
            _leaping = false;
            _liftoff = 0f;
            _jump.Reset();
        }

        /// <summary>Let go by a machine: on its wheels again, at rest.</summary>
        private void Free()
        {
            _body.isKinematic = false;
            _body.linearVelocity = Vector3.zero;
            _lastVelocity = Vector3.zero;
            _forwardSpeed = 0f;
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

        private static int AbilityBit(RoverAbility ability)
        {
            return 1 << (int)ability;
        }

        /// <summary>Hover-Jump charge, 0..1 (0 when not charging): drives the crouch and the effort squint.</summary>
        public float JumpCharge => _jump.Charge;

        /// <summary>True from a Hover-Jump take-off until touchdown.</summary>
        public bool IsLeaping => _leaping;

        public void Initialize(GameContext context)
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return;
            }

            _input = context.Input;
            _events = context.Events;
            _landing = new LandingDetector(_tuning.Landing);
            _stuck = new StuckDetector(_tuning.Recovery);
            _jump = new HoverJump(_tuning.HoverJump);
            _stillness = new RoverStillness(_tuning.Stillness);
            _boost = new BoostDrive(_tuning.Boost);
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
            context.Register<IRoverCargoSeat>(_kit);
            _dockChanges = _events.Subscribe<RoverDockChanged>(OnDockChanged);

            bool visualsReady = _visualRig.Initialize(this);
            bool kitReady = _kit.Initialize(context, this, _visualRig);
            bool effectsReady = _wheelFx.Initialize(context, this);
            bool coilsReady = _hoverCoils.Initialize(context, this);
            bool motesReady = _lampMotes.Initialize(this);
            _initialized = visualsReady && kitReady && effectsReady && coilsReady && motesReady;
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

        private bool ValidateWiring()
        {
            bool ok = true;
            ok &= Require(_tuning != null, "RoverTuning is not assigned.");
            ok &= Require(_body != null, "Physics sphere Rigidbody is not assigned.");
            ok &= Require(_sphere != null, "Physics sphere SphereCollider is not assigned.");
            ok &= Require(_visualRig != null, "RoverVisualRig is not assigned.");
            ok &= Require(_wheelFx != null, "RoverWheelFx is not assigned.");
            ok &= Require(_kit != null, "RoverKit is not assigned.");
            ok &= Require(_hoverCoils != null, "RoverHoverCoils is not assigned.");
            ok &= Require(_lampMotes != null, "RoverLampMotes is not assigned.");
            ok &= Require(_tetherOrigin != null && _cargoSocket != null, "TetherOrigin/CargoSocket are not assigned.");
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

            if (_cradled)
            {
                ReadInput(dt);
                _body.MovePosition(_cradleBody);
                _previousHeading = _heading;
                _heading = Mathf.Repeat(_cradleHeading, 360f);
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
            ProbeGround(out float speedIntoGround, velocity);
            if (_liftoff > 0f)
            {
                // Leaving the ground after a Hover-Jump: no downforce or grip pulling 07 back for a moment.
                _liftoff -= dt;
                _hasContact = false;
            }

            bool wasGrounded = _landing.IsGrounded;
            if (_landing.Step(_hasContact, speedIntoGround, dt))
            {
                Vector3 contact = _body.position - _contactNormal * _tuning.Ground.SphereRadius;
                _events.Publish(new RoverLanded(contact, _landing.LastImpactSpeed, _landing.LastAirTime));
            }

            if (!wasGrounded && _landing.IsGrounded)
            {
                _leaping = false;
                _jump.NotifyLanded();
            }

            StepHoverJump(velocity, dt);
            StepBoost(dt);

            Vector3 normal = _hasContact ? _contactNormal : Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(yaw * Vector3.forward, normal).normalized;
            _forwardSpeed = Vector3.Dot(velocity, forward);

            Steer(dt);
            Drive(velocity, normal, forward, dt);
            if (_stuck.Step(_throttle, _body.position, dt))
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
            _kit.SetDocked(false);
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
            _forwardSpeed = 0f;
            _throttle = 0f;
            _steer = 0f;
            _steerDirection = 1f;
            _contactNormal = normal;
            _groundNormal = normal;
            _leaping = false;
            _liftoff = 0f;
            _jump.Reset();
            _landing.Reset();
            _stuck.Reset();
            _stillness.Reset();

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

            _kit.SetDocked(dock.Docked);
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
            _leaping = false;
            _liftoff = 0f;
            _jump.Reset();
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
            _jumpHeld = _driveSource != null
                ? _driveSource is IRoverJumpSource jumpSource && jumpSource.JumpHeld
                : _input.JumpHeld;
        }

        /// <summary>
        /// Hover-Jump (M3-03): ignored unless 07 owns it, only on the ground and never while gameplay holds it still.
        /// A leap sets the take-off speed for the charged height, keeps the forward speed and leaves the ground
        /// cleanly.
        /// </summary>
        private void StepHoverJump(Vector3 velocity, float dt)
        {
            bool enabled = Has(RoverAbility.HoverJump) && !_holds.IsHeld;
            bool grounded = _landing.IsGrounded && !_leaping && _liftoff <= 0f;
            switch (_jump.Step(enabled, _jumpHeld, grounded, dt))
            {
                case HoverJumpEvent.ChargeProgress:
                    _events.Publish(new RoverJumpCharged(_jump.LastStrength));
                    break;
                case HoverJumpEvent.Leap:
                    float strength = _jump.LastStrength;
                    float takeOff = _jump.TakeOffSpeed(strength, _tuning.Ground.AirRiseGravity);
                    float rise = Mathf.Max(velocity.y, 0f) + takeOff;
                    _body.AddForce(Vector3.up * (rise - velocity.y), ForceMode.VelocityChange);
                    _leaping = true;
                    _liftoff = _tuning.HoverJump.LiftoffTime;
                    _hasContact = false;
                    _events.Publish(new RoverJumped(strength));
                    break;
                case HoverJumpEvent.Cancelled:
                    _events.Publish(new RoverJumpCancelled());
                    break;
            }
        }

        /// <summary>
        /// The Boost Coils (M3-11): engaged while 07 owns them and cruises on open, flat-ish ground with Drive held,
        /// free of holds, leaps and lifts; the extra top speed eases in and out and <see cref="RoverBoostChanged"/>
        /// marks each change.
        /// </summary>
        private void StepBoost(float dt)
        {
            bool free = _hasContact && _landing.IsGrounded && !_holds.IsHeld && !_leaping && _liftoff <= 0f;
            float slope = Vector3.Angle(_contactNormal, Vector3.up);
            var sample = new BoostSample(Has(RoverAbility.BoostCoils), _throttle, _steer, _forwardSpeed,
                _tuning.Drive.TopSpeed, slope, free);
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

            _hasContact = Physics.SphereCast(origin, probeRadius, Vector3.down, out RaycastHit hit, distance,
                    Layers.DriveableMask, QueryTriggerInteraction.Ignore)
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
            float airSteer = _leaping ? _tuning.HoverJump.AirSteer : steering.AirTurnFactor;
            _yawRate = SteeringModel.YawRate(steering, _steer, _steerDirection, _forwardSpeed,
                _tuning.Drive.TopSpeed, true) * (_hasContact ? 1f : airSteer);

            _previousHeading = _heading;
            _heading = Mathf.Repeat(_heading + _yawRate * dt, 360f);
        }

        private void Drive(Vector3 velocity, Vector3 normal, Vector3 forward, float dt)
        {
            GroundSettings ground = _tuning.Ground;
            float drive = _holds.IsHeld
                ? LongitudinalDrive.HoldAcceleration(_tuning.Drive, _forwardSpeed, dt)
                : LongitudinalDrive.Acceleration(_tuning.Drive, _forwardSpeed, _throttle, _boost.ExtraSpeed, dt);

            if (_hasContact)
            {
                Vector3 acceleration = forward * drive
                    + GroundModel.SlopeAcceleration(ground, Physics.gravity, normal, forward, _forwardSpeed)
                    - normal * ground.Downforce;
                _body.AddForce(acceleration, ForceMode.Acceleration);
                _body.AddForce(GroundModel.GripVelocityChange(velocity, normal, forward, ground.GripRate, dt),
                    ForceMode.VelocityChange);
            }
            else
            {
                float extraGravity = GroundModel.ExtraAirGravity(ground, velocity.y, Physics.gravity.y);
                Vector3 acceleration = forward * (drive * ground.AirThrottleFactor) + Vector3.down * extraGravity;
                _body.AddForce(acceleration, ForceMode.Acceleration);
                if (_leaping)
                {
                    SteerAndCushionLeap(velocity, forward, dt);
                }
            }
        }

        /// <summary>
        /// During a leap the gentle air-steer also bends the flight path (a share of the ground grip), and near the
        /// ground the cushion slows the descent so every landing is soft.
        /// </summary>
        private void SteerAndCushionLeap(Vector3 velocity, Vector3 forward, float dt)
        {
            GroundSettings ground = _tuning.Ground;
            HoverJumpSettings jump = _tuning.HoverJump;
            Vector3 bend = GroundModel.GripVelocityChange(velocity, Vector3.up, forward,
                ground.GripRate * jump.AirSteer, dt);
            float radius = ground.SphereRadius;
            float cushion = 0f;
            if (Physics.Raycast(_body.position, Vector3.down, out RaycastHit hit, jump.CushionProbe + radius,
                    Layers.DriveableMask, QueryTriggerInteraction.Ignore))
            {
                // Aim for the landing speed where the ground probe reports touchdown, a little above the surface.
                float touchdownGap = ground.GroundSnapDistance + radius * (1f - ground.ProbeRadiusFactor);
                cushion = _jump.CushionVelocityChange(velocity.y, hit.distance - radius - touchdownGap);
            }

            _body.AddForce(bend + Vector3.up * cushion, ForceMode.VelocityChange);
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
            _visualRig.Tick(deltaTime);
            _kit.Tick(deltaTime);
            _wheelFx.Tick();
            _hoverCoils.Tick(deltaTime);
            _lampMotes.Tick(deltaTime);
        }

        /// <summary>This frame's motion and the player's hands, as <see cref="RoverStillness"/> reads them.</summary>
        private StillnessSample SampleStillness()
        {
            Vector2 drive = _driveSource != null ? _driveSource.Drive : _input.Drive;
            bool engaged = _recovering || _cradled || _holds.IsHeld || _jump.Charge > 0f;
            return new StillnessSample(_landing.IsGrounded, Speed, drive, _input.LookDelta, _input.LookRate, engaged);
        }

        private void OnDestroy()
        {
            _dockChanges?.Dispose();
        }
    }
}
