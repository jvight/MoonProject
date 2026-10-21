namespace MoonProject.Gameplay
{
    /// <summary>What the winch does about a towed load this physics step (<see cref="TowSnag"/>).</summary>
    public enum TowSnagAction
    {
        /// <summary>Nothing: the load follows, or has only just caught.</summary>
        None = 0,

        /// <summary>The load is caught: lift it a little toward the winch and pay out slack; the hook stays on.</summary>
        LiftFree = 1,

        /// <summary>The cable snapped: 07 was pulled far past all its slack (off an edge).</summary>
        LetGo = 2,
    }
}
