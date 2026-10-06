using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Chooses where each relic is buried, deterministically from the tuning seed and the analytic surface:
    /// onboarding relics in a fan ahead of 07's spawn heading close to home, wanderers spread evenly around the basin
    /// at mid range, and the rim relic near the playable edge toward The Peak on the spot with the clearest view of
    /// it. Every site is flat enough to park over, on drivable floor away from its edge, away from home and from the
    /// others.
    /// Runs once at initialisation (allocates); throws if the surface leaves no valid spot (broken world tuning).
    /// </summary>
    public static class RelicSitePlanner
    {
        /// <summary>Spacing is relaxed by this factor when no spot satisfies it (a crowded or rugged world).</summary>
        private const float RelaxedSpacingFactor = 0.6f;

        /// <summary>Metres per step when marching out to the drivable edge.</summary>
        private const float ReachStep = 2f;

        public static RelicSite[] Plan(ITerrainQuery terrain, IWorldLayout layout, RelicPlacementTuning tuning,
            IReadOnlyList<RelicPlacementBand> bands)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            if (bands == null)
            {
                throw new ArgumentNullException(nameof(bands));
            }

            var planner = new Planner(terrain, layout, tuning);
            var sites = new RelicSite[bands.Count];

            // Close sites first (they matter most for the first minutes), then the rim view, then the wanderers,
            // which spread into whatever directions are left.
            PlaceBand(planner, bands, RelicPlacementBand.Onboarding, sites);
            PlaceBand(planner, bands, RelicPlacementBand.RimView, sites);
            PlaceBand(planner, bands, RelicPlacementBand.Wanderer, sites);
            return sites;
        }

        private static void PlaceBand(Planner planner, IReadOnlyList<RelicPlacementBand> bands,
            RelicPlacementBand band, RelicSite[] sites)
        {
            int count = 0;
            for (int i = 0; i < bands.Count; i++)
            {
                if (bands[i] == band)
                {
                    count++;
                }
            }

            int ordinal = 0;
            for (int i = 0; i < bands.Count; i++)
            {
                if (bands[i] != band)
                {
                    continue;
                }

                sites[i] = planner.Place(band, ordinal, count);
                ordinal++;
            }
        }

        private sealed class Planner
        {
            private readonly ITerrainQuery _terrain;
            private readonly IWorldLayout _layout;
            private readonly RelicPlacementTuning _tuning;
            private readonly List<Vector3> _placed = new List<Vector3>();
            private readonly float _minNormalY;
            private readonly float _wandererOffset;
            private readonly float _maxReach;
            private DeterministicRandom _random;

            public Planner(ITerrainQuery terrain, IWorldLayout layout, RelicPlacementTuning tuning)
            {
                _terrain = terrain;
                _layout = layout;
                _tuning = tuning;
                _random = new DeterministicRandom(tuning.Seed);
                _minNormalY = SurfaceRules.MinNormalY(tuning.MaxSlopeDegrees);
                _wandererOffset = _random.Range(0f, 360f);
                Rect area = terrain.PlayableArea;
                _maxReach = 2f * Mathf.Max(area.width, area.height);
            }

            public RelicSite Place(RelicPlacementBand band, int ordinal, int count)
            {
                float spacing = _tuning.MinSiteSpacing;
                for (int pass = 0; pass < 2; pass++)
                {
                    if (TryPlace(band, ordinal, count, spacing, out RelicSite site))
                    {
                        _placed.Add(site.Position);
                        return site;
                    }

                    spacing *= RelaxedSpacingFactor;
                }

                throw new InvalidOperationException(
                    $"{nameof(RelicSitePlanner)}: no flat, free, drivable spot for {band} relic {ordinal + 1}/{count}. " +
                    $"Check the world surface or {nameof(RelicPlacementTuning)}.");
            }

            private bool TryPlace(RelicPlacementBand band, int ordinal, int count, float spacing, out RelicSite site)
            {
                Vector3 basePosition = _layout.BasePosition;
                bool found = false;
                float bestScore = float.MinValue;
                site = default;
                for (int attempt = 0; attempt < _tuning.AttemptsPerSite; attempt++)
                {
                    Vector3 direction;
                    float distance;
                    switch (band)
                    {
                        case RelicPlacementBand.Onboarding:
                        {
                            float slot = count == 1 ? 0f : (ordinal + 0.5f) / count * 2f - 1f;
                            float jitter = _tuning.OnboardingSpread / Mathf.Max(1, count);
                            float bearing = _tuning.OnboardingBearing + slot * _tuning.OnboardingSpread +
                                            _random.Range(-jitter, jitter);
                            direction = SurfaceRules.BearingDirection(bearing);
                            Vector2 range = _tuning.OnboardingDistance;
                            distance = _random.Range(range.x, range.y);
                            break;
                        }

                        case RelicPlacementBand.RimView:
                        {
                            Vector3 toPeak = _layout.PeakPosition - basePosition;
                            float bearing = SurfaceRules.Bearing(toPeak) +
                                            _random.Range(-_tuning.RimBearingSpread, _tuning.RimBearingSpread);
                            direction = SurfaceRules.BearingDirection(bearing);
                            float reach = SurfaceRules.DrivableReach(_terrain, basePosition, direction,
                                _tuning.EdgeMargin, ReachStep, _maxReach);
                            Vector2 fraction = _tuning.RimReach;
                            distance = reach * _random.Range(fraction.x, fraction.y);
                            break;
                        }

                        default:
                        {
                            float sector = 360f / Mathf.Max(1, count);
                            float bearing = _wandererOffset + ordinal * sector +
                                            _random.Range(-sector * 0.35f, sector * 0.35f);
                            direction = SurfaceRules.BearingDirection(bearing);
                            Vector2 range = _tuning.WandererDistance;
                            distance = _random.Range(range.x, range.y);
                            break;
                        }
                    }

                    float x = basePosition.x + direction.x * distance;
                    float z = basePosition.z + direction.z * distance;
                    if (!IsValid(x, z, spacing))
                    {
                        continue;
                    }

                    Vector3 candidate = SurfaceRules.OnSurface(_terrain, x, z);
                    if (band != RelicPlacementBand.RimView)
                    {
                        site = new RelicSite(candidate, _terrain.SampleNormal(x, z));
                        return true;
                    }

                    float score = SightClearance(candidate);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        site = new RelicSite(candidate, _terrain.SampleNormal(x, z));
                        found = true;
                    }
                }

                return found;
            }

            private bool IsValid(float x, float z, float spacing)
            {
                if (!SurfaceRules.InsideDrivable(_terrain, x, z, _tuning.EdgeMargin))
                {
                    return false;
                }

                var flat = new Vector3(x, 0f, z);
                if (SurfaceRules.HorizontalDistance(flat, _layout.BasePosition) < _tuning.BaseClearance)
                {
                    return false;
                }

                for (int i = 0; i < _placed.Count; i++)
                {
                    if (SurfaceRules.HorizontalDistance(flat, _placed[i]) < spacing)
                    {
                        return false;
                    }
                }

                return SurfaceRules.IsFlat(_terrain, x, z, _minNormalY, _tuning.FlatnessProbeRadius);
            }

            /// <summary>
            /// Smallest height (m) of the eye-to-summit line above the ground along the checked part of the line:
            /// positive means The Peak is in clear view.
            /// </summary>
            private float SightClearance(Vector3 site)
            {
                Vector3 eye = site + Vector3.up * _tuning.RimEyeHeight;
                Vector3 summit = _layout.PeakPosition;
                int samples = _tuning.RimSightSamples;
                float clearance = float.MaxValue;
                for (int i = 1; i <= samples; i++)
                {
                    float t = _tuning.RimSightReach * i / samples;
                    Vector3 point = Vector3.Lerp(eye, summit, t);
                    clearance = Mathf.Min(clearance, point.y - _terrain.SampleHeight(point.x, point.z));
                }

                return clearance;
            }
        }
    }
}
