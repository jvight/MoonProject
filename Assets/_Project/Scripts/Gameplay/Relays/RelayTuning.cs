using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The relay network (docs/features/M3-06): each mast's reach, the escalating recipes, where a mast's relay part
    /// lies, the restoration beat (stitch, straighten, lamp, link pulse), the hop pads and the radio-hop's fade.
    /// Created by the Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class RelayTuning : ScriptableObject
    {
        [Header("Reach")]
        [Tooltip("Radius (m) a lit mast adds to the station's reach. The world's chain needs at least 105 m " +
                 "(relay.2 to relay.3 is 209.4 m).")]
        [Range(50f, 400f)] [SerializeField] private float _mastReach = 110f;

        [Header("Recipes")]
        [Tooltip("Materials each restoration takes, in the order the masts are restored (escalating: the long-term " +
                 "sink; VISION ruling 5: the sites yield at least twice every recipe).")]
        [SerializeField] private Recipe[] _costs =
        {
            new Recipe(2, 1, 0), new Recipe(3, 1, 0), new Recipe(4, 1, 0), new Recipe(4, 2, 0),
        };

        [Header("Relay part")]
        [Tooltip("The part lies this far (m) from its mast's pad centre, nearest the middle of the range first " +
                 "(min, max; the design caps it at 40).")]
        [SerializeField] private Vector2 _partDistance = new Vector2(14f, 34f);

        [Tooltip("Preferred distance (m) of the part from its mast.")]
        [Range(5f, 40f)] [SerializeField] private float _partPreferred = 22f;

        [Tooltip("Metres between the rings the part's spot is searched on.")]
        [Range(1f, 10f)] [SerializeField] private float _partRingStep = 4f;

        [Tooltip("Degrees between the spots tried on one ring.")]
        [Range(5f, 90f)] [SerializeField] private float _partAngleStep = 20f;

        [Tooltip("Steepest ground (degrees) a part may rest on.")]
        [Range(2f, 30f)] [SerializeField] private float _partMaxSlope = 14f;

        [Tooltip("Drivable ground (m) the part keeps on every side.")]
        [Range(0f, 10f)] [SerializeField] private float _partMargin = 2f;

        [Tooltip("Spots driven to from the pad (an A* over drivable ground) before giving up on a mast.")]
        [Range(1, 60)] [SerializeField] private int _partPathTries = 16;

        [Header("Restoring")]
        [Tooltip("07 can restore a mast within this many metres (horizontal) of its part socket.")]
        [Range(1f, 15f)] [SerializeField] private float _restoreRadius = 6f;

        [Tooltip("Seconds Interact is held to begin a restoration.")]
        [Range(0.1f, 3f)] [SerializeField] private float _restoreHold = 0.8f;

        [Tooltip("Seconds 07's beam stitches the mast (shorter than a friend's repair).")]
        [Range(0.5f, 6f)] [SerializeField] private float _stitch = 2f;

        [Tooltip("Seconds the relay part glides along the beam into the part socket (within the stitch).")]
        [Range(0.2f, 4f)] [SerializeField] private float _partSlide = 1.3f;

        [Tooltip("Height (m) of the part's arc on its way in.")]
        [Range(0f, 2f)] [SerializeField] private float _partArc = 0.4f;

        [Tooltip("Seconds the mast takes to straighten up, with a creak.")]
        [Range(0.3f, 5f)] [SerializeField] private float _straighten = 1.6f;

        [Tooltip("Overshoot of the straightening (0 = none, ~1.7 = classic back-ease): it rocks past upright once.")]
        [Range(0f, 3f)] [SerializeField] private float _straightenOvershoot = 1.2f;

        [Tooltip("Seconds the lamp takes to warm up.")]
        [Range(0.2f, 5f)] [SerializeField] private float _lampWarm = 1.4f;

        [Header("Lamp")]
        [Tooltip("Lamp glow of a lit mast (linear emission multiplier, 1 = as authored; carried by bloom).")]
        [Range(0f, 4f)] [SerializeField] private float _lampGlow = 1f;

        [Tooltip("Lamp glow of a restored mast that does not reach a lit node yet: a low listening glow.")]
        [Range(0f, 1f)] [SerializeField] private float _waitingGlow = 0.12f;

        [Tooltip("Seconds (time constant) for the lamp to follow its glow.")]
        [Range(0.05f, 3f)] [SerializeField] private float _lampEase = 0.35f;

        [Header("Link pulse")]
        [Tooltip("Speed (m/s) of the pulse running along the ground toward the node the mast links to.")]
        [Range(5f, 200f)] [SerializeField] private float _pulseSpeed = 45f;

        [Tooltip("Length (m) of the travelling pulse of light.")]
        [Range(2f, 60f)] [SerializeField] private float _pulseLength = 16f;

        [Tooltip("Width (m) of the pulse line.")]
        [Range(0.05f, 2f)] [SerializeField] private float _pulseWidth = 0.35f;

        [Tooltip("Brightness of the pulse.")]
        [Range(0f, 4f)] [SerializeField] private float _pulseGlow = 1.1f;

        [Tooltip("Height (m) of the pulse above the ground.")]
        [Range(0f, 1f)] [SerializeField] private float _pulseLift = 0.15f;

        [Tooltip("Share of the way along which the pulse fades out (it arrives as a whisper).")]
        [Range(0f, 1f)] [SerializeField] private float _pulseFade = 0.35f;

        [Tooltip("Seconds between masts lighting one after another when one restoration links a whole chain.")]
        [Range(0f, 5f)] [SerializeField] private float _chainDelay = 0.9f;

        [Header("Hop pads")]
        [Tooltip("07 counts as parked on a node's pad within this many metres of its centre (World's pads: 3 m).")]
        [Range(1f, 6f)] [SerializeField] private float _padRadius = 3f;

        [Tooltip("Width (m) of the pad's ring of light.")]
        [Range(0.05f, 2f)] [SerializeField] private float _padRingWidth = 0.3f;

        [Tooltip("Pad brightness when a hop is possible from it.")]
        [Range(0f, 3f)] [SerializeField] private float _padInviting = 0.3f;

        [Tooltip("Pad brightness while 07 is parked on it.")]
        [Range(0f, 3f)] [SerializeField] private float _padOccupied = 0.8f;

        [Tooltip("Seconds per breath of a waiting pad.")]
        [Range(0.5f, 10f)] [SerializeField] private float _padBreathPeriod = 3.6f;

        [Tooltip("How deeply a waiting pad breathes (0 = steady).")]
        [Range(0f, 1f)] [SerializeField] private float _padBreathDepth = 0.35f;

        [Tooltip("Seconds (time constant) for a pad to brighten or dim.")]
        [Range(0f, 3f)] [SerializeField] private float _padEase = 0.5f;

        [Tooltip("Segments of a pad ring.")]
        [Range(8, 128)] [SerializeField] private int _padSegments = 48;

        [Header("Radio-hop")]
        [Tooltip("07 counts as parked below this speed (m/s).")]
        [Range(0.05f, 3f)] [SerializeField] private float _hopMaxSpeed = 0.8f;

        [Tooltip("Seconds Interact is held, with the list open, to hop to the chosen node (a tap picks the next).")]
        [Range(0.2f, 3f)] [SerializeField] private float _hopConfirmHold = 0.7f;

        [Tooltip("Drive input above this closes the list (the player drives off instead).")]
        [Range(0.05f, 1f)] [SerializeField] private float _hopCancelDrive = 0.35f;

        [Tooltip("Seconds the view eases to a soft dark as the static rises.")]
        [Range(0.2f, 3f)] [SerializeField] private float _hopFadeOut = 0.8f;

        [Tooltip("Seconds the view rests dark while 07 is placed on the target pad.")]
        [Range(0f, 2f)] [SerializeField] private float _hopDark = 0.4f;

        [Tooltip("Seconds the view eases back in as the static resolves.")]
        [Range(0.2f, 3f)] [SerializeField] private float _hopFadeIn = 0.8f;

        public float MastReach => _mastReach;
        public int CostCount => _costs.Length;
        public Vector2 PartDistance => _partDistance;
        public float PartPreferred => _partPreferred;
        public float PartRingStep => _partRingStep;
        public float PartAngleStep => _partAngleStep;
        public float PartMaxSlope => _partMaxSlope;
        public float PartMargin => _partMargin;
        public int PartPathTries => _partPathTries;
        public float RestoreRadius => _restoreRadius;
        public float RestoreHold => _restoreHold;
        public float Stitch => _stitch;
        public float PartSlide => _partSlide;
        public float PartArc => _partArc;
        public float Straighten => _straighten;
        public float StraightenOvershoot => _straightenOvershoot;
        public float LampWarm => _lampWarm;
        public float LampGlow => _lampGlow;
        public float WaitingGlow => _waitingGlow;
        public float LampEase => _lampEase;
        public float PulseSpeed => _pulseSpeed;
        public float PulseLength => _pulseLength;
        public float PulseWidth => _pulseWidth;
        public float PulseGlow => _pulseGlow;
        public float PulseLift => _pulseLift;
        public float PulseFade => _pulseFade;
        public float ChainDelay => _chainDelay;
        public float PadRadius => _padRadius;
        public float HopMaxSpeed => _hopMaxSpeed;
        public float HopConfirmHold => _hopConfirmHold;
        public float HopCancelDrive => _hopCancelDrive;
        public float HopFadeOut => _hopFadeOut;
        public float HopDark => _hopDark;
        public float HopFadeIn => _hopFadeIn;

        /// <summary>The hop pads' look: invisible with nowhere to go, breathing when a hop is possible.</summary>
        public PadLook PadLook => new PadLook(_padRadius, _padRingWidth, _padSegments, 0f, _padInviting,
            _padOccupied, 0f, _padBreathPeriod, _padBreathDepth, _padEase);

        /// <summary>The recipe of the restoration that follows <paramref name="restored"/> earlier ones.</summary>
        public Recipe CostAfter(int restored)
        {
            return _costs[Mathf.Clamp(restored, 0, _costs.Length - 1)];
        }

        /// <summary>Every restoration's recipe together (the network's whole sink).</summary>
        public Recipe TotalCost(int masts)
        {
            var total = new Recipe(0, 0, 0);
            for (int i = 0; i < masts; i++)
            {
                total = total.Plus(CostAfter(i));
            }

            return total;
        }

        /// <summary>Null when the tuning can price <paramref name="masts"/> masts, else the problem.</summary>
        public string Validate(int masts)
        {
            if (_costs == null || _costs.Length < masts)
            {
                return $"RelayTuning prices {(_costs == null ? 0 : _costs.Length)} restorations for {masts} masts.";
            }

            for (int i = 0; i < _costs.Length; i++)
            {
                if (_costs[i].IsFree || (i > 0 && _costs[i].Total < _costs[i - 1].Total))
                {
                    return "RelayTuning's recipes must cost something and never fall.";
                }
            }

            return _partDistance.x <= 0f || _partDistance.y < _partDistance.x
                ? "RelayTuning's part distance range is empty."
                : null;
        }
    }
}
