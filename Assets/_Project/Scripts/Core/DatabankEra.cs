namespace MoonProject.Core
{
    /// <summary>The era a databank entry's subject belongs to (docs/design/tech-eras.md §1).</summary>
    public enum DatabankEra
    {
        None = 0,

        /// <summary>P · Probes, 1966–68.</summary>
        Probes = 1,

        /// <summary>S · Sorties, 1969–77.</summary>
        Sorties = 2,

        /// <summary>B · Build-out, 1978–83.</summary>
        BuildOut = 3,

        /// <summary>L · Lumen, 1984–88.</summary>
        Lumen = 4,

        /// <summary>M · Meridian, 1988.</summary>
        Meridian = 5,

        /// <summary>N · Now, 2024.</summary>
        Now = 6,
    }
}
