namespace MoonProject.Core
{
    /// <summary>
    /// Exposes the dominant region where 07 currently stands and the blended camera far clip plane
    /// at 07's position (registered by WorldSystem).
    /// </summary>
    public interface IWorldRegions
    {
        /// <summary>The dominant region id where 07 currently stands (never null; starts at <see cref="RegionIds.Basin"/>).</summary>
        string CurrentRegionId { get; }

        /// <summary>Blended camera far clip plane (m) at 07's current position.</summary>
        float CurrentFarClipPlane { get; }
    }
}
