namespace MoonProject.Core
{
    /// <summary>
    /// How far power has come back through the base (docs/features/M3-16, M3-19 D8): each radio tower level wakes one
    /// more stage. A fresh game is <see cref="Asleep"/>: only 07's charging dock glows.
    /// </summary>
    public enum BasePowerStage
    {
        Asleep = 0,

        /// <summary>Tower L1: home and the crew's lab under the belly lift.</summary>
        Home = 1,

        /// <summary>Tower L2: Kenji's Rover Bay and its plotter.</summary>
        Bay = 2,

        /// <summary>Tower L3: the lab's survey chart (the light table and the chart pen).</summary>
        Chart = 3,
    }
}
