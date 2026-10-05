using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Longitudinal feel: top speeds and how quickly the rover eases into, out of and through them. Times are the
    /// designer-facing numbers; the accelerations the model uses are derived from them.
    /// </summary>
    [Serializable]
    public sealed class DriveSettings
    {
        /// <summary>atanh(0.9): the speed curve v = top * tanh(t * accel / top) reaches 90% at this many time constants.</summary>
        private const float NinetyPercentTimeConstants = 1.4722195f;

        [Tooltip("Forward top speed on flat ground, m/s.")]
        [Range(2f, 20f)]
        [SerializeField] private float _topSpeed = 8f;

        [Tooltip("Reverse top speed, m/s. Slower than forward so backing up feels careful.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _reverseTopSpeed = 3.5f;

        [Tooltip("Seconds from standstill to 90% of top speed at full throttle. The speed curve eases out as it "
            + "approaches top speed; the throttle easing below adds a soft start.")]
        [Range(0.3f, 8f)]
        [SerializeField] private float _accelerationTime = 2.2f;

        [Tooltip("Seconds from standstill to 90% of reverse top speed.")]
        [Range(0.3f, 8f)]
        [SerializeField] private float _reverseAccelerationTime = 1.5f;

        [Tooltip("Seconds to roll to a stop from top speed after releasing the throttle.")]
        [Range(0.3f, 8f)]
        [SerializeField] private float _coastStopTime = 2f;

        [Tooltip("Shape of the coast-down. 0 = constant deceleration (abrupt final stop); higher values slow down more "
            + "early and let the last metres roll out softly.")]
        [Range(0f, 0.9f)]
        [SerializeField] private float _coastEase = 0.5f;

        [Tooltip("Seconds to stop from top speed while holding full opposite input.")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _brakeStopTime = 1.1f;

        [Tooltip("Shape of the braking curve, same meaning as Coast Ease.")]
        [Range(0f, 0.9f)]
        [SerializeField] private float _brakeEase = 0.35f;

        [Tooltip("Half-life (s) of the throttle easing in when pressed. Removes frame-one jolts on keyboards.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _throttleRiseHalfLife = 0.08f;

        [Tooltip("Half-life (s) of the throttle easing out when released.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _throttleFallHalfLife = 0.1f;

        [Tooltip("Throttle magnitudes below this count as released.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _inputDeadZone = 0.05f;

        [Tooltip("Below this speed (m/s), holding the opposite direction stops braking and starts driving that way.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _reverseEngageSpeed = 0.25f;

        public float TopSpeed => _topSpeed;

        public float ReverseTopSpeed => _reverseTopSpeed;

        public float AccelerationTime => _accelerationTime;

        public float ReverseAccelerationTime => _reverseAccelerationTime;

        public float CoastStopTime => _coastStopTime;

        public float CoastEase => _coastEase;

        public float BrakeStopTime => _brakeStopTime;

        public float BrakeEase => _brakeEase;

        public float ThrottleRiseHalfLife => _throttleRiseHalfLife;

        public float ThrottleFallHalfLife => _throttleFallHalfLife;

        public float InputDeadZone => _inputDeadZone;

        public float ReverseEngageSpeed => _reverseEngageSpeed;

        /// <summary>Peak forward acceleration (m/s^2) at standstill, derived from <see cref="AccelerationTime"/>.</summary>
        public float ForwardAcceleration => NinetyPercentTimeConstants * _topSpeed / _accelerationTime;

        /// <summary>Peak reverse acceleration (m/s^2), derived from <see cref="ReverseAccelerationTime"/>.</summary>
        public float ReverseAcceleration => NinetyPercentTimeConstants * _reverseTopSpeed / _reverseAccelerationTime;

        /// <summary>Coast deceleration (m/s^2) at top speed; stopping time from top speed is CoastStopTime exactly.</summary>
        public float CoastDeceleration => _topSpeed / (_coastStopTime * (1f - _coastEase));

        /// <summary>Full-input brake deceleration (m/s^2) at top speed.</summary>
        public float BrakeDeceleration => _topSpeed / (_brakeStopTime * (1f - _brakeEase));
    }
}
