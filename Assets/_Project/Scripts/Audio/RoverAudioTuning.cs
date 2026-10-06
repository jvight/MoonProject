using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How the rover sounds: electric hum pitch/volume from speed and throttle, dust crunch from speed while
    /// grounded, and when the suspension creaks. Created by the Audio/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class RoverAudioTuning : ScriptableObject
    {
        // Keeps ranges non-degenerate if an asset is edited into min >= max.
        private const float MinSpan = 0.01f;

        [Header("Hum pitch")]
        [Tooltip("Hum pitch when parked (AudioSource.pitch; the loop is D2 at 1.0).")]
        [Range(0.5f, 2f)] [SerializeField] private float _idlePitch = 0.8f;

        [Tooltip("Hum pitch at top speed.")]
        [Range(0.5f, 2f)] [SerializeField] private float _topSpeedPitch = 1.45f;

        [Tooltip("Shape of the speed -> pitch curve (below 1 rises early, above 1 rises late).")]
        [Range(0.3f, 3f)] [SerializeField] private float _speedPitchCurve = 0.8f;

        [Tooltip("Extra pitch while the throttle is held (motor under load).")]
        [Range(0f, 0.5f)] [SerializeField] private float _throttlePitchBonus = 0.08f;

        [Tooltip("Extra pitch while airborne with throttle held (wheels spinning free).")]
        [Range(0f, 0.5f)] [SerializeField] private float _airbornePitchBonus = 0.1f;

        [Tooltip("Seconds for the hum pitch to follow (time constant).")]
        [Range(0.01f, 2f)] [SerializeField] private float _pitchSmoothing = 0.35f;

        [Header("Hum volume")]
        [Tooltip("Hum volume when parked (scales the cue volume).")]
        [Range(0f, 1f)] [SerializeField] private float _idleVolume = 0.35f;

        [Tooltip("Hum volume at top speed.")]
        [Range(0f, 1f)] [SerializeField] private float _topSpeedVolume = 0.8f;

        [Tooltip("Extra hum volume while the throttle is held.")]
        [Range(0f, 0.5f)] [SerializeField] private float _throttleVolumeBonus = 0.15f;

        [Tooltip("Seconds for the hum volume to follow (time constant).")]
        [Range(0.01f, 2f)] [SerializeField] private float _volumeSmoothing = 0.25f;

        [Header("Dust crunch")]
        [Tooltip("Crunch volume at full speed on the ground.")]
        [Range(0f, 1f)] [SerializeField] private float _crunchMaxVolume = 0.9f;

        [Tooltip("Normalised speed where the crunch starts.")]
        [Range(0f, 1f)] [SerializeField] private float _crunchSpeedStart = 0.03f;

        [Tooltip("Normalised speed where the crunch reaches full volume.")]
        [Range(0.01f, 1f)] [SerializeField] private float _crunchSpeedFull = 0.6f;

        [Tooltip("Crunch pitch at a crawl.")]
        [Range(0.5f, 2f)] [SerializeField] private float _crunchPitchSlow = 0.85f;

        [Tooltip("Crunch pitch at top speed.")]
        [Range(0.5f, 2f)] [SerializeField] private float _crunchPitchFast = 1.15f;

        [Tooltip("Seconds for the crunch to swell in (time constant).")]
        [Range(0.01f, 2f)] [SerializeField] private float _crunchRiseTime = 0.12f;

        [Tooltip("Seconds for the crunch to fade when slowing or leaving the ground (time constant).")]
        [Range(0.01f, 2f)] [SerializeField] private float _crunchFallTime = 0.08f;

        [Header("Suspension creaks")]
        [Tooltip("Ground-normal turn rate (deg/s) that starts a creak (bumps, ridges).")]
        [Min(1f)] [SerializeField] private float _creakTurnRateMin = 40f;

        [Tooltip("Ground-normal turn rate (deg/s) that gives the strongest creak.")]
        [Min(1f)] [SerializeField] private float _creakTurnRateFull = 160f;

        [Tooltip("Minimum seconds between creaks.")]
        [Min(0f)] [SerializeField] private float _creakCooldown = 0.5f;

        [Tooltip("Strength (volume scale) of the softest creak.")]
        [Range(0f, 1f)] [SerializeField] private float _creakMinStrength = 0.35f;

        [Tooltip("Seconds after a landing before the springs creak (they settle after the thump).")]
        [Min(0f)] [SerializeField] private float _landingCreakDelay = 0.14f;

        [Tooltip("Impact speed (m/s) below which a landing does not creak.")]
        [Min(0f)] [SerializeField] private float _landingCreakMinImpact = 0.8f;

        [Tooltip("Impact speed (m/s) that gives the strongest landing creak.")]
        [Min(0.1f)] [SerializeField] private float _landingCreakFullImpact = 3.5f;

        [Header("Wake-up")]
        [Tooltip("Hum pitch the motor powers up from when 07 wakes (it glides up to the idle pitch).")]
        [Range(0.25f, 2f)] [SerializeField] private float _wakeStartPitch = 0.55f;

        [Tooltip("Seconds (time constant) for the hum to swell in when 07 wakes; silent while asleep.")]
        [Range(0.01f, 5f)] [SerializeField] private float _wakeHumTime = 0.8f;

        [Header("Recovery lift")]
        [Tooltip("Semitones the lift servo glides up over the lift; stay in D major pentatonic from D: " +
                 "2, 4, 7, 9 or 12.")]
        [Range(0f, 12f)] [SerializeField] private float _liftRiseSemitones = 2f;

        [Tooltip("Seconds for the lift sound to swell in (capped at half the lift).")]
        [Min(0.01f)] [SerializeField] private float _liftFadeIn = 0.4f;

        [Tooltip("Seconds for the lift sound to ease out before touchdown (capped at half the lift).")]
        [Min(0.01f)] [SerializeField] private float _liftFadeOut = 0.35f;

        [Tooltip("Volume scale of the lift loop (times the cue volume).")]
        [Range(0f, 1f)] [SerializeField] private float _liftVolume = 1f;

        [Tooltip("Volume scale of the settle when the lift sets 07 down.")]
        [Range(0f, 1f)] [SerializeField] private float _settleVolume = 1f;

        [Header("Placement")]
        [Tooltip("3D amount of the hum/crunch loops (1 = fully positional, lower keeps them centred and close).")]
        [Range(0f, 1f)] [SerializeField] private float _loopSpatialBlend = 0.75f;

        public float IdlePitch => _idlePitch;
        public float TopSpeedPitch => _topSpeedPitch;
        public float SpeedPitchCurve => _speedPitchCurve;
        public float ThrottlePitchBonus => _throttlePitchBonus;
        public float AirbornePitchBonus => _airbornePitchBonus;
        public float PitchSmoothing => _pitchSmoothing;
        public float IdleVolume => _idleVolume;
        public float TopSpeedVolume => _topSpeedVolume;
        public float ThrottleVolumeBonus => _throttleVolumeBonus;
        public float VolumeSmoothing => _volumeSmoothing;
        public float CrunchMaxVolume => _crunchMaxVolume;
        public float CrunchSpeedStart => _crunchSpeedStart;
        public float CrunchSpeedFull => Mathf.Max(_crunchSpeedFull, _crunchSpeedStart + MinSpan);
        public float CrunchPitchSlow => _crunchPitchSlow;
        public float CrunchPitchFast => _crunchPitchFast;
        public float CrunchRiseTime => _crunchRiseTime;
        public float CrunchFallTime => _crunchFallTime;
        public float CreakTurnRateMin => _creakTurnRateMin;
        public float CreakTurnRateFull => Mathf.Max(_creakTurnRateFull, _creakTurnRateMin + MinSpan);
        public float CreakCooldown => _creakCooldown;
        public float CreakMinStrength => _creakMinStrength;
        public float LandingCreakDelay => _landingCreakDelay;
        public float LandingCreakMinImpact => _landingCreakMinImpact;
        public float LandingCreakFullImpact => Mathf.Max(_landingCreakFullImpact, _landingCreakMinImpact + MinSpan);
        public float LoopSpatialBlend => _loopSpatialBlend;
        public float WakeStartPitch => _wakeStartPitch;
        public float WakeHumTime => _wakeHumTime;
        public float LiftRiseSemitones => _liftRiseSemitones;
        public float LiftFadeIn => _liftFadeIn;
        public float LiftFadeOut => _liftFadeOut;
        public float LiftVolume => _liftVolume;
        public float SettleVolume => _settleVolume;
    }
}
