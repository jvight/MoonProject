using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>What <see cref="TargetFraming"/> reads each frame.</summary>
    public readonly struct TargetFramingInput
    {
        /// <param name="hasTarget">A scan or interaction target is available.</param>
        /// <param name="roverPosition">07's position (world).</param>
        /// <param name="targetPosition">The target's point (world).</param>
        /// <param name="viewYaw">The world yaw (deg) the camera looks along.</param>
        /// <param name="speed">07's speed (m/s).</param>
        /// <param name="looking">The player is moving the view this frame.</param>
        /// <param name="mayFrame">Nothing else frames the view.</param>
        public TargetFramingInput(bool hasTarget, Vector3 roverPosition, Vector3 targetPosition, float viewYaw,
            float speed, bool looking, bool mayFrame)
        {
            HasTarget = hasTarget;
            RoverPosition = roverPosition;
            TargetPosition = targetPosition;
            ViewYaw = viewYaw;
            Speed = speed;
            Looking = looking;
            MayFrame = mayFrame;
        }

        public bool HasTarget { get; }

        public Vector3 RoverPosition { get; }

        public Vector3 TargetPosition { get; }

        public float ViewYaw { get; }

        public float Speed { get; }

        public bool Looking { get; }

        public bool MayFrame { get; }
    }

    /// <summary>
    /// Lifts the chase camera and slides it a little toward the target's side while 07 is stopped or slow near
    /// something to scan or use, so 07's own body never hides it. It only adds to the player's view: the mouse is never
    /// overridden, it just starts the camera easing back, and driving off releases it.
    /// </summary>
    public sealed class TargetFraming
    {
        private readonly RoverCameraTuning _tuning;
        private float _calmTime;
        private float _raw;
        private float _side;

        public TargetFraming(RoverCameraTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>How strongly the framing applies, 0..1 (eased).</summary>
        public float Weight => Smoothing.SmoothStep(0f, 1f, _raw);

        /// <summary>Extra camera elevation (deg).</summary>
        public float Elevation => _tuning.FramingLift * Weight;

        /// <summary>Extra yaw (deg) toward the target's side.</summary>
        public float YawOffset => _side * _tuning.FramingYaw * Weight;

        public void Step(in TargetFramingInput input, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            _calmTime = input.Speed <= _tuning.FramingMaxSpeed && !input.Looking ? _calmTime + deltaTime : 0f;
            bool wants = false;
            if (input.HasTarget && input.MayFrame)
            {
                Vector3 toTarget = input.TargetPosition - input.RoverPosition;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;
                float bearing = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                float across = Mathf.DeltaAngle(input.ViewYaw, bearing);
                wants = distance <= _tuning.FramingRange && Mathf.Abs(across) <= _tuning.FramingViewCone
                    && _calmTime >= _tuning.FramingEngageDelay;
                float side = Mathf.Clamp(across / _tuning.FramingSideFullAngle, -1f, 1f);
                _side = _raw <= 0f ? side : Smoothing.Damp(_side, side, _tuning.FramingSideHalfLife, deltaTime);
            }

            float seconds = wants ? _tuning.FramingRampSeconds : _tuning.FramingReleaseSeconds;
            _raw = Mathf.MoveTowards(_raw, wants ? 1f : 0f, deltaTime / seconds);
        }
    }
}
