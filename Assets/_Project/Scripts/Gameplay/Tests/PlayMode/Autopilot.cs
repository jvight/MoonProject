using UnityEngine;
using MoonProject.Core;
using MoonProject.Rover;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Holds 07's wheel in the playthrough like a calm player would: it steers toward a target, eases off as it gets
    /// close, slows down for sharp turns and lets go of the throttle once it has arrived. Read by the real
    /// RoverController through <see cref="IRoverDriveSource"/>, so drive easing, steering and physics are the game's.
    /// </summary>
    public sealed class Autopilot : IRoverDriveSource
    {
        /// <summary>Degrees of heading error that ask for full steering lock.</summary>
        private const float FullLockAngle = 35f;

        /// <summary>Above this heading error (degrees) the autopilot slows down to turn.</summary>
        private const float TurnSlowAngle = 70f;

        private const float TurnThrottle = 0.4f;
        private const float MinThrottle = 0.2f;

        private readonly IRoverState _rover;

        public Autopilot(IRoverState rover)
        {
            _rover = rover;
        }

        /// <summary>Where to go; null lets go of the wheel.</summary>
        public Vector3? Target { get; set; }

        /// <summary>Close enough (m, horizontal).</summary>
        public float ArriveRadius { get; set; } = 2f;

        /// <summary>Starts slowing down this far (m) from the target.</summary>
        public float SlowRadius { get; set; } = 10f;

        public float MaxThrottle { get; set; } = 1f;

        public bool Arrived => Target == null || Distance <= ArriveRadius;

        public float Distance => Target == null ? 0f : SurfaceRules.HorizontalDistance(_rover.Position, Target.Value);

        public Vector2 Drive
        {
            get
            {
                if (Target == null || Arrived)
                {
                    return Vector2.zero;
                }

                Vector3 to = Target.Value - _rover.Position;
                to.y = 0f;
                Vector3 forward = _rover.Rotation * Vector3.forward;
                forward.y = 0f;
                float angle = Vector3.SignedAngle(forward, to, Vector3.up);
                float steer = Mathf.Clamp(angle / FullLockAngle, -1f, 1f);
                float throttle = Mathf.Clamp(Distance / SlowRadius, MinThrottle, MaxThrottle);
                if (Mathf.Abs(angle) > TurnSlowAngle)
                {
                    throttle = Mathf.Min(throttle, TurnThrottle);
                }

                return new Vector2(steer, throttle);
            }
        }

        /// <summary>Targets <paramref name="target"/> and returns this for chaining.</summary>
        public Autopilot GoTo(Vector3 target, float arriveRadius, float maxThrottle)
        {
            Target = target;
            ArriveRadius = arriveRadius;
            MaxThrottle = maxThrottle;
            return this;
        }
    }
}
