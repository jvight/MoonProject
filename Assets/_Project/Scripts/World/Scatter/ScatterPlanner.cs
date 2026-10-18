using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Decides where every rock lies. Poisson-disk sites per class are thinned by a density field: seeded clusters,
    /// crater-rim aprons, and radial boulder trails; for boulders a very sparse scatter on the open floor, concentrated
    /// ejecta clusters and boulder trails around crater rims, and talus falls at the foot of the basin rim. The base
    /// pad, the ramps and bowls, crater interiors, steep faces, Whispering Canyon's floors and the driving lanes are
    /// kept clear of boulders; inside the canyon pebbles keep off the chasm and the centre line, and no rock at all
    /// lies on Kestrel-3's scorched crater and furrow. Pure and deterministic for (surface, settings, seed).
    /// </summary>
    public sealed class ScatterPlanner
    {
        private const uint PebbleSalt = 0x68BC21EBu;
        private const uint BoulderSalt = 0x02E5BE93u;
        private const uint ClusterSalt = 0x967A889Bu;
        private const uint TrailSalt = 0x4F1B9D27u;
        private const uint AcceptSalt = 0xB0E5A9F1u;
        private const uint ShapeSalt = 0x7DFC2C4Du;

        // Hash inputs quantise site positions to centimetres: stable per site, independent of sampling order.
        private const float HashQuantum = 100f;

        private readonly MoonSurface _surface;
        private readonly ScatterSettings _settings;
        private readonly uint _seed;
        private readonly GradientNoise _clusterNoise;
        private readonly GradientNoise _trailNoise;
        private readonly Vector2[] _laneEnds;

        // Anchor spaces kept clear of rocks: x, z of the centre and its radius.
        private readonly Vector3[] _clearings;
        private readonly float _pebbleMaxSlopeCos;
        private readonly float _boulderMaxSlopeCos;

        /// <param name="anchors">The world's anchors: no rock lies inside any anchor's radius.</param>
        public ScatterPlanner(MoonSurface surface, ScatterSettings settings, IWorldAnchors anchors)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            _clearings = new Vector3[anchors.Count];
            var placesOnTheFloor = new List<Vector2>();
            for (int i = 0; i < anchors.Count; i++)
            {
                WorldAnchor anchor = anchors.Get(i);
                var site = new Vector2(anchor.Position.x, anchor.Position.z);
                _clearings[i] = new Vector3(site.x, site.y, anchor.Radius);
                bool lanePlace = anchor.Id.StartsWith(WorldAnchorIds.RelayPrefix, StringComparison.Ordinal)
                    || anchor.Id.StartsWith(WorldAnchorIds.SitePrefix, StringComparison.Ordinal);
                if (lanePlace && !surface.Canyon.Bounds.Contains(site))
                {
                    placesOnTheFloor.Add(site);
                }
            }

            string error = settings.Validate();
            if (error != null)
            {
                throw new ArgumentException("Invalid scatter settings: " + error, nameof(settings));
            }

            _seed = (uint)surface.Seed;
            _clusterNoise = new GradientNoise(Hashing.Mix(_seed ^ ClusterSalt));
            _trailNoise = new GradientNoise(Hashing.Mix(_seed ^ TrailSalt));
            _pebbleMaxSlopeCos = Mathf.Cos(settings.PebbleMaxSlope * Mathf.Deg2Rad);
            _boulderMaxSlopeCos = Mathf.Cos(settings.BoulderMaxSlope * Mathf.Deg2Rad);

            var ends = new List<Vector2>
            {
                new Vector2(surface.PeakSummit.x, surface.PeakSummit.z).normalized * settings.ExtentRadius,
            };
            foreach (Ramp ramp in surface.Ramps)
            {
                ends.Add(ramp.Crest);
            }

            foreach (Crater crater in surface.Craters)
            {
                if (crater.IsPlayBowl)
                {
                    ends.Add(crater.Center);
                }
            }

            ends.Add(surface.Canyon.MainPath.PointAt(0f));
            ends.Add(surface.Canyon.ExitFoot);
            ends.Add(surface.Shaping.Ruins.Center);
            ends.AddRange(placesOnTheFloor);
            foreach (float bearing in settings.LaneBearings ?? Array.Empty<float>())
            {
                ends.Add(MoonSurface.BearingToDirection(bearing) * settings.ExtentRadius);
            }

            _laneEnds = ends.ToArray();
        }

        /// <summary>Lane segments run from the base to these points.</summary>
        public IReadOnlyList<Vector2> LaneEnds => _laneEnds;

        public List<ScatterInstance> Plan()
        {
            var instances = new List<ScatterInstance>();
            AddClass(instances, ScatterKind.Pebble, _settings.PebbleSpacing, Hashing.Mix(_seed ^ PebbleSalt));
            AddClass(instances, ScatterKind.Boulder, _settings.BoulderSpacing, Hashing.Mix(_seed ^ BoulderSalt));
            return instances;
        }

        /// <summary>Chance (0..1) that a pebble site at (x, z) holds a pebble.</summary>
        public float PebbleChance(float x, float z)
        {
            float distFromOrigin = Mathf.Sqrt(x * x + z * z);
            if (distFromOrigin < _surface.PadRadius + _settings.PadClearance
                || NearRuins(new Vector2(x, z)) || Scorched(x, z))
            {
                return 0f;
            }

            if (_surface.Canyon.Bounds.Contains(new Vector2(x, z))
                && (_surface.Sample(x, z).Chasm > 0f || (_surface.Canyon.TryFloor(x, z, out bool _, out float _,
                    out float centre) && centre < _settings.CanyonPebbleClear)))
            {
                return 0f;
            }

            SurfaceSample sample = _surface.Sample(x, z);
            float cluster = Cluster(x, z);
            float trail = BoulderTrail(x, z, sample);
            float homeEdgeApron = SmoothMath.Bump((distFromOrigin - (_surface.PadRadius + _settings.PadClearance + 9f)) / 10f);

            float chance = _settings.PebbleDensity * (0.28f + 1.15f * cluster)
                + 0.34f * sample.CraterRim
                + 0.30f * trail
                + 0.20f * homeEdgeApron * (0.4f + 0.6f * cluster);

            if (DistanceToLanes(new Vector2(x, z)) < _settings.LaneHalfWidth)
            {
                chance *= _settings.LaneKeptPebbles;
            }

            return Mathf.Clamp01(_surface.Shaping.Blast.ScatterChance(x, z, chance, true));
        }

        /// <summary>Chance (0..1) that a boulder site at (x, z) holds a boulder.</summary>
        public float BoulderChance(float x, float z)
        {
            var point = new Vector2(x, z);
            float minBoulderRadius = _surface.PadRadius + _settings.PadClearance + 2.5f;
            if (point.magnitude < minBoulderRadius
                || DistanceToLanes(point) < _settings.LaneHalfWidth || NearPlayFeature(point)
                || NearRuins(point) || Scorched(x, z))
            {
                return 0f;
            }

            SurfaceSample sample = _surface.Sample(x, z);
            if (sample.CraterBowl > 0f || sample.CanyonFloor > 0f)
            {
                return 0f;
            }

            float talus = SmoothMath.Bump((-_surface.FloorEdgeDistance(x, z) - _settings.TalusOffset)
                / _settings.TalusHalfWidth);
            float cluster = Cluster(x, z);
            float trail = BoulderTrail(x, z, sample);

            // Sparse on open ground (only inside tight cluster peaks); dense on crater rims, boulder trails, and talus pockets
            float openSparse = _settings.BoulderDensity * SmoothMath.Smootherstep(0.83f, 0.96f, cluster);
            float rimCluster = _settings.CraterRimBoulders * sample.CraterRim
                * SmoothMath.Smootherstep(0.46f, 0.78f, cluster);
            float trailCluster = 0.52f * trail * SmoothMath.Smootherstep(0.42f, 0.76f, cluster);
            float talusCluster = _settings.TalusBoulders * talus * SmoothMath.Smootherstep(0.62f, 0.88f, cluster);

            float chance = openSparse + rimCluster + trailCluster + talusCluster;
            return Mathf.Clamp01(_surface.Shaping.Blast.ScatterChance(x, z, chance, false));
        }

        /// <summary>
        /// Evaluates radial ejecta boulder trails extending outward from larger craters plus a weathered boulder trail
        /// pocket near the edge of the home pad.
        /// </summary>
        private float BoulderTrail(float x, float z, SurfaceSample sample)
        {
            float trail = 0f;

            // Elongated anisotropic boulder-trail ribbons tied to crater rims and nearby ejecta aprons
            float ribbonNoise = _trailNoise.Fractal(x / 34f, z / 14f, 2, 2.1f, 0.5f);
            float ribbon = SmoothMath.Smootherstep(0.24f, 0.52f, ribbonNoise);

            if (sample.CraterRim > 0.02f)
            {
                trail = Mathf.Max(trail, sample.CraterRim * (0.45f + 0.55f * ribbon));
            }

            // Radial boulder trails thrown from medium/large impact craters
            IReadOnlyList<Crater> craters = _surface.Craters;
            var pt = new Vector2(x, z);
            for (int i = 0; i < craters.Count; i++)
            {
                Crater c = craters[i];
                if (c.IsPlayBowl || c.Radius < 11f)
                {
                    continue;
                }

                Vector2 delta = pt - c.Center;
                float maxReach = c.Radius * 2.35f;
                float dSq = delta.sqrMagnitude;
                if (dSq > maxReach * maxReach || dSq < c.Radius * c.Radius * 0.85f)
                {
                    continue;
                }

                float dist = Mathf.Sqrt(dSq);
                float radialT = (dist - c.Radius * 0.92f) / (c.Radius * 1.43f);
                float radialFalloff = 1f - SmoothMath.Smootherstep(0f, 1f, radialT);
                float angle = Mathf.Atan2(delta.y, delta.x);
                float phase = (i * 2.39996f) + (c.Radius * 0.37f);
                float ray1 = Mathf.Cos(angle * 2f + phase);
                float ray2 = Mathf.Cos(angle * 3f - phase * 1.3f);
                float rayMask = SmoothMath.Smootherstep(0.58f, 0.88f, 0.55f * ray1 + 0.45f * ray2);
                trail = Mathf.Max(trail, radialFalloff * rayMask * (0.5f + 0.5f * ribbon));
            }

            return Mathf.Clamp01(trail);
        }

        private bool Scorched(float x, float z)
        {
            return _surface.Kestrel.ScorchAt(x, z) > _settings.ScorchClear;
        }

        private bool InClearing(Vector2 site, float size)
        {
            foreach (Vector3 clearing in _clearings)
            {
                float reach = clearing.z + size + _settings.AnchorClearance;
                if ((site - new Vector2(clearing.x, clearing.y)).sqrMagnitude < reach * reach)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Distance from <paramref name="point"/> to the nearest driving lane, metres.</summary>
        public float DistanceToLanes(Vector2 point)
        {
            float best = float.MaxValue;
            for (int i = 0; i < _laneEnds.Length; i++)
            {
                Vector2 end = _laneEnds[i];
                float t = Mathf.Clamp01(Vector2.Dot(point, end) / end.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(point, end * t));
            }

            return best;
        }

        private void AddClass(List<ScatterInstance> instances, ScatterKind kind, float spacing, uint seed)
        {
            bool boulder = kind == ScatterKind.Boulder;
            Vector2 sizeRange = boulder ? _settings.BoulderSize : _settings.PebbleSize;
            float maxSlopeCos = boulder ? _boulderMaxSlopeCos : _pebbleMaxSlopeCos;
            foreach (Vector2 site in PoissonDiskSampler.SampleDisc(_settings.ExtentRadius, spacing, seed))
            {
                int qx = Mathf.RoundToInt(site.x * HashQuantum);
                int qz = Mathf.RoundToInt(site.y * HashQuantum);
                float chance = boulder ? BoulderChance(site.x, site.y) : PebbleChance(site.x, site.y);
                if (Hashing.ToUnit(Hashing.Hash(qx, qz, seed ^ AcceptSalt)) >= chance)
                {
                    continue;
                }

                Vector3 normal = _surface.SampleNormal(site.x, site.y);
                if (normal.y < maxSlopeCos)
                {
                    continue;
                }

                uint shape = Hashing.Hash(qx, qz, seed ^ ShapeSalt);
                float sizeT = Hashing.ToUnit(Hashing.Mix(shape));
                float size = Mathf.Lerp(sizeRange.x, sizeRange.y, sizeT * sizeT);
                if (InClearing(site, size))
                {
                    continue;
                }

                float yaw = Hashing.ToUnit(Hashing.Mix(shape ^ 0x5bd1e995u)) * 360f;
                var position = new Vector3(site.x, _surface.SampleHeight(site.x, site.y), site.y);

                // Per-instance anisotropic 3D scale variation (normalized so max horizontal axis == 1)
                float sx = Mathf.Lerp(0.78f, 1.22f, Hashing.ToUnit(Hashing.Mix(shape ^ 0x27d4eb2du)));
                float sy = Mathf.Lerp(0.72f, 1.24f, Hashing.ToUnit(Hashing.Mix(shape ^ 0x165667b1u)));
                float sz = Mathf.Lerp(0.78f, 1.22f, Hashing.ToUnit(Hashing.Mix(shape ^ 0xd3a2646cu)));
                float maxHoriz = Mathf.Max(sx, sz);
                var scaleAspect = new Vector3(sx / maxHoriz, sy, sz / maxHoriz);

                // Per-instance burial depth: some sit proud with full regolith skirt, others are deeply half-buried
                float burialT = Hashing.ToUnit(Hashing.Mix(shape ^ 0xfd7046c5u));
                float maxBurialFrac = boulder ? 0.16f : 0.12f;
                float burialDepth = size * scaleAspect.y * (burialT * burialT) * maxBurialFrac;

                instances.Add(new ScatterInstance(
                    kind,
                    position,
                    normal,
                    yaw,
                    size,
                    (int)(shape >> 1),
                    scaleAspect,
                    burialDepth));
            }
        }

        private float Cluster(float x, float z)
        {
            float wavelength = _settings.ClusterWavelength;
            float noise = _clusterNoise.Fractal(x / wavelength, z / wavelength, 2, 2f, 0.5f);
            return Mathf.Clamp01(0.5f + noise * _settings.ClusterContrast);
        }

        private bool NearPlayFeature(Vector2 point)
        {
            float clearance = _settings.FeatureClearance;
            foreach (Ramp ramp in _surface.Ramps)
            {
                if (Vector2.Distance(point, ramp.Crest) < ramp.BoundingRadius + clearance)
                {
                    return true;
                }
            }

            foreach (Crater crater in _surface.Craters)
            {
                if (crater.IsPlayBowl && Vector2.Distance(point, crater.Center) < crater.OuterRadius + clearance)
                {
                    return true;
                }
            }

            return false;
        }

        private bool NearRuins(Vector2 point)
        {
            GroundShape ruins = _surface.Shaping.Ruins;
            return Vector2.Distance(point, ruins.Center) < ruins.Radius + _settings.PadClearance;
        }
    }
}
