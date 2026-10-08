using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Decides where every rock lies. Poisson-disk sites per class are thinned by a density field: seeded clusters
    /// for pebbles; for boulders a sparse scatter on the open floor, ejecta on raised crater rims and a talus band
    /// at the foot of the rim. The base pad, the ramps and bowls, crater interiors, steep faces, Whispering Canyon's
    /// floors and the driving lanes (base to The Peak, each play feature, the canyon's mouth and exit, the relays and
    /// salvage sites on the floor, and a few extra bearings) are kept clear of boulders; inside the canyon pebbles
    /// keep off the chasm and the centre line, and no rock at all lies on Kestrel-3's scorched crater and furrow.
    /// Pure and deterministic for (surface, settings, seed).
    /// </summary>
    public sealed class ScatterPlanner
    {
        private const uint PebbleSalt = 0x68BC21EBu;
        private const uint BoulderSalt = 0x02E5BE93u;
        private const uint ClusterSalt = 0x967A889Bu;
        private const uint AcceptSalt = 0xB0E5A9F1u;
        private const uint ShapeSalt = 0x7DFC2C4Du;

        // Hash inputs quantise site positions to centimetres: stable per site, independent of sampling order.
        private const float HashQuantum = 100f;

        private readonly MoonSurface _surface;
        private readonly ScatterSettings _settings;
        private readonly uint _seed;
        private readonly GradientNoise _clusterNoise;
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
            if (Mathf.Sqrt(x * x + z * z) < _surface.PadRadius + _settings.PadClearance || Scorched(x, z))
            {
                return 0f;
            }

            if (_surface.Canyon.Bounds.Contains(new Vector2(x, z))
                && (_surface.Sample(x, z).Chasm > 0f || (_surface.Canyon.TryFloor(x, z, out bool _, out float _,
                    out float centre) && centre < _settings.CanyonPebbleClear)))
            {
                return 0f;
            }

            float chance = _settings.PebbleDensity * 2f * Cluster(x, z);
            if (DistanceToLanes(new Vector2(x, z)) < _settings.LaneHalfWidth)
            {
                chance *= _settings.LaneKeptPebbles;
            }

            return Mathf.Clamp01(chance);
        }

        /// <summary>Chance (0..1) that a boulder site at (x, z) holds a boulder.</summary>
        public float BoulderChance(float x, float z)
        {
            var point = new Vector2(x, z);
            if (point.magnitude < _surface.PadRadius + _settings.PadClearance * 2f
                || DistanceToLanes(point) < _settings.LaneHalfWidth || NearPlayFeature(point) || Scorched(x, z))
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
            float chance = _settings.BoulderDensity * 2f * cluster + _settings.CraterRimBoulders * sample.CraterRim
                + _settings.TalusBoulders * talus * (0.25f + cluster);
            return Mathf.Clamp01(chance);
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
                instances.Add(new ScatterInstance(kind, position, normal, yaw, size, (int)(shape >> 1)));
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
    }
}
