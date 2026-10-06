using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// How the visual model moves on top of the physics: ground alignment, per-wheel suspension, the jelly lean that
    /// sells the low-gravity feel, the antenna wobble and the headlamp.
    /// </summary>
    [CreateAssetMenu(fileName = "RoverRigTuning", menuName = "MoonProject/Rover/Rover Rig Tuning")]
    public sealed class RoverRigTuning : ScriptableObject
    {
        [Header("Ground alignment")]
        [Tooltip("Spring frequency (Hz) of the body settling onto the plane fitted through the wheel contacts.")]
        [Range(0.2f, 8f)]
        [SerializeField] private float _alignFrequency = 2.2f;

        [Tooltip("Damping ratio of the ground alignment. Slightly under 1: settles with a barely visible overshoot.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _alignDamping = 0.75f;

        [Tooltip("Spring frequency (Hz) of the body attitude while airborne (slower: it drifts level gently).")]
        [Range(0.1f, 8f)]
        [SerializeField] private float _airAlignFrequency = 0.9f;

        [Tooltip("Damping ratio of the body attitude while airborne.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _airAlignDamping = 0.85f;

        [Tooltip("How much the nose follows the flight path in the air (0 = stays level, 1 = points along it).")]
        [Range(0f, 1f)]
        [SerializeField] private float _airPitchFollow = 0.3f;

        [Tooltip("Forward speed (m/s) at which the nose fully follows the flight path; slower hops stay level.")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _airPitchFullSpeed = 3f;

        [Tooltip("Largest nose up/down angle (deg) the flight path can cause in the air.")]
        [Range(0f, 45f)]
        [SerializeField] private float _maxAirPitch = 14f;

        [Tooltip("Largest body tilt (deg) from ground alignment. The rover can never flip: the body always targets an "
            + "attitude within this limit, and drifts back upright in the air.")]
        [Range(5f, 60f)]
        [SerializeField] private float _maxTilt = 35f;

        [Header("Wheels & suspension")]
        [Tooltip("Wheel radius (m) of the RoverModel wheels (rig contract: 0.35).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _wheelRadius = 0.35f;

        [Tooltip("How far (m) a wheel can travel up into the body over a bump.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _suspensionCompression = 0.16f;

        [Tooltip("How far (m) a wheel can hang down below its rest position over a dip or in the air.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _suspensionDroop = 0.22f;

        [Tooltip("Height (m) above the wheel centre where its ground ray starts.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _suspensionCastHeight = 0.6f;

        [Tooltip("Spring frequency (Hz) of each wheel following the ground.")]
        [Range(0.5f, 15f)]
        [SerializeField] private float _suspensionFrequency = 5.5f;

        [Tooltip("Damping ratio of each wheel's suspension.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _suspensionDamping = 0.5f;

        [Tooltip("Front wheel steering angle (deg) at full steering input.")]
        [Range(0f, 45f)]
        [SerializeField] private float _frontSteerAngle = 24f;

        [Tooltip("Rear wheels steer opposite to the front by this fraction (all-wheel steering look).")]
        [Range(0f, 1f)]
        [SerializeField] private float _rearSteerFactor = 0.4f;

        [Header("Jelly lean")]
        [Tooltip("Spring frequency (Hz) of the chassis pitch/roll lean.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _leanFrequency = 1.5f;

        [Tooltip("Damping ratio of the lean. ~0.42 gives one soft overshoot and a barely visible second one.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _leanDamping = 0.42f;

        [Tooltip("Nose-up lean (deg) per m/s^2 of forward acceleration (nose dips when slowing).")]
        [Range(0f, 5f)]
        [SerializeField] private float _pitchPerAcceleration = 1.1f;

        [Tooltip("Outward roll (deg) per m/s^2 of sideways acceleration in turns.")]
        [Range(0f, 5f)]
        [SerializeField] private float _rollPerAcceleration = 0.9f;

        [Tooltip("Largest lean angle (deg).")]
        [Range(0f, 20f)]
        [SerializeField] private float _maxLean = 9f;

        [Tooltip("Height (m) of the point the chassis leans around.")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _leanPivotHeight = 0.55f;

        [Tooltip("Spring frequency (Hz) of the chassis bobbing up and down on its suspension.")]
        [Range(0.2f, 8f)]
        [SerializeField] private float _heaveFrequency = 2.3f;

        [Tooltip("Damping ratio of the chassis bob.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _heaveDamping = 0.45f;

        [Tooltip("Chassis drop (m) per m/s^2 of upward acceleration (landing squash, airborne float).")]
        [Range(0f, 0.02f)]
        [SerializeField] private float _heavePerAcceleration = 0.0025f;

        [Tooltip("Largest chassis bob (m) either way.")]
        [Range(0f, 0.4f)]
        [SerializeField] private float _maxHeave = 0.14f;

        [Tooltip("Accelerations above this (m/s^2, e.g. bumping a rock) are clipped before driving the jelly.")]
        [Range(10f, 500f)]
        [SerializeField] private float _accelerationLimit = 200f;

        [Header("Antenna")]
        [Tooltip("Spring frequency (Hz) of the antenna wobble.")]
        [Range(0.2f, 10f)]
        [SerializeField] private float _antennaFrequency = 2.4f;

        [Tooltip("Damping ratio of the antenna wobble (low: it keeps swaying a little after a stop).")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _antennaDamping = 0.15f;

        [Tooltip("Antenna sway (deg) per m/s^2 of acceleration.")]
        [Range(0f, 10f)]
        [SerializeField] private float _antennaDegreesPerAcceleration = 2.2f;

        [Tooltip("Largest antenna sway (deg).")]
        [Range(0f, 45f)]
        [SerializeField] private float _antennaMaxAngle = 22f;

        [Header("Headlamp")]
        [Tooltip("Headlamp intensity (warm spot light at HeadlampSocket).")]
        [Range(0f, 50f)]
        [SerializeField] private float _headlampIntensity = 6f;

        [Tooltip("Headlamp range (m).")]
        [Range(1f, 60f)]
        [SerializeField] private float _headlampRange = 18f;

        [Tooltip("Headlamp outer cone angle (deg).")]
        [Range(10f, 150f)]
        [SerializeField] private float _headlampSpotAngle = 62f;

        [Tooltip("Headlamp inner (full brightness) cone angle (deg); a wide soft edge reads as cosy.")]
        [Range(1f, 150f)]
        [SerializeField] private float _headlampInnerSpotAngle = 30f;

        public float AlignFrequency => _alignFrequency;

        public float AlignDamping => _alignDamping;

        public float AirAlignFrequency => _airAlignFrequency;

        public float AirAlignDamping => _airAlignDamping;

        public float AirPitchFollow => _airPitchFollow;

        public float AirPitchFullSpeed => _airPitchFullSpeed;

        public float MaxAirPitch => _maxAirPitch;

        public float MaxTilt => _maxTilt;

        public float WheelRadius => _wheelRadius;

        public float SuspensionCompression => _suspensionCompression;

        public float SuspensionDroop => _suspensionDroop;

        public float SuspensionCastHeight => _suspensionCastHeight;

        public float SuspensionFrequency => _suspensionFrequency;

        public float SuspensionDamping => _suspensionDamping;

        public float FrontSteerAngle => _frontSteerAngle;

        public float RearSteerFactor => _rearSteerFactor;

        public float LeanFrequency => _leanFrequency;

        public float LeanDamping => _leanDamping;

        public float PitchPerAcceleration => _pitchPerAcceleration;

        public float RollPerAcceleration => _rollPerAcceleration;

        public float MaxLean => _maxLean;

        public float LeanPivotHeight => _leanPivotHeight;

        public float HeaveFrequency => _heaveFrequency;

        public float HeaveDamping => _heaveDamping;

        public float HeavePerAcceleration => _heavePerAcceleration;

        public float MaxHeave => _maxHeave;

        public float AccelerationLimit => _accelerationLimit;

        public float AntennaFrequency => _antennaFrequency;

        public float AntennaDamping => _antennaDamping;

        public float AntennaDegreesPerAcceleration => _antennaDegreesPerAcceleration;

        public float AntennaMaxAngle => _antennaMaxAngle;

        public float HeadlampIntensity => _headlampIntensity;

        public float HeadlampRange => _headlampRange;

        public float HeadlampSpotAngle => _headlampSpotAngle;

        public float HeadlampInnerSpotAngle => Mathf.Min(_headlampInnerSpotAngle, _headlampSpotAngle);
    }
}
