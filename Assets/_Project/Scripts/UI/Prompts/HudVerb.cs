namespace MoonProject.UI
{
    /// <summary>
    /// A verb the HUD keeps track of meeting in this save (docs/features/M3-16: the HUD reveals itself one verb at a
    /// time): most HUD pieces show only once their verb is met; the lab's first-time prompts (M3-19) show only until it
    /// is. Saved by name in the prompts ledger.
    /// </summary>
    internal enum HudVerb
    {
        Material = 0,
        LitMast = 1,
        Tape = 2,

        /// <summary>The player turned the lab's turntable with the stick.</summary>
        LabTurn = 3,

        /// <summary>The player looked at a lab station.</summary>
        LabLook = 4,

        /// <summary>The player brought the belly lift down (or came away from a look).</summary>
        LabDown = 5,
    }
}
