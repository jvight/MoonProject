using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Salvage (docs/features/M3-13): how generously 07 aims at a piece, how long the beam cuts, how much each piece
    /// is worth, how a drag piece is pulled clear, and how a cut piece breaks off and folds into 07. Created by the
    /// Gameplay/Tuning builder; runtime code only reads it.
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

        [Tooltip("Steps a cut climbs through: each one publishes an Act beat (the sustained rumble and sizzle).")]
        [Range(1, 8)] [SerializeField] private int _cutSteps = 5;

        [Tooltip("Seconds the beam slides to the next segment of a big piece, still lit, with no new charge.")]
        [Range(0.1f, 0.6f)] [SerializeField] private float _segmentSlideSeconds = 0.25f;

        [Header("Piece sizes (docs/features/M3-26 §4.1; VISION ruling 5: the sites give about twice the recipes)")]
        [Tooltip("Small, medium, large plate and hull plate, in PieceSize order: segments, seconds per segment at " +
                 "tier 0, units.")]
        [SerializeField] private PieceSizeRow[] _sizes =
        {
            new PieceSizeRow(1, 1.2f, 2), new PieceSizeRow(1, 2.5f, 3), new PieceSizeRow(2, 2.5f, 5),
            new PieceSizeRow(3, 3.5f, 8),
        };

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

        [Header("No loss (VISION ruling 1)")]
        [Tooltip("A drag piece resting off the drivable floor this many seconds eases back beside its site.")]
        [Range(0.2f, 10f)] [SerializeField] private float _returnDelay = 2f;

        [Tooltip("A drag piece this many metres under the surface (it fell through) eases back at once.")]
        [Range(0.2f, 10f)] [SerializeField] private float _belowSurfaceLimit = 1.5f;

        [Tooltip("Metres outside its site's footprint, and inside the drivable floor, where a lost piece is set down.")]
        [Range(0.5f, 15f)] [SerializeField] private float _returnMargin = 3f;

        [Tooltip("Height (m) of the soft arc a returning piece floats along, and of its hover before it settles.")]
        [Range(0f, 5f)] [SerializeField] private float _returnHover = 1.2f;

        [Tooltip("Seconds a return takes over no distance at all...")]
        [Range(0.5f, 10f)] [SerializeField] private float _returnDuration = 3f;

        [Tooltip("...plus this many seconds per metre it floats (a long way back stays gentle).")]
        [Range(0f, 0.2f)] [SerializeField] private float _returnPerMetre = 0.02f;

        [Header("Break off (feel-pass §3.1: crack, tumble, fold, tie-off)")]
        [Tooltip("Seconds of the crack: the piece holds while the wreck shudders, then it lets go.")]
        [Range(0f, 0.4f)] [SerializeField] private float _crackSeconds = 0.12f;

        [Tooltip("Seconds the freed piece tumbles slowly under the Moon's gravity before it folds.")]
        [Range(0.5f, 2f)] [SerializeField] private float _tumbleSeconds = 1.2f;

        [Tooltip("Metres the piece rises at the top of its tumble (it falls back a little under 1/6 g).")]
        [Range(0f, 1.5f)] [SerializeField] private float _tumbleRise = 0.5f;

        [Tooltip("Metres a cut piece eases out of its cut face as it comes away.")]
        [Range(0f, 2f)] [SerializeField] private float _breakPop = 0.45f;

        [Tooltip("Degrees a cut piece turns over its tumble.")]
        [Range(0f, 180f)] [SerializeField] private float _breakTumble = 35f;

        [Tooltip("Seconds the piece's facets take to fold into a bundle of its material.")]
        [Range(0.2f, 1.5f)] [SerializeField] private float _foldSeconds = 0.6f;

        [Tooltip("Seconds of the bundle's tie-off pop.")]
        [Range(0.05f, 0.6f)] [SerializeField] private float _tieSeconds = 0.2f;

        [Tooltip("The bundle's scale at the top of its tie-off pop (1 = none).")]
        [Range(1f, 1.4f)] [SerializeField] private float _tiePop = 1.12f;

        [Header("Wreck shudder at the crack")]
        [Tooltip("Metres the wreck shudders when a piece cracks free.")]
        [Range(0f, 0.05f)] [SerializeField] private float _shudderAmplitude = 0.01f;

        [Tooltip("Seconds the shudder lasts, fading out.")]
        [Range(0.05f, 0.5f)] [SerializeField] private float _shudderSeconds = 0.15f;

        [Tooltip("Shakes per second of the shudder.")]
        [Range(5f, 60f)] [SerializeField] private float _shudderFrequency = 30f;

        [Header("Into 07")]
        [Tooltip("Flight time (s) of a bundle right next to 07.")]
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

        [Tooltip("Minimum seconds between two arrivals: a run of bundles plays as an arpeggio, never a chord.")]
        [Range(0f, 0.5f)] [SerializeField] private float _minArrivalInterval = 0.12f;

        [Tooltip("Seconds within which the next piece salvaged at the same site climbs one " +
                 "step of the salvage melody: long enough to drive round a wreck to its next piece.")]
        [Range(1f, 90f)] [SerializeField] private float _chainWindow = 40f;

        [Header("Arrival flash")]
        [Tooltip("Pooled flashes (enough for a run of bundles arriving together).")]
        [Range(1, 16)] [SerializeField] private int _flashPoolSize = 4;

        [Tooltip("Seconds a flash lasts.")]
        [Range(0.05f, 2f)] [SerializeField] private float _flashDuration = 0.45f;

        [Tooltip("Flash radius (m) at its start and end.")]
        [SerializeField] private Vector2 _flashRadius = new Vector2(0.12f, 0.5f);

        [Tooltip("Peak brightness of a flash (around 1 stays cyan; higher blooms white).")]
        [Range(0f, 10f)] [SerializeField] private float _flashIntensity = 1.2f;

        [Header("Landmark pilot (M3-18 L1)")]
        [Tooltip("Brightness of a site's landmark pilot once its heart is found, as a multiple of the palette's " +
                 "authored emission (dim and warm: well under 1).")]
        [Range(0f, 2f)] [SerializeField] private float _landmarkGlow = 0.5f;

        [Tooltip("Seconds (time constant) for the pilot to come up when the heart is found.")]
        [Range(0.1f, 10f)] [SerializeField] private float _landmarkEase = 2.5f;

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
        public float DragClearance => _dragClearance;
        public float DragMass => _dragMass;
        public float DragLinearDamping => _dragLinearDamping;
        public float DragAngularDamping => _dragAngularDamping;
        public float DragBounciness => _dragBounciness;
        public float DragFriction => _dragFriction;
        public float ReturnDelay => _returnDelay;
        public float BelowSurfaceLimit => _belowSurfaceLimit;
        public float ReturnMargin => _returnMargin;
        public float ReturnHover => _returnHover;
        public float ReturnDuration => _returnDuration;
        public float ReturnPerMetre => _returnPerMetre;
        public int CutSteps => _cutSteps;
        public float SegmentSlideSeconds => _segmentSlideSeconds;

        /// <summary>The size table's row for <paramref name="size"/>.</summary>
        public PieceSizeRow Size(PieceSize size)
        {
            int row = (int)size;
            if (row < 0 || row >= _sizes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "The size table has no row for it.");
            }

            return _sizes[row];
        }
        public float CrackSeconds => _crackSeconds;
        public float TumbleSeconds => _tumbleSeconds;
        public float TumbleRise => _tumbleRise;
        public float FoldSeconds => _foldSeconds;
        public float TieSeconds => _tieSeconds;
        public float TiePop => _tiePop;
        public float ShudderAmplitude => _shudderAmplitude;
        public float ShudderSeconds => _shudderSeconds;
        public float ShudderFrequency => _shudderFrequency;

        /// <summary>Seconds from the break to the bundle's flight: crack, tumble, fold and tie-off.</summary>
        public float BreakDuration => _crackSeconds + _tumbleSeconds + _foldSeconds + _tieSeconds;
        public float BreakPop => _breakPop;
        public float BreakTumble => _breakTumble;
        public float FlightDuration => _flightDuration;
        public float FlightPerMetre => _flightPerMetre;
        public float FlightLift => _flightLift;
        public float SpiralRadius => _spiralRadius;
        public float SpiralTurns => _spiralTurns;
        public float FlightSpin => _flightSpin;
        public float ArrivalScale => _arrivalScale;
        public float MinArrivalInterval => _minArrivalInterval;
        public float ChainWindow => _chainWindow;
        public int FlashPoolSize => _flashPoolSize;
        public float FlashDuration => _flashDuration;
        public Vector2 FlashRadius => _flashRadius;
        public float FlashIntensity => _flashIntensity;
        public float LandmarkGlow => _landmarkGlow;
        public float LandmarkEase => _landmarkEase;
    }
}
