using UnityEngine;
using MoonProject.Art;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Kenji's Rover Bay: its pad on the turntable (the diegetic shop for rover kit), 07 feeding its hopper, its work
    /// lamps and the lights under them, its sign, and the weld sparks of a fitting. Created by the Gameplay/Tuning
    /// builder; runtime code only reads it.
    /// </summary>
    public sealed class WorkshopTuning : ScriptableObject
    {
        [Header("Pad")]
        [Tooltip("07 counts as parked on the bay's turntable within this many metres of its centre (the ring of " +
                 "light lies on the turntable at this radius; the turntable is 1.75 m across).")]
        [Range(0.5f, 6f)] [SerializeField] private float _padRadius = 1.8f;

        [Tooltip("Width (m) of the pad's ring of light.")]
        [Range(0.05f, 2f)] [SerializeField] private float _padRingWidth = 0.35f;

        [Tooltip("Pad brightness while the next ability is not affordable yet (it breathes softly).")]
        [Range(0f, 3f)] [SerializeField] private float _padIdle = 0.22f;

        [Tooltip("Pad brightness when the next ability is affordable: an invitation.")]
        [Range(0f, 3f)] [SerializeField] private float _padInviting = 0.65f;

        [Tooltip("Pad brightness while 07 is parked on it.")]
        [Range(0f, 3f)] [SerializeField] private float _padOccupied = 1f;

        [Tooltip("Pad brightness once every piece of kit in the bay is bought.")]
        [Range(0f, 3f)] [SerializeField] private float _padDone = 0.12f;

        [Tooltip("Seconds per breath of the waiting pad.")]
        [Range(0.5f, 10f)] [SerializeField] private float _breathPeriod = 3.2f;

        [Tooltip("How deeply the waiting pad breathes (0 = steady).")]
        [Range(0f, 1f)] [SerializeField] private float _padBreathDepth = 0.35f;

        [Tooltip("Seconds (time constant) for the pad to brighten or dim; a purchase flare fades the same way.")]
        [Range(0f, 3f)] [SerializeField] private float _padEase = 0.4f;

        [Tooltip("Segments of the pad ring.")]
        [Range(8, 128)] [SerializeField] private int _padSegments = 48;

        [Tooltip("Pad brightness the moment an ability is bought; it eases back down by itself.")]
        [Range(0f, 6f)] [SerializeField] private float _padFlare = 2.4f;

        [Header("Hopper feed")]
        [Tooltip("Seconds 07's beam reaches the bay's hopper before the first bundle of materials leaves 07.")]
        [Range(0f, 1f)] [SerializeField] private float _feedBeamLead = 0.15f;

        [Tooltip("Seconds each bundle takes from 07's cargo socket into the hopper's mouth.")]
        [Range(0.1f, 2f)] [SerializeField] private float _feedFlight = 0.55f;

        [Tooltip("Seconds between one bundle leaving 07 and the next (one bundle per material the recipe spends).")]
        [Range(0f, 1f)] [SerializeField] private float _feedStagger = 0.15f;

        [Tooltip("Metres a bundle's flight bows up above the straight line.")]
        [Range(0f, 2f)] [SerializeField] private float _feedLift = 0.45f;

        [Tooltip("Share of a flight a bundle spends popping out of 07, and again shrinking into the mouth.")]
        [Range(0.05f, 0.5f)] [SerializeField] private float _feedGrowShare = 0.25f;

        [Header("Work lamps")]
        [Tooltip("The two work lamps left on over the bay (linear emission multiplier, 1 = as authored): a warm " +
                 "welcome from across the base.")]
        [Range(0f, 3f)] [SerializeField] private float _lampIdle = 0.15f;

        [Tooltip("The lamps while 07 is parked on the turntable, leaning in to work (linear emission multiplier).")]
        [Range(0f, 4f)] [SerializeField] private float _lampOccupied = 0.75f;

        [Tooltip("The lamps the moment a kit piece is set on 07, the weld (linear emission multiplier); they ease " +
                 "back by themselves.")]
        [Range(0f, 8f)] [SerializeField] private float _lampFlare = 4.7f;

        [Tooltip("Seconds (time constant) for the lamps to brighten or dim.")]
        [Range(0f, 3f)] [SerializeField] private float _lampEase = 0.35f;

        [Header("Work lights")]
        [Tooltip("Colour of the warm point light under each work lamp.")]
        [SerializeField] private Color _lightColor = Palette.Get(PaletteSwatch.WarmLamp);

        [Tooltip("Intensity of the work lights while the bay waits: a low warm glow inside it.")]
        [Range(0f, 2f)] [SerializeField] private float _lightIdle = 0.25f;

        [Tooltip("Intensity of the work lights while the bay works (07 feeding the hopper, a piece being fitted), " +
                 "so its interior and the arms read.")]
        [Range(0f, 8f)] [SerializeField] private float _lightWorking = 3f;

        [Tooltip("Seconds (time constant) for the work lights to come up or ease down.")]
        [Range(0f, 3f)] [SerializeField] private float _lightEase = 0.5f;

        [Tooltip("Range (m) of each work light.")]
        [Range(1f, 20f)] [SerializeField] private float _lightRange = 6f;

        [Tooltip("Metres in front of each lamp's glass, along its aim, where its light sits.")]
        [Range(0f, 1f)] [SerializeField] private float _lightOffset = 0.25f;

        [Tooltip("Soft shadows from the work lights while the bay works (none while it waits).")]
        [SerializeField] private bool _lightShadows = true;

        [Tooltip("Seconds the bay keeps working after a piece is set on: the weld, the arms folding away and the " +
                 "turntable showing the piece.")]
        [Range(0f, 10f)] [SerializeField] private float _workLinger = 3f;

        [Tooltip("The bay's lit sign never glows dimmer than this (linear emission multiplier): a landmark from " +
                 "across the base. Above it, it follows the lamps.")]
        [Range(0f, 3f)] [SerializeField] private float _signGlow = 0.6f;

        [Header("Weld sparks")]
        [Tooltip("Bursts of sparks from the fitting arm's tip as the piece is set on 07.")]
        [Range(1, 8)] [SerializeField] private int _sparkBursts = 3;

        [Tooltip("Sparks per burst.")]
        [Range(1, 100)] [SerializeField] private int _sparkCount = 18;

        [Tooltip("Seconds between bursts.")]
        [Range(0.02f, 1f)] [SerializeField] private float _sparkInterval = 0.16f;

        [Tooltip("Launch speed range (m/s).")]
        [SerializeField] private Vector2 _sparkSpeed = new Vector2(1.2f, 3.2f);

        [Tooltip("Lifetime range (s).")]
        [SerializeField] private Vector2 _sparkLifetime = new Vector2(0.5f, 1.1f);

        [Tooltip("Size range (m).")]
        [SerializeField] private Vector2 _sparkSize = new Vector2(0.03f, 0.07f);

        [Tooltip("Half-angle (degrees) of the cone the sparks fly out in.")]
        [Range(0f, 80f)] [SerializeField] private float _sparkSpread = 35f;

        [Tooltip("Degrees the cone tips away from the way the arm's tip points.")]
        [Range(0f, 90f)] [SerializeField] private float _sparkTilt = 35f;

        [Tooltip("Fraction of Earth gravity pulling the sparks down (the moon's is about 0.17: floaty arcs).")]
        [Range(0f, 1f)] [SerializeField] private float _sparkGravity = 0.17f;

        [Tooltip("Streak length per m/s of speed (stretched sprites).")]
        [Range(0f, 0.3f)] [SerializeField] private float _sparkStreak = 0.05f;

        [Tooltip("An arm (gantry or floor) is the one fitting when its tip is this many metres from where it rests " +
                 "(the floor arm rises only a short way to 07's belly): its tip throws the weld sparks as the piece " +
                 "is set on.")]
        [Range(0.02f, 2f)] [SerializeField] private float _weldArmTravel = 0.12f;

        public float PadFlare => _padFlare;
        public float LampIdle => _lampIdle;
        public float LampOccupied => _lampOccupied;
        public float LampFlare => _lampFlare;
        public float LampEase => _lampEase;
        public Color LightColor => _lightColor;
        public float LightIdle => _lightIdle;
        public float LightWorking => _lightWorking;
        public float LightEase => _lightEase;
        public float LightRange => _lightRange;
        public float LightOffset => _lightOffset;
        public bool LightCastsShadows => _lightShadows;
        public float WorkLinger => _workLinger;
        public float SignGlow => _signGlow;
        public float WeldArmTravel => _weldArmTravel;
        public int SparkBursts => _sparkBursts;
        public int SparkCount => _sparkCount;
        public float SparkInterval => _sparkInterval;
        public float SparkSpread => _sparkSpread;
        public float SparkTilt => _sparkTilt;
        public float SparkGravity => _sparkGravity;
        public float SparkStreak => _sparkStreak;

        public Vector2 SparkSpeed => Ordered(_sparkSpeed);
        public Vector2 SparkLifetime => Ordered(_sparkLifetime);
        public Vector2 SparkSize => Ordered(_sparkSize);

        public FeedLook FeedLook => new FeedLook(_feedBeamLead, _feedFlight, _feedStagger, _feedLift, _feedGrowShare);

        public PadLook PadLook => new PadLook(_padRadius, _padRingWidth, _padSegments, _padIdle, _padInviting,
            _padOccupied, _padDone, _breathPeriod, _padBreathDepth, _padEase);

        /// <summary>A (min, max) range that is never inverted or negative.</summary>
        private static Vector2 Ordered(Vector2 range)
        {
            return new Vector2(Mathf.Max(0f, Mathf.Min(range.x, range.y)), Mathf.Max(0f, Mathf.Max(range.x, range.y)));
        }
    }
}
