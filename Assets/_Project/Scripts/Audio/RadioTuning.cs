using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The radio: how far the clear signal reaches, how clarity maps to low-pass/static/volume/wobble, and how the
    /// station crossfades between tracks. Created by the Audio/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class RadioTuning : ScriptableObject
    {
        [Header("Signal field")]
        [Tooltip("Metres from the base where the music is perfectly clear (radio tower upgrades raise it at runtime).")]
        [Min(0f)] [SerializeField] private float _signalRadius = 60f;

        [Tooltip("Metres over which the signal fades from clear to gone beyond the radius.")]
        [Min(1f)] [SerializeField] private float _falloffWidth = 140f;

        [Tooltip("Seconds (time constant) for a new signal radius to take effect: progress is heard as a slow bloom.")]
        [Min(0f)] [SerializeField] private float _radiusEaseTime = 3f;

        [Tooltip("Seconds (time constant) smoothing the clarity the player hears.")]
        [Min(0f)] [SerializeField] private float _claritySmoothing = 0.8f;

        [Header("Clarity -> sound")]
        [Tooltip("Low-pass cutoff (Hz) with no signal: warm, muffled haze.")]
        [Range(200f, 4000f)] [SerializeField] private float _minCutoff = 800f;

        [Tooltip("Low-pass cutoff (Hz) with a perfect signal (22 kHz = open).")]
        [Range(4000f, 22000f)] [SerializeField] private float _maxCutoff = 22000f;

        [Tooltip("Clarity exponent before the logarithmic cutoff sweep (above 1 keeps the haze longer).")]
        [Range(0.2f, 4f)] [SerializeField] private float _cutoffCurve = 1.2f;

        [Tooltip("Resonance (Q) of the radio low-pass; 1 is neutral, a touch more sounds like an old speaker.")]
        [Range(1f, 3f)] [SerializeField] private float _lowpassResonance = 1.1f;

        [Tooltip("Static volume with no signal (scales the static cue).")]
        [Range(0f, 1f)] [SerializeField] private float _staticMaxVolume = 0.75f;

        [Tooltip("Static volume with a perfect signal (a faint vinyl presence).")]
        [Range(0f, 1f)] [SerializeField] private float _staticFloorVolume = 0.05f;

        [Tooltip("Shape of clarity -> static (above 1 drops the static faster as the signal improves).")]
        [Range(0.2f, 4f)] [SerializeField] private float _staticCurve = 1.3f;

        [Tooltip("Music volume with no signal (the music never vanishes, it sinks into the haze).")]
        [Range(0f, 1f)] [SerializeField] private float _musicVolumeAtNoSignal = 0.45f;

        [Header("Wow / flutter")]
        [Tooltip("Pitch wobble (cents) with no signal; the tracks already carry their own gentle tape wow.")]
        [Range(0f, 50f)] [SerializeField] private float _maxWobbleCents = 18f;

        [Tooltip("Pitch wobble (cents) with a perfect signal.")]
        [Range(0f, 10f)] [SerializeField] private float _baseWobbleCents = 1f;

        [Tooltip("Slow wow rate (Hz).")]
        [Range(0.05f, 3f)] [SerializeField] private float _wowRate = 0.55f;

        [Tooltip("Fast flutter rate (Hz).")]
        [Range(2f, 15f)] [SerializeField] private float _flutterRate = 6.3f;

        [Tooltip("Share of flutter in the wobble (0 = only wow).")]
        [Range(0f, 1f)] [SerializeField] private float _flutterShare = 0.3f;

        [Header("Playlist")]
        [Tooltip("Seconds of the dial-tuning crossfade between tracks.")]
        [Range(0.5f, 6f)] [SerializeField] private float _crossfadeDuration = 2.4f;

        [Tooltip("Seconds before a track ends that the crossfade begins.")]
        [Range(0f, 6f)] [SerializeField] private float _crossfadeLead = 1.4f;

        [Tooltip("Fraction of the crossfade over which the outgoing track fades out.")]
        [Range(0.1f, 1f)] [SerializeField] private float _outgoingFadeEnd = 0.55f;

        [Tooltip("Fraction of the crossfade at which the incoming track starts and fades in.")]
        [Range(0f, 0.9f)] [SerializeField] private float _incomingStart = 0.45f;

        [Tooltip("Extra static at the middle of a crossfade (the dial passes between stations).")]
        [Range(0f, 1f)] [SerializeField] private float _tuneStaticBoost = 0.35f;

        [Tooltip("Volume scale of the dial-tuning swish.")]
        [Range(0f, 1f)] [SerializeField] private float _tuneSwishVolume = 0.8f;

        [Header("Wake-up (the radio crackles on when 07 wakes)")]
        [Tooltip("Seconds of static after 07 starts waking on its own before the music begins.")]
        [Min(0f)] [SerializeField] private float _wakeMusicDelay = 1.6f;

        [Tooltip("Seconds over which the music then resolves out of the static.")]
        [Min(0.01f)] [SerializeField] private float _wakeMusicFade = 4f;

        [Tooltip("Static before the music when the player woke 07 by driving (quicker wake-up).")]
        [Min(0f)] [SerializeField] private float _playerWakeMusicDelay = 0.6f;

        [Tooltip("Music resolve time when the player woke 07 by driving.")]
        [Min(0.01f)] [SerializeField] private float _playerWakeMusicFade = 1.8f;

        [Tooltip("Seconds for the set to power on (static and swish swell in instead of switching on).")]
        [Min(0.01f)] [SerializeField] private float _wakePowerTime = 0.3f;

        [Tooltip("Extra static while the radio is still finding the station (melts away as the music resolves).")]
        [Range(0f, 1f)] [SerializeField] private float _wakeStaticBoost = 0.55f;

        public float SignalRadius => _signalRadius;
        public float FalloffWidth => _falloffWidth;
        public float RadiusEaseTime => _radiusEaseTime;
        public float ClaritySmoothing => _claritySmoothing;
        public float MinCutoff => _minCutoff;
        public float MaxCutoff => Mathf.Max(_maxCutoff, _minCutoff);
        public float CutoffCurve => _cutoffCurve;
        public float LowpassResonance => _lowpassResonance;
        public float StaticMaxVolume => _staticMaxVolume;
        public float StaticFloorVolume => _staticFloorVolume;
        public float StaticCurve => _staticCurve;
        public float MusicVolumeAtNoSignal => _musicVolumeAtNoSignal;
        public float MaxWobbleCents => _maxWobbleCents;
        public float BaseWobbleCents => _baseWobbleCents;
        public float WowRate => _wowRate;
        public float FlutterRate => _flutterRate;
        public float FlutterShare => _flutterShare;
        public float CrossfadeDuration => _crossfadeDuration;
        public float CrossfadeLead => _crossfadeLead;
        public float OutgoingFadeEnd => _outgoingFadeEnd;
        public float IncomingStart => _incomingStart;
        public float TuneStaticBoost => _tuneStaticBoost;
        public float TuneSwishVolume => _tuneSwishVolume;
        public float WakeMusicDelay => _wakeMusicDelay;
        public float WakeMusicFade => _wakeMusicFade;
        public float PlayerWakeMusicDelay => _playerWakeMusicDelay;
        public float PlayerWakeMusicFade => _playerWakeMusicFade;
        public float WakePowerTime => _wakePowerTime;
        public float WakeStaticBoost => _wakeStaticBoost;
    }
}
