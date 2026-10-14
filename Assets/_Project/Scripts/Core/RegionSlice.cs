using System;
using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// A region identifier and its normalised blend weight in <c>[0, 1]</c> at a sampled position.
    /// </summary>
    public readonly struct RegionWeight
    {
        /// <summary>Stable region identifier (<see cref="RegionIds"/>).</summary>
        public string RegionId { get; }

        /// <summary>Normalised blend weight in <c>[0, 1]</c>.</summary>
        public float Weight { get; }

        /// <summary>Creates a region blend weight entry.</summary>
        public RegionWeight(string regionId, float weight)
        {
            RegionId = regionId ?? throw new ArgumentNullException(nameof(regionId));
            Weight = weight;
        }
    }

    /// <summary>
    /// Static metadata for a named world region, published by <see cref="IWorldRegions"/>.
    /// </summary>
    public readonly struct RegionInfo
    {
        /// <summary>Stable region identifier (<see cref="RegionIds"/>).</summary>
        public string Id { get; }

        /// <summary>Representative world-space centre of the region.</summary>
        public Vector3 Centre { get; }

        /// <summary>Bounding outer radius in metres around <see cref="Centre"/>.</summary>
        public float OuterRadius { get; }

        /// <summary>Recommended camera far clip plane in metres while inside this region.</summary>
        public float FarClipPlane { get; }

        /// <summary>Multiplier on ambient battery drain inside this region (<c>1f</c> in standard regions).</summary>
        public float AmbientDrainMultiplier { get; }

        /// <summary>True when the region's ceiling blocks sky visibility and direct radio reach.</summary>
        public bool BlocksSkyAndRadio { get; }

        /// <summary>Creates metadata for a world region.</summary>
        public RegionInfo(string id, Vector3 centre, float outerRadius, float farClipPlane,
            float ambientDrainMultiplier = 1f, bool blocksSkyAndRadio = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Region id must not be empty.", nameof(id));
            }

            Id = id;
            Centre = centre;
            OuterRadius = outerRadius;
            FarClipPlane = farClipPlane;
            AmbientDrainMultiplier = ambientDrainMultiplier;
            BlocksSkyAndRadio = blocksSkyAndRadio;
        }
    }
}
