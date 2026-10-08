using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Salvage (docs/features/M3-13): how generously 07 aims at a piece, how long the beam cuts, how much each piece
    /// is worth, how a drag piece is pulled clear, how a cut piece breaks off and folds into 07, and how the loose
    /// trail bits are drawn in. Created by the Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class SalvageTuning : ScriptableObject
    {
        [Header("Aim (VISION ruling 2: generous)")]
        [Tooltip("07 can cut a piece whose cut point lies within this many metres (horizontal) of it.")]
        [Range(2f, 20f)] [SerializeField] private float _reachRadius = 8f;

        [Tooltip("Once cutting, the beam holds on until 07 is this many times the reach away.")]
        [Range(1f, 2f)] [SerializeField] private float _reachHysteresis = 1.3f;

        [Tooltip("Half-angle (degrees) of the soft cone around the view in which a piece can be picked.")]
        [Range(5f, 60f)] [SerializeField] private float _aimCone = 26f;

        [Tooltip("A picked piece stays picked inside this wider half-angle (degrees), so it never flickers.")]
        [Range(5f, 80f)] [SerializeField] private float _stickyCone = 36f;

        [Tooltip("Degrees of aim that one metre of distance is worth when two pieces compete (nearer wins ties).")]
        [Range(0f, 10f)] [SerializeField] private float _distanceWeight = 1.5f;

        [Header("Halo")]
        [Tooltip("Scale of the halo shell around a piece's mesh.")]
        [Range(1.01f, 1.3f)] [SerializeField] private float _haloScale = 1.06f;

        [Tooltip("Halo brightness of the piece a hold would cut.")]
        [Range(0f, 2f)] [SerializeField] private float _hoverHalo = 0.55f;

        [Tooltip("Halo brightness while the beam cuts it.")]
        [Range(0f, 2f)] [SerializeField] private float _cutHalo = 0.9f;

        [Tooltip("Seconds (time constant) for a piece's halo to ease.")]
        [Range(0.01f, 1f)] [SerializeField] private float _haloEase = 0.15f;

        [Header("Cut")]
        [Tooltip("Holding the beam asks 07 to ease to a stop; the cut starts below this speed (m/s).")]
        [Range(0.1f, 5f)] [SerializeField] private float _engageSpeed = 1.2f;

        [Tooltip("Once cutting, 07 may drift this many times faster before the beam lets go (no flicker).")]
        [Range(1f, 3f)] [SerializeField] private float _engageHysteresis = 1.5f;

        [Tooltip("Seconds of cutting per material unit the piece yields...")]
        [Range(0.2f, 3f)] [SerializeField] private float _cutSecondsPerUnit = 1f;

        [Tooltip("...kept within this range (s): every cut is a short, calm beat.")]
        [SerializeField] private Vector2 _cutSeconds = new Vector2(2f, 4f);

        [Tooltip("Sideways sweeps per second of the beam's end along the cut.")]
        [Range(0f, 4f)] [SerializeField] private float _beamSweepRate = 0.9f;

        [Tooltip("Sideways reach (m) of the beam's sweep along the cut.")]
        [Range(0f, 1f)] [SerializeField] private float _beamSweep = 0.12f;

        [Tooltip("Sparks per second at the cut while the beam works.")]
        [Range(0f, 120f)] [SerializeField] private float _sparkRate = 26f;

        [Tooltip("Spark speed (m/s, min and max).")]
        [SerializeField] private Vector2 _sparkSpeed = new Vector2(0.6f, 1.8f);

        [Tooltip("Spark life (s, min and max).")]
        [SerializeField] private Vector2 _sparkLifetime = new Vector2(0.35f, 0.8f);

        [Tooltip("Spark size (m, min and max).")]
        [SerializeField] private Vector2 _sparkSize = new Vector2(0.02f, 0.05f);

        [Tooltip("Spread (degrees) of the spark cone around the cut face's normal.")]
        [Range(0f, 90f)] [SerializeField] private float _sparkSpread = 40f;

        [Tooltip("Share of the moon's gravity the sparks feel (they drift down slowly).")]
        [Range(0f, 1f)] [SerializeField] private float _sparkGravity = 0.16f;

        [Tooltip("How long the sparks stretch along their motion.")]
        [Range(0f, 1f)] [SerializeField] private float _sparkStreak = 0.12f;

        [Tooltip("Most sparks alive at once.")]
        [Range(4, 200)] [SerializeField] private int _maxSparks = 48;

        [Header("Yield (VISION ruling 5: the sites give at least twice every recipe)")]
        [Tooltip("A piece smaller than this volume (m³, its mesh bounds) yields the small amount...")]
        [Range(0.1f, 10f)] [SerializeField] private float _smallVolume = 1f;

        [Tooltip("...one smaller than this volume (m³) the medium amount, and anything bigger the large amount.")]
        [Range(0.1f, 20f)] [SerializeField] private float _mediumVolume = 3f;

        [Tooltip("Units a small piece yields.")]
        [Range(1, 10)] [SerializeField] private int _smallYield = 2;

        [Tooltip("Units a medium piece yields.")]
        [Range(1, 10)] [SerializeField] private int _mediumYield = 3;

        [Tooltip("Units a large piece yields.")]
        [Range(1, 10)] [SerializeField] private int _largeYield = 4;

        [Tooltip("Units a drag piece yields (it is pulled clear before it can be cut).")]
        [Range(1, 10)] [SerializeField] private int _dragYield = 4;

        [Tooltip("Units a loose trail bit yields.")]
        [Range(1, 10)] [SerializeField] private int _trailYield = 1;

        [Header("Drag pieces")]
        [Tooltip("A drag piece pulled this many metres (horizontal) from where it hung lets go of the tether and " +
                 "becomes cuttable.")]
        [Range(1f, 10f)] [SerializeField] private float _dragClearance = 3f;

        [Tooltip("Mass (kg) of a drag piece on the tether.")]
        [Range(2f, 40f)] [SerializeField] private float _dragMass = 14f;

        [Tooltip("Linear damping of a loose drag piece (calm, floaty).")]
        [Range(0f, 5f)] [SerializeField] private float _dragLinearDamping = 0.6f;

        [Tooltip("Angular damping of a loose drag piece.")]
        [Range(0f, 5f)] [SerializeField] private float _dragAngularDamping = 1.2f;

        [Tooltip("Bounciness of a drag piece's collider (low: it settles).")]
        [Range(0f, 1f)] [SerializeField] private float _dragBounciness = 0.1f;

        [Tooltip("Friction of a drag piece's collider.")]
        [Range(0f, 1f)] [SerializeField] private float _dragFriction = 0.8f;

        [Header("Break off")]
        [Tooltip("Seconds a cut piece takes to come away and fold into a bundle.")]
        [Range(0.1f, 2f)] [SerializeField] private float _breakDuration = 0.7f;

        [Tooltip("Metres a cut piece eases out of its cut face as it comes away.")]
        [Range(0f, 2f)] [SerializeField] private float _breakPop = 0.45f;

        [Tooltip("Degrees a cut piece tumbles while it folds.")]
        [Range(0f, 180f)] [SerializeField] private float _breakTumble = 35f;

        [Header("Into 07")]
        [Tooltip("Loose trail bits within this many metres of 07 lift off and spiral into it.")]
        [Range(1f, 20f)] [SerializeField] private float _magnetRadius = 5.5f;

        [Tooltip("Flight time (s) of a bundle or bit right next to 07.")]
        [Range(0.1f, 3f)] [SerializeField] private float _flightDuration = 0.7f;

        [Tooltip("Extra flight time (s) per metre of distance.")]
        [Range(0f, 0.5f)] [SerializeField] private float _flightPerMetre = 0.06f;

        [Tooltip("Height (m) a bundle lifts during its flight.")]
        [Range(0f, 4f)] [SerializeField] private float _flightLift = 0.9f;

        [Tooltip("Radius (m) of the spiral around the flight path.")]
        [Range(0f, 3f)] [SerializeField] private float _spiralRadius = 0.5f;

        [Tooltip("Turns of the spiral during a flight.")]
        [Range(0f, 4f)] [SerializeField] private float _spiralTurns = 1f;

        [Tooltip("Turn speed (degrees per second) of a bundle in flight.")]
        [Range(0f, 720f)] [SerializeField] private float _flightSpin = 180f;

        [Tooltip("Scale of a bundle as it reaches the cargo socket (it folds in).")]
        [Range(0.05f, 1f)] [SerializeField] private float _arrivalScale = 0.3f;

        [Tooltip("Minimum seconds between two arrivals: a run of bits plays as an arpeggio, never a chord.")]
        [Range(0f, 0.5f)] [SerializeField] private float _minArrivalInterval = 0.12f;

        [Tooltip("Seconds within which the next piece salvaged at the same site (or along the trail) climbs one " +
                 "step of the salvage melody: long enough to drive round a wreck to its next piece.")]
        [Range(1f, 90f)] [SerializeField] private float _chainWindow = 40f;

        [Header("Loose trail bits")]
        [Tooltip("How deep (share of its height) a trail bit lies in the dust.")]
        [Range(0f, 0.8f)] [SerializeField] private float _trailSink = 0.3f;

        [Tooltip("Resting tilt (degrees) of a trail bit, so none lies perfectly flat.")]
        [Range(0f, 30f)] [SerializeField] private float _trailTilt = 12f;

        [Tooltip("07 glances at the nearest loose trail bit within this many metres.")]
        [Range(0f, 40f)] [SerializeField] private float _glanceRadius = 12f;

        [Header("Arrival flash")]
        [Tooltip("Pooled flashes (enough for a run of trail bits).")]
        [Range(1, 16)] [SerializeField] private int _flashPoolSize = 4;

        [Tooltip("Seconds a flash lasts.")]
        [Range(0.05f, 2f)] [SerializeField] private float _flashDuration = 0.45f;

        [Tooltip("Flash radius (m) at its start and end.")]
        [SerializeField] private Vector2 _flashRadius = new Vector2(0.12f, 0.5f);

        [Tooltip("Peak brightness of a flash (around 1 stays cyan; higher blooms white).")]
        [Range(0f, 10f)] [SerializeField] private float _flashIntensity = 1.2f;

        public float ReachRadius => _reachRadius;
        public float ReachHysteresis => _reachHysteresis;
        public float AimCone => _aimCone;
        public float StickyCone => Mathf.Max(_stickyCone, _aimCone);
        public float DistanceWeight => _distanceWeight;
        public float HaloScale => _haloScale;
        public float HoverHalo => _hoverHalo;
        public float CutHalo => _cutHalo;
        public float HaloEase => _haloEase;
        public float EngageSpeed => _engageSpeed;
        public float EngageHysteresis => _engageHysteresis;
        public float CutSecondsPerUnit => _cutSecondsPerUnit;
        public Vector2 CutSeconds => Ordered(_cutSeconds);
        public float BeamSweepRate => _beamSweepRate;
        public float BeamSweep => _beamSweep;
        public float SparkRate => _sparkRate;
        public Vector2 SparkSpeed => Ordered(_sparkSpeed);
        public Vector2 SparkLifetime => Ordered(_sparkLifetime);
        public Vector2 SparkSize => Ordered(_sparkSize);
        public float SparkSpread => _sparkSpread;
        public float SparkGravity => _sparkGravity;
        public float SparkStreak => _sparkStreak;
        public int MaxSparks => _maxSparks;
        public float SmallVolume => _smallVolume;
        public float MediumVolume => Mathf.Max(_mediumVolume, _smallVolume);
        public int SmallYield => _smallYield;
        public int MediumYield => _mediumYield;
        public int LargeYield => _largeYield;
        public int DragYield => _dragYield;
        public int TrailYield => _trailYield;
        public float DragClearance => _dragClearance;
        public float DragMass => _dragMass;
        public float DragLinearDamping => _dragLinearDamping;
        public float DragAngularDamping => _dragAngularDamping;
        public float DragBounciness => _dragBounciness;
        public float DragFriction => _dragFriction;
        public float BreakDuration => _breakDuration;
        public float BreakPop => _breakPop;
        public float BreakTumble => _breakTumble;
        public float MagnetRadius => _magnetRadius;
        public float FlightDuration => _flightDuration;
        public float FlightPerMetre => _flightPerMetre;
        public float FlightLift => _flightLift;
        public float SpiralRadius => _spiralRadius;
        public float SpiralTurns => _spiralTurns;
        public float FlightSpin => _flightSpin;
        public float ArrivalScale => _arrivalScale;
        public float MinArrivalInterval => _minArrivalInterval;
        public float ChainWindow => _chainWindow;
        public float TrailSink => _trailSink;
        public float TrailTilt => _trailTilt;
        public float GlanceRadius => _glanceRadius;
        public int FlashPoolSize => _flashPoolSize;
        public float FlashDuration => _flashDuration;
        public Vector2 FlashRadius => _flashRadius;
        public float FlashIntensity => _flashIntensity;

        private static Vector2 Ordered(Vector2 range)
        {
            return range.x <= range.y ? range : new Vector2(range.y, range.x);
        }
    }
}
