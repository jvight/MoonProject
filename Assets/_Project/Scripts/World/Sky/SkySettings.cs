using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The night sky: gradient, stars, milky way, shooting stars and Earth. Pushed to the sky and Earth shaders as
    /// global properties by <see cref="SkyShaderGlobals"/>; IWorldLayout.EarthDirection comes from here too.
    /// </summary>
    [Serializable]
    public sealed class SkySettings
    {
        [Header("Gradient")]
        [Tooltip("Colour at the zenith. The horizon uses the fog colour so distant hills melt into the sky.")]
        [SerializeField] private Color _skyTop = new Color32(0x0B, 0x0E, 0x2A, 0xFF);

        [Tooltip("Soft violet glow hugging the horizon (added on top of the gradient).")]
        [SerializeField] private Color _horizonGlow = new Color(0.2f, 0.13f, 0.34f);

        [Tooltip("Height of the horizon glow, as the sine of the elevation where it has faded to ~37%.")]
        [Range(0.01f, 0.6f)]
        [SerializeField] private float _horizonGlowHeight = 0.16f;

        [Tooltip("The faint, thin band of light right along the horizon line (added on top of the glow): it sets the " +
            "far rim against the sky and makes the horizon feel far away.")]
        [SerializeField] private Color _horizonBand = new Color(0.2f, 0.15f, 0.3f);

        [Tooltip("Height of the thin horizon band, as the sine of the elevation where it has faded to ~37%.")]
        [Range(0.002f, 0.2f)]
        [SerializeField] private float _horizonBandHeight = 0.03f;

        [Tooltip("Exponent of the horizon-to-zenith blend. Lower = the horizon colour climbs higher.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _gradientExponent = 0.55f;

        [Header("Stars")]
        [Tooltip("Fraction of star cells that hold a star (dense = 0.5+).")]
        [Range(0f, 1f)]
        [SerializeField] private float _starDensity = 0.45f;

        [Tooltip("Brightness of the stars.")]
        [Range(0f, 8f)]
        [SerializeField] private float _starBrightness = 0.45f;

        [Tooltip("Power-law exponent of the star counts: the number of stars brighter than b falls as b to the " +
            "minus this. Higher = fewer bright stars; ~1.5 gives a sky of faint dust with a handful of bright ones.")]
        [Range(0.8f, 4f)]
        [SerializeField] private float _starPowerLaw = 1.5f;

        [Tooltip("Core size of a faint star in screen pixels; brighter stars are a little larger.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _starSize = 0.9f;

        [Tooltip("Soft glow around the brightest stars (0 = none). Gentle, never a flare.")]
        [Range(0f, 1f)]
        [SerializeField] private float _starGlow = 0.22f;

        [Tooltip("Share of the stars that twinkle (0..1). Only a few.")]
        [Range(0f, 1f)]
        [SerializeField] private float _twinkleShare = 0.15f;

        [Tooltip("Twinkle speed (radians per second). Slow: a breath, never a flicker.")]
        [Range(0f, 3f)]
        [SerializeField] private float _twinkleSpeed = 0.45f;

        [Tooltip("How deep a twinkle dims and brightens its star (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _twinkleDepth = 0.35f;

        [Header("Milky way")]
        [Tooltip("Bearing of the point where the milky way band crosses the horizon highest, degrees.")]
        [Range(0f, 360f)]
        [SerializeField] private float _milkyWayBearing = 120f;

        [Tooltip("Tilt of the milky way band from vertical, degrees.")]
        [Range(0f, 90f)]
        [SerializeField] private float _milkyWayTilt = 35f;

        [Tooltip("Colour of the milky way haze.")]
        [SerializeField] private Color _milkyWayColor = new Color(0.36f, 0.3f, 0.6f);

        [Tooltip("Angular half-width of the band, radians.")]
        [Range(0.02f, 0.6f)]
        [SerializeField] private float _milkyWayWidth = 0.17f;

        [Tooltip("Strength of the milky way haze.")]
        [Range(0f, 2f)]
        [SerializeField] private float _milkyWayIntensity = 0.45f;

        [Header("Earth")]
        [Tooltip("Bearing of Earth from the base, degrees clockwise from +Z. The rover starts facing +Z.")]
        [Range(0f, 360f)]
        [SerializeField] private float _earthBearing = 338f;

        [Tooltip("Elevation of Earth's centre above the horizon, degrees. Low and huge reads as a dreamy anchor.")]
        [Range(5f, 80f)]
        [SerializeField] private float _earthElevation = 16f;

        [Tooltip("Apparent diameter of Earth, degrees (the real one is ~2; dreams are bigger).")]
        [Range(1f, 40f)]
        [SerializeField] private float _earthDiameter = 13f;

        [Tooltip("Subdivisions of Earth's icosphere: 2 = 320 chunky facets.")]
        [Range(1, 4)]
        [SerializeField] private int _earthSubdivisions = 2;

        [Tooltip("Fraction of Earth's faces that are land.")]
        [Range(0f, 1f)]
        [SerializeField] private float _earthLandFraction = 0.36f;

        [Tooltip("Fraction of Earth's faces that are cloud or ice.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _earthCloudFraction = 0.1f;

        [Tooltip("Direction of the sun lighting Earth, relative to the viewer: x = right, y = up, " +
            "z = toward the viewer. Sets Earth's phase.")]
        [SerializeField] private Vector3 _earthSunDirection = new Vector3(-0.75f, 0.35f, 0.55f);

        [Tooltip("Pale, cool haze Earth's ocean, land and cloud fade toward: far away and calm, a cool jewel rather " +
            "than saturated cyan.")]
        [SerializeField] private Color _earthHaze = new Color(0.72f, 0.78f, 0.9f);

        [Tooltip("How far Earth's face colours fade toward the haze (0 = the palette's own ocean and land).")]
        [Range(0f, 1f)]
        [SerializeField] private float _earthPaleness = 0.3f;

        [Tooltip("Brightness of Earth's night side (0 = black).")]
        [Range(0f, 1f)]
        [SerializeField] private float _earthNightBrightness = 0.22f;

        [Tooltip("Emissive strength of Earth's lit side. Keep it soft: Earth is cool and beautiful, but the warm " +
            "lamps of home must win the eye (and the bloom).")]
        [Range(0f, 4f)]
        [SerializeField] private float _earthGlow = 0.72f;

        [Tooltip("Colour of Earth's atmosphere rim and halo: a pale, cool blue that sits in the violet sky rather than " +
            "a cyan ring around it.")]
        [SerializeField] private Color _earthAtmosphere = new Color(0.6f, 0.72f, 0.95f);

        [Tooltip("Strength of the atmosphere rim along Earth's facets (on top of the lit side).")]
        [Range(0f, 2f)]
        [SerializeField] private float _earthRim = 0.35f;

        [Tooltip("Angular size of the soft halo around Earth, as a fraction of its radius.")]
        [Range(0f, 2f)]
        [SerializeField] private float _earthHaloSize = 0.6f;

        [Tooltip("Strength of the soft halo around Earth.")]
        [Range(0f, 2f)]
        [SerializeField] private float _earthHaloStrength = 0.16f;

        [Tooltip("Strength of the thin atmospheric limb hugging Earth's edge (brightest on its sunlit side).")]
        [Range(0f, 4f)]
        [SerializeField] private float _earthLimb = 0.5f;

        [Tooltip("Width of the atmospheric limb, as a fraction of Earth's radius.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _earthLimbWidth = 0.06f;

        [Tooltip("Seconds per Earth rotation. Slow enough to notice only after a while.")]
        [Range(10f, 3600f)]
        [SerializeField] private float _earthSpinPeriod = 900f;

        [Header("Shooting stars")]
        [Tooltip("Average seconds between shooting stars.")]
        [Range(5f, 600f)]
        [SerializeField] private float _shootingStarPeriod = 38f;

        [Tooltip("Seconds a shooting star takes to cross. Slow and gentle.")]
        [Range(0.3f, 6f)]
        [SerializeField] private float _shootingStarDuration = 2.4f;

        [Tooltip("Brightness of shooting stars.")]
        [Range(0f, 10f)]
        [SerializeField] private float _shootingStarBrightness = 3f;

        public Color SkyTop => _skyTop;
        public Color HorizonGlow => _horizonGlow;
        public float HorizonGlowHeight => _horizonGlowHeight;
        public Color HorizonBand => _horizonBand;
        public float HorizonBandHeight => _horizonBandHeight;
        public float GradientExponent => _gradientExponent;
        public float StarDensity => _starDensity;
        public float StarBrightness => _starBrightness;
        public float StarPowerLaw => _starPowerLaw;
        public float StarSize => _starSize;
        public float StarGlow => _starGlow;
        public float TwinkleShare => _twinkleShare;
        public float TwinkleSpeed => _twinkleSpeed;
        public float TwinkleDepth => _twinkleDepth;
        public float MilkyWayBearing => _milkyWayBearing;
        public float MilkyWayTilt => _milkyWayTilt;
        public Color MilkyWayColor => _milkyWayColor;
        public float MilkyWayWidth => _milkyWayWidth;
        public float MilkyWayIntensity => _milkyWayIntensity;
        public float EarthBearing => _earthBearing;
        public float EarthElevation => _earthElevation;
        public float EarthDiameter => _earthDiameter;
        public int EarthSubdivisions => _earthSubdivisions;
        public float EarthLandFraction => _earthLandFraction;
        public float EarthCloudFraction => _earthCloudFraction;
        public Vector3 EarthSunDirection => _earthSunDirection;
        public Color EarthHaze => _earthHaze;
        public float EarthPaleness => _earthPaleness;
        public float EarthNightBrightness => _earthNightBrightness;
        public float EarthGlow => _earthGlow;
        public Color EarthAtmosphere => _earthAtmosphere;
        public float EarthRim => _earthRim;
        public float EarthHaloSize => _earthHaloSize;
        public float EarthHaloStrength => _earthHaloStrength;
        public float EarthLimb => _earthLimb;
        public float EarthLimbWidth => _earthLimbWidth;
        public float EarthSpinPeriod => _earthSpinPeriod;
        public float ShootingStarPeriod => _shootingStarPeriod;
        public float ShootingStarDuration => _shootingStarDuration;
        public float ShootingStarBrightness => _shootingStarBrightness;

        /// <summary>Unit vector from the world toward Earth's centre.</summary>
        public Vector3 EarthDirection
        {
            get
            {
                float elevation = _earthElevation * Mathf.Deg2Rad;
                Vector2 horizontal = MoonSurface.BearingToDirection(_earthBearing) * Mathf.Cos(elevation);
                return new Vector3(horizontal.x, Mathf.Sin(elevation), horizontal.y);
            }
        }

        /// <summary>Normal of the plane the milky way band lies in.</summary>
        public Vector3 MilkyWayAxis
        {
            get
            {
                Vector2 across = MoonSurface.BearingToDirection(_milkyWayBearing + 90f);
                float tilt = _milkyWayTilt * Mathf.Deg2Rad;
                return new Vector3(across.x * Mathf.Cos(tilt), Mathf.Sin(tilt), across.y * Mathf.Cos(tilt)).normalized;
            }
        }
    }
}
