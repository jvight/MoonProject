using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Tuning for the low circling Sun, the opening eclipse and the look C "Low sun" sunlit atmosphere (0.5.7
    /// SKY-CYCLE, decision L1). The earthlit night keeps <see cref="AtmosphereSettings"/> and <see cref="SkySettings"/>
    /// (look A "Moon grey") unchanged; as <see cref="SunClock"/> moves the Sun and <see cref="SkyCycleSystem"/> queries
    /// local sunlight at 07's position, the sky, fog, ambient and single shadow-casting key light blend between the
    /// earthlit night, the eclipse and look C's warm low sun.
    /// </summary>
    [Serializable]
    public sealed class SkyCycleSettings
    {
        [Header("Clock and Sun path")]
        [Tooltip("Length of one full lunar day (one 360-degree circle of the Sun around the horizon), seconds.")]
        [Range(300f, 14400f)]
        [SerializeField] private float _dayLengthSeconds = 7200f;

        [Header("The first day follows the opening (opening v2 §9.2)")]
        [Tooltip("Real seconds from the first charge to the Sun's limb breaking out from behind Earth (the diamond " +
            "ring): a breath, never a wait.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _eclipseBreakoutLead = 1f;

        [Tooltip("Clock seconds after the eclipse releases (the first charge) where the Sun holds at golden hour, " +
            "just clear of Earth, until the base is powered.")]
        [Range(5f, 600f)]
        [SerializeField] private float _goldenHourSeconds = 45f;

        [Tooltip("Elevation where the first day's Sun holds at last light after power-on, degrees: low over the rim, " +
            "long shadows, until a battery bank is ready for the night.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _lastLightElevation = 3f;

        [Tooltip("Real seconds the Sun takes to sink from golden hour to last light after power-on.")]
        [Range(10f, 600f)]
        [SerializeField] private float _lastLightSinkSeconds = 120f;

        [Tooltip("Elevation where the first sunset is over and the normal day pace takes over, degrees (below 0).")]
        [Range(-4f, -0.1f)]
        [SerializeField] private float _nightfallElevation = -1.5f;

        [Tooltip("Real seconds the first sunset takes, from last light to nightfall, once the night is ready.")]
        [Range(30f, 600f)]
        [SerializeField] private float _firstSunsetSeconds = 150f;

        [Tooltip("Clock time (seconds into Day 1) used when loading an older save that has no world.sky section: " +
            "starts after the opening eclipse in full low sunlight.")]
        [Range(60f, 3600f)]
        [SerializeField] private float _fallbackSunlitTimeSeconds = 900f;

        [Tooltip("Apparent diameter of the Sun's disc in the sky, degrees. ~0.8 degrees makes the Sun break out from " +
            "Earth's limb in 5-20 seconds at the 2-hour lunar day rate.")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _sunDiameter = 0.8f;

        [Tooltip("Bearing offset from Earth's centre where the Sun sits during the opening eclipse hold, degrees " +
            "clockwise: just inside Earth's right limb so the Sun is fully eclipsed at hold and breaks out on release.")]
        [Range(0f, 15f)]
        [SerializeField] private float _startBearingOffset = 6.20f;

        [Tooltip("Highest elevation the low circling Sun reaches above the horizon, degrees. Clears the basin rim " +
            "while keeping shadows long.")]
        [Range(8f, 35f)]
        [SerializeField] private float _maxElevation = 17.3f;

        [Tooltip("Lowest elevation the Sun sinks to below the horizon during the earthlit night, degrees. Tuned so " +
            "the earthlit night lasts roughly one third of the lunar day.")]
        [Range(-25f, -1f)]
        [SerializeField] private float _minElevation = -4.5f;

        [Tooltip("How many degrees ahead of the start bearing Day 1's peak Sun elevation sits, so the Sun climbs " +
            "slightly as it emerges from behind Earth's upper-right limb.")]
        [Range(0f, 60f)]
        [SerializeField] private float _initialPeakLead = 18f;

        [Tooltip("Degrees per lunar day that the Sun path's peak-elevation bearing precesses. Keeps the Sun passing " +
            "below Earth's lower limb on Days 2-4 so there is no repeat eclipse for at least three lunar days.")]
        [Range(30f, 90f)]
        [SerializeField] private float _nodalDriftPerDay = 64f;

        [Tooltip("Elevation band above the geometric horizon that counts as Sunrise (rising) or Sunset (falling), " +
            "degrees. Covers the basin's ~7-12.5 degree crater rim horizon as seen from the basin floor.")]
        [Range(0.5f, 15f)]
        [SerializeField] private float _twilightElevation = 12.5f;

        [Tooltip("Seconds after the Sun clears Earth's limb on Day 1 that count as the opening Sunrise phase while " +
            "low dawn light sweeps the basin.")]
        [Range(10f, 300f)]
        [SerializeField] private float _postEclipseSunriseSeconds = 60f;

        [Header("Local sunlight query (SunlightAt)")]
        [Tooltip("How far outward along the Sun's bearing SunlightAt marches terrain height samples to find the " +
            "local horizon, metres. 500 m reaches from home across the basin floor to the crater rim crest.")]
        [Range(50f, 1200f)]
        [SerializeField] private float _horizonDistance = 500f;

        [Tooltip("Step between terrain height samples along the Sun's azimuth in SunlightAt, metres.")]
        [Range(5f, 100f)]
        [SerializeField] private float _horizonStep = 25f;

        [Tooltip("Height above the local ground where a solar panel or 07's solar wing catches the Sun, metres.")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _receiverHeight = 1.5f;

        [Tooltip("Angular width of the soft ramp across the local terrain horizon edge in SunlightAt, degrees.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _horizonSoftness = 1.6f;

        [Tooltip("Smoothing time for blending the scene lights from SunlightAt at 07's position, seconds.")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _keySmoothingSeconds = 1.8f;

        [Tooltip("Exponent shaping how fast the sky, fog and haze turn to day as the basin-wide sunlight rises " +
            "(below 1: the day look arrives early in a sunrise, so dawn already reads as day).")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _dayBlendExponent = 0.65f;

        [Tooltip("Basin-wide sunlight (0..1) at which the key light hands over from the earthlight to the Sun: low, " +
            "so the first limb of dawn already lights the landscape with warm grazing light.")]
        [Range(0.005f, 0.5f)]
        [SerializeField] private float _dawnKeyCrossover = 0.04f;

        [Tooltip("Share of the sunlit ambient kept while 07 sits in a local ridge or crater shadow: high, so a local " +
            "shadow only eases the ambient a little and the basin never goes dark around 07.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _localShadowAmbientFloor = 0.85f;

        [Tooltip("Share of the Sun's rise (after the crossover) over which its key light fades in from nothing, so " +
            "the handover never pops.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _sunEmergenceShare = 0.08f;

        [Tooltip("Exponent shaping the Sun key light's rise after the crossover (below 1: it reaches strength early).")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _sunKeyCurveExponent = 0.55f;

        [Tooltip("Share of the full Sun key intensity at the very start of dawn (it grows to 1 with the Sun's rise).")]
        [Range(0f, 1f)]
        [SerializeField] private float _dawnKeyFloor = 0.42f;

        [Tooltip("Share of the full Sun shadow strength at the very start of dawn (it grows to 1 with the Sun's " +
            "rise): long grazing shadows read from the first light.")]
        [Range(0f, 1f)]
        [SerializeField] private float _dawnShadowFloor = 0.7f;

        [Tooltip("Seconds after releasing the opening eclipse over which the basin's sunlight ramps in as the Sun's " +
            "limb clears Earth.")]
        [Range(2f, 60f)]
        [SerializeField] private float _dawnSweepSeconds = 18f;

        [Header("Look C (Low sun) lighting")]
        [Tooltip("Colour of the directional Sun key light in full sunlit (look C 'Low sun'): warm-white so high " +
            "ground catches warm gold while staying under Art's reference light.")]
        [SerializeField] private Color _sunlitKeyColor = new Color(1.0f, 0.96f, 0.87f);

        [Tooltip("Warmer golden-amber tint of the Sun key light right at dawn break-out and sunset.")]
        [SerializeField] private Color _dawnKeyColor = new Color(1.0f, 0.84f, 0.62f);

        [Tooltip("Intensity of the directional Sun key light in full sunlit. Kept under Art's bloom cap so lit dust " +
            "and cream panels never glow like lamps.")]
        [Range(0.5f, 3.5f)]
        [SerializeField] private float _sunlitKeyIntensity = 2.08f;

        [Tooltip("Shadow strength of the directional Sun key light in full sunlit: crisp long shadows.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _sunlitShadowStrength = 0.86f;

        [Tooltip("Colour of the shadowless fill opposite the Sun in look C: cool sky bounce so shadows stay deep and " +
            "cool while built props keep their facets.")]
        [SerializeField] private Color _sunlitFillColor = new Color(0.60f, 0.72f, 0.96f);

        [Tooltip("Intensity of the shadowless fill in look C.")]
        [Range(0f, 1f)]
        [SerializeField] private float _sunlitFillIntensity = 0.52f;

        [Tooltip("Trilight sky ambient in look C: cool slate-blue so shadows stay deep, cool and readable.")]
        [SerializeField] private Color _sunlitAmbientSky = new Color(0.27f, 0.31f, 0.40f);

        [Tooltip("Trilight equator ambient in look C.")]
        [SerializeField] private Color _sunlitAmbientEquator = new Color(0.19f, 0.22f, 0.29f);

        [Tooltip("Trilight ground ambient in look C.")]
        [SerializeField] private Color _sunlitAmbientGround = new Color(0.11f, 0.12f, 0.16f);

        [Tooltip("Fog colour in look C: dark neutral-cool slate so the airless sky stays black while the far rim " +
            "stays grounded.")]
        [SerializeField] private Color _sunlitFogColor = new Color(0.048f, 0.052f, 0.068f);

        [Tooltip("Basin haze colour in look C: warm sunlit dust veil over the low basin floor.")]
        [SerializeField] private Color _sunlitHazeColor = new Color(0.25f, 0.23f, 0.20f);

        [Tooltip("Zenith sky colour in look C: deep black airless space.")]
        [SerializeField] private Color _sunlitSkyTop = new Color(0.010f, 0.014f, 0.028f);

        [Tooltip("Soft horizon glow colour in look C: warm golden-amber rim and silhouette bounce.")]
        [SerializeField] private Color _sunlitHorizonGlow = new Color(0.28f, 0.20f, 0.12f);

        [Tooltip("Thin horizon band colour in look C: warm gold rim setting the far mountains against the black sky.")]
        [SerializeField] private Color _sunlitHorizonBand = new Color(0.36f, 0.27f, 0.17f);

        [Tooltip("Colour of the Sun's disc in the sky.")]
        [SerializeField] private Color _sunDiscColor = new Color(1.0f, 0.95f, 0.84f);

        [Tooltip("Linear HDR intensity of the Sun's small hot core (and the diamond-ring bead on Earth's limb). Only " +
            "this small core crosses the bloom threshold.")]
        [Range(1.2f, 4f)]
        [SerializeField] private float _sunCoreIntensity = 2.2f;

        [Tooltip("Linear strength of the soft corona around the Sun's disc; kept well below the bloom threshold.")]
        [Range(0f, 1f)]
        [SerializeField] private float _sunCoronaStrength = 0.42f;

        [Tooltip("Star brightness multiplier during full sunlight (stars dim strongly while the eye is adapted to " +
            "bright sunlit ground and return at sunset).")]
        [Range(0f, 1.2f)]
        [SerializeField] private float _sunlitStarScale = 0.08f;

        [Tooltip("Milky Way brightness multiplier during full sunlight (dims strongly while the eye is adapted to " +
            "sunlit ground and returns at sunset).")]
        [Range(0f, 1f)]
        [SerializeField] private float _sunlitMilkyWayScale = 0.03f;

        [Header("Eclipse and Earthshine")]
        [Tooltip("Colour of the thin red-orange ring of refracted sunlight around Earth's disc during the eclipse.")]
        [SerializeField] private Color _eclipseRingColor = new Color(0.96f, 0.36f, 0.12f);

        [Tooltip("Strength of the red-orange refraction ring around Earth during the eclipse.")]
        [Range(0f, 3f)]
        [SerializeField] private float _eclipseRingStrength = 1.15f;

        [Tooltip("Width of the red-orange refraction ring around Earth during the eclipse, as a fraction of Earth's " +
            "angular radius.")]
        [Range(0.02f, 0.25f)]
        [SerializeField] private float _eclipseRingWidth = 0.085f;

        [Tooltip("Star brightness multiplier during the eclipse (stars at their brightest).")]
        [Range(1f, 2.5f)]
        [SerializeField] private float _eclipseStarScale = 1.45f;

        [Tooltip("Minimum fraction of full earthlight intensity when Earth is a thin crescent outside eclipse.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _minEarthPhaseLight = 0.4f;

        [Tooltip("Key light intensity during total eclipse: very dark ground so 07's and the lander's lamps carry " +
            "the frame, yet readable enough to see the terrain facets.")]
        [Range(0.05f, 0.8f)]
        [SerializeField] private float _eclipseKeyIntensity = 0.44f;

        [Tooltip("Shadowless fill intensity during total eclipse.")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _eclipseFillIntensity = 0.24f;

        [Tooltip("Multiplier on the trilight ambient during total eclipse so the ground is very dark but readable.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _eclipseAmbientScale = 0.68f;

        public float DayLengthSeconds => _dayLengthSeconds;
        public float EclipseBreakoutLead => _eclipseBreakoutLead;
        public float GoldenHourSeconds => _goldenHourSeconds;
        public float LastLightElevation => _lastLightElevation;
        public float LastLightSinkSeconds => _lastLightSinkSeconds;
        public float NightfallElevation => _nightfallElevation;
        public float FirstSunsetSeconds => _firstSunsetSeconds;
        public float FallbackSunlitTimeSeconds => _fallbackSunlitTimeSeconds;
        public float SunDiameter => _sunDiameter;
        public float StartBearingOffset => _startBearingOffset;
        public float MaxElevation => _maxElevation;
        public float MinElevation => _minElevation;
        public float InitialPeakLead => _initialPeakLead;
        public float NodalDriftPerDay => _nodalDriftPerDay;
        public float TwilightElevation => _twilightElevation;
        public float PostEclipseSunriseSeconds => _postEclipseSunriseSeconds;
        public float HorizonDistance => _horizonDistance;
        public float HorizonStep => _horizonStep;
        public float ReceiverHeight => _receiverHeight;
        public float HorizonSoftness => _horizonSoftness;
        public float KeySmoothingSeconds => _keySmoothingSeconds;
        public float DayBlendExponent => _dayBlendExponent;
        public float DawnKeyCrossover => _dawnKeyCrossover;
        public float LocalShadowAmbientFloor => _localShadowAmbientFloor;
        public float SunEmergenceShare => _sunEmergenceShare;
        public float SunKeyCurveExponent => _sunKeyCurveExponent;
        public float DawnKeyFloor => _dawnKeyFloor;
        public float DawnShadowFloor => _dawnShadowFloor;
        public float DawnSweepSeconds => _dawnSweepSeconds;
        public Color SunlitKeyColor => _sunlitKeyColor;
        public Color DawnKeyColor => _dawnKeyColor;
        public float SunlitKeyIntensity => _sunlitKeyIntensity;
        public float SunlitShadowStrength => _sunlitShadowStrength;
        public Color SunlitFillColor => _sunlitFillColor;
        public float SunlitFillIntensity => _sunlitFillIntensity;
        public Color SunlitAmbientSky => _sunlitAmbientSky;
        public Color SunlitAmbientEquator => _sunlitAmbientEquator;
        public Color SunlitAmbientGround => _sunlitAmbientGround;
        public Color SunlitFogColor => _sunlitFogColor;
        public Color SunlitHazeColor => _sunlitHazeColor;
        public Color SunlitSkyTop => _sunlitSkyTop;
        public Color SunlitHorizonGlow => _sunlitHorizonGlow;
        public Color SunlitHorizonBand => _sunlitHorizonBand;
        public Color SunDiscColor => _sunDiscColor;
        public float SunCoreIntensity => _sunCoreIntensity;
        public float SunCoronaStrength => _sunCoronaStrength;
        public float SunlitStarScale => _sunlitStarScale;
        public float SunlitMilkyWayScale => _sunlitMilkyWayScale;
        public Color EclipseRingColor => _eclipseRingColor;
        public float EclipseRingStrength => _eclipseRingStrength;
        public float EclipseRingWidth => _eclipseRingWidth;
        public float EclipseStarScale => _eclipseStarScale;
        public float MinEarthPhaseLight => _minEarthPhaseLight;
        public float EclipseKeyIntensity => _eclipseKeyIntensity;
        public float EclipseFillIntensity => _eclipseFillIntensity;
        public float EclipseAmbientScale => _eclipseAmbientScale;

        public string Validate()
        {
            if (_dayLengthSeconds <= 0f)
            {
                return "SkyCycle.DayLengthSeconds must be positive.";
            }

            if (_lastLightElevation >= _maxElevation || _nightfallElevation <= _minElevation)
            {
                return "SkyCycle.LastLightElevation must be under MaxElevation and NightfallElevation above " +
                       "MinElevation, so the first day reaches both.";
            }

            if (_maxElevation <= 0f || _minElevation >= 0f || _minElevation >= _maxElevation)
            {
                return "SkyCycle.MinElevation must be negative and less than positive MaxElevation.";
            }

            if (_horizonDistance <= 0f || _horizonStep <= 0f)
            {
                return "SkyCycle.HorizonDistance and HorizonStep must be positive.";
            }

            return null;
        }
    }
}
