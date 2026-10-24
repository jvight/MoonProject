using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Rover effects: tire tracks, rolling dust, the landing dust ring and the dust motes hanging in 07's lamp.
    /// Colours, meshes and particle curves are authored by the Rover builder from the palette; the amounts and timings
    /// players feel live here.
    /// </summary>
    [CreateAssetMenu(fileName = "RoverFxTuning", menuName = "MoonProject/Rover/Rover Fx Tuning")]
    public sealed class RoverFxTuning : ScriptableObject
    {
        [Header("Tire tracks")]
        [Tooltip("Width (m) of each track ribbon.")]
        [Range(0.05f, 0.8f)]
        [SerializeField] private float _trackWidth = 0.24f;

        [Tooltip("Distance (m) between track points. Smaller follows tight turns more closely.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _trackSegmentLength = 0.3f;

        [Tooltip("Seconds a track stays on the ground before it has faded away completely.")]
        [Range(1f, 1200f)]
        [SerializeField] private float _trackLifetime = 600f;

        [Tooltip("Fraction of the lifetime a track stays at full strength before fading out.")]
        [Range(0f, 1f)]
        [SerializeField] private float _trackFadeStart = 0.88f;

        [Tooltip("Strength of a fresh track (vertex alpha; the material colour sets how dark it can get).")]
        [Range(0f, 1f)]
        [SerializeField] private float _trackOpacity = 0.68f;

        [Tooltip("Height (m) the ribbon floats above the ground to stay clear of it.")]
        [Range(0f, 0.1f)]
        [SerializeField] private float _trackLift = 0.02f;

        [Tooltip("Track points kept per wheel (memory is fixed; the oldest are recycled).")]
        [Range(16, 4096)]
        [SerializeField] private int _trackCapacity = 1024;

        [Tooltip("How far (m) above and below a wheel socket the ground is searched for track points.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _trackGroundProbe = 0.75f;

        [Header("Rolling dust")]
        [Tooltip("Below this speed (m/s) the wheels raise no dust.")]
        [Range(0f, 5f)]
        [SerializeField] private float _dustMinSpeed = 0.6f;

        [Tooltip("Speed (m/s) at which dust reaches its full rate.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _dustFullSpeed = 7f;

        [Tooltip("Dust grains per second per side at full rate.")]
        [Range(0f, 100f)]
        [SerializeField] private float _dustMaxRate = 52f;

        [Tooltip("Seconds each kicked-up dust grain stays in flight before hitting the ground.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _dustLifetime = 0.45f;

        [Tooltip("Size (m) of a kicked-up dust grain spray cluster; never swells in vacuum.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _dustSize = 0.055f;

        [Tooltip("Initial speed (m/s) of a ballistic dust spray kicked up by the wheel.")]
        [Range(0f, 5f)]
        [SerializeField] private float _dustSpeed = 1.25f;

        [Tooltip("Atmospheric drag on dust (1/s): zero in lunar vacuum so grains fly a clean parabola.")]
        [Range(0f, 10f)]
        [SerializeField] private float _dustDrag = 0f;

        [Tooltip("Peak opacity of a dust spray (lunar regolith is fine and crisp).")]
        [Range(0f, 1f)]
        [SerializeField] private float _dustOpacity = 0.62f;

        [Tooltip("Fraction of world (lunar, 1.62 m/s^2) gravity acting on dust: 1.0 for true ballistic arcs.")]
        [Range(0f, 2f)]
        [SerializeField] private float _dustGravity = 1f;

        [Tooltip("Warm grey regolith tint matching the sunlit lunar ground (lighter grain shade).")]
        [SerializeField] private Color _dustColor = new Color(0.50f, 0.46f, 0.41f, 1f);

        [Tooltip("Darker warm grey regolith tint for grain-to-grain shade variation.")]
        [SerializeField] private Color _dustShadowColor = new Color(0.38f, 0.35f, 0.31f, 1f);

        [Tooltip("Longitudinal acceleration (m/s^2) at which wheel slip adds the full acceleration dust boost.")]
        [Range(0.2f, 10f)]
        [SerializeField] private float _dustAccelFull = 2f;

        [Tooltip("Extra fraction of dust emission added when accelerating or braking at full slip.")]
        [Range(0f, 2f)]
        [SerializeField] private float _dustAccelBoost = 0.5f;

        [Tooltip("Yaw rate (deg/s) at which hard-turning wheel scrub adds the full turn dust boost.")]
        [Range(5f, 180f)]
        [SerializeField] private float _dustTurnFullYawRate = 45f;

        [Tooltip("Extra fraction of dust emission added when turning hard.")]
        [Range(0f, 2f)]
        [SerializeField] private float _dustTurnBoost = 0.6f;

        [Tooltip("Ballistic skid dust grains per second kicked up under a towed load at full speed.")]
        [Range(0f, 100f)]
        [SerializeField] private float _towSkidDustMaxRate = 38f;

        [Header("Landing ring")]
        [Tooltip("Impact speed (m/s) of the softest landing that still raises a splash ring.")]
        [Range(0f, 5f)]
        [SerializeField] private float _landingMinImpact = 0.5f;

        [Tooltip("Impact speed (m/s) that raises the biggest splash ring.")]
        [Range(0.5f, 15f)]
        [SerializeField] private float _landingFullImpact = 4f;

        [Tooltip("Air time (s) from which a landing splash ring grows with the flight, even when the touchdown is soft.")]
        [Range(0f, 10f)]
        [SerializeField] private float _landingRingAirTimeFrom = 1f;

        [Tooltip("Air time (s) that earns the biggest splash ring.")]
        [Range(0.5f, 15f)]
        [SerializeField] private float _landingRingAirTimeFull = 3.5f;

        [Tooltip("Grains in the softest landing splash ring.")]
        [Range(1, 200)]
        [SerializeField] private int _landingMinCount = 16;

        [Tooltip("Grains in the biggest landing splash ring.")]
        [Range(1, 200)]
        [SerializeField] private int _landingMaxCount = 42;

        [Tooltip("Outward ballistic speed (m/s) of the softest landing splash ring.")]
        [Range(0f, 10f)]
        [SerializeField] private float _landingMinSpeed = 0.9f;

        [Tooltip("Outward ballistic speed (m/s) of the biggest landing splash ring.")]
        [Range(0f, 10f)]
        [SerializeField] private float _landingMaxSpeed = 1.7f;

        [Tooltip("Grain cluster size (m) in the softest landing splash ring.")]
        [Range(0.02f, 2f)]
        [SerializeField] private float _landingMinSize = 0.05f;

        [Tooltip("Grain cluster size (m) in the biggest landing splash ring.")]
        [Range(0.02f, 2f)]
        [SerializeField] private float _landingMaxSize = 0.085f;

        [Tooltip("Seconds a landing grain arc stays in flight before landing.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _landingLifetime = 0.5f;

        [Tooltip("Drag on the landing splash ring (1/s): zero in lunar vacuum.")]
        [Range(0f, 10f)]
        [SerializeField] private float _landingDrag = 0f;

        [Tooltip("Peak opacity of a landing grain cluster.")]
        [Range(0f, 1f)]
        [SerializeField] private float _landingOpacity = 0.58f;

        [Header("Lamp motes")]
        [Tooltip("Maximum dust motes alive at once in the headlamp's beam while driving.")]
        [Range(0, 200)]
        [SerializeField] private int _moteCount = 40;

        [Tooltip("Seconds each kicked-up mote lives as it arcs through the beam and falls under lunar gravity.")]
        [Range(0.2f, 30f)]
        [SerializeField] private float _moteLifetime = 0.9f;

        [Tooltip("Fraction of a mote's life spent fading in, and again fading out.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _moteFade = 0.25f;

        [Tooltip("Smallest mote size (m).")]
        [Range(0.002f, 0.2f)]
        [SerializeField] private float _moteMinSize = 0.012f;

        [Tooltip("Largest mote size (m).")]
        [Range(0.002f, 0.2f)]
        [SerializeField] private float _moteMaxSize = 0.026f;

        [Tooltip("Initial ballistic speed (m/s) of a kicked-up mote entering the headlamp beam.")]
        [Range(0f, 5f)]
        [SerializeField] private float _moteDrift = 1.4f;

        [Tooltip("Strength (m/s) of curling air noise (0 in lunar vacuum).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _moteWander = 0f;

        [Tooltip("How quickly the wandering air changes (unused when wander is 0).")]
        [Range(0.01f, 3f)]
        [SerializeField] private float _moteWanderFrequency = 0.35f;

        [Tooltip("Motes appear within this half-angle (deg) of the lamp's axis, inside its beam.")]
        [Range(1f, 60f)]
        [SerializeField] private float _moteSpread = 20f;

        [Tooltip("Distance (m) in front of the lamp over which motes appear (near the lens).")]
        [Range(0.2f, 10f)]
        [SerializeField] private float _moteReach = 2.6f;

        [Tooltip("Motes closer to the lens than this (m) fade out (they would be out of focus).")]
        [Range(0f, 2f)]
        [SerializeField] private float _moteNearFade = 0.3f;

        [Tooltip("How much dimmer a mote at the far end of the reach is than one near the lens (the light thins).")]
        [Range(0f, 1f)]
        [SerializeField] private float _moteFalloff = 0.75f;

        [Tooltip("Peak opacity of a mote fully in the beam (very subtle).")]
        [Range(0f, 1f)]
        [SerializeField] private float _moteOpacity = 0.5f;

        [Tooltip("Brightness of the motes' warm light (linear HDR multiplier). Kept under the grade's bloom threshold "
            + "(~1.25) so motes never bloom like stars and read as snowfall (M3-18 V7).")]
        [Range(0f, 8f)]
        [SerializeField] private float _moteBrightness = 1f;

        [Tooltip("Speed (m/s) below which no motes are kicked into the beam (parked in vacuum = 0 motes).")]
        [Range(0f, 5f)]
        [SerializeField] private float _moteFadeSpeed = 0.3f;

        [Tooltip("Speed (m/s) at which kicked-up beam motes reach full visibility.")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _moteGoneSpeed = 2.5f;

        [Tooltip("Half-life (s) of the motes ramping with speed (in and out).")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _moteVisibilityHalfLife = 0.18f;

        [Tooltip("How much a mote's glint dims as it slowly turns in the light (0 = steady).")]
        [Range(0f, 1f)]
        [SerializeField] private float _moteGlintDepth = 0.45f;

        [Tooltip("Seconds per glint of a turning mote (slow: never a sparkle).")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _moteGlintPeriod = 5f;

        [Header("Cutter nozzle (chest turret aperture)")]
        [Tooltip("The 6-blade mechanical iris, hot core and warm spill light on CutterOrigin.")]
        [SerializeField] private CutterNozzleSettings _cutterNozzle = new CutterNozzleSettings();

        public float TrackWidth => _trackWidth;

        public float TrackSegmentLength => _trackSegmentLength;

        public float TrackLifetime => _trackLifetime;

        public float TrackFadeStart => _trackFadeStart;

        public float TrackOpacity => _trackOpacity;

        public float TrackLift => _trackLift;

        public int TrackCapacity => _trackCapacity;

        public float TrackGroundProbe => _trackGroundProbe;

        public float DustMinSpeed => _dustMinSpeed;

        public float DustFullSpeed => _dustFullSpeed;

        public float DustMaxRate => _dustMaxRate;

        public float DustLifetime => _dustLifetime;

        public float DustSize => _dustSize;

        public float DustSpeed => _dustSpeed;

        public float DustDrag => _dustDrag;

        public float DustOpacity => _dustOpacity;

        public float DustGravity => _dustGravity;

        public Color DustColor => _dustColor;

        public Color DustShadowColor => _dustShadowColor;

        public float DustAccelFull => _dustAccelFull;

        public float DustAccelBoost => _dustAccelBoost;

        public float DustTurnFullYawRate => _dustTurnFullYawRate;

        public float DustTurnBoost => _dustTurnBoost;

        public float TowSkidDustMaxRate => _towSkidDustMaxRate;

        public float LandingMinImpact => _landingMinImpact;

        public float LandingFullImpact => _landingFullImpact;

        public float LandingRingAirTimeFrom => _landingRingAirTimeFrom;

        public float LandingRingAirTimeFull => Mathf.Max(_landingRingAirTimeFrom + 0.01f, _landingRingAirTimeFull);

        public int LandingMinCount => _landingMinCount;

        public int LandingMaxCount => Mathf.Max(_landingMinCount, _landingMaxCount);

        public float LandingMinSpeed => _landingMinSpeed;

        public float LandingMaxSpeed => _landingMaxSpeed;

        public float LandingMinSize => _landingMinSize;

        public float LandingMaxSize => _landingMaxSize;

        public float LandingLifetime => _landingLifetime;

        public float LandingDrag => _landingDrag;

        public float LandingOpacity => _landingOpacity;

        public int MoteCount => _moteCount;

        public float MoteLifetime => _moteLifetime;

        public float MoteFade => _moteFade;

        public float MoteMinSize => Mathf.Min(_moteMinSize, _moteMaxSize);

        public float MoteMaxSize => _moteMaxSize;

        public float MoteDrift => _moteDrift;

        public float MoteWander => _moteWander;

        public float MoteWanderFrequency => _moteWanderFrequency;

        public float MoteSpread => _moteSpread;

        public float MoteReach => _moteReach;

        public float MoteNearFade => Mathf.Min(_moteNearFade, _moteReach);

        public float MoteFalloff => _moteFalloff;

        public float MoteOpacity => _moteOpacity;

        public float MoteBrightness => _moteBrightness;

        public float MoteFadeSpeed => Mathf.Min(_moteFadeSpeed, _moteGoneSpeed);

        public float MoteGoneSpeed => _moteGoneSpeed;

        public float MoteVisibilityHalfLife => _moteVisibilityHalfLife;

        public float MoteGlintDepth => _moteGlintDepth;

        public float MoteGlintPeriod => _moteGlintPeriod;

        public CutterNozzleSettings CutterNozzle => _cutterNozzle;
    }
}
