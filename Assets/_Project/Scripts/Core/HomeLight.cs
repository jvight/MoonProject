namespace MoonProject.Core
{
    /// <summary>Home's warm points, woken stage by stage as power returns (docs/features/M3-16, M3-19).</summary>
    public enum HomeLight
    {
        Dock = 0,
        Windows = 1,
        PorchLamps = 2,
        Halo = 3,
        BayLamps = 4,

        /// <summary>The belly lift's work lamp and the floor hatch's rim lamps (M3-19): they wake with home.</summary>
        LiftLamps = 5,

        /// <summary>The crew's lab: its wall and reading lamps and the big porthole's glow (M3-19 D4).</summary>
        LabLamps = 6,

        /// <summary>The survey chart's light table in the lab (M3-07, M3-19 D8): it wakes with tower L3.</summary>
        ChartTable = 7,
    }
}
