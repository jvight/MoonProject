namespace MoonProject.Core
{
    /// <summary>The databank's categories (docs/design/databank.md §1), in the order 07's page lists them.</summary>
    public enum DatabankCategory
    {
        /// <summary>Rovers, landers, satellites, Meridian machines and machine life.</summary>
        Machines = 0,

        /// <summary>Ruins, instruments, masts and dishes.</summary>
        Structures = 1,

        /// <summary>Sites and regions, once reached.</summary>
        Places = 2,

        /// <summary>The eclipse, earthshine, quakes, flashes, dust glow (and rare life until it has its own).</summary>
        Phenomena = 3,

        /// <summary>People and documents as scanned prints: maker cards, posters, the crew roster, letters.</summary>
        Archive = 4,
    }
}
