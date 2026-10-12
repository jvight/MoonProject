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

        /// <summary>
        /// The tether (lift, tow, reel, place): Kenji's spare tether ring, fitted at his garage port
        /// (docs/design/first-hour-v2.md §3). Asleep until then.
        /// </summary>
        Tether = 7,

        /// <summary>
        /// The Side Bins (docs/features/M3-25 §2.2): two low panniers between the wheels, four more slots in the hold.
        /// </summary>
        SideBins = 8,

        /// <summary>
        /// The Focus Lens (docs/features/M3-26 §3): a ringed lens on the emitter; cutter tier 1, a shorter charge, a
        /// quicker and cheaper cut.
        /// </summary>
        FocusLens = 9,
    }
}
