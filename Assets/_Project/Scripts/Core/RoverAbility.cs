namespace MoonProject.Core
{
    /// <summary>Rover abilities unlocked through the workshop (see docs/DESIGN.md "Workshop").</summary>
    public enum RoverAbility
    {
        HoverJump = 0,
        MagneticTreads = 1,
        CargoCradle = 2,
        WideSonar = 3,
        WarmHeadlamp = 4,
        BoostCoils = 5,

        /// <summary>Cell Pack II: two more cells (4 to 6) and the battery spine on 07's back (docs/features/M3-20).</summary>
        CellPack = 6,
    }
}
