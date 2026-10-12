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
