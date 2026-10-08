namespace MoonProject.Gameplay
{
    /// <summary>
    /// What the player can do right now and where, registered in the GameContext for the UI's context prompts.
    /// Read-only and allocation-free: poll it every frame. Which prompts to show, and only the first few times,
    /// is the UI's call.
    /// </summary>
    public interface IInteractionHints
    {
        /// <summary>
        /// The most relevant action now, in this order: Deposit, Repair, Restore, Tune, Hop, Upgrade, Excavate, Tether,
        /// Reel, Ping
        /// (<see cref="InteractionKind.None"/> before the game is running).
        /// </summary>
        InteractionHint Primary { get; }

        /// <summary>True (with where and whether it is ready) when <paramref name="kind"/> is available now.</summary>
        bool TryGet(InteractionKind kind, out InteractionHint hint);
    }
}
