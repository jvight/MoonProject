using UnityEngine;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// How a player's keys steer 07 to a spot: turn in place when the spot is well off the nose, drive when it is
    /// ahead, ease the throttle in the last metres. Pure maths on positions; the caller turns the answer into
    /// W/A/D presses.
    /// </summary>
    public static class DriveSteering
    {
        /// <summary>Degrees off the nose within which the bot stops turning and drives straight.</summary>
        public const float TurnDeadzoneDegrees = 6f;

        /// <summary>Degrees off the nose beyond which the bot turns on the spot instead of driving.</summary>
        public const float TurnInPlaceDegrees = 40f;

        /// <summary>Metres from the spot within which the throttle eases to <see cref="CreepThrottle"/>.</summary>
        public const float SlowRadius = 10f;

        public const float CreepThrottle = 0.4f;

        /// <summary>One frame's decision: turn -1 (A) / 0 / +1 (D), throttle 0..1 (W), and whether 07 is there.</summary>
        public readonly struct Command
        {
            public Command(float turn, float throttle, bool arrived)
            {
                Turn = turn;
                Throttle = throttle;
                Arrived = arrived;
            }

            public float Turn { get; }

            public float Throttle { get; }

            public bool Arrived { get; }
        }

        /// <summary>Degrees, on the ground, from the nose to <paramref name="target"/>: positive means to the right.</summary>
        public static float HeadingError(Vector3 position, Vector3 forward, Vector3 target)
        {
            Vector3 ahead = Flat(forward);
            Vector3 toTarget = Flat(target - position);
            if (ahead.sqrMagnitude < 1e-6f || toTarget.sqrMagnitude < 1e-6f)
            {
                return 0f;
            }

            return Vector3.SignedAngle(ahead, toTarget, Vector3.up);
        }

        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            return Flat(b - a).magnitude;
        }

        public static Command Decide(Vector3 position, Vector3 forward, Vector3 target, float arriveRadius)
        {
            float distance = FlatDistance(position, target);
            if (distance <= arriveRadius)
            {
                return new Command(0f, 0f, true);
            }

            float error = HeadingError(position, forward, target);
            float turn = Mathf.Abs(error) <= TurnDeadzoneDegrees ? 0f : Mathf.Sign(error);
            float throttle = Mathf.Abs(error) > TurnInPlaceDegrees ? 0f
                : distance > SlowRadius ? 1f : CreepThrottle;
            return new Command(turn, throttle, false);
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }
    }
}
