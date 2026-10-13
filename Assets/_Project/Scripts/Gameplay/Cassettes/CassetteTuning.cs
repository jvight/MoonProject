using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How a tape waits in its opened container and pops out into 07 when it drives up. Created by the Gameplay/Tuning
    /// builder; runtime code only reads it.
    /// </summary>
    public sealed class CassetteTuning : ScriptableObject
    {
        [Header("Waiting in its container")]
        [Tooltip("Lean (degrees) of a waiting tape back against its container's open mouth.")]
        [Range(0f, 80f)] [SerializeField] private float _restLean = 35f;

        [Tooltip("Seconds a tape takes to rise off its opened container's ItemSocket to the open mouth (0.5.5 §2).")]
        [Range(0.1f, 3f)] [SerializeField] private float _presentSeconds = 0.7f;

        [Tooltip("07 glances at a waiting tape within this many metres.")]
        [Range(1f, 60f)] [SerializeField] private float _glanceRadius = 16f;

        [Header("Pickup")]
        [Tooltip("A waiting tape within this many metres of 07 pops out of its container and drifts in.")]
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

        public float RestLean => _restLean;
        public float PresentSeconds => _presentSeconds;
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
