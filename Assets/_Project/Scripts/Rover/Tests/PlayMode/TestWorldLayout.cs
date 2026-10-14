using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// <see cref="IWorldLayout"/> stand-in: Earth straight ahead and high; the base at the origin unless a test moves
    /// it (to bring 07 "home").
    /// </summary>
    public sealed class TestWorldLayout : IWorldLayout
    {
        public Vector3 BasePosition { get; set; } = Vector3.zero;

        public Vector3 PeakPosition => new Vector3(0f, 120f, 280f);

        public Vector3 EarthDirection => new Vector3(0f, 0.6f, 0.8f);
    }

    /// <summary>
    /// <see cref="IWorldRegions"/> stand-in for rover tests: defaults to <see cref="RegionIds.Basin"/> with the
    /// standard <c>3200 m</c> far clip plane unless a test overrides <see cref="CurrentFarClipPlane"/>.
    /// </summary>
    public sealed class TestWorldRegions : IWorldRegions
    {
        public const float DefaultFarClipPlane = 3200f;

        public string CurrentRegionId { get; set; } = RegionIds.Basin;

        public float CurrentFarClipPlane { get; set; } = DefaultFarClipPlane;

        public float SampleDrainMultiplier(Vector3 position) => 1f;

        public float SampleSkyOcclusion(Vector3 position) => 0f;

        public float SampleWeight(string regionId, Vector3 position) =>
            regionId == CurrentRegionId ? 1f : 0f;

        public string SampleRegionId(Vector3 position) => CurrentRegionId;

        public bool TryGetRegion(string regionId, out RegionInfo info)
        {
            if (regionId == RegionIds.Basin || regionId == RegionIds.Canyon)
            {
                info = new RegionInfo(regionId, Vector3.zero, 420f, CurrentFarClipPlane);
                return true;
            }

            info = default;
            return false;
        }

        public int GetWeights(Vector3 position, RegionWeight[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return 0;
            }

            buffer[0] = new RegionWeight(CurrentRegionId, 1f);
            return 1;
        }
    }
}
