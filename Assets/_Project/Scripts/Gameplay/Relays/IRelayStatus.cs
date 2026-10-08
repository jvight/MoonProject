namespace MoonProject.Gameplay
{
    /// <summary>
    /// The relay network as the UI shows it (the restore prompt's cost and hold, the pause line "Relays 2/4"),
    /// registered in the GameContext. Allocation-free: poll it every frame.
    /// </summary>
    public interface IRelayStatus
    {
        /// <summary>Relay masts in the game.</summary>
        int MastCount { get; }

        /// <summary>Masts restored and linked to home.</summary>
        int LitMasts { get; }

        /// <summary>The next restoration's recipe (escalating with each one).</summary>
        Recipe NextCost { get; }

        /// <summary>0..1 how far Interact has been held toward starting a restoration.</summary>
        float RestoreHold { get; }
    }
}
