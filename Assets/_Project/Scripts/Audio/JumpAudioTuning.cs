using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How the Hover-Jump sounds: the charge hum, the leap, the air while airborne and the cushioned landing.
    /// Created by the Audio/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class JumpAudioTuning : ScriptableObject
    {
        // Keeps ranges non-degenerate if an asset is edited into min >= max.
        private const float MinSpan = 0.01f;

        [Header("Charge hum")]
        [Tooltip("Seconds for the charge hum to swell in when charging starts.")]
        [Range(0.01f, 1f)] [SerializeField] private float _chargeFadeIn = 0.12f;

        [Tooltip("Seconds (time constant) of the glide between pentatonic steps.")]
        [Range(0.005f, 0.5f)] [SerializeField] private float _chargeGlide = 0.05f;

        [Tooltip("Charge hum volume at the start of a charge (it swells to 1 at full charge).")]
        [Range(0f, 1f)] [SerializeField] private float _chargeStartVolume = 0.55f;

        [Tooltip("Seconds for the hum to hand over to the leap.")]
        [Range(0.01f, 0.5f)] [SerializeField] private float _chargeLeapFade = 0.06f;

        [Tooltip("Seconds for the hum to fade when a charge ends without a leap.")]
        [Range(0.01f, 1f)] [SerializeField] private float _chargeCancelFade = 0.2f;

        [Header("Leap")]
        [Tooltip("Strength below which the leap uses the small 'hop' sound.")]
        [Range(0f, 1f)] [SerializeField] private float _hopThreshold = 0.35f;

        [Tooltip("Leap volume scale for a tap (it rises to 1 at full strength).")]
        [Range(0f, 1f)] [SerializeField] private float _leapMinVolume = 0.5f;

        [Tooltip("Coil twang volume scale for a tap (it rises to 1 at full strength).")]
        [Range(0f, 1f)] [SerializeField] private float _twangMinVolume = 0.35f;

        [Header("Airborne wind")]
        [Tooltip("Seconds airborne before any wind (small bumps stay quiet).")]
        [Min(0f)] [SerializeField] private float _windMinAirTime = 0.25f;

        [Tooltip("Seconds of air time over which the wind swells to full.")]
        [Min(0.05f)] [SerializeField] private float _windSwellTime = 1.2f;

        [Tooltip("Speed (m/s) at which the wind reaches its full level and highest pitch.")]
        [Min(0.1f)] [SerializeField] private float _windFullSpeed = 12f;

        [Tooltip("Wind level at a standstill in the air (scaled by the swell).")]
        [Range(0f, 1f)] [SerializeField] private float _windSlowGain = 0.35f;

        [Tooltip("Wind pitch when slow.")]
        [Range(0.5f, 2f)] [SerializeField] private float _windSlowPitch = 0.9f;

        [Tooltip("Wind pitch at full speed.")]
        [Range(0.5f, 2f)] [SerializeField] private float _windFastPitch = 1.25f;

        [Tooltip("Seconds (time constant) for the wind to rise.")]
        [Range(0.01f, 2f)] [SerializeField] private float _windRiseTime = 0.35f;

        [Tooltip("Seconds (time constant) for the wind to resolve after touchdown.")]
        [Range(0.01f, 2f)] [SerializeField] private float _windFallTime = 0.2f;

        [Header("Cushioned landing (after a leap)")]
        [Tooltip("Impact speed (m/s) below which the cushion is silent.")]
        [Min(0f)] [SerializeField] private float _cushionMinImpact = 0.3f;

        [Tooltip("Impact speed (m/s) for the fullest cushion.")]
        [Min(0.1f)] [SerializeField] private float _cushionFullImpact = 4f;

        [Tooltip("Cushion volume scale for the softest landing.")]
        [Range(0f, 1f)] [SerializeField] private float _cushionSoftVolume = 0.35f;

        [Tooltip("Cushion volume scale for the hardest landing.")]
        [Range(0f, 1f)] [SerializeField] private float _cushionHardVolume = 1f;

        public float ChargeFadeIn => _chargeFadeIn;
        public float ChargeGlide => _chargeGlide;
        public float ChargeStartVolume => _chargeStartVolume;
        public float ChargeLeapFade => _chargeLeapFade;
        public float ChargeCancelFade => _chargeCancelFade;
        public float HopThreshold => _hopThreshold;
        public float LeapMinVolume => _leapMinVolume;
        public float TwangMinVolume => _twangMinVolume;
        public float WindMinAirTime => _windMinAirTime;
        public float WindSwellTime => _windSwellTime;
        public float WindFullSpeed => _windFullSpeed;
        public float WindSlowGain => _windSlowGain;
        public float WindSlowPitch => _windSlowPitch;
        public float WindFastPitch => _windFastPitch;
        public float WindRiseTime => _windRiseTime;
        public float WindFallTime => _windFallTime;
        public float CushionMinImpact => _cushionMinImpact;
        public float CushionFullImpact => Mathf.Max(_cushionFullImpact, _cushionMinImpact + MinSpan);
        public float CushionSoftVolume => _cushionSoftVolume;
        public float CushionHardVolume => _cushionHardVolume;
    }
}
