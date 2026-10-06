using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Kenji's workbench: its pad (the diegetic shop for rover abilities), its work lamp and the sparks of an upgrade.
    /// Created by the Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class WorkshopTuning : ScriptableObject
    {
        [Header("Pad")]
        [Tooltip("Pad centre relative to the lander's WorkshopAnchor, in lander space (m): in front of the bench.")]
        [SerializeField] private Vector3 _padOffset = new Vector3(0f, 0f, 3.2f);

        [Tooltip("07 counts as parked on the pad within this many metres of its centre.")]
        [Range(0.5f, 6f)] [SerializeField] private float _padRadius = 2.4f;

        [Tooltip("Width (m) of the pad's ring of light.")]
        [Range(0.05f, 2f)] [SerializeField] private float _padRingWidth = 0.35f;

        [Tooltip("Pad brightness while the next ability is not affordable yet (it breathes softly).")]
        [Range(0f, 3f)] [SerializeField] private float _padIdle = 0.22f;

        [Tooltip("Pad brightness when the next ability is affordable: an invitation.")]
        [Range(0f, 3f)] [SerializeField] private float _padInviting = 0.65f;

        [Tooltip("Pad brightness while 07 is parked on it.")]
        [Range(0f, 3f)] [SerializeField] private float _padOccupied = 1f;

        [Tooltip("Pad brightness once every ability on the bench is bought.")]
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

        [Header("Work lamp")]
        [Tooltip("The lamp left on over the bench (1 = as authored): a warm welcome from across the base.")]
        [Range(0f, 3f)] [SerializeField] private float _lampIdle = 0.6f;

        [Tooltip("The lamp while 07 is parked at the bench, leaning in to work.")]
        [Range(0f, 4f)] [SerializeField] private float _lampOccupied = 1.3f;

        [Tooltip("The lamp the moment an ability is bought (HDR); it eases back by itself.")]
        [Range(0f, 8f)] [SerializeField] private float _lampFlare = 3f;

        [Tooltip("Seconds (time constant) for the lamp to brighten or dim.")]
        [Range(0f, 3f)] [SerializeField] private float _lampEase = 0.35f;

        [Header("Upgrade sparks")]
        [Tooltip("Bursts of sparks from between the vice jaws when an ability is bought.")]
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

        [Tooltip("Degrees the cone tips up from the bench's front.")]
        [Range(0f, 90f)] [SerializeField] private float _sparkTilt = 35f;

        [Tooltip("Fraction of Earth gravity pulling the sparks down (the moon's is about 0.17: floaty arcs).")]
        [Range(0f, 1f)] [SerializeField] private float _sparkGravity = 0.17f;

        [Tooltip("Streak length per m/s of speed (stretched sprites).")]
        [Range(0f, 0.3f)] [SerializeField] private float _sparkStreak = 0.05f;

        public Vector3 PadOffset => _padOffset;
        public float PadFlare => _padFlare;
        public float LampIdle => _lampIdle;
        public float LampOccupied => _lampOccupied;
        public float LampFlare => _lampFlare;
        public float LampEase => _lampEase;
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

        public PadLook PadLook => new PadLook(_padRadius, _padRingWidth, _padSegments, _padIdle, _padInviting,
            _padOccupied, _padDone, _breathPeriod, _padBreathDepth, _padEase);

        /// <summary>A (min, max) range that is never inverted or negative.</summary>
        private static Vector2 Ordered(Vector2 range)
        {
            return new Vector2(Mathf.Max(0f, Mathf.Min(range.x, range.y)), Mathf.Max(0f, Mathf.Max(range.x, range.y)));
        }
    }
}
