namespace MoonProject.Gameplay.Lift
{
    /// <summary>What the lift's cycle reads each frame from the world around it.</summary>
    public readonly struct LiftInput
    {
        public LiftInput(bool powered, bool carryingHome, bool atStitchSpot, bool onPlatform, float stillSeconds,
            bool down)
        {
            Powered = powered;
            CarryingHome = carryingHome;
            AtStitchSpot = atStitchSpot;
            OnPlatform = onPlatform;
            StillSeconds = stillSeconds;
            Down = down;
        }

        /// <summary>The base power has reached home (tower L1).</summary>
        public bool Powered { get; }

        /// <summary>07 is home carrying something for the lab: a relic.</summary>
        public bool CarryingHome { get; }

        /// <summary>07 rests by the jammed platform, at the spot it stitches the cable from.</summary>
        public bool AtStitchSpot { get; }

        /// <summary>07 is on the platform's rover spot, to ride it.</summary>
        public bool OnPlatform { get; }

        /// <summary>Seconds since 07 last moved or the player gave input.</summary>
        public float StillSeconds { get; }

        /// <summary>The player asked to go down and nothing in the lab still needs 07 there.</summary>
        public bool Down { get; }
    }
}
