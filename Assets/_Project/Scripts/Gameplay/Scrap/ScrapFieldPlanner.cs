using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Scatters the scrap field, deterministically from the tuning seed: a welcome ring of clusters around home,
    /// trails of clusters along the gentle paths from home toward every relic site and The Peak, clusters around
    /// each site, then lone clusters over the basin, topped up until the field holds the minimum total value.
    /// Pieces lie only on drivable, gentle ground, away from the lander yard and the dig spots. Runs once at
    /// initialisation (allocates); throws when the world leaves no room for the minimum value (broken tuning).
    /// </summary>
    public static class ScrapFieldPlanner
    {
        /// <summary>Retries for a ring or point-of-interest cluster whose first spot is rejected.</summary>
        private const int ClusterRetries = 5;

        /// <summary>Tries per piece inside a cluster before the cluster settles for fewer pieces.</summary>
        private const int PieceAttempts = 6;

        /// <summary>Metres per step when marching toward the drivable edge for The Peak's trail.</summary>
        private const float ReachStep = 2f;

        public static List<ScrapSpawn> Plan(ITerrainQuery terrain, IWorldLayout layout, ScrapTuning tuning,
            IReadOnlyList<RelicSite> sites, IReadOnlyList<ScrapVariant> variants)
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

            if (sites == null)
            {
                throw new ArgumentNullException(nameof(sites));
            }

            if (variants == null || variants.Count == 0)
            {
                throw new ArgumentException("At least one scrap variant is needed.", nameof(variants));
            }

            var field = new Field(terrain, layout, tuning, sites, variants);
            field.PlaceBaseRing();
            field.PlaceTrails();
            field.PlacePointsOfInterest();
            field.PlaceFill(tuning.FillClusters, tuning.FillAttempts);
            field.TopUp();
            return field.Spawns;
        }

        /// <summary>Total wallet value of <paramref name="spawns"/>.</summary>
        public static int TotalValue(IReadOnlyList<ScrapSpawn> spawns, IReadOnlyList<ScrapVariant> variants)
        {
            int total = 0;
            for (int i = 0; i < spawns.Count; i++)
            {
                total += variants[spawns[i].Variant].Value;
            }

            return total;
        }

        private sealed class Field
        {
            private readonly ITerrainQuery _terrain;
            private readonly IWorldLayout _layout;
            private readonly ScrapTuning _tuning;
            private readonly IReadOnlyList<RelicSite> _sites;
            private readonly IReadOnlyList<ScrapVariant> _variants;
            private readonly List<Vector3> _clusters = new List<Vector3>();
            private readonly float _minNormalY;
            private readonly float _totalWeight;
            private readonly float _fillHalfSize;
            private DeterministicRandom _random;

            public Field(ITerrainQuery terrain, IWorldLayout layout, ScrapTuning tuning,
                IReadOnlyList<RelicSite> sites, IReadOnlyList<ScrapVariant> variants)
            {
                _terrain = terrain;
                _layout = layout;
                _tuning = tuning;
                _sites = sites;
                _variants = variants;
                _random = new DeterministicRandom(tuning.Seed);
                _minNormalY = SurfaceRules.MinNormalY(tuning.MaxSlopeDegrees);
                for (int i = 0; i < variants.Count; i++)
                {
                    _totalWeight += variants[i].Weight;
                }

                // The playable rectangle is inscribed in the floor; its circumscribing square covers the whole floor.
                Rect area = terrain.PlayableArea;
                _fillHalfSize = 0.75f * Mathf.Max(area.width, area.height);
            }

            public List<ScrapSpawn> Spawns { get; } = new List<ScrapSpawn>();

            public int TotalValue { get; private set; }

            public void PlaceBaseRing()
            {
                int count = _tuning.BaseRingClusters;
                float offset = _random.Range(0f, 360f);
                for (int i = 0; i < count; i++)
                {
                    float sector = 360f / count;
                    for (int attempt = 0; attempt < ClusterRetries; attempt++)
                    {
                        float bearing = offset + i * sector + _random.Range(-0.3f, 0.3f) * sector;
                        Vector2 ring = _tuning.BaseRing;
                        if (TryCluster(Around(_layout.BasePosition, bearing, _random.Range(ring.x, ring.y))))
                        {
                            break;
                        }
                    }
                }
            }

            public void PlaceTrails()
            {
                Vector3 home = _layout.BasePosition;
                for (int i = 0; i < _sites.Count; i++)
                {
                    Vector3 toSite = Flat(_sites[i].Position - home);
                    PlaceTrail(home, toSite.normalized, toSite.magnitude - _tuning.PoiRing.x);
                }

                Vector3 toPeak = Flat(_layout.PeakPosition - home).normalized;
                Rect area = _terrain.PlayableArea;
                float reach = SurfaceRules.DrivableReach(_terrain, home, toPeak, _tuning.EdgeMargin, ReachStep,
                    2f * Mathf.Max(area.width, area.height));
                PlaceTrail(home, toPeak, reach);
            }

            public void PlacePointsOfInterest()
            {
                for (int i = 0; i < _sites.Count; i++)
                {
                    for (int k = 0; k < _tuning.PoiClusters; k++)
                    {
                        for (int attempt = 0; attempt < ClusterRetries; attempt++)
                        {
                            Vector2 ring = _tuning.PoiRing;
                            Vector3 point = Around(_sites[i].Position, _random.Range(0f, 360f),
                                _random.Range(ring.x, ring.y));
                            if (TryCluster(point))
                            {
                                break;
                            }
                        }
                    }
                }
            }

            public void PlaceFill(int clusters, int attemptsPerCluster)
            {
                for (int i = 0; i < clusters; i++)
                {
                    for (int attempt = 0; attempt < attemptsPerCluster; attempt++)
                    {
                        if (TryCluster(RandomFloorPoint()))
                        {
                            break;
                        }
                    }
                }
            }

            public void TopUp()
            {
                for (int attempt = 0; attempt < _tuning.TopUpAttempts && TotalValue < _tuning.MinTotalValue;
                     attempt++)
                {
                    TryCluster(RandomFloorPoint());
                }

                if (TotalValue < _tuning.MinTotalValue)
                {
                    throw new InvalidOperationException(
                        $"{nameof(ScrapFieldPlanner)}: the basin holds only {TotalValue} scrap value, below the " +
                        $"minimum {_tuning.MinTotalValue}. Check the world surface or {nameof(ScrapTuning)}.");
                }
            }

            private void PlaceTrail(Vector3 home, Vector3 direction, float length)
            {
                var side = new Vector3(direction.z, 0f, -direction.x);
                for (float distance = _tuning.TrailStart; distance < length; distance += _tuning.TrailSpacing)
                {
                    float wander = _random.Range(-_tuning.TrailJitter, _tuning.TrailJitter);
                    TryCluster(home + direction * distance + side * wander);
                }
            }

            private Vector3 RandomFloorPoint()
            {
                Vector3 home = _layout.BasePosition;
                return new Vector3(home.x + _random.Range(-_fillHalfSize, _fillHalfSize), 0f,
                    home.z + _random.Range(-_fillHalfSize, _fillHalfSize));
            }

            private bool TryCluster(Vector3 centre)
            {
                float clusterRadius = _tuning.ClusterRadius.y;
                if (!SurfaceRules.InsideDrivable(_terrain, centre.x, centre.z, _tuning.EdgeMargin + clusterRadius) ||
                    _terrain.SampleNormal(centre.x, centre.z).y < _minNormalY ||
                    !IsClearOfHomeAndSites(centre, clusterRadius))
                {
                    return false;
                }

                float spacingSq = _tuning.MinClusterSpacing * _tuning.MinClusterSpacing;
                for (int i = 0; i < _clusters.Count; i++)
                {
                    if (SurfaceRules.HorizontalDistanceSquared(centre, _clusters[i]) < spacingSq)
                    {
                        return false;
                    }
                }

                _clusters.Add(centre);
                PlacePieces(centre);
                return true;
            }

            private void PlacePieces(Vector3 centre)
            {
                Vector2Int range = _tuning.PiecesPerCluster;
                int count = _random.RangeInclusive(range.x, range.y);
                float radius = _random.Range(_tuning.ClusterRadius.x, _tuning.ClusterRadius.y);
                float spacingSq = _tuning.MinPieceSpacing * _tuning.MinPieceSpacing;
                int first = Spawns.Count;
                for (int piece = 0; piece < count; piece++)
                {
                    for (int attempt = 0; attempt < PieceAttempts; attempt++)
                    {
                        float angle = _random.Range(0f, 2f * Mathf.PI);
                        float distance = radius * Mathf.Sqrt(_random.Next01());
                        float x = centre.x + Mathf.Cos(angle) * distance;
                        float z = centre.z + Mathf.Sin(angle) * distance;
                        var flat = new Vector3(x, 0f, z);
                        if (!_terrain.IsDrivable(x, z) || _terrain.SampleNormal(x, z).y < _minNormalY ||
                            !IsClearOfHomeAndSites(flat, 0f) || Crowded(flat, first, spacingSq))
                        {
                            continue;
                        }

                        int variant = PickVariant();
                        var position = new Vector3(x, _terrain.SampleHeight(x, z) + _tuning.HoverHeight, z);
                        Spawns.Add(new ScrapSpawn(position, variant, _random.Range(0f, 2f * Mathf.PI),
                            _random.Range(0f, 360f)));
                        TotalValue += _variants[variant].Value;
                        break;
                    }
                }
            }

            private bool Crowded(Vector3 point, int first, float spacingSq)
            {
                for (int i = first; i < Spawns.Count; i++)
                {
                    if (SurfaceRules.HorizontalDistanceSquared(point, Spawns[i].Position) < spacingSq)
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool IsClearOfHomeAndSites(Vector3 point, float padding)
            {
                float home = _tuning.BaseClearRadius + padding;
                if (SurfaceRules.HorizontalDistanceSquared(point, _layout.BasePosition) < home * home)
                {
                    return false;
                }

                float site = _tuning.SiteClearRadius + padding;
                for (int i = 0; i < _sites.Count; i++)
                {
                    if (SurfaceRules.HorizontalDistanceSquared(point, _sites[i].Position) < site * site)
                    {
                        return false;
                    }
                }

                return true;
            }

            private int PickVariant()
            {
                float roll = _random.Next01() * _totalWeight;
                for (int i = 0; i < _variants.Count; i++)
                {
                    roll -= _variants[i].Weight;
                    if (roll < 0f)
                    {
                        return i;
                    }
                }

                return _variants.Count - 1;
            }

            private static Vector3 Around(Vector3 centre, float bearing, float distance)
            {
                Vector3 direction = SurfaceRules.BearingDirection(bearing);
                return new Vector3(centre.x + direction.x * distance, 0f, centre.z + direction.z * distance);
            }

            private static Vector3 Flat(Vector3 vector)
            {
                return new Vector3(vector.x, 0f, vector.z);
            }
        }
    }
}
