using UnityEngine;
using MoonProject.Core;
using MoonProject.Rover;

namespace MoonProject.World.PlayModeTests
{
    /// <summary>
    /// Holds 07's wheel for the canyon session: steers toward a target at a set throttle (easing off only for sharp
    /// turns). Read by the real RoverController through <see cref="IRoverDriveSource"/>, so easing and physics are the
    /// game's own.
    /// </summary>
    public sealed class CanyonPilot : IRoverDriveSource
    {
        /// <summary>Degrees of heading error that ask for full steering lock.</summary>
        private const float FullLockAngle = 35f;

        /// <summary>Above this heading error (degrees) the pilot slows down to turn.</summary>
        private const float TurnSlowAngle = 70f;

        private const float TurnThrottle = 0.4f;

        /// <summary>
        /// Above this heading error (degrees) the target is behind: the pilot backs up on the opposite lock (a
        /// three-point turn), so a narrow chamber's back wall never pins 07 nose-first.
        /// </summary>
        private const float ReverseAngle = 120f;

        private readonly IRoverState _rover;

        public CanyonPilot(IRoverState rover)
        {
            _rover = rover;
        }

        /// <summary>Where to steer; null lets go of the wheel.</summary>
        public Vector3? Target { get; set; }

        public float Throttle { get; set; } = 1f;

        /// <summary>Horizontal distance to the target (0 without one).</summary>
        public float Distance
        {
            get
            {
                if (Target == null)
                {
                    return 0f;
                }

                Vector3 to = Target.Value - _rover.Position;
                to.y = 0f;
                return to.magnitude;
            }
        }

        public Vector2 Drive
        {
            get
            {
                if (Target == null)
                {
                    return Vector2.zero;
                }

                Vector3 to = Target.Value - _rover.Position;
                to.y = 0f;
                Vector3 forward = _rover.Rotation * Vector3.forward;
                forward.y = 0f;
                float angle = Vector3.SignedAngle(forward, to, Vector3.up);
                if (Mathf.Abs(angle) > ReverseAngle)
                {
                    return new Vector2(-Mathf.Sign(angle), -TurnThrottle);
                }

                float steer = Mathf.Clamp(angle / FullLockAngle, -1f, 1f);
                float throttle = Mathf.Abs(angle) > TurnSlowAngle ? Mathf.Min(Throttle, TurnThrottle) : Throttle;
                return new Vector2(steer, throttle);
            }
        }

        /// <summary>Lets go of the wheel.</summary>
        public void Release()
        {
            Target = null;
        }
    }
}
