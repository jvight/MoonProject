using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// The analytic moon surface: a vast crater basin with a flat base pad at the origin, a gentle bowl floor with
    /// rolling dunes, seeded craters and hand-placed play features, jagged rim mountains, The Peak and distant
    /// ranges. Pure, deterministic for a (settings, seed) pair, C1-smooth everywhere, immutable and thread-safe.
    /// </summary>
    public sealed class MoonSurface : ITerrainQuery
    {
        // Central-difference step for normals: well below the 2 m mesh cell, far above float noise at 1 km.
        private const float NormalStep = 0.25f;
        private const int PlacementAttemptsPerCrater = 400;
        private const float TwoPi = 6.28318531f;

        // Fraction of The Peak's flank that is rounded into the summit plateau (keeps the horn profile C1).
        private const float PeakCap = 0.08f;

        // Rim width (fraction of radius) of the soft lip around play bowls.
        private const float BowlLipWidth = 0.5f;

        // Dune ripples are faded out inside features so craters and ramps read cleanly. The fade starts this far
        // into a crater (fraction of its radius) and spans at least this many dune wavelengths, so that removing
        // the ripples never steepens the ground noticeably.
        private const float CraterCalmStart = 0.4f;
        private const float MinCalmSpanInWavelengths = 0.45f;

        // Ramps quiet the dunes over a footprint this much larger than the ramp itself, for the same reason.
        private const float RampCalmScale = 1.7f;

        // Salts decorrelate the noise channels and the crater placement derived from one world seed.
        private const uint HillSalt = 0x9E3779B9u;
        private const uint DuneMeanderSalt = 0x85EBCA6Bu;
        private const uint DuneCoverageSalt = 0xC2B2AE35u;
        private const uint RimWarpSalt = 0x27D4EB2Fu;
        private const uint MountainSalt = 0x165667B1u;
        private const uint SaddleSalt = 0xD3A2646Cu;
        private const uint CragSalt = 0xFD7046C5u;
        private const uint FarRangeSalt = 0xB55A4F09u;
        private const uint GullySalt = 0x94D049BBu;

        // The gullies rib the wall between this fraction of the foothills and the crest.
        private const float GullyStartInFoothills = 0.4f;

        // Gullies drift sideways once per this many gully spacings down the wall, so they are not perfectly radial.
        private const float GullyRadialDrift = 2f;
        private const uint CraterSalt = 0x7FEB352Du;

        private readonly GradientNoise _hillNoise;
        private readonly GradientNoise _duneMeanderNoise;
        private readonly GradientNoise _duneCoverageNoise;
        private readonly GradientNoise _rimWarpNoise;
        private readonly GradientNoise _mountainNoise;
        private readonly GradientNoise _saddleNoise;
        private readonly GradientNoise _cragNoise;
        private readonly GradientNoise _farRangeNoise;
        private readonly GradientNoise _gullyNoise;

        private readonly float _padRadius;
        private readonly float _padRadiusSq;
        private readonly float _padBlendEnd;
        private readonly float _floorRadius;
        private readonly float _bowlRise;
        private readonly float _rimWarpAmplitude;
        private readonly float _invRimWarpWavelength;
        private readonly float _rimWarpStart;
        private readonly float _foothillHeight;
        private readonly float _foothillEnd;
        private readonly float _wallStart;
        private readonly float _crestRadius;
        private readonly float _crestHeight;
        private readonly float _outerDrop;
        private readonly float _outerPlainRadius;
        private readonly float _mountainHeight;
        private readonly float _invMountainWavelength;
        private readonly int _mountainOctaves;
        private readonly float _ridgeSoftness;
        private readonly float _saddleDepth;
        private readonly float _invSaddleWavelength;
        private readonly float _gullyHeight;
        private readonly float _gullyCenter;
        private readonly float _gullyHalfWidth;
        private readonly float _gullyAngularScale;
        private readonly float _invGullyRadial;
        private readonly float _peakX;
        private readonly float _peakZ;
        private readonly float _peakHeight;
        private readonly float _peakFootprint;
        private readonly float _peakFootprintSq;
        private readonly float _summitRadius;
        private readonly float _peakSharpness;
        private readonly float _peakCragHeight;
        private readonly float _cragAngularScale;
        private readonly float _invCragRadialWavelength;
        private readonly float _hillHeight;
        private readonly float _invHillWavelength;
        private readonly float _duneHeight;
        private readonly float _duneWaveNumber;
        private readonly float _duneWindX;
        private readonly float _duneWindZ;
        private readonly float _duneSkew;
        private readonly float _duneMeander;
        private readonly float _invDuneMeanderWavelength;
        private readonly float _duneCoverageMin;
        private readonly float _invDuneCoverageWavelength;
        private readonly float _farRangeStart;
        private readonly float _farRangeFull;
        private readonly float _farRangeHeight;
        private readonly float _invFarRangeWavelength;

        private readonly Crater[] _craters;
        private readonly float[] _craterX;
        private readonly float[] _craterZ;
        private readonly float[] _craterOuterSq;
        private readonly float[] _craterInvRadius;
        private readonly float[] _craterDepth;
        private readonly float[] _craterRimHeight;
        private readonly float[] _craterInvRimWidth;
        private readonly float[] _craterCalmStart;
        private readonly float[] _craterCalmEnd;

        private readonly Ramp[] _ramps;
        private readonly float[] _rampCalmBoundSq;

        public MoonSurface(SurfaceSettings settings, int seed)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            string error = settings.Validate();
            if (error != null)
            {
                throw new ArgumentException("Invalid surface settings: " + error, nameof(settings));
            }

            Seed = seed;
            uint baseSeed = (uint)seed;
            _hillNoise = new GradientNoise(Hashing.Mix(baseSeed ^ HillSalt));
            _duneMeanderNoise = new GradientNoise(Hashing.Mix(baseSeed ^ DuneMeanderSalt));
            _duneCoverageNoise = new GradientNoise(Hashing.Mix(baseSeed ^ DuneCoverageSalt));
            _rimWarpNoise = new GradientNoise(Hashing.Mix(baseSeed ^ RimWarpSalt));
            _mountainNoise = new GradientNoise(Hashing.Mix(baseSeed ^ MountainSalt));
            _saddleNoise = new GradientNoise(Hashing.Mix(baseSeed ^ SaddleSalt));
            _cragNoise = new GradientNoise(Hashing.Mix(baseSeed ^ CragSalt));
            _farRangeNoise = new GradientNoise(Hashing.Mix(baseSeed ^ FarRangeSalt));
            _gullyNoise = new GradientNoise(Hashing.Mix(baseSeed ^ GullySalt));

            _padRadius = settings.PadRadius;
            _padRadiusSq = _padRadius * _padRadius;
            _padBlendEnd = _padRadius + settings.PadBlend;
            _floorRadius = settings.FloorRadius;
            _bowlRise = settings.BowlRise;
            _rimWarpAmplitude = settings.RimWarpAmplitude;
            _invRimWarpWavelength = 1f / settings.RimWarpWavelength;

            // Below this radius every rim term is exactly zero whatever the warp, so the warp noise can be skipped.
            _rimWarpStart = _floorRadius - _rimWarpAmplitude - 1f;
            _foothillHeight = settings.FoothillHeight;
            _foothillEnd = _floorRadius + settings.FoothillWidth;
            _wallStart = _floorRadius + settings.WallStartOffset;
            _crestRadius = settings.CrestRadius;
            _crestHeight = settings.CrestHeight;
            _outerDrop = _bowlRise + _crestHeight - settings.OuterPlainHeight;
            _outerPlainRadius = settings.OuterPlainRadius;
            _mountainHeight = settings.MountainHeight;
            _invMountainWavelength = 1f / settings.MountainWavelength;
            _mountainOctaves = settings.MountainOctaves;
            _ridgeSoftness = settings.RidgeSoftness;
            _saddleDepth = settings.SaddleDepth;
            _invSaddleWavelength = 1f / settings.SaddleWavelength;
            _gullyHeight = settings.WallGullyHeight;
            float gullyStart = _floorRadius + settings.FoothillWidth * GullyStartInFoothills;
            _gullyCenter = (gullyStart + _crestRadius) * 0.5f;
            _gullyHalfWidth = (_crestRadius - gullyStart) * 0.5f;
            _gullyAngularScale = settings.WallGullyCount / TwoPi;
            _invGullyRadial = settings.WallGullyCount / (TwoPi * _crestRadius * GullyRadialDrift);

            Vector2 peakDirection = BearingToDirection(settings.PeakBearing);
            _peakX = peakDirection.x * settings.PeakDistance;
            _peakZ = peakDirection.y * settings.PeakDistance;
            _peakHeight = settings.PeakHeight;
            _peakFootprint = settings.PeakFootprint;
            _peakFootprintSq = _peakFootprint * _peakFootprint;
            _summitRadius = settings.SummitRadius;
            _peakSharpness = settings.PeakSharpness;
            _peakCragHeight = settings.PeakCragHeight;

            // Ridged noise has roughly one crest per unit, so a circle of circumference N units carries ~N ridges.
            _cragAngularScale = settings.PeakRidgeCount / TwoPi;
            _invCragRadialWavelength = 1f / settings.PeakFootprint;

            _hillHeight = settings.HillHeight;
            _invHillWavelength = 1f / settings.HillWavelength;
            _duneHeight = settings.DuneHeight;
            _duneWaveNumber = TwoPi / settings.DuneWavelength;
            Vector2 wind = BearingToDirection(settings.DuneWindBearing);
            _duneWindX = wind.x;
            _duneWindZ = wind.y;
            _duneSkew = settings.DuneSkew;
            _duneMeander = settings.DuneMeander;
            _invDuneMeanderWavelength = 1f / settings.DuneMeanderWavelength;
            _duneCoverageMin = settings.DuneCoverageMin;
            _invDuneCoverageWavelength = 1f / settings.DuneCoverageWavelength;

            _farRangeStart = settings.FarRangeStart;
            _farRangeFull = settings.FarRangeFull;
            _farRangeHeight = settings.FarRangeHeight;
            _invFarRangeWavelength = 1f / settings.FarRangeWavelength;

            DrivableRadius = _floorRadius - _rimWarpAmplitude;
            float half = DrivableRadius * 0.70710678f;
            PlayableArea = new Rect(-half, -half, half * 2f, half * 2f);
            PeakSummit = new Vector3(_peakX, _peakHeight, _peakZ);
            SummitRadius = _summitRadius;
            PadRadius = _padRadius;
            FloorRadius = _floorRadius;
            CrestRadius = _crestRadius;

            _ramps = ResolveRamps(settings.Ramps);
            _rampCalmBoundSq = new float[_ramps.Length];
            for (int i = 0; i < _ramps.Length; i++)
            {
                float bound = _ramps[i].BoundingRadius * RampCalmScale;
                _rampCalmBoundSq[i] = bound * bound;
            }

            _craters = PlaceCraters(settings, baseSeed);
            int count = _craters.Length;
            _craterX = new float[count];
            _craterZ = new float[count];
            _craterOuterSq = new float[count];
            _craterInvRadius = new float[count];
            _craterDepth = new float[count];
            _craterRimHeight = new float[count];
            _craterInvRimWidth = new float[count];
            _craterCalmStart = new float[count];
            _craterCalmEnd = new float[count];
            float minCalmSpan = settings.DuneWavelength * MinCalmSpanInWavelengths;
            for (int i = 0; i < count; i++)
            {
                Crater crater = _craters[i];
                float calmStart = crater.Radius * CraterCalmStart;
                float calmEnd = Mathf.Max(crater.OuterRadius, calmStart + minCalmSpan);
                _craterX[i] = crater.Center.x;
                _craterZ[i] = crater.Center.y;
                _craterOuterSq[i] = calmEnd * calmEnd;
                _craterInvRadius[i] = 1f / crater.Radius;
                _craterDepth[i] = crater.Depth;
                _craterRimHeight[i] = crater.RimHeight;
                _craterInvRimWidth[i] = 1f / crater.RimWidth;
                _craterCalmStart[i] = calmStart;
                _craterCalmEnd[i] = calmEnd;
            }
        }

        public int Seed { get; }

        /// <summary>Square inscribed in the drivable floor: every point inside is drivable floor.</summary>
        public Rect PlayableArea { get; }

        /// <summary>Radius of the largest base-centred disc that is drivable floor in every direction.</summary>
        public float DrivableRadius { get; }

        /// <summary>Centre of The Peak's flat summit plateau (the global maximum of the surface).</summary>
        public Vector3 PeakSummit { get; }

        public float SummitRadius { get; }

        public float PadRadius { get; }

        /// <summary>Nominal floor radius before the rim warp (the floor edge wanders by the warp amplitude).</summary>
        public float FloorRadius { get; }

        public float CrestRadius { get; }

        /// <summary>Seeded impact craters followed by the hand-placed play bowls.</summary>
        public IReadOnlyList<Crater> Craters => _craters;

        public IReadOnlyList<Ramp> Ramps => _ramps;

        /// <summary>Unit XZ direction for a bearing in degrees clockwise from +Z.</summary>
        public static Vector2 BearingToDirection(float bearingDegrees)
        {
            float radians = bearingDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }

        public float SampleHeight(float x, float z)
        {
            return Sample(x, z).Height;
        }

        /// <summary>Normal from central differences of the analytic height (independent of any mesh).</summary>
        public Vector3 SampleNormal(float x, float z)
        {
            float dx = SampleHeight(x + NormalStep, z) - SampleHeight(x - NormalStep, z);
            float dz = SampleHeight(x, z + NormalStep) - SampleHeight(x, z - NormalStep);
            return new Vector3(-dx, 2f * NormalStep, -dz).normalized;
        }

        /// <summary>True on the basin floor inside the (warped) rim and away from The Peak's flanks.</summary>
        public bool IsDrivableFloor(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            if (r <= _rimWarpStart)
            {
                return true;
            }

            float dx = x - _peakX;
            float dz = z - _peakZ;
            return WarpedRadius(x, z, r) < _floorRadius && dx * dx + dz * dz > _peakFootprintSq;
        }

        /// <summary>
        /// Signed distance from (x, z) to the warped floor edge where the rim foothills begin: positive on the floor,
        /// negative on the rim.
        /// </summary>
        public float FloorEdgeDistance(float x, float z)
        {
            return _floorRadius - WarpedRadius(x, z, Mathf.Sqrt(x * x + z * z));
        }

        /// <summary>Height plus region weights at world XZ.</summary>
        public SurfaceSample Sample(float x, float z)
        {
            float r2 = x * x + z * z;
            if (r2 <= _padRadiusSq)
            {
                return default;
            }

            float r = (float)Math.Sqrt(r2);
            float height = _bowlRise * SmoothMath.Smootherstep(_padRadius, _floorRadius, r);
            float rw = WarpedRadius(x, z, r);
            float rimZone = SmoothMath.Smootherstep(_floorRadius, _crestRadius, rw);

            if (rw > _floorRadius)
            {
                height += RimProfile(rw);
                height += Mountains(x, z, rw);
                height += WallGullies(x, z, r, rw);
            }

            if (r > _farRangeStart)
            {
                height += _farRangeHeight * SmoothMath.Smootherstep(_farRangeStart, _farRangeFull, r)
                    * _farRangeNoise.Ridged(x * _invFarRangeWavelength, z * _invFarRangeWavelength, 3, 2f, 0.5f,
                        _ridgeSoftness);
            }

            float craterBowl = 0f;
            float craterRim = 0f;
            float floorWeight = 1f - SmoothMath.Smootherstep(_floorRadius, _foothillEnd, rw);
            if (floorWeight > 0f)
            {
                float calm = 1f;
                height += CraterHeights(x, z, ref calm, ref craterBowl, ref craterRim);
                height += RampHeights(x, z, ref calm);
                float floorDetail = floorWeight * SmoothMath.Smootherstep(_padRadius, _padBlendEnd, r);
                if (floorDetail > 0f)
                {
                    height += floorDetail * (Hills(x, z) + calm * Dunes(x, z));
                }
            }

            float dpx = x - _peakX;
            float dpz = z - _peakZ;
            float dp2 = dpx * dpx + dpz * dpz;
            if (dp2 < _peakFootprintSq)
            {
                height = Peak(dpx, dpz, dp2, height);
            }

            return new SurfaceSample(height, craterBowl, craterRim, rimZone);
        }

        private float WarpedRadius(float x, float z, float r)
        {
            if (r <= _rimWarpStart)
            {
                return r;
            }

            return r + _rimWarpAmplitude
                * _rimWarpNoise.Fractal(x * _invRimWarpWavelength, z * _invRimWarpWavelength, 2, 2f, 0.5f);
        }

        private float RimProfile(float rw)
        {
            float foothill = _foothillHeight * SmoothMath.Smootherstep(_floorRadius, _foothillEnd, rw);
            float wall = (_crestHeight - _foothillHeight) * SmoothMath.Smootherstep(_wallStart, _crestRadius, rw);
            float outer = _outerDrop * SmoothMath.Smootherstep(_crestRadius, _outerPlainRadius, rw);
            return foothill + wall - outer;
        }

        private float Mountains(float x, float z, float rw)
        {
            if (rw <= _wallStart || rw >= _outerPlainRadius)
            {
                return 0f;
            }

            float envelope = SmoothMath.Smootherstep(_wallStart, _crestRadius, rw)
                * (1f - SmoothMath.Smootherstep(_crestRadius, _outerPlainRadius, rw));
            float saddle = 1f - _saddleDepth
                * (0.5f + 0.5f * _saddleNoise.Sample(x * _invSaddleWavelength, z * _invSaddleWavelength));
            float ridges = _mountainNoise.Ridged(x * _invMountainWavelength, z * _invMountainWavelength,
                _mountainOctaves, 2f, 0.5f, _ridgeSoftness);
            return _mountainHeight * envelope * saddle * ridges;
        }

        /// <summary>
        /// Radial gullies and buttresses ribbing the inner wall. Sampled on the direction from the base (like The
        /// Peak's crags) so they run down the wall; zero on the floor, so the drivable area is untouched.
        /// </summary>
        private float WallGullies(float x, float z, float r, float rw)
        {
            float mask = SmoothMath.Bump((rw - _gullyCenter) / _gullyHalfWidth);
            if (mask <= 0f)
            {
                return 0f;
            }

            float inverse = 1f / r;
            float ridges = _gullyNoise.Ridged(x * inverse * _gullyAngularScale + rw * _invGullyRadial,
                z * inverse * _gullyAngularScale, 2, 2f, 0.5f, _ridgeSoftness);
            return _gullyHeight * mask * (2f * ridges - 1f);
        }

        private float CraterHeights(float x, float z, ref float calm, ref float bowl, ref float rim)
        {
            float sum = 0f;
            for (int i = 0; i < _craterX.Length; i++)
            {
                float dx = x - _craterX[i];
                float dz = z - _craterZ[i];
                float d2 = dx * dx + dz * dz;
                if (d2 >= _craterOuterSq[i])
                {
                    continue;
                }

                float d = (float)Math.Sqrt(d2);
                float t = d * _craterInvRadius[i];
                calm *= SmoothMath.Smootherstep(_craterCalmStart[i], _craterCalmEnd[i], d);
                if (t < 1f)
                {
                    sum -= _craterDepth[i] * (1f - SmoothMath.Smootherstep(0f, 1f, t));
                    bowl = Mathf.Max(bowl, 1f - t);
                }

                if (_craterRimHeight[i] > 0f)
                {
                    float lip = SmoothMath.Bump((t - 1f) * _craterInvRimWidth[i]);
                    sum += _craterRimHeight[i] * lip;
                    rim = Mathf.Max(rim, lip);
                }
            }

            return sum;
        }

        private float RampHeights(float x, float z, ref float calm)
        {
            float sum = 0f;
            for (int i = 0; i < _ramps.Length; i++)
            {
                Ramp ramp = _ramps[i];
                float dx = x - ramp.Crest.x;
                float dz = z - ramp.Crest.y;
                if (dx * dx + dz * dz >= _rampCalmBoundSq[i])
                {
                    continue;
                }

                float along = dx * ramp.Direction.x + dz * ramp.Direction.y;
                float across = dz * ramp.Direction.x - dx * ramp.Direction.y;
                float length = along < 0f ? ramp.RiseLength : ramp.FallLength;
                sum += ramp.Height * SmoothMath.Bump(along / length) * SmoothMath.Bump(across / ramp.HalfWidth);
                calm *= 1f - SmoothMath.Bump(along / (length * RampCalmScale))
                    * SmoothMath.Bump(across / (ramp.HalfWidth * RampCalmScale));
            }

            return sum;
        }

        private float Hills(float x, float z)
        {
            return _hillHeight * _hillNoise.Fractal(x * _invHillWavelength, z * _invHillWavelength, 2, 2f, 0.45f);
        }

        private float Dunes(float x, float z)
        {
            float meander = _duneMeander
                * _duneMeanderNoise.Sample(x * _invDuneMeanderWavelength, z * _invDuneMeanderWavelength);
            float phase = (x * _duneWindX + z * _duneWindZ + meander) * _duneWaveNumber;
            float coverage = SmoothMath.Lerp(_duneCoverageMin, 1f,
                0.5f + 0.5f * _duneCoverageNoise.Sample(x * _invDuneCoverageWavelength,
                    z * _invDuneCoverageWavelength));
            float wave = (float)Math.Sin(phase + _duneSkew * Math.Sin(phase));
            return _duneHeight * coverage * wave;
        }

        private float Peak(float dx, float dz, float d2, float height)
        {
            float d = (float)Math.Sqrt(d2);

            // Horn profile: (1 - t)^sharpness drops away steeply right below the summit, and the quadratic cap over
            // the first few percent of the flank keeps the slope continuous at the plateau edge.
            float t = Mathf.Clamp01((d - _summitRadius) / (_peakFootprint - _summitRadius));
            float capped = t < PeakCap ? t * t / (2f * PeakCap) : t - PeakCap * 0.5f;
            float blend = (float)Math.Pow(Mathf.Max(0f, 1f - capped / (1f - PeakCap * 0.5f)), _peakSharpness);

            // Buttresses and gullies run radially: the crag noise is sampled on a circle around the summit, drifting
            // slowly outwards. They are added before blending towards the summit, so they fade out on the plateau
            // (where the direction is undefined) and can never lift a point above it: the settings validation keeps
            // rim + crags below the summit height.
            float cragMask = SmoothMath.Smootherstep(_summitRadius, _summitRadius * 3f, d)
                * SmoothMath.Bump(d / _peakFootprint);
            float rough = height;
            if (cragMask > 0f)
            {
                float inverse = 1f / d;
                float ridges = _cragNoise.Ridged(dx * inverse * _cragAngularScale + d * _invCragRadialWavelength,
                    dz * inverse * _cragAngularScale, 2, 2f, 0.5f, _ridgeSoftness);
                rough += _peakCragHeight * cragMask * (2f * ridges - 1f);
            }

            return rough + (_peakHeight - rough) * blend;
        }

        private static Ramp[] ResolveRamps(RampPlacement[] placements)
        {
            int count = placements == null ? 0 : placements.Length;
            var ramps = new Ramp[count];
            for (int i = 0; i < count; i++)
            {
                RampPlacement placement = placements[i];
                Vector2 crest = BearingToDirection(placement.Bearing) * placement.Distance;
                Vector2 direction = BearingToDirection(placement.Bearing + placement.HeadingOffset);
                ramps[i] = new Ramp(crest, direction, placement.RiseLength, placement.FallLength,
                    placement.HalfWidth, placement.Height);
            }

            return ramps;
        }

        private Crater[] PlaceCraters(SurfaceSettings settings, uint baseSeed)
        {
            BowlPlacement[] bowls = settings.Bowls ?? Array.Empty<BowlPlacement>();
            var placed = new List<Crater>(settings.CraterCount + bowls.Length);
            for (int i = 0; i < bowls.Length; i++)
            {
                BowlPlacement bowl = bowls[i];
                Vector2 center = BearingToDirection(bowl.Bearing) * bowl.Distance;
                placed.Add(new Crater(center, bowl.Radius, bowl.Depth, bowl.LipHeight, BowlLipWidth, true));
            }

            var random = new SeededRandom(Hashing.Mix(baseSeed ^ CraterSalt));
            float clearance = settings.CraterClearance;
            float minDistance = _padBlendEnd + clearance;
            int seeded = 0;
            int attempts = settings.CraterCount * PlacementAttemptsPerCrater;
            for (int attempt = 0; attempt < attempts && seeded < settings.CraterCount; attempt++)
            {
                bool small = random.NextFloat() < settings.SmallCraterFraction;
                Vector2 range = small ? settings.SmallCraterRadius : settings.MediumCraterRadius;
                float radius = random.Range(range.x, range.y);
                float outer = radius * (1f + settings.CraterRimWidth);
                float inner = minDistance + outer;
                float outerLimit = DrivableRadius - outer - clearance;
                if (outerLimit <= inner)
                {
                    continue;
                }

                float angle = random.Range(0f, TwoPi);
                float distance = Mathf.Sqrt(random.Range(inner * inner, outerLimit * outerLimit));

                var center = new Vector2(Mathf.Sin(angle) * distance, Mathf.Cos(angle) * distance);
                if (!IsClear(center, outer + clearance, placed))
                {
                    continue;
                }

                float depth = radius * random.Range(settings.CraterDepthRatio.x, settings.CraterDepthRatio.y);
                float rim = radius * random.Range(settings.CraterRimRatio.x, settings.CraterRimRatio.y);
                placed.Add(new Crater(center, radius, depth, rim, settings.CraterRimWidth, false));
                seeded++;
            }

            // Seeded craters first, play bowls last: indices of seeded craters stay stable when bowls are edited.
            var ordered = new Crater[placed.Count];
            placed.CopyTo(bowls.Length, ordered, 0, seeded);
            placed.CopyTo(0, ordered, seeded, bowls.Length);
            return ordered;
        }

        private bool IsClear(Vector2 center, float reach, List<Crater> placed)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                if (Vector2.Distance(center, placed[i].Center) < reach + placed[i].OuterRadius)
                {
                    return false;
                }
            }

            for (int i = 0; i < _ramps.Length; i++)
            {
                if (Vector2.Distance(center, _ramps[i].Crest) < reach + _ramps[i].BoundingRadius)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
