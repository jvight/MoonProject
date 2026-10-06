using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Every number that shapes the moon surface (<see cref="MoonSurface"/>). Distances are metres in world space,
    /// bearings are degrees clockwise from +Z (north) seen from above, the base sits at the origin at height 0.
    /// </summary>
    [Serializable]
    public sealed class SurfaceSettings
    {
        [Header("Base pad")]
        [Tooltip("Radius of the perfectly flat pad at the origin where the base sits (height exactly 0).")]
        [Range(5f, 80f)]
        [SerializeField] private float _padRadius = 25f;

        [Tooltip("Distance beyond the pad over which dunes and the bowl fade in. Larger = softer pad edge.")]
        [Range(5f, 120f)]
        [SerializeField] private float _padBlend = 30f;

        [Header("Basin floor")]
        [Tooltip("Radius where the drivable floor ends and the rim foothills begin (before the rim warp).")]
        [Range(100f, 1000f)]
        [SerializeField] private float _floorRadius = 320f;

        [Tooltip("How much higher the floor edge sits than the centre: the gentle bowl of the basin.")]
        [Range(0f, 40f)]
        [SerializeField] private float _bowlRise = 6f;

        [Header("Rim")]
        [Tooltip("How far the rim outline wanders in and out, metres. Breaks the perfect circle.")]
        [Range(0f, 80f)]
        [SerializeField] private float _rimWarpAmplitude = 18f;

        [Tooltip("Wavelength of the rim outline wander, metres.")]
        [Range(50f, 1000f)]
        [SerializeField] private float _rimWarpWavelength = 300f;

        [Tooltip("Height the foothills climb above the floor edge before the steep wall starts.")]
        [Range(0f, 40f)]
        [SerializeField] private float _foothillHeight = 12f;

        [Tooltip("Radial width of the foothills, metres.")]
        [Range(10f, 200f)]
        [SerializeField] private float _foothillWidth = 55f;

        [Tooltip("Distance past the floor radius where the steep inner wall starts climbing.")]
        [Range(0f, 150f)]
        [SerializeField] private float _wallStartOffset = 30f;

        [Tooltip("Radius of the rim crest, metres.")]
        [Range(150f, 1500f)]
        [SerializeField] private float _crestRadius = 420f;

        [Tooltip("Baseline crest height above the floor edge (before mountains are added).")]
        [Range(10f, 150f)]
        [SerializeField] private float _crestHeight = 46f;

        [Tooltip("Radius where the outer slope of the rim levels out into the outer plain.")]
        [Range(200f, 2000f)]
        [SerializeField] private float _outerPlainRadius = 660f;

        [Tooltip("Absolute height of the outer plain beyond the rim.")]
        [Range(-50f, 100f)]
        [SerializeField] private float _outerPlainHeight = 26f;

        [Header("Rim mountains")]
        [Tooltip("Height of the jagged ridges stacked on the rim crest.")]
        [Range(0f, 120f)]
        [SerializeField] private float _mountainHeight = 62f;

        [Tooltip("Wavelength of the largest ridges, metres.")]
        [Range(20f, 500f)]
        [SerializeField] private float _mountainWavelength = 150f;

        [Tooltip("Ridge detail octaves. More = more jagged.")]
        [Range(1, 6)]
        [SerializeField] private int _mountainOctaves = 3;

        [Tooltip("Rounding of each ridge crest in noise units. Smaller = sharper crests.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _ridgeSoftness = 0.08f;

        [Tooltip("How deep the saddles between summits are, as a fraction of the mountain height.")]
        [Range(0f, 1f)]
        [SerializeField] private float _saddleDepth = 0.6f;

        [Tooltip("Wavelength of the summit/saddle rhythm along the rim, metres.")]
        [Range(50f, 1500f)]
        [SerializeField] private float _saddleWavelength = 330f;

        [Tooltip("Depth of the gullies and buttresses ribbing the inner rim wall, metres.")]
        [Range(0f, 30f)]
        [SerializeField] private float _wallGullyHeight = 9f;

        [Tooltip("Approximate number of gullies around the whole rim.")]
        [Range(8f, 200f)]
        [SerializeField] private float _wallGullyCount = 70f;

        [Header("The Peak")]
        [Tooltip("Bearing of The Peak from the base, degrees clockwise from +Z. The rover starts facing +Z.")]
        [Range(0f, 360f)]
        [SerializeField] private float _peakBearing = 12f;

        [Tooltip("Distance of The Peak's summit from the base, metres.")]
        [Range(100f, 1500f)]
        [SerializeField] private float _peakDistance = 440f;

        [Tooltip("Absolute summit height. Must stay above every other point of the world.")]
        [Range(50f, 400f)]
        [SerializeField] private float _peakHeight = 150f;

        [Tooltip("Radius of The Peak's footprint, metres.")]
        [Range(20f, 400f)]
        [SerializeField] private float _peakFootprint = 105f;

        [Tooltip("Radius of the flat summit plateau (room for the broken satellite dish), metres.")]
        [Range(1f, 40f)]
        [SerializeField] private float _summitRadius = 8f;

        [Tooltip("Profile exponent of The Peak's flanks. 1 = domed, 2+ = a sharp horn.")]
        [Range(1f, 4f)]
        [SerializeField] private float _peakSharpness = 2.4f;

        [Tooltip("Height of the buttress ridges and gullies running down The Peak's flanks.")]
        [Range(0f, 40f)]
        [SerializeField] private float _peakCragHeight = 20f;

        [Tooltip("Approximate number of buttress ridges around The Peak.")]
        [Range(2f, 16f)]
        [SerializeField] private float _peakRidgeCount = 7f;

        [Header("Dunes")]
        [Tooltip("Height of the broad rolling hills on the floor.")]
        [Range(0f, 20f)]
        [SerializeField] private float _hillHeight = 5.5f;

        [Tooltip("Wavelength of the broad rolling hills, metres.")]
        [Range(40f, 600f)]
        [SerializeField] private float _hillWavelength = 200f;

        [Tooltip("Height of the dune waves (crest to mean). Crests give floaty hops at speed.")]
        [Range(0f, 6f)]
        [SerializeField] private float _duneHeight = 1.4f;

        [Tooltip("Wavelength between dune crests, metres.")]
        [Range(10f, 200f)]
        [SerializeField] private float _duneWavelength = 46f;

        [Tooltip("Bearing the dune crests face, degrees clockwise from +Z.")]
        [Range(0f, 360f)]
        [SerializeField] private float _duneWindBearing = 65f;

        [Tooltip("Asymmetry of the dunes: gentle windward side, steeper lee side.")]
        [Range(0f, 0.8f)]
        [SerializeField] private float _duneSkew = 0.35f;

        [Tooltip("How far the dune lines meander, metres.")]
        [Range(0f, 60f)]
        [SerializeField] private float _duneMeander = 16f;

        [Tooltip("Wavelength of the dune meander, metres.")]
        [Range(20f, 600f)]
        [SerializeField] private float _duneMeanderWavelength = 140f;

        [Tooltip("Dune strength in the calmest patches (fraction of full height).")]
        [Range(0f, 1f)]
        [SerializeField] private float _duneCoverageMin = 0.25f;

        [Tooltip("Size of the calm and duney patches, metres.")]
        [Range(40f, 800f)]
        [SerializeField] private float _duneCoverageWavelength = 230f;

        [Tooltip("Height of the fine grain undulation that gives every floor facet its own tilt, so the low-poly " +
            "facets read even on gentle ground. It is driven over: keep it small.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _grainHeight = 0.12f;

        [Tooltip("Wavelength of the grain undulation, metres (a few terrain cells).")]
        [Range(2f, 40f)]
        [SerializeField] private float _grainWavelength = 7f;

        [Header("Craters")]
        [Tooltip("Number of seeded small and medium craters on the floor.")]
        [Range(0, 80)]
        [SerializeField] private int _craterCount = 25;

        [Tooltip("Fraction of the craters that are small.")]
        [Range(0f, 1f)]
        [SerializeField] private float _smallCraterFraction = 0.6f;

        [Tooltip("Radius range of small craters (x = min, y = max), metres.")]
        [SerializeField] private Vector2 _smallCraterRadius = new Vector2(6f, 13f);

        [Tooltip("Radius range of medium craters (x = min, y = max), metres.")]
        [SerializeField] private Vector2 _mediumCraterRadius = new Vector2(16f, 34f);

        [Tooltip("Crater depth as a fraction of its radius (x = min, y = max). Keep low: walls must stay drivable.")]
        [SerializeField] private Vector2 _craterDepthRatio = new Vector2(0.06f, 0.095f);

        [Tooltip("Raised rim height as a fraction of the crater radius (x = min, y = max). Low = old, soft crater.")]
        [SerializeField] private Vector2 _craterRimRatio = new Vector2(0.02f, 0.045f);

        [Tooltip("Width of the raised rim as a fraction of the crater radius.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _craterRimWidth = 0.65f;

        [Tooltip("Extra clearance kept between crater footprints and other features, metres.")]
        [Range(0f, 40f)]
        [SerializeField] private float _craterClearance = 6f;

        [Header("Play features")]
        [Tooltip("Hand-placed ramps for floaty hops.")]
        [SerializeField] private RampPlacement[] _ramps =
        {
            new RampPlacement(38f, 95f, 0f, 20f, 30f, 20f, 3f),
            new RampPlacement(205f, 150f, 25f, 22f, 32f, 22f, 3.4f),
        };

        [Tooltip("Hand-placed shallow bowls to swoop through.")]
        [SerializeField] private BowlPlacement[] _bowls =
        {
            new BowlPlacement(292f, 125f, 32f, 3.6f, 0.6f),
            new BowlPlacement(140f, 215f, 40f, 4.4f, 0.7f),
        };

        [Header("Far field")]
        [Tooltip("Radius where the distant ranges beyond the rim start rising.")]
        [Range(500f, 4000f)]
        [SerializeField] private float _farRangeStart = 950f;

        [Tooltip("Radius where the distant ranges reach full height.")]
        [Range(500f, 6000f)]
        [SerializeField] private float _farRangeFull = 1700f;

        [Tooltip("Height of the distant ranges. Keep below The Peak.")]
        [Range(0f, 300f)]
        [SerializeField] private float _farRangeHeight = 105f;

        [Tooltip("Wavelength of the distant ranges, metres.")]
        [Range(100f, 3000f)]
        [SerializeField] private float _farRangeWavelength = 380f;

        public float PadRadius => _padRadius;
        public float PadBlend => _padBlend;
        public float FloorRadius => _floorRadius;
        public float BowlRise => _bowlRise;
        public float RimWarpAmplitude => _rimWarpAmplitude;
        public float RimWarpWavelength => _rimWarpWavelength;
        public float FoothillHeight => _foothillHeight;
        public float FoothillWidth => _foothillWidth;
        public float WallStartOffset => _wallStartOffset;
        public float CrestRadius => _crestRadius;
        public float CrestHeight => _crestHeight;
        public float OuterPlainRadius => _outerPlainRadius;
        public float OuterPlainHeight => _outerPlainHeight;
        public float MountainHeight => _mountainHeight;
        public float MountainWavelength => _mountainWavelength;
        public int MountainOctaves => _mountainOctaves;
        public float RidgeSoftness => _ridgeSoftness;
        public float SaddleDepth => _saddleDepth;
        public float SaddleWavelength => _saddleWavelength;
        public float WallGullyHeight => _wallGullyHeight;
        public float WallGullyCount => _wallGullyCount;
        public float PeakBearing => _peakBearing;
        public float PeakDistance => _peakDistance;
        public float PeakHeight => _peakHeight;
        public float PeakFootprint => _peakFootprint;
        public float SummitRadius => _summitRadius;
        public float PeakSharpness => _peakSharpness;
        public float PeakCragHeight => _peakCragHeight;
        public float PeakRidgeCount => _peakRidgeCount;
        public float HillHeight => _hillHeight;
        public float HillWavelength => _hillWavelength;
        public float DuneHeight => _duneHeight;
        public float DuneWavelength => _duneWavelength;
        public float DuneWindBearing => _duneWindBearing;
        public float DuneSkew => _duneSkew;
        public float DuneMeander => _duneMeander;
        public float DuneMeanderWavelength => _duneMeanderWavelength;
        public float DuneCoverageMin => _duneCoverageMin;
        public float DuneCoverageWavelength => _duneCoverageWavelength;
        public float GrainHeight => _grainHeight;
        public float GrainWavelength => _grainWavelength;
        public int CraterCount => _craterCount;
        public float SmallCraterFraction => _smallCraterFraction;
        public Vector2 SmallCraterRadius => _smallCraterRadius;
        public Vector2 MediumCraterRadius => _mediumCraterRadius;
        public Vector2 CraterDepthRatio => _craterDepthRatio;
        public Vector2 CraterRimRatio => _craterRimRatio;
        public float CraterRimWidth => _craterRimWidth;
        public float CraterClearance => _craterClearance;
        public RampPlacement[] Ramps => _ramps;
        public BowlPlacement[] Bowls => _bowls;
        public float FarRangeStart => _farRangeStart;
        public float FarRangeFull => _farRangeFull;
        public float FarRangeHeight => _farRangeHeight;
        public float FarRangeWavelength => _farRangeWavelength;

        /// <summary>Returns null when the settings describe a valid surface, otherwise the problem.</summary>
        public string Validate()
        {
            if (_padRadius + _padBlend >= _floorRadius)
            {
                return "Pad radius + pad blend must be smaller than the floor radius.";
            }

            if (_floorRadius - _rimWarpAmplitude <= _padRadius + _padBlend)
            {
                return "Rim warp is so large that the rim would reach the base pad.";
            }

            if (_floorRadius + _wallStartOffset >= _crestRadius)
            {
                return "The steep wall must start before the crest radius.";
            }

            if (_crestRadius >= _outerPlainRadius)
            {
                return "The outer plain must start beyond the crest.";
            }

            if (_farRangeStart >= _farRangeFull)
            {
                return "Far ranges must reach full height beyond the radius where they start.";
            }

            if (_summitRadius >= _peakFootprint)
            {
                return "The summit plateau must be smaller than The Peak's footprint.";
            }

            if (_smallCraterRadius.x <= 0f || _smallCraterRadius.x > _smallCraterRadius.y
                || _mediumCraterRadius.x <= 0f || _mediumCraterRadius.x > _mediumCraterRadius.y)
            {
                return "Crater radius ranges need 0 < min <= max.";
            }

            if (_craterDepthRatio.x < 0f || _craterDepthRatio.x > _craterDepthRatio.y
                || _craterRimRatio.x < 0f || _craterRimRatio.x > _craterRimRatio.y)
            {
                return "Crater depth and rim ratio ranges need 0 <= min <= max.";
            }

            float highestRim = _bowlRise + _crestHeight + _mountainHeight + _peakCragHeight + _wallGullyHeight;
            if (highestRim >= _peakHeight || _outerPlainHeight + _farRangeHeight >= _peakHeight)
            {
                return "Rim mountains, gullies, crags or far ranges could rise above The Peak; lower them or raise it.";
            }

            return null;
        }
    }
}
