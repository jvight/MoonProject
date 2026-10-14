using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// Queries the world's named regions, their smooth blend weights at any world position, and the currently blended
    /// camera far clip / environmental modifiers (registered by WorldSystem). Allocation-free per frame.
    /// </summary>
    public interface IWorldRegions
    {
        /// <summary>The dominant region id where 07 currently stands (never null; starts at <see cref="RegionIds.Basin"/>).</summary>
        string CurrentRegionId { get; }

        /// <summary>Blended camera far clip plane (m) at 07's current position.</summary>
        float CurrentFarClipPlane { get; }

        /// <summary>Blended battery idle/drive drain multiplier at <paramref name="position"/> (&gt;1 in cold shadow).</summary>
        float SampleDrainMultiplier(Vector3 position);

        /// <summary>0..1 share of sky/radio occlusion at <paramref name="position"/> (1 inside roofed regions).</summary>
        float SampleSkyOcclusion(Vector3 position);

        /// <summary>0..1 smooth blend weight of <paramref name="regionId"/> at <paramref name="position"/>.</summary>
        float SampleWeight(string regionId, Vector3 position);

        /// <summary>Dominant region id at an arbitrary world <paramref name="position"/>.</summary>
        string SampleRegionId(Vector3 position);

        /// <summary>Looks up a region's static descriptor by <paramref name="regionId"/>.</summary>
        bool TryGetRegion(string regionId, out RegionInfo info);

        /// <summary>Writes active non-zero region weights at <paramref name="position"/> into <paramref name="buffer"/>.</summary>
        int GetWeights(Vector3 position, RegionWeight[] buffer);
    }
}
