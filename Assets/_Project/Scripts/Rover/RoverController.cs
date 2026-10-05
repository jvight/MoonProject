using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.Rover
{
    /// <summary>
    /// The player rover: a hidden, rotation-locked physics sphere pushed by accelerations along a steered heading, with
    /// the visual model following the interpolated sphere. Registers itself as <see cref="IRoverState"/>, publishes
    /// <see cref="RoverLanded"/>, and ticks its visual rig and wheel effects in a fixed order every frame.
    /// The maths lives in plain classes (<see cref="LongitudinalDrive"/>, <see cref="SteeringModel"/>,
    /// <see cref="GroundModel"/>, <see cref="LandingDetector"/>); this component only wires them to physics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverController : MonoBehaviour, IGameSystem, IRoverState
    {
        /// <summary>The probe starts this fraction of the radius above the centre so slight sinking still hits.</summary>
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

        private InputReader _input;
        private EventBus _events;
        private LandingDetector _landing;
        private bool _initialized;

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

        public RoverTuning Tuning => _tuning;

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
            ConfigureBody();
            _heading = transform.eulerAngles.y;
            _previousHeading = _heading;
            _visualHeading = _heading;
            context.Register<IRoverState>(this);

            bool visualsReady = _visualRig.Initialize(this);
            bool effectsReady = _wheelFx.Initialize(context, this);
            _initialized = visualsReady && effectsReady;
            enabled = _initialized;
        }

        private bool ValidateWiring()
        {
            bool ok = true;
            ok &= Require(_tuning != null, "RoverTuning is not assigned.");
            ok &= Require(_body != null, "Physics sphere Rigidbody is not assigned.");
            ok &= Require(_sphere != null, "Physics sphere SphereCollider is not assigned.");
            ok &= Require(_visualRig != null, "RoverVisualRig is not assigned.");
            ok &= Require(_wheelFx != null, "RoverWheelFx is not assigned.");
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
            Vector3 spawn = transform.position + transform.up * ground.SphereRadius;
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
            Vector3 velocity = _body.linearVelocity;
            Quaternion yaw = Quaternion.Euler(0f, _heading, 0f);
            _localAcceleration = Quaternion.Inverse(yaw) * ((velocity - _lastVelocity) / dt);

            ReadInput(dt);
            ProbeGround(out float speedIntoGround, velocity);

            if (_landing.Step(_hasContact, speedIntoGround, dt))
            {
                Vector3 contact = _body.position - _contactNormal * _tuning.Ground.SphereRadius;
                _events.Publish(new RoverLanded(contact, _landing.LastImpactSpeed, _landing.LastAirTime));
            }

            Vector3 normal = _hasContact ? _contactNormal : Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(yaw * Vector3.forward, normal).normalized;
            _forwardSpeed = Vector3.Dot(velocity, forward);

            Steer(dt);
            Drive(velocity, normal, forward, dt);

            float smoothing = Smoothing.Factor(_tuning.Ground.GroundNormalHalfLife, dt);
            Vector3 targetNormal = _landing.IsGrounded ? normal : Vector3.up;
            _groundNormal = Vector3.Slerp(_groundNormal, targetNormal, smoothing);
            _lastVelocity = velocity;
        }

        private void ReadInput(float dt)
        {
            Vector2 raw = _input.Drive;
            DriveSettings drive = _tuning.Drive;
            _throttle = Ease(_throttle, raw.y, drive.ThrottleRiseHalfLife, drive.ThrottleFallHalfLife, dt);
            _steer = Ease(_steer, raw.x, _tuning.Steering.SteerRiseHalfLife, _tuning.Steering.SteerReturnHalfLife, dt);
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
            _yawRate = SteeringModel.YawRate(steering, _steer, _steerDirection, _forwardSpeed,
                _tuning.Drive.TopSpeed, _hasContact);

            _previousHeading = _heading;
            _heading = Mathf.Repeat(_heading + _yawRate * dt, 360f);
        }

        private void Drive(Vector3 velocity, Vector3 normal, Vector3 forward, float dt)
        {
            GroundSettings ground = _tuning.Ground;
            float drive = LongitudinalDrive.Acceleration(_tuning.Drive, _forwardSpeed, _throttle, dt);

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

            _visualRig.Tick(Time.deltaTime);
            _wheelFx.Tick();
        }
    }
}
