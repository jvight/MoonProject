namespace MoonProject.World
{
    /// <summary>
    /// Home's warm points, each woken by a base power stage (docs/features/M3-16): the dock glows from the start, the
    /// lander's windows, porch lamps and halo wake with Home, the bay's lamps with Bay, the lift's lamps with Lift.
    /// </summary>
    public enum HomeLight
    {
        Dock = 0,
        Windows = 1,
        PorchLamps = 2,
        Halo = 3,
        BayLamps = 4,
        LiftLamps = 5,
    }
}
