namespace MoonProject.Core
{
    /// <summary>
    /// 07's systems as its boot readout lists them (docs/design/opening-v2.md §10), in the readout's order.
    /// </summary>
    public enum RoverSystem
    {
        Wheels = 0,
        Lamps = 1,
        Scan = 2,
        Assembly = 3,
        Cutter = 4,
        SolarWing = 5,
        Antenna = 6,
        Sockets = 7,
    }
}
