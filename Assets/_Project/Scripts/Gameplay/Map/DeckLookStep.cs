namespace MoonProject.Gameplay.Map
{
    /// <summary>What one step of <see cref="DeckLook"/> started or finished (the map station plays these as beats).</summary>
    public enum DeckLookStep
    {
        None = 0,

        /// <summary>07 settled on the deck: run the sweep.</summary>
        SweepStarted = 1,

        /// <summary>The first sweep ended: the hatch swings open for good (save it).</summary>
        HatchOpened = 2,

        /// <summary>The view eases into the map: show it and start the chart pen.</summary>
        LookStarted = 3,

        /// <summary>The player left the map: hide it; the lift takes 07 down.</summary>
        LookEnded = 4,

        /// <summary>The player left before the map opened (or the lift moved): let the deck go.</summary>
        Released = 5,
    }
}
