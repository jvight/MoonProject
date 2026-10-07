namespace MoonProject.Core
{
    /// <summary>The three detents of Bell's radio dial (docs/features/M3-05). Values are saved: never renumber.</summary>
    public enum RadioChannel
    {
        /// <summary>Ro's show: every owned track shuffled (the base tracks plus collected tapes). The default.</summary>
        LumenAfterDark = 0,

        /// <summary>One chosen cassette on repeat.</summary>
        TapeDeck = 1,

        /// <summary>No music: only the moon's ambience and the occasional ticker line.</summary>
        QuietHours = 2,
    }
}
