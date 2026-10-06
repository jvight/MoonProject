using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Wheel effects: tire tracks, rolling dust and the landing dust ring. Colours, meshes and particle curves are
    /// authored by the Rover builder from the palette; the amounts and timings players feel live here.
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
        [Range(1f, 120f)]
        [SerializeField] private float _trackLifetime = 24f;

        [Tooltip("Fraction of the lifetime a track stays at full strength before fading out.")]
        [Range(0f, 1f)]
        [SerializeField] private float _trackFadeStart = 0.55f;

        [Tooltip("Strength of a fresh track (vertex alpha; the material colour sets how dark it can get).")]
        [Range(0f, 1f)]
        [SerializeField] private float _trackOpacity = 0.6f;

        [Tooltip("Height (m) the ribbon floats above the ground to stay clear of it.")]
        [Range(0f, 0.1f)]
        [SerializeField] private float _trackLift = 0.02f;

        [Tooltip("Track points kept per side (memory is fixed; the oldest are recycled).")]
        [Range(16, 4096)]
        [SerializeField] private int _trackCapacity = 640;

        [Tooltip("How far (m) above and below a dust socket the ground is searched for track points.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _trackGroundProbe = 0.6f;

        [Header("Rolling dust")]
        [Tooltip("Below this speed (m/s) the wheels raise no dust.")]
        [Range(0f, 5f)]
        [SerializeField] private float _dustMinSpeed = 0.6f;

        [Tooltip("Speed (m/s) at which dust reaches its full rate.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _dustFullSpeed = 7f;

        [Tooltip("Dust puffs per second per side at full rate.")]
        [Range(0f, 100f)]
        [SerializeField] private float _dustMaxRate = 26f;

        [Tooltip("Seconds each dust puff lingers before it has faded away.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _dustLifetime = 1.0f;

        [Tooltip("Size (m) of a dust puff when it appears; it swells to about twice that as it fades.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _dustSize = 0.2f;

        [Tooltip("Initial speed (m/s) of a dust puff kicked up by the wheel.")]
        [Range(0f, 5f)]
        [SerializeField] private float _dustSpeed = 1.3f;

        [Tooltip("Drag on dust (1/s): high, so a puff pops up quickly and then settles into a slow drift.")]
        [Range(0f, 10f)]
        [SerializeField] private float _dustDrag = 3f;

        [Tooltip("Peak opacity of a dust puff (lunar dust is fine and thin).")]
        [Range(0f, 1f)]
        [SerializeField] private float _dustOpacity = 0.5f;

        [Tooltip("Fraction of world (lunar) gravity acting on dust. Low: it hangs in the air dreamily.")]
        [Range(0f, 2f)]
        [SerializeField] private float _dustGravity = 0.3f;

        [Header("Landing ring")]
        [Tooltip("Impact speed (m/s) of the softest landing that still raises a ring.")]
        [Range(0f, 5f)]
        [SerializeField] private float _landingMinImpact = 0.5f;

        [Tooltip("Impact speed (m/s) that raises the biggest ring.")]
        [Range(0.5f, 15f)]
        [SerializeField] private float _landingFullImpact = 4f;

        [Tooltip("Puffs in the softest ring.")]
        [Range(1, 200)]
        [SerializeField] private int _landingMinCount = 10;

        [Tooltip("Puffs in the biggest ring.")]
        [Range(1, 200)]
        [SerializeField] private int _landingMaxCount = 36;

        [Tooltip("Outward speed (m/s) of the softest ring.")]
        [Range(0f, 10f)]
        [SerializeField] private float _landingMinSpeed = 1.2f;

        [Tooltip("Outward speed (m/s) of the biggest ring.")]
        [Range(0f, 10f)]
        [SerializeField] private float _landingMaxSpeed = 3f;

        [Tooltip("Puff size (m) in the softest ring.")]
        [Range(0.02f, 2f)]
        [SerializeField] private float _landingMinSize = 0.2f;

        [Tooltip("Puff size (m) in the biggest ring.")]
        [Range(0.02f, 2f)]
        [SerializeField] private float _landingMaxSize = 0.42f;

        [Tooltip("Seconds a landing puff lingers.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _landingLifetime = 1.3f;

        [Tooltip("Drag on the landing ring (1/s): it bursts out, then slows and hangs.")]
        [Range(0f, 10f)]
        [SerializeField] private float _landingDrag = 2.2f;

        [Tooltip("Peak opacity of a landing puff.")]
        [Range(0f, 1f)]
        [SerializeField] private float _landingOpacity = 0.6f;

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

        public float LandingMinImpact => _landingMinImpact;

        public float LandingFullImpact => _landingFullImpact;

        public int LandingMinCount => _landingMinCount;

        public int LandingMaxCount => Mathf.Max(_landingMinCount, _landingMaxCount);

        public float LandingMinSpeed => _landingMinSpeed;

        public float LandingMaxSpeed => _landingMaxSpeed;

        public float LandingMinSize => _landingMinSize;

        public float LandingMaxSize => _landingMaxSize;

        public float LandingLifetime => _landingLifetime;

        public float LandingDrag => _landingDrag;

        public float LandingOpacity => _landingOpacity;
    }
}
