using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>What the <see cref="CameraOrbit"/> reads each frame: the player's look and 07's motion.</summary>
    public readonly struct CameraOrbitInput
    {
        /// <param name="look">Orbit change requested this frame: x = orbit right, y = camera up (deg).</param>
        /// <param name="lookFromStick">The look came from the gamepad stick (its idle clock is shorter).</param>
        /// <param name="recenter">The recenter button was pressed this frame.</param>
        /// <param name="roverYaw">07's heading (deg, world).</param>
        /// <param name="forwardSpeed">07's speed along its heading (m/s); negative when reversing.</param>
        /// <param name="throttle">The player's throttle, -1..1.</param>
        /// <param name="normalizedSpeed">Speed as a fraction of top speed.</param>
        /// <param name="descentAngle">Angle (deg) of travel below the horizon; negative when climbing.</param>
        /// <param name="grounded">Whether 07 is on the ground (the lift holds steady in the air).</param>
        /// <param name="mayRecenter">Nothing else frames the view (no moment, no wide shot, no crawl home).</param>
        public CameraOrbitInput(Vector2 look, bool lookFromStick, bool recenter, float roverYaw, float forwardSpeed,
            float throttle, float normalizedSpeed, float descentAngle, bool grounded, bool mayRecenter)
        {
            Look = look;
            LookFromStick = lookFromStick;
            Recenter = recenter;
            RoverYaw = roverYaw;
            ForwardSpeed = forwardSpeed;
            Throttle = throttle;
            NormalizedSpeed = normalizedSpeed;
            DescentAngle = descentAngle;
            Grounded = grounded;
            MayRecenter = mayRecenter;
        }

        public Vector2 Look { get; }

        public bool LookFromStick { get; }

        public bool Recenter { get; }

        public float RoverYaw { get; }

        public float ForwardSpeed { get; }

        public float Throttle { get; }

        public float NormalizedSpeed { get; }

        public float DescentAngle { get; }

        public bool Grounded { get; }

        public bool MayRecenter { get; }
    }
}
