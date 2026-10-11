namespace MoonProject.Gameplay.Map
{
    /// <summary>What <see cref="DeckLook"/> reads each frame.</summary>
    public readonly struct DeckLookInput
    {
        public DeckLookInput(bool atDeck, bool sweepDone, bool lookingAtHatch, float stillSeconds, bool leave,
            float now)
        {
            AtDeck = atDeck;
            SweepDone = sweepDone;
            LookingAtHatch = lookingAtHatch;
            StillSeconds = stillSeconds;
            Leave = leave;
            Now = now;
        }

        /// <summary>The lift rests at the deck with 07 on it.</summary>
        public bool AtDeck { get; }

        /// <summary>Every place in the sweep has blinked.</summary>
        public bool SweepDone { get; }

        /// <summary>The view's centre is on the hatch window.</summary>
        public bool LookingAtHatch { get; }

        /// <summary>Seconds since the player last looked around (the lookout breathes while they rest).</summary>
        public float StillSeconds { get; }

        /// <summary>The player drives: leave the deck.</summary>
        public bool Leave { get; }

        public float Now { get; }
    }
}
