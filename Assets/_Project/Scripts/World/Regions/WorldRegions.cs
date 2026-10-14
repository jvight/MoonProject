using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// Evaluates region volumes, smooth signed-distance seam weights, dominant-region hysteresis, and
    /// <see cref="RegionEntered"/> events. Allocation-free per frame.
    /// </summary>
    public sealed class WorldRegions : IWorldRegions
    {
        private readonly RegionSlot[] _slots;
        private readonly float[] _currentWeights;
        private readonly float _hysteresisThreshold;
        private readonly float _fallbackBlendBand;
        private readonly int _fallbackIndex;
        private EventBus _events;
        private int _currentRegionIndex;
        private bool _hasPlaced;

        public WorldRegions(IWorldAnchors anchors, RegionSettings settings, EventBus events = null)
        {
            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            RegionAtmosphereProfile basin = settings.Basin;
            RegionAtmosphereProfile canyon = settings.Canyon;
            RegionVolume canyonVolume = RegionVolume.CreateCanyon(anchors, canyon.OuterRadius, canyon.BlendBand, 1);
            RegionVolume basinVolume = RegionVolume.CreateDisc(RegionIds.Basin, Vector3.zero, basin.OuterRadius,
                basin.BlendBand, 0, true);

            _slots = new[]
            {
                new RegionSlot(canyonVolume, canyon),
                new RegionSlot(basinVolume, basin),
            };
            _fallbackIndex = 1;
            _fallbackBlendBand = basinVolume.BlendBand;
            _hysteresisThreshold = settings.HysteresisThreshold;
            _currentWeights = new float[_slots.Length];
            _currentRegionIndex = _fallbackIndex;
            _events = events;
            EvaluateWeights(Vector3.zero, _currentWeights);
            CurrentFarClipPlane = BlendFarClip(_currentWeights);
        }

        public string CurrentRegionId => _slots[_currentRegionIndex].Profile.RegionId;

        public float CurrentFarClipPlane { get; private set; }

        /// <summary>Attaches the event bus used to publish <see cref="RegionEntered"/> transitions.</summary>
        public void BindEvents(EventBus events)
        {
            _events = events;
        }

        /// <summary>
        /// Steps the rover's current position during normal driving: blends weights, updates
        /// <see cref="CurrentFarClipPlane"/>, applies hysteresis (<c>&gt;= 0.55</c>), and publishes
        /// <see cref="RegionEntered"/> with <c>placed = false</c> when the dominant region changes.
        /// </summary>
        public void Step(Vector3 position)
        {
            EvaluateWeights(position, _currentWeights);
            CurrentFarClipPlane = BlendFarClip(_currentWeights);
            _hasPlaced = true;

            int candidate = DominantIndex(_currentWeights);
            if (candidate != _currentRegionIndex && _currentWeights[candidate] >= _hysteresisThreshold)
            {
                string previous = _slots[_currentRegionIndex].Profile.RegionId;
                _currentRegionIndex = candidate;
                _events?.Publish(new RegionEntered(_slots[_currentRegionIndex].Profile.RegionId, previous, false));
            }
        }

        /// <summary>
        /// Snaps the rover's current position on placement (boot, respawn, or unstuck): evaluates dominant region
        /// without hysteresis (<c>&gt; 0.5</c>) and publishes <see cref="RegionEntered"/> with <c>placed = true</c>
        /// on initial placement or when the dominant region changes.
        /// </summary>
        public void PlaceAt(Vector3 position)
        {
            EvaluateWeights(position, _currentWeights);
            CurrentFarClipPlane = BlendFarClip(_currentWeights);

            int dominant = DominantIndex(_currentWeights);
            bool changed = !_hasPlaced || dominant != _currentRegionIndex;
            string previous = _hasPlaced ? _slots[_currentRegionIndex].Profile.RegionId : string.Empty;
            _currentRegionIndex = dominant;
            _hasPlaced = true;
            if (changed)
            {
                _events?.Publish(new RegionEntered(_slots[_currentRegionIndex].Profile.RegionId, previous, true));
            }
        }

        /// <summary>0..1 smooth blend weight of <paramref name="regionId"/> at <paramref name="position"/>.</summary>
        public float SampleWeight(string regionId, Vector3 position)
        {
            int index = FindSlotIndex(regionId);
            if (index < 0)
            {
                return 0f;
            }

            Span<float> weights = stackalloc float[_slots.Length];
            EvaluateWeights(position, weights);
            return weights[index];
        }

        /// <summary>Dominant region id at an arbitrary world <paramref name="position"/>.</summary>
        public string SampleRegionId(Vector3 position)
        {
            Span<float> weights = stackalloc float[_slots.Length];
            EvaluateWeights(position, weights);
            return _slots[DominantIndex(weights)].Profile.RegionId;
        }

        /// <summary>True when <paramref name="regionId"/> is a known region in this world.</summary>
        public bool HasRegion(string regionId)
        {
            return FindSlotIndex(regionId) >= 0;
        }

        /// <summary>Evaluates normalised region weights at <paramref name="position"/> into <paramref name="weights"/>.</summary>
        internal void EvaluateWeights(Vector3 position, Span<float> weights)
        {
            float remaining = 1f;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (i == _fallbackIndex)
                {
                    continue;
                }

                RegionVolume volume = _slots[i].Volume;
                float sd = volume.SignedDistance(position);
                float maxBand = 2f * _slots[i].Profile.OuterRadius;
                float band = Mathf.Max(Mathf.Min(maxBand, volume.BlendBand + _fallbackBlendBand), 1e-4f);
                float t = Mathf.Clamp01(0.5f - sd / band);
                float raw = t * t * (3f - 2f * t);
                float weight = raw * remaining;
                weights[i] = weight;
                remaining = Mathf.Max(0f, remaining - weight);
            }

            weights[_fallbackIndex] = remaining;
        }

        internal RegionAtmosphereProfile ProfileAt(int slotIndex)
        {
            return _slots[slotIndex].Profile;
        }

        internal int SlotCount => _slots.Length;

        private float BlendFarClip(ReadOnlySpan<float> weights)
        {
            float farClip = 0f;
            for (int i = 0; i < _slots.Length; i++)
            {
                farClip += weights[i] * _slots[i].Profile.FarClipPlane;
            }

            return farClip;
        }

        private int DominantIndex(ReadOnlySpan<float> weights)
        {
            int best = _fallbackIndex;
            float bestWeight = weights[_fallbackIndex];
            for (int i = 0; i < _slots.Length; i++)
            {
                if (weights[i] > bestWeight)
                {
                    bestWeight = weights[i];
                    best = i;
                }
            }

            return best;
        }

        private int FindSlotIndex(string regionId)
        {
            if (string.IsNullOrEmpty(regionId))
            {
                return -1;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Profile.RegionId == regionId)
                {
                    return i;
                }
            }

            return -1;
        }

        private readonly struct RegionSlot
        {
            public RegionVolume Volume { get; }
            public RegionAtmosphereProfile Profile { get; }

            public RegionSlot(RegionVolume volume, RegionAtmosphereProfile profile)
            {
                Volume = volume;
                Profile = profile;
            }
        }
    }
}
