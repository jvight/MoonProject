namespace MoonProject.Gameplay.Lift
{
    /// <summary>What the lift's cycle reads each frame from the world around it.</summary>
    public readonly struct LiftInput
    {
        public LiftInput(bool powered, bool affordable, bool onFeedPad, bool onPlatform, float stillSeconds,
            bool feedDone, bool anyInput)
        {
            Powered = powered;
            Affordable = affordable;
            OnFeedPad = onFeedPad;
            OnPlatform = onPlatform;
            StillSeconds = stillSeconds;
            FeedDone = feedDone;
            AnyInput = anyInput;
        }

        /// <summary>The base power has reached the lift stage.</summary>
        public bool Powered { get; }

        /// <summary>07 carries the winch's recipe.</summary>
        public bool Affordable { get; }

        /// <summary>07 is on the winch's feed pad.</summary>
        public bool OnFeedPad { get; }

        /// <summary>07 is on the platform's rover spot.</summary>
        public bool OnPlatform { get; }

        /// <summary>Seconds since 07 last moved or the player gave input.</summary>
        public float StillSeconds { get; }

        /// <summary>The hopper feed finished this frame.</summary>
        public bool FeedDone { get; }

        /// <summary>The player gave drive or look input this frame.</summary>
        public bool AnyInput { get; }
    }
}
