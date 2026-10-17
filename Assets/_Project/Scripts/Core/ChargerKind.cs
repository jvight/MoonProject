namespace MoonProject.Core
{
    /// <summary>What 07 is charging at (docs/features/M3-20 "Chargers").</summary>
    public enum ChargerKind
    {
        /// <summary>The charging dock at home: a full charge inside the dock's evening.</summary>
        Dock = 0,

        /// <summary>A lit relay mast's 4 m charge pad.</summary>
        Mast = 1,

        /// <summary>A charging pad 07 built (opening-v2 B5): fed by the lander's reserve, then by the base's power.</summary>
        Pad = 2,
    }
}
