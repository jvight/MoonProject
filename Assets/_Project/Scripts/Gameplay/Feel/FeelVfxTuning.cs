using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Every look number of the cut's feel kit (docs/design/feel-pass.md §3.1, §4): the charge-up motes and bead, the
    /// ignition flash, the beam's core and sheath, the cut light, the heat seam's cooling, the vacuum sparks and slag
    /// beads, the crack and the bundle's tie-off. Timings the cut state owns (charge length, break sequence) come in
    /// on the FeelBeat; this holds only how each beat looks.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Gameplay/Feel VFX Tuning", fileName = "FeelVfxTuning")]
    public sealed class FeelVfxTuning : ScriptableObject
    {
        [Header("Charge-up")]
        [Tooltip("Motes drawn in toward the emitter during a charge-up.")]
        [Range(4, 24)] [SerializeField] private int _moteCount = 18;

        [Tooltip("Radius of the sphere round the emitter the motes start on (m).")]
        [Range(0.1f, 1.5f)] [SerializeField] private float _moteRadius = 0.6f;

        [Tooltip("Size of a mote sprite (m).")]
        [Range(0.005f, 0.12f)] [SerializeField] private float _moteSize = 0.05f;

        [Tooltip("How long a mote's streak trails toward the emitter (seconds of its inward speed).")]
        [Range(0f, 0.3f)] [SerializeField] private float _moteStreak = 0.05f;

        [Tooltip("How far a mote's path curls round the emitter on its way in (turns).")]
        [Range(0f, 1f)] [SerializeField] private float _moteSwirl = 0.3f;

        [Tooltip("Ease-in power of the motes' approach: higher holds them out longer, then rushes them in.")]
        [Range(1f, 4f)] [SerializeField] private float _moteEase = 2.4f;

        [Tooltip("How long cancelled motes take to scatter outward and fade (s).")]
        [Range(0.05f, 0.6f)] [SerializeField] private float _moteDisperseSeconds = 0.2f;

        [Tooltip("Colour of the motes and the glow bead (cyan-white, linear).")]
        [SerializeField] private Color _moteColor = new Color(0.75f, 1f, 1f, 1f);

        [Tooltip("The glow bead's full diameter at the end of the charge (m).")]
        [Range(0.01f, 0.12f)] [SerializeField] private float _beadSize = 0.04f;

        [Tooltip("The bead's soft halo, as a multiple of the bead's size.")]
        [Range(1f, 8f)] [SerializeField] private float _beadHalo = 4f;

        [Tooltip("The bead halo's opacity at full charge.")]
        [Range(0f, 1f)] [SerializeField] private float _beadHaloAlpha = 0.45f;

        [Tooltip("The glow over 07's lens as it warms toward cyan-white with the charge (diameter, m).")]
        [Range(0.05f, 0.4f)] [SerializeField] private float _lensGlow = 0.16f;

        [Tooltip("The lens glow's opacity at full charge.")]
        [Range(0f, 1f)] [SerializeField] private float _lensGlowAlpha = 0.6f;

        [Tooltip("The emitter light's intensity at the end of the charge.")]
        [Range(0f, 4f)] [SerializeField] private float _emitterLight = 1.5f;

        [Tooltip("The emitter light's range (m).")]
        [Range(0.2f, 4f)] [SerializeField] private float _emitterRange = 1.2f;

        [Header("Ignition flash")]
        [Tooltip("The flash's radius at the emitter and the cut point (m).")]
        [Range(0.05f, 0.8f)] [SerializeField] private float _flashRadius = 0.3f;

        [Tooltip("How long the white flash itself shows (s).")]
        [Range(0.02f, 0.2f)] [SerializeField] private float _flashSeconds = 0.06f;

        [Tooltip("The flash light's peak intensity at the cut point.")]
        [Range(0f, 6f)] [SerializeField] private float _flashLight = 2.5f;

        [Tooltip("How long the flash light decays to nothing (s).")]
        [Range(0.05f, 0.6f)] [SerializeField] private float _flashDecaySeconds = 0.18f;

        [Tooltip("The flash light's range (m).")]
        [Range(0.5f, 8f)] [SerializeField] private float _flashRange = 3f;

        [Header("Beam")]
        [Tooltip("Width of the beam's hot white core (m).")]
        [Range(0.005f, 0.06f)] [SerializeField] private float _coreWidth = 0.02f;

        [Tooltip("Width of the cyan sheath round the core (m).")]
        [Range(0.02f, 0.2f)] [SerializeField] private float _sheathWidth = 0.06f;

        [Tooltip("How much the sheath's width shimmers (share of its width).")]
        [Range(0f, 0.5f)] [SerializeField] private float _shimmer = 0.12f;

        [Tooltip("How fast the sheath shimmers (Hz).")]
        [Range(1f, 30f)] [SerializeField] private float _shimmerRate = 9f;

        [Tooltip("How long the beam takes to reach the cut point after ignition (s, eased out).")]
        [Range(0.02f, 0.3f)] [SerializeField] private float _extendSeconds = 0.08f;

        [Tooltip("How long the beam takes to retract into the emitter on release (s).")]
        [Range(0.02f, 0.4f)] [SerializeField] private float _retractSeconds = 0.12f;

        [Header("Cut light")]
        [Tooltip("The warm light at the cut while the beam works.")]
        [Range(0f, 4f)] [SerializeField] private float _cutLight = 1.12f;

        [Tooltip("The cut light's range (m): it lights the wreck, the ground and 07's front.")]
        [Range(0.5f, 8f)] [SerializeField] private float _cutRange = 3.5f;

        [Tooltip("The cut light's flicker (share of its intensity).")]
        [Range(0f, 0.4f)] [SerializeField] private float _cutFlicker = 0.1f;

        [Tooltip("The cut light's flicker rate (Hz).")]
        [Range(1f, 30f)] [SerializeField] private float _cutFlickerRate = 12f;

        [Tooltip("How long the cut light fades when the beam lets go (s).")]
        [Range(0.05f, 0.6f)] [SerializeField] private float _cutFadeSeconds = 0.2f;

        [Tooltip("The cut light's colour (orange-white, linear).")]
        [SerializeField] private Color _cutLightColor = new Color(1f, 0.72f, 0.42f, 1f);

        [Header("Heat seam")]
        [Tooltip("Seam age when it has cooled from white-yellow to orange (s).")]
        [Range(0.2f, 3f)] [SerializeField] private float _seamOrangeSeconds = 1f;

        [Tooltip("Seam age when it has cooled to a dull red (s).")]
        [Range(1f, 6f)] [SerializeField] private float _seamRedSeconds = 3f;

        [Tooltip("Seam age when it has gone dark, a charred line (s).")]
        [Range(2f, 12f)] [SerializeField] private float _seamDarkSeconds = 6f;

        [Tooltip("Width of the seam strip on the piece (m).")]
        [Range(0.005f, 0.08f)] [SerializeField] private float _seamWidth = 0.04f;

        [Tooltip("How far the seam stands off the piece's face, so it never flickers into it (m).")]
        [Range(0f, 0.02f)] [SerializeField] private float _seamLift = 0.004f;

        [Header("Vacuum debris")]
        [Tooltip("Lunar gravity the sparks and beads fall under (m/s²); no drag at all (R11).")]
        [Range(0.5f, 3f)] [SerializeField] private float _gravity = 1.62f;

        [Tooltip("A spark's launch speed range (m/s).")]
        [SerializeField] private Vector2 _sparkSpeed = new Vector2(1.5f, 4f);

        [Tooltip("Sparks per second at the start of a cut and at its end (the rate rises with progress).")]
        [SerializeField] private Vector2 _sparkRate = new Vector2(10f, 24f);

        [Tooltip("How wide the sparks spray round the cut face's normal (degrees).")]
        [Range(5f, 90f)] [SerializeField] private float _sparkSpread = 35f;

        [Tooltip("Speed kept on the one bounce (restitution).")]
        [Range(0f, 0.8f)] [SerializeField] private float _restitution = 0.3f;

        [Tooltip("How long a slag bead lies glowing before it has cooled out (s, random in range).")]
        [SerializeField] private Vector2 _beadLifetime = new Vector2(1.5f, 2.5f);

        [Tooltip("How long a spark may fly before it is dropped if it never lands (s).")]
        [Range(0.5f, 5f)] [SerializeField] private float _sparkMaxFlight = 2.5f;

        [Tooltip("Size of a spark (m) and of a slag bead (m).")]
        [SerializeField] private Vector2 _debrisSize = new Vector2(0.018f, 0.03f);

        [Tooltip("How far a spark's streak trails behind it (seconds of flight).")]
        [Range(0f, 0.2f)] [SerializeField] private float _sparkStreak = 0.04f;

        [Tooltip("Spark colour when it leaves the cut, and a bead's colour when it lands (the sprite material " +
                 "supplies the glow's strength).")]
        [SerializeField] private Color _sparkColor = new Color(1f, 0.8f, 0.5f, 1f);

        [Tooltip("A bead's colour as it cools out (linear).")]
        [SerializeField] private Color _beadCoolColor = new Color(0.6f, 0.08f, 0.02f, 1f);

        [Tooltip("Sparks the emitter vents at the crack.")]
        [Range(0, 16)] [SerializeField] private int _ventSparks = 6;

        [Tooltip("Slag beads that drop off a tumbling piece (random in range).")]
        [SerializeField] private Vector2Int _tumbleBeads = new Vector2Int(2, 4);

        [Tooltip("Pool sizes: sparks, beads, motes (zero GC; never exceeded).")]
        [SerializeField] private Vector3Int _pools = new Vector3Int(64, 24, 24);

        [Header("Dust (leap, landing, tether)")]
        [Tooltip("Dust puffs that may be in the air or settling at once (zero GC; never exceeded).")]
        [Range(8, 128)] [SerializeField] private int _dustPool = 48;

        [Tooltip("Dust colour (lavender regolith; the sprite material supplies its glow, so keep it dim).")]
        [SerializeField] private Color _dustColor = new Color(0.6f, 0.58f, 0.7f, 0.55f);

        [Tooltip("Size of a dust puff (m).")]
        [Range(0.01f, 0.3f)] [SerializeField] private float _dustSize = 0.12f;

        [Tooltip("Radius the ring's puffs start at round the beat's centre: just outside 07's wheels (m).")]
        [Range(0f, 2f)] [SerializeField] private float _ringRadius = 1.1f;

        [Tooltip("A ring puff's outward speed at the weakest and the strongest beat (m/s).")]
        [SerializeField] private Vector2 _dustSpeed = new Vector2(0.6f, 1.8f);

        [Tooltip("How steeply a ring puff rises as it flies out (upward share of its speed).")]
        [Range(0f, 1.5f)] [SerializeField] private float _dustRise = 0.45f;

        [Tooltip("How long a dust puff lives: it arcs, settles and fades within this (s).")]
        [Range(0.3f, 3f)] [SerializeField] private float _dustSettleSeconds = 1.2f;

        [Tooltip("Puffs in a landing ring at full strength; a leap's ring uses this times the leap share.")]
        [Range(4, 64)] [SerializeField] private int _ringDust = 28;

        [Tooltip("Share of a landing ring's puffs a leap's take-off throws.")]
        [Range(0f, 1f)] [SerializeField] private float _leapRingShare = 0.6f;

        [Tooltip("Puffs that fall off an object the tether lifts.")]
        [Range(0, 32)] [SerializeField] private int _tetherDust = 10;

        [Header("Leap coils")]
        [Tooltip("The coil flash's size at take-off (m), and its light's peak.")]
        [SerializeField] private Vector2 _coilFlash = new Vector2(0.5f, 1.8f);

        [Header("Install")]
        [Tooltip("The alignment light's level while a part lines up with its socket.")]
        [Range(0f, 3f)] [SerializeField] private float _alignLight = 0.9f;

        [Tooltip("How far the power-on sweep travels along the new part (m).")]
        [Range(0.1f, 2f)] [SerializeField] private float _sweepLength = 0.6f;

        [Tooltip("The power-on sweep's length in time when the beat gives none (s).")]
        [Range(0.1f, 2f)] [SerializeField] private float _sweepSeconds = 0.6f;

        [Header("Crack")]
        [Tooltip("How long the seam's end flashes white at the crack (s).")]
        [Range(0.02f, 0.3f)] [SerializeField] private float _crackFlashSeconds = 0.08f;

        [Tooltip("The cut light's spike at the crack (multiplies its sustained level).")]
        [Range(1f, 4f)] [SerializeField] private float _crackSpike = 2f;

        [Tooltip("How long the emitter's glow fades after the crack (s).")]
        [Range(0.1f, 1.2f)] [SerializeField] private float _emitterFadeSeconds = 0.4f;

        [Header("Bundle tie-off")]
        [Tooltip("The bundle's scale at the top of its tie-off pop.")]
        [Range(1f, 1.4f)] [SerializeField] private float _tiePop = 1.12f;

        [Tooltip("How long the tie-off pop lasts (s).")]
        [Range(0.05f, 0.6f)] [SerializeField] private float _tieSeconds = 0.2f;

        public int MoteCount => Mathf.Min(_moteCount, MotePool);
        public float MoteRadius => _moteRadius;
        public float MoteSize => _moteSize;
        public float MoteStreak => _moteStreak;
        public float BeadHalo => _beadHalo;
        public float BeadHaloAlpha => _beadHaloAlpha;
        public float LensGlow => _lensGlow;
        public float LensGlowAlpha => _lensGlowAlpha;
        public float MoteSwirl => _moteSwirl;
        public float MoteEase => _moteEase;
        public float MoteDisperseSeconds => _moteDisperseSeconds;
        public Color MoteColor => _moteColor;
        public float BeadSize => _beadSize;
        public float EmitterLight => _emitterLight;
        public float EmitterRange => _emitterRange;
        public float FlashRadius => _flashRadius;
        public float FlashSeconds => _flashSeconds;
        public float FlashLight => _flashLight;
        public float FlashDecaySeconds => _flashDecaySeconds;
        public float FlashRange => _flashRange;
        public float CoreWidth => _coreWidth;
        public float SheathWidth => _sheathWidth;
        public float Shimmer => _shimmer;
        public float ShimmerRate => _shimmerRate;
        public float ExtendSeconds => _extendSeconds;
        public float RetractSeconds => _retractSeconds;
        public float CutLight => _cutLight;
        public float CutRange => _cutRange;
        public float CutFlicker => _cutFlicker;
        public float CutFlickerRate => _cutFlickerRate;
        public float CutFadeSeconds => _cutFadeSeconds;
        public Color CutLightColor => _cutLightColor;
        public float SeamOrangeSeconds => _seamOrangeSeconds;
        public float SeamRedSeconds => Mathf.Max(_seamRedSeconds, _seamOrangeSeconds);
        public float SeamDarkSeconds => Mathf.Max(_seamDarkSeconds, SeamRedSeconds);
        public float SeamWidth => _seamWidth;
        public float SeamLift => _seamLift;
        public float Gravity => _gravity;
        public Vector2 SparkSpeed => _sparkSpeed;
        public Vector2 SparkRate => _sparkRate;
        public float SparkSpread => _sparkSpread;
        public float Restitution => _restitution;
        public Vector2 BeadLifetime => _beadLifetime;
        public float SparkMaxFlight => _sparkMaxFlight;
        public float SparkSize => _debrisSize.x;
        public float SlagSize => _debrisSize.y;
        public float SparkStreak => _sparkStreak;
        public Color SparkColor => _sparkColor;
        public Color BeadCoolColor => _beadCoolColor;
        public int VentSparks => _ventSparks;
        public Vector2Int TumbleBeads => _tumbleBeads;
        public int SparkPool => Mathf.Max(1, _pools.x);
        public int BeadPool => Mathf.Max(1, _pools.y);
        public int MotePool => Mathf.Max(1, _pools.z);
        public int DustPool => Mathf.Max(1, _dustPool);
        public Color DustColor => _dustColor;
        public float DustSize => _dustSize;
        public float RingRadius => _ringRadius;
        public Vector2 DustSpeed => _dustSpeed;
        public float DustRise => _dustRise;
        public float DustSettleSeconds => _dustSettleSeconds;
        public int RingDust => _ringDust;
        public float LeapRingShare => _leapRingShare;
        public int TetherDust => _tetherDust;
        public float CoilFlashSize => _coilFlash.x;
        public float CoilFlashLight => _coilFlash.y;
        public float AlignLight => _alignLight;
        public float SweepLength => _sweepLength;
        public float SweepSeconds => _sweepSeconds;
        public float CrackFlashSeconds => _crackFlashSeconds;
        public float CrackSpike => _crackSpike;
        public float EmitterFadeSeconds => _emitterFadeSeconds;
        public float TiePop => _tiePop;
        public float TieSeconds => _tieSeconds;
    }
}
