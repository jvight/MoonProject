namespace MoonProject.Core
{
    /// <summary>
    /// The session's flow, owned and registered by App (docs/features/M3-09). Every boot starts in
    /// <see cref="GameFlowState.Title"/> over the loaded world, unless the player just chose New Game, which boots
    /// straight into play. Gameplay, Rover and UI read <see cref="State"/> to hold input, the HUD and the wake while
    /// the title is up.
    /// </summary>
    public interface IGameFlow
    {
        GameFlowState State { get; }

        /// <summary>True when this boot loaded a save the player can continue (the title offers Continue).</summary>
        bool HasSave { get; }
    }
}
