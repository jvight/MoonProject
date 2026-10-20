namespace MoonProject.Gameplay
{
    /// <summary>What the winch does about a towed load this physics step (<see cref="TowSnag"/>).</summary>
    public enum TowSnagAction
    {
        /// <summary>Nothing: the load follows, or has only just caught.</summary>
        None = 0,

        /// <summary>The load is caught: lift it up and a little toward the winch to free it.</summary>
        LiftFree = 1,

        /// <summary>The load fell too far behind: unhook it softly where it is.</summary>
        LetGo = 2,
    }
}
