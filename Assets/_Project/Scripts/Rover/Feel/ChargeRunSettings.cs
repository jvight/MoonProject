using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The charge run's lamps along 07's back (docs/features/M3-26 §2): their glow levels and the flare, spike, pulse,
    /// drain and fade timings. The run's own length is the Anticipate beat's.
    /// </summary>
    [Serializable]
    public sealed class ChargeRunSettings
    {
        [Tooltip("The run's colour per cutter tier (M3-26 §2.1): dim cyan, a cleaner cyan, the ribs' bright edge, then "
            + "a white-hot core with an amber edge. Warm machinery, never red.")]
        [SerializeField] private Color[] _tierColours =
        {
            new Color(0.35f, 0.75f, 0.8f),
            new Color(0.45f, 0.9f, 1f),
            new Color(0.75f, 0.97f, 1f),
            new Color(1f, 0.85f, 0.6f),
        };

        [Tooltip("A lamp's glow at rest, before any run (dim, a little dusty).")]
        [Range(0f, 2f)]
        [SerializeField] private float _restGlow = 0.15f;

        [Tooltip("A lamp's glow once the run has passed it (charged).")]
        [Range(0f, 8f)]
        [SerializeField] private float _chargedGlow = 1.6f;

        [Tooltip("The flare as the run reaches a lamp (+60%).")]
        [Range(0f, 2f)]
        [SerializeField] private float _flare = 0.6f;

        [Tooltip("Seconds a lamp's flare lasts.")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _flareSeconds = 0.08f;

        [Tooltip("Every lamp spikes once at ignition (share of the charged glow).")]
        [Range(0f, 3f)]
        [SerializeField] private float _ignitionSpike = 1f;

        [Tooltip("Seconds the ignition spike lasts.")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _spikeSeconds = 0.12f;

        [Tooltip("Each cut step pulses the lamps (share of the charged glow).")]
        [Range(0f, 2f)]
        [SerializeField] private float _stepPulse = 0.3f;

        [Tooltip("Seconds a cut step's pulse lasts.")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _pulseSeconds = 0.12f;

        [Tooltip("Seconds the lamps take to drain back toward the battery on a release.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _drainSeconds = 0.2f;

        [Tooltip("Seconds the lamps take to fade as the emitter vents at the break.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _fadeSeconds = 0.4f;

        [Tooltip("The cutter tier from which the Focus Lens is fitted on 07's eye.")]
        [Range(0, 3)]
        [SerializeField] private int _lensFromTier = 1;

        [Header("The bay's test cut after a cutter upgrade")]
        [Tooltip("Seconds after the upgrade before the test cut's charge run starts (the part seats first).")]
        [Range(0f, 5f)]
        [SerializeField] private float _testCutDelay = 1.2f;

        [Tooltip("Seconds the test coupon burns after ignition before it cracks (charge + burn stays under a second).")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _testCutBurn = 0.25f;

        /// <summary>The run's colour at cutter tier <paramref name="tier"/> (the last tuned one beyond).</summary>
        public Color TierColour(int tier)
        {
            return _tierColours.Length == 0
                ? Color.white
                : _tierColours[Mathf.Clamp(tier, 0, _tierColours.Length - 1)];
        }

        public int LensFromTier => _lensFromTier;

        public float TestCutDelay => _testCutDelay;

        public float TestCutBurn => _testCutBurn;

        public float RestGlow => _restGlow;

        public float ChargedGlow => _chargedGlow;

        public float Flare => _flare;

        public float FlareSeconds => _flareSeconds;

        public float IgnitionSpike => _ignitionSpike;

        public float SpikeSeconds => _spikeSeconds;

        public float StepPulse => _stepPulse;

        public float PulseSeconds => _pulseSeconds;

        public float DrainSeconds => _drainSeconds;

        public float FadeSeconds => _fadeSeconds;
    }
}
