using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Tuning for the world's regions: dominant-region hysteresis and per-region atmosphere/volume profiles.
    /// </summary>
    [Serializable]
    public sealed class RegionSettings
    {
        public const float DefaultHysteresisThreshold = 0.55f;

        [Tooltip("Blend weight (0.5..1) a new region must reach before replacing the current dominant region.")]
        [Range(0.5f, 0.95f)]
        [SerializeField] private float _hysteresisThreshold = DefaultHysteresisThreshold;

        [Tooltip("Home basin fallback region profile (Look A).")]
        [SerializeField] private RegionAtmosphereProfile _basin = RegionAtmosphereProfile.CreateBasin();

        [Tooltip("Whispering Canyon region profile (Look A).")]
        [SerializeField] private RegionAtmosphereProfile _canyon = RegionAtmosphereProfile.CreateCanyon();

        public RegionSettings()
        {
        }

        public RegionSettings(RegionAtmosphereProfile basin, RegionAtmosphereProfile canyon,
            float hysteresisThreshold = DefaultHysteresisThreshold)
        {
            _basin = basin ?? throw new ArgumentNullException(nameof(basin));
            _canyon = canyon ?? throw new ArgumentNullException(nameof(canyon));
            _hysteresisThreshold = hysteresisThreshold;
        }

        public float HysteresisThreshold => _hysteresisThreshold;

        public RegionAtmosphereProfile Basin => _basin;

        public RegionAtmosphereProfile Canyon => _canyon;

        /// <summary>Looks up a configured profile by <paramref name="regionId"/>.</summary>
        public bool TryGetProfile(string regionId, out RegionAtmosphereProfile profile)
        {
            if (regionId == RegionIds.Basin)
            {
                profile = _basin;
                return true;
            }

            if (regionId == RegionIds.Canyon)
            {
                profile = _canyon;
                return true;
            }

            profile = null;
            return false;
        }

        /// <summary>Returns null when valid, otherwise a description of the first problem found.</summary>
        public string Validate()
        {
            if (_hysteresisThreshold < 0.5f || _hysteresisThreshold >= 1f)
            {
                return "Regions: hysteresisThreshold must lie in [0.5, 1).";
            }

            if (_basin == null || _basin.RegionId != RegionIds.Basin)
            {
                return "Regions: basin profile must have regionId 'basin'.";
            }

            if (_canyon == null || _canyon.RegionId != RegionIds.Canyon)
            {
                return "Regions: canyon profile must have regionId 'canyon'.";
            }

            return _basin.Validate() ?? _canyon.Validate();
        }
    }
}
