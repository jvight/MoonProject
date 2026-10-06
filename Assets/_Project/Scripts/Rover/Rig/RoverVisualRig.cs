using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// Moves the art box's RoverModel on top of the physics sphere. Hierarchy (built by the Rover builder):
    /// <c>Visual (this: follows the sphere, heading + ground alignment) / Chassis (jelly lean + bob) / RoverModel</c>.
    /// Wheels ray-cast the ground every frame from the leaned chassis, so they stay planted while the body squashes,
    /// leans and bobs above them, and the rocker-bogie arms follow the wheels. Neck, Head, Eyelid and SolarWing
    /// belong to <see cref="RoverBodyLanguage"/>. Ticked by <see cref="RoverController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverVisualRig : MonoBehaviour
    {
        [Tooltip("Rig tuning (Assets/_Project/Data/Tuning/RoverRigTuning.asset).")]
        [SerializeField] private RoverRigTuning _tuning;

        [Tooltip("Child between this transform and RoverModel; carries the jelly lean and bob.")]
        [SerializeField] private Transform _chassis;

        [Tooltip("RoverModel wheels in rig order: Wheel_FL, Wheel_FR, Wheel_ML, Wheel_MR, Wheel_RL, Wheel_RR.")]
        [SerializeField] private Transform[] _wheels = new Transform[RoverModelNodes.WheelCount];

        [Tooltip("RoverModel 'Bogie_L' rocker arm (pitch = local X).")]
        [SerializeField] private Transform _bogieLeft;

        [Tooltip("RoverModel 'Bogie_R' rocker arm (pitch = local X).")]
        [SerializeField] private Transform _bogieRight;

        [Tooltip("RoverModel 'Antenna' node (pivot at its base).")]
        [SerializeField] private Transform _antenna;

        [Tooltip("Warm spot light under RoverModel 'HeadlampSocket'.")]
        [SerializeField] private Light _headlamp;

        private RoverController _rover;
        private BodyAttitude _attitude;
        private ChassisJelly _jelly;
        private Vector3[] _wheelRestPositions;
        private Quaternion[] _wheelRestRotations;
        private float[] _wheelSteerFactors;
        private float[] _wheelTargets;
        private float[] _wheelSpin;
        private DampedSpring[] _wheelSuspension;
        private Vector3[] _contacts;
        private Vector3 _chassisRestPosition;
        private Quaternion _chassisRestRotation;
        private Quaternion _antennaRestRotation;
        private Quaternion _bogieLeftRestRotation;
        private Quaternion _bogieRightRestRotation;
        private Vector3 _lastPosition;

        public Vector3 RootPosition => transform.position;

        public Quaternion RootRotation => transform.rotation;

        /// <summary>Validates the wiring and snaps the model onto the sphere; false (and logged) when broken.</summary>
        public bool Initialize(RoverController rover)
        {
            _rover = rover;
            if (!ValidateWiring())
            {
                enabled = false;
                return false;
            }

            _attitude = new BodyAttitude(_tuning);
            _jelly = new ChassisJelly(_tuning);
            CacheRestPose();
            ApplyHeadlamp();
            Snap();
            return true;
        }

        private bool ValidateWiring()
        {
            bool ok = Require(_tuning != null, "RoverRigTuning is not assigned.")
                & Require(_chassis != null, "Chassis transform is not assigned.")
                & Require(_antenna != null, "Antenna node is not assigned.")
                & Require(_bogieLeft != null && _bogieRight != null, "Bogie_L/Bogie_R nodes are not assigned.")
                & Require(_headlamp != null, "Headlamp light is not assigned.")
                & Require(_wheels != null && _wheels.Length == RoverModelNodes.WheelCount,
                    $"Exactly {RoverModelNodes.WheelCount} wheels are required.");
            if (_wheels != null)
            {
                for (int i = 0; i < _wheels.Length; i++)
                {
                    ok &= Require(_wheels[i] != null, $"Wheel {i} ({SafeWheelName(i)}) is not assigned.");
                }
            }

            return ok;
        }

        private static string SafeWheelName(int index)
        {
            return index < RoverModelNodes.WheelCount ? RoverModelNodes.Wheel(index) : "extra";
        }

        private bool Require(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"{nameof(RoverVisualRig)}: {message}", this);
            }

            return condition;
        }

        private void CacheRestPose()
        {
            int count = RoverModelNodes.WheelCount;
            _wheelRestPositions = new Vector3[count];
            _wheelRestRotations = new Quaternion[count];
            _wheelSteerFactors = new float[count];
            _wheelTargets = new float[count];
            _wheelSpin = new float[count];
            _wheelSuspension = new DampedSpring[count];
            _contacts = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                _wheelRestPositions[i] = _wheels[i].localPosition;
                _wheelRestRotations[i] = _wheels[i].localRotation;
                int row = RoverModelNodes.WheelRow(i);
                _wheelSteerFactors[i] = row == 0 ? 1f : row == 2 ? -_tuning.RearSteerFactor : 0f;
            }

            _chassisRestPosition = _chassis.localPosition;
            _chassisRestRotation = _chassis.localRotation;
            _antennaRestRotation = _antenna.localRotation;
            _bogieLeftRestRotation = _bogieLeft.localRotation;
            _bogieRightRestRotation = _bogieRight.localRotation;
        }

        private void ApplyHeadlamp()
        {
            _headlamp.type = LightType.Spot;
            _headlamp.intensity = _tuning.HeadlampIntensity;
            _headlamp.range = _tuning.HeadlampRange;
            _headlamp.spotAngle = _tuning.HeadlampSpotAngle;
            _headlamp.innerSpotAngle = _tuning.HeadlampInnerSpotAngle;
        }

        /// <summary>Places the model upright on the sphere with every spring at rest (spawn).</summary>
        public void Snap()
        {
            _attitude.Reset(0f, 0f);
            _jelly.Reset();
            Quaternion rotation = _rover.HeadingRotation;
            Vector3 position = _rover.SpherePosition - rotation * Vector3.up * _rover.SphereRadius;
            transform.SetPositionAndRotation(position, rotation);
            _lastPosition = position;
            for (int i = 0; i < _wheelSuspension.Length; i++)
            {
                _wheelSuspension[i].Reset(0f);
                _wheelTargets[i] = 0f;
            }

            ApplyChassis();
            ApplyWheels(0f, 0f);
            ApplyBogies();
        }

        /// <summary>A quick antenna wiggle (deg/s), e.g. when 07 perks up.</summary>
        public void KickAntenna(float degreesPerSecond)
        {
            _jelly.KickAntenna(degreesPerSecond);
        }

        /// <summary>An extra chassis squash (m/s downward when negative), e.g. a hard-landing "oof".</summary>
        public void KickHeave(float metresPerSecond)
        {
            _jelly.KickHeave(metresPerSecond);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            Quaternion heading = _rover.HeadingRotation;
            Vector3 sphere = _rover.SpherePosition;
            float radius = _rover.SphereRadius;

            // Ray-cast from last frame's attitude at the new position; the springs absorb the one-frame lag.
            Quaternion rotation = heading * GroundPlaneFit.Tilt(_attitude.Pitch, _attitude.Roll);
            transform.SetPositionAndRotation(sphere - rotation * Vector3.up * radius, rotation);
            int contacts = CastWheels(heading);

            if (_rover.IsGrounded)
            {
                Vector3 localNormal = GroundPlaneFit.TryFit(_contacts, contacts, out Vector3 fitted)
                    ? fitted
                    : Quaternion.Inverse(heading) * _rover.GroundNormal;
                _attitude.StepGrounded(localNormal, deltaTime);
            }
            else
            {
                _attitude.StepAirborne(_rover.Velocity.y, _rover.ForwardSpeed, deltaTime);
            }

            rotation = heading * GroundPlaneFit.Tilt(_attitude.Pitch, _attitude.Roll);
            Vector3 position = sphere - rotation * Vector3.up * radius;
            float travelled = Vector3.Dot(position - _lastPosition, rotation * Vector3.forward);
            _lastPosition = position;
            transform.SetPositionAndRotation(position, rotation);

            _jelly.Step(_rover.LocalAcceleration, _rover.JumpCharge, deltaTime);
            ApplyChassis();
            StepSuspension(deltaTime);
            ApplyWheels(travelled, deltaTime);
            ApplyBogies();
            _antenna.localRotation =
                GroundPlaneFit.Tilt(_jelly.AntennaPitch, _jelly.AntennaRoll) * _antennaRestRotation;
        }

        private int CastWheels(Quaternion heading)
        {
            float castHeight = _tuning.SuspensionCastHeight;
            float wheelRadius = _tuning.WheelRadius;
            float droop = _tuning.SuspensionDroop;
            float length = castHeight + wheelRadius + droop;
            Quaternion toHeading = Quaternion.Inverse(heading);
            Vector3 root = transform.position;
            int count = 0;

            for (int i = 0; i < _wheels.Length; i++)
            {
                Transform parent = _wheels[i].parent;
                Vector3 origin = parent.TransformPoint(_wheelRestPositions[i] + Vector3.up * castHeight);
                if (Physics.Raycast(origin, -parent.up, out RaycastHit hit, length, Layers.DriveableMask,
                        QueryTriggerInteraction.Ignore))
                {
                    _wheelTargets[i] = Mathf.Clamp(castHeight + wheelRadius - hit.distance, -droop,
                        _tuning.SuspensionCompression);
                    _contacts[count++] = toHeading * (hit.point - root);
                }
                else
                {
                    _wheelTargets[i] = -droop;
                }
            }

            return count;
        }

        private void StepSuspension(float deltaTime)
        {
            for (int i = 0; i < _wheelSuspension.Length; i++)
            {
                _wheelSuspension[i].Step(_wheelTargets[i], _tuning.SuspensionFrequency, _tuning.SuspensionDamping,
                    deltaTime);
                _wheelSuspension[i].Clamp(-_tuning.SuspensionDroop, _tuning.SuspensionCompression);
            }
        }

        private void ApplyChassis()
        {
            Vector3 pivot = Vector3.up * _tuning.LeanPivotHeight;
            Quaternion lean = GroundPlaneFit.Tilt(_jelly.Pitch, _jelly.Roll);
            _chassis.localPosition = _chassisRestPosition + pivot - lean * pivot + Vector3.up * _jelly.Heave;
            _chassis.localRotation = lean * _chassisRestRotation;
        }

        /// <summary>Each rocker arm pitches to the line between its front and rear wheel (visual only).</summary>
        private void ApplyBogies()
        {
            _bogieLeft.localRotation = _bogieLeftRestRotation * Quaternion.AngleAxis(
                -BogiePitch(RoverModelNodes.FrontLeftIndex, RoverModelNodes.RearLeftIndex), Vector3.right);
            _bogieRight.localRotation = _bogieRightRestRotation * Quaternion.AngleAxis(
                -BogiePitch(RoverModelNodes.FrontRightIndex, RoverModelNodes.RearRightIndex), Vector3.right);
        }

        /// <summary>Nose-up pitch (deg) of the line from the rear to the front wheel of one side.</summary>
        private float BogiePitch(int front, int rear)
        {
            float rise = _wheelSuspension[front].Value - _wheelSuspension[rear].Value;
            float run = _wheelRestPositions[front].z - _wheelRestPositions[rear].z;
            return Mathf.Atan2(rise, Mathf.Max(run, 0.01f)) * Mathf.Rad2Deg;
        }

        private void ApplyWheels(float travelled, float deltaTime)
        {
            float yawRadians = _rover.YawRate * Mathf.Deg2Rad * deltaTime;
            float steer = _rover.SteerInput * _tuning.FrontSteerAngle;
            float degreesPerMetre = Mathf.Rad2Deg / _tuning.WheelRadius;

            for (int i = 0; i < _wheels.Length; i++)
            {
                Vector3 rest = _wheelRestPositions[i];

                // Turning right moves the left wheels forward and the right wheels back (skid-steer pivot).
                float distance = travelled - yawRadians * rest.x;
                _wheelSpin[i] = Mathf.Repeat(_wheelSpin[i] + distance * degreesPerMetre, 360f);

                Transform wheel = _wheels[i];
                wheel.localPosition = rest + Vector3.up * _wheelSuspension[i].Value;
                wheel.localRotation = Quaternion.AngleAxis(steer * _wheelSteerFactors[i], Vector3.up)
                    * _wheelRestRotations[i]
                    * Quaternion.AngleAxis(_wheelSpin[i], Vector3.right);
            }
        }
    }
}
