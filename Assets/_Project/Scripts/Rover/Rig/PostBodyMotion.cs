namespace MoonProject.Rover
{
    /// <summary>
    /// The self-test's motion on 07's running gear: the motor's shudder and lurch, and each wheel's twitch (deg).
    /// Set by the opening, applied by <see cref="RoverVisualRig"/> on top of the ordinary suspension and spin.
    /// </summary>
    public struct PostBodyMotion
    {
        /// <summary>The chassis' vertical shake (m).</summary>
        public float Shudder;

        /// <summary>The chassis' forward lurch (m).</summary>
        public float Lurch;

        public float Wheel0;
        public float Wheel1;
        public float Wheel2;
        public float Wheel3;

        /// <summary>Wheel <paramref name="index"/>'s twitch (deg).</summary>
        public float Wheel(int index)
        {
            switch (index)
            {
                case 0: return Wheel0;
                case 1: return Wheel1;
                case 2: return Wheel2;
                default: return Wheel3;
            }
        }
    }
}
