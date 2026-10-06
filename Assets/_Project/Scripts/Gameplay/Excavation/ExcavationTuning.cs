using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The tractor beam: how close 07 must be, how the relic rises toward its eye, the beam's light and the swirling
    /// dust. Created by the Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class ExcavationTuning : ScriptableObject
    {
        [Header("Reach")]
        [Tooltip("07 can lift a relic anywhere within this many metres (horizontal) of its site.")]
        [Range(1f, 15f)] [SerializeField] private float _reachRadius = 5f;

        [Tooltip("Once lifting, the beam holds on until 07 is this many times the reach away (no flicker at the edge).")]
        [Range(1f, 2f)] [SerializeField] private float _reachHysteresis = 1.3f;

        [Tooltip("Holding the beam asks 07 to ease to a stop; the beam takes hold below this speed (m/s).")]
        [Range(0.1f, 5f)] [SerializeField] private float _engageSpeed = 1.2f;

        [Header("Rise")]
        [Tooltip("Seconds to lift a weightless relic all the way.")]
        [Range(0.5f, 20f)] [SerializeField] private float _baseDuration = 4.5f;

        [Tooltip("Extra seconds per kilogram: heavy memories come up slower.")]
        [Range(0f, 1f)] [SerializeField] private float _secondsPerKg = 0.12f;

        [Tooltip("The relic rises to a point this far (m, horizontal) ahead of 07's eye, clear of the rover.")]
        [Range(1.5f, 8f)] [SerializeField] private float _presentDistance = 3.2f;

        [Tooltip("Height (m) above the ground of that point.")]
        [Range(0.3f, 4f)] [SerializeField] private float _presentHeight = 1.3f;

        [Tooltip("Turn speed (degrees per second) of a rising relic.")]
        [Range(0f, 180f)] [SerializeField] private float _riseSpin = 40f;

        [Tooltip("Upward speed (m/s) of the little hop when a relic is free.")]
        [Range(0f, 4f)] [SerializeField] private float _surfaceHop = 0.9f;

        [Tooltip("Spin (degrees per second) the relic keeps when it is handed to physics.")]
        [Range(0f, 180f)] [SerializeField] private float _surfaceSpin = 35f;

        [Header("Beam")]
        [Tooltip("Brightness of the beam while lifting (HDR).")]
        [Range(0f, 10f)] [SerializeField] private float _beamIntensity = 1.6f;

        [Tooltip("Brightness while 07 is still easing to a stop over the site (the beam reaching out).")]
        [Range(0f, 5f)] [SerializeField] private float _reachingIntensity = 0.35f;

        [Tooltip("Seconds (time constant) for the beam to brighten.")]
        [Range(0f, 2f)] [SerializeField] private float _beamFadeIn = 0.3f;

        [Tooltip("Seconds (time constant) for the beam to fade.")]
        [Range(0f, 3f)] [SerializeField] private float _beamFadeOut = 0.45f;

        [Tooltip("Radius (m) of the beam where it meets the relic.")]
        [Range(0.1f, 3f)] [SerializeField] private float _beamRadius = 0.9f;

        [Header("Dust")]
        [Tooltip("Dust motes per second swirling around the site while lifting.")]
        [Range(0f, 200f)] [SerializeField] private float _dustRate = 30f;

        [Tooltip("Seconds a mote lives (min, max).")]
        [SerializeField] private Vector2 _dustLifetime = new Vector2(1.2f, 2.2f);

        [Tooltip("Mote size in metres (min, max).")]
        [SerializeField] private Vector2 _dustSize = new Vector2(0.25f, 0.6f);

        [Tooltip("Radius (m) of the ring the dust rises from.")]
        [Range(0.1f, 5f)] [SerializeField] private float _dustRadius = 1.3f;

        [Tooltip("Swirl speed of the dust around the site (radians per second).")]
        [Range(0f, 6f)] [SerializeField] private float _dustSwirl = 1.4f;

        [Tooltip("Upward drift (m/s) of the dust.")]
        [Range(0f, 3f)] [SerializeField] private float _dustRise = 0.5f;

        [Tooltip("Most motes alive at once.")]
        [Range(10, 500)] [SerializeField] private int _maxDust = 120;

        public float ReachRadius => _reachRadius;
        public float ReachHysteresis => _reachHysteresis;
        public float EngageSpeed => _engageSpeed;
        public float BaseDuration => _baseDuration;
        public float SecondsPerKg => _secondsPerKg;
        public float PresentDistance => _presentDistance;
        public float PresentHeight => _presentHeight;
        public float RiseSpin => _riseSpin;
        public float SurfaceHop => _surfaceHop;
        public float SurfaceSpin => _surfaceSpin;
        public float BeamIntensity => _beamIntensity;
        public float ReachingIntensity => _reachingIntensity;
        public float BeamFadeIn => _beamFadeIn;
        public float BeamFadeOut => _beamFadeOut;
        public float BeamRadius => _beamRadius;
        public float DustRate => _dustRate;
        public Vector2 DustLifetime => _dustLifetime;
        public Vector2 DustSize => _dustSize;
        public float DustRadius => _dustRadius;
        public float DustSwirl => _dustSwirl;
        public float DustRise => _dustRise;
        public int MaxDust => _maxDust;

        /// <summary>Seconds to lift a relic of <paramref name="mass"/> kg from fully buried to free.</summary>
        public float DurationFor(float mass)
        {
            return _baseDuration + Mathf.Max(0f, mass) * _secondsPerKg;
        }
    }
}
