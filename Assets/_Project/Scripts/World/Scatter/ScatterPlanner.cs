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
        private const uint FieldSalt = 0x1B873593u;
        private const uint RubbleSalt = 0x5BE0CD19u;

        // A rubble stone's height is scaled by a share in this range, and it sinks by up to this share of its scaled
        // size: low and set in the dust, but standing proud enough to cast a shadow in raking light.
        private const float RubbleSquatMin = 0.8f;
        private const float RubbleSquatMax = 1.3f;
        private const float RubbleBurial = 0.15f;

        // A rubble stone's ground normal comes from three height samples this far apart, metres: a stone needs only the
        // ground's lean, and the full five-sample normal would double the plan's cost.
        private const float RubbleNormalStep = 0.25f;

        // A pebble in a lane is held to this share of the cobble cutoff: below it, so it never gets a collider.
        private const float LaneCobbleShare = 0.95f;
        private const uint PatchSalt = 0x9B05688Cu;
        private const uint ApronSalt = 0x510E527Fu;

        // The rubble patch mask eases in over this much of the patch noise's range around its threshold.
        private const float PatchEdge = 0.15f;

        // The boulder field mask eases in over this much of the field noise's range around its threshold.
        private const float FieldEdge = 0.12f;

        // Hash inputs quantise site positions to centimetres: stable per site, independent of sampling order.
        private const float HashQuantum = 100f;

        private readonly MoonSurface _surface;
        private readonly ScatterSettings _settings;
        private readonly uint _seed;
        private readonly GradientNoise _clusterNoise;
        private readonly GradientNoise _trailNoise;
        private readonly GradientNoise _fieldNoise;
        private readonly float _fieldThreshold;
        private readonly GradientNoise _patchNoise;
        private readonly uint _rubbleSeed;
        private readonly float _patchThreshold;
        private readonly Vector2[] _laneEnds;

        // Spaces kept clear of rocks, the anchors' and the Scout's crater (no flat anchor): x, z of the centre and its
        // radius.
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

            _clearings = new Vector3[anchors.Count + 1];
            GroundShaping shaping = surface.Shaping;
            _clearings[anchors.Count] = new Vector3(shaping.ScoutCraterCenter.x, shaping.ScoutCraterCenter.y,
                shaping.ScoutCraterRadius);
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
            _fieldNoise = new GradientNoise(Hashing.Mix(_seed ^ FieldSalt));

            // Fractal noise sits near 0 with a spread of about +-0.5: a threshold this far up leaves the coverage asked.
            _fieldThreshold = 0.5f - settings.BoulderFieldCoverage;
            _patchNoise = new GradientNoise(Hashing.Mix(_seed ^ PatchSalt));
            _rubbleSeed = Hashing.Mix(_seed ^ RubbleSalt);
            _patchThreshold = 0.5f - settings.RubblePatchCoverage;
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

            // Boulders gather in a few broad fields and leave long empty stretches of plain between them (owner,
            // 2026-10-19); inside a field they stay sparse on open ground (tight cluster peaks only) and dense on crater
            // rims and boulder trails. The talus at the rim's foot is its own apron.
            float field = BoulderField(x, z);
            float openSparse = _settings.BoulderDensity * _settings.BoulderFieldDensity * field
                * SmoothMath.Smootherstep(0.83f, 0.96f, cluster);
            float rimCluster = _settings.CraterRimBoulders * sample.CraterRim * field
                * SmoothMath.Smootherstep(0.46f, 0.78f, cluster);
            float trailCluster = 0.52f * trail * field * SmoothMath.Smootherstep(0.42f, 0.76f, cluster);
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

                if (!boulder && (_surface.Relief.OnOpeningSite(site)
                        || Mathf.Min(DistanceToLanes(site), _surface.Relief.DistanceToProtectedLanes(site))
                        < _settings.LaneHalfWidth))
                {
                    // In a lane or on the opening's ground a pebble stays below the cobble cutoff: it never collides,
                    // so driving there stays smooth.
                    size = Mathf.Min(size, _settings.CobbleColliderMinSize * LaneCobbleShare);
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

        /// <summary>
        /// Plans rubble grid cell (<paramref name="i"/>, <paramref name="j"/>), <see cref="ScatterSettings.RubbleSpacing"/>
        /// wide (owner's Apollo 11 reference): at most one jittered stone, kept more often inside a rubble patch than on
        /// the stretches between, never on the opening's sites and lanes, the pad, the ruins, the scorch, the canyon,
        /// the anchors' clearings or steep ground. The same cell always gives the same stone. Visual only.
        /// </summary>
        public void PlanRubbleCell(int i, int j, List<ScatterInstance> stones)
        {
            float cell = _settings.RubbleSpacing;
            float radius = _surface.DrivableRadius;
            float x = (i + Hashing.ToUnit(Hashing.Hash(i, j, 1, _rubbleSeed))) * cell;
            float z = (j + Hashing.ToUnit(Hashing.Hash(i, j, 2, _rubbleSeed))) * cell;
            if (x * x + z * z > radius * radius)
            {
                return;
            }

            float chance = Mathf.Lerp(_settings.RubbleSparse, _settings.RubbleDense, RubblePatch(x, z));
            if (Hashing.ToUnit(Hashing.Hash(i, j, 3, _rubbleSeed)) < chance)
            {
                AddStone(stones, x, z, Hashing.Hash(i, j, 4, _rubbleSeed));
            }
        }

        /// <summary>
        /// The boulders of <paramref name="plan"/> that wear an apron of rubble, each with its place in the plan (which
        /// seeds its apron).
        /// </summary>
        public void CollectRubbleAprons(IReadOnlyList<ScatterInstance> plan, List<RubbleApron> aprons)
        {
            for (int b = 0; b < plan.Count; b++)
            {
                ScatterInstance boulder = plan[b];
                if (boulder.Kind == ScatterKind.Boulder)
                {
                    aprons.Add(new RubbleApron(b, new Vector2(boulder.Position.x, boulder.Position.z), boulder.Size,
                        boulder.Size * _settings.RubbleApronReach,
                        Mathf.RoundToInt(_settings.RubbleApronCount * boulder.Size)));
                }
            }
        }

        /// <summary>
        /// The stones of <paramref name="apron"/>'s rubble that lie inside <paramref name="area"/> (the rest belong to
        /// the chunks round it); returns how many of its stones it looked at.
        /// </summary>
        public int PlanRubbleApron(RubbleApron apron, Rect area, List<ScatterInstance> stones)
        {
            for (int k = 0; k < apron.Count; k++)
            {
                uint hash = Hashing.Hash(apron.Index, k, _rubbleSeed ^ ApronSalt);
                float angle = Hashing.ToUnit(Hashing.Mix(hash)) * Mathf.PI * 2f;
                float t = Hashing.ToUnit(Hashing.Mix(hash ^ 0x2545F491u));
                float distance = Mathf.Lerp(apron.Size * 0.5f, apron.Reach, t * t);
                float x = apron.Center.x + Mathf.Sin(angle) * distance;
                float z = apron.Center.y + Mathf.Cos(angle) * distance;
                if (x >= area.xMin && x < area.xMax && z >= area.yMin && z < area.yMax)
                {
                    AddStone(stones, x, z, Hashing.Mix(hash ^ 0x68E31DA4u));
                }
            }

            return apron.Count;
        }

        /// <summary>One rubble stone at (x, z) when the ground there keeps rubble.</summary>
        private void AddStone(List<ScatterInstance> instances, float x, float z, uint shape)
        {
            var point = new Vector2(x, z);
            float sizeT = Hashing.ToUnit(Hashing.Mix(shape));
            float size = Mathf.Lerp(_settings.RubbleSize.x, _settings.RubbleSize.y, sizeT * sizeT);
            float lanes = Mathf.Min(DistanceToLanes(point), _surface.Relief.DistanceToProtectedLanes(point));
            if (point.magnitude < _surface.PadRadius + _settings.PadClearance || _surface.Relief.OnOpeningSite(point)
                || lanes < _settings.RubbleLaneHalfWidth || NearRuins(point) || Scorched(x, z)
                || _surface.Canyon.Bounds.Contains(point) || InClearing(point, size))
            {
                return;
            }

            float height = _surface.SampleHeight(x, z);
            var normal = new Vector3(height - _surface.SampleHeight(x + RubbleNormalStep, z), RubbleNormalStep,
                height - _surface.SampleHeight(x, z + RubbleNormalStep)).normalized;
            if (normal.y < _pebbleMaxSlopeCos)
            {
                return;
            }

            float yaw = Hashing.ToUnit(Hashing.Mix(shape ^ 0x5bd1e995u)) * 360f;
            float squat = Mathf.Lerp(RubbleSquatMin, RubbleSquatMax, Hashing.ToUnit(Hashing.Mix(shape ^ 0x165667b1u)));
            float burial = size * squat * RubbleBurial * Hashing.ToUnit(Hashing.Mix(shape ^ 0xfd7046c5u));
            instances.Add(new ScatterInstance(ScatterKind.Rubble, new Vector3(x, height, z),
                normal, yaw, size, (int)(shape >> 1), new Vector3(1f, squat, 1f), burial));
        }

        /// <summary>How deep inside a rubble patch (x, z) lies: 0 on the emptier stretches, 1 within.</summary>
        private float RubblePatch(float x, float z)
        {
            float wavelength = _settings.RubblePatchWavelength;
            float noise = _patchNoise.Fractal(x / wavelength, z / wavelength, 2, 2f, 0.5f);
            return SmoothMath.Smootherstep(_patchThreshold - PatchEdge, _patchThreshold + PatchEdge, noise);
        }

        /// <summary>How deep inside a boulder field (x, z) lies: 0 on the bare plain between, 1 within.</summary>
        private float BoulderField(float x, float z)
        {
            float wavelength = _settings.BoulderFieldWavelength;
            float noise = _fieldNoise.Fractal(x / wavelength, z / wavelength, 2, 2f, 0.5f);
            return SmoothMath.Smootherstep(_fieldThreshold - FieldEdge, _fieldThreshold + FieldEdge, noise);
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
