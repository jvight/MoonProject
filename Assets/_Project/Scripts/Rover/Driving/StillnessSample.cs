using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>One frame of what <see cref="RoverStillness"/> needs to know about 07 and the player's hands.</summary>
    public readonly struct StillnessSample
    {
        /// <param name="grounded">07 is on the ground (airborne is never still).</param>
        /// <param name="speed">Horizontal speed (m/s).</param>
        /// <param name="drive">Raw drive input this frame (x = steer, y = throttle).</param>
        /// <param name="lookDelta">Mouse look movement this frame (pixels).</param>
        /// <param name="lookStick">Look stick deflection (-1..1).</param>
        /// <param name="engaged">
        /// Something is happening to 07 even though it stands still: a recovery lift, a Hover-Jump charge, or an
        /// interaction holding it parked (the excavation beam, a friend's repair).
        /// </param>
        public StillnessSample(bool grounded, float speed, Vector2 drive, Vector2 lookDelta, Vector2 lookStick,
            bool engaged)
        {
            Grounded = grounded;
            Speed = speed;
            Drive = drive;
            LookDelta = lookDelta;
            LookStick = lookStick;
            Engaged = engaged;
        }

        public bool Grounded { get; }

        public float Speed { get; }

        public Vector2 Drive { get; }

        public Vector2 LookDelta { get; }

        public Vector2 LookStick { get; }

        public bool Engaged { get; }
    }
}
