namespace MoonProject.UI
{
    /// <summary>
    /// A verb whose HUD piece shows only once the player has met it in this save (docs/features/M3-16: the HUD
    /// reveals itself one verb at a time). Saved by name in the prompts ledger.
    /// </summary>
    internal enum HudVerb
    {
        Material = 0,
        Ping = 1,
        LitMast = 2,
        Tape = 3,
    }
}
