using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How cassettes are placed in the basin and how a tape waits in the dust and pops out into 07 when it drives
    /// through. Created by the Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class CassetteTuning : ScriptableObject
    {
        [Header("Basin planner")]
        [Tooltip("Candidate spots tried for a basin cassette.")]
        [Range(16, 8192)] [SerializeField] private int _attempts = 2000;

        [Tooltip("Distance range (m) of a basin cassette from the base centre.")]
        [SerializeField] private Vector2 _distance = new Vector2(60f, 170f);

        [Tooltip("Metres kept between a basin cassette and the edge of the drivable floor.")]
        [Range(0f, 60f)] [SerializeField] private float _edgeMargin = 10f;

        [Tooltip("Steepest ground (degrees) a basin cassette may wait on (07 drives through it).")]
        [Range(2f, 30f)] [SerializeField] private float _maxSlope = 14f;

        [Tooltip("Radius (m) around the spot that must also be that gentle.")]
        [Range(0.5f, 10f)] [SerializeField] private float _flatProbe = 2f;

        [Tooltip("Minimum metres from any relic site, friend site or friend part (and from other cassettes).")]
        [Range(0f, 100f)] [SerializeField] private float _clearance = 25f;

        [Tooltip("Horizontal distance (m) at which the ground is probed for a small crater rim to tuck against.")]
        [Range(1f, 15f)] [SerializeField] private float _rimProbe = 4f;

        [Tooltip("How much higher (m) the rim must rise over that distance to count as one (min, max).")]
        [SerializeField] private Vector2 _rimRise = new Vector2(0.25f, 2.5f);

        [Tooltip("Directions probed around a spot for the rim.")]
        [Range(4, 64)] [SerializeField] private int _rimDirections = 16;

        [Header("Waiting in the dust")]
        [Tooltip("Height (m) of the tape's pivot above the ground: a little under its half height reads half-buried.")]
        [Range(0f, 0.5f)] [SerializeField] private float _restHeight = 0.07f;

        [Tooltip("Lean (degrees) of a waiting tape back against its rim (or the wall behind it).")]
        [Range(0f, 80f)] [SerializeField] private float _restLean = 35f;

        [Tooltip("07 glances at a waiting tape within this many metres.")]
        [Range(1f, 60f)] [SerializeField] private float _glanceRadius = 16f;

        [Header("Pickup")]
        [Tooltip("A tape within this many metres of 07 pops out of the dust and drifts in.")]
        [Range(1f, 15f)] [SerializeField] private float _magnetRadius = 4.5f;

        [Tooltip("Flight time (s) of a tape right next to 07.")]
        [Range(0.2f, 4f)] [SerializeField] private float _flightDuration = 1.1f;

        [Tooltip("Extra flight time (s) per metre.")]
        [Range(0f, 0.5f)] [SerializeField] private float _flightPerMetre = 0.08f;

        [Tooltip("Height (m) a tape lifts in its flight.")]
        [Range(0f, 4f)] [SerializeField] private float _flightLift = 1.5f;

        [Tooltip("Radius (m) of a tape's spiral.")]
        [Range(0f, 3f)] [SerializeField] private float _spiralRadius = 0.35f;

        [Tooltip("Turns of a tape's spiral.")]
        [Range(0f, 4f)] [SerializeField] private float _spiralTurns = 0.75f;

        [Tooltip("The little spin (degrees per second) of a tape in flight.")]
        [Range(0f, 1440f)] [SerializeField] private float _flightSpin = 420f;

        [Tooltip("Scale of a tape as it reaches 07.")]
        [Range(0.05f, 1f)] [SerializeField] private float _arrivalScale = 0.45f;

        [Tooltip("Seconds of the warm flash when a tape reaches 07.")]
        [Range(0.05f, 3f)] [SerializeField] private float _flashDuration = 0.6f;

        [Tooltip("Radius (m) of that flash as it swells (from, to).")]
        [SerializeField] private Vector2 _flashRadius = new Vector2(0.15f, 0.6f);

        [Tooltip("Brightness of that flash.")]
        [Range(0f, 4f)] [SerializeField] private float _flashIntensity = 1.2f;

        public int Attempts => _attempts;
        public Vector2 Distance => Ordered(_distance);
        public float EdgeMargin => _edgeMargin;
        public float MaxSlope => _maxSlope;
        public float FlatProbe => _flatProbe;
        public float Clearance => _clearance;
        public float RimProbe => _rimProbe;
        public Vector2 RimRise => Ordered(_rimRise);
        public int RimDirections => _rimDirections;
        public float RestHeight => _restHeight;
        public float RestLean => _restLean;
        public float GlanceRadius => _glanceRadius;
        public float MagnetRadius => _magnetRadius;
        public float FlightDuration => _flightDuration;
        public float FlightPerMetre => _flightPerMetre;
        public float FlightLift => _flightLift;
        public float SpiralRadius => _spiralRadius;
        public float SpiralTurns => _spiralTurns;
        public float FlightSpin => _flightSpin;
        public float ArrivalScale => _arrivalScale;
        public float FlashDuration => _flashDuration;
        public Vector2 FlashRadius => Ordered(_flashRadius);
        public float FlashIntensity => _flashIntensity;

        private static Vector2 Ordered(Vector2 range)
        {
            return range.x <= range.y ? range : new Vector2(range.y, range.x);
        }
    }
}
