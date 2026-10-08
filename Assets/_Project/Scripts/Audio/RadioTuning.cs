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

        [Header("Bell's dial (stations)")]
        [Tooltip("Seconds of the static-swish crossfade when the dial turns to another station.")]
        [Range(0.3f, 4f)] [SerializeField] private float _stationSwitchDuration = 1.2f;

        [Tooltip("Seconds of the quiet, swish-free crossfade when the station changes without a dial turn (the dial " +
                 "locked again).")]
        [Range(0.05f, 3f)] [SerializeField] private float _silentSwitchFade = 0.6f;

        [Tooltip("Static level on Quiet Hours (scales the clarity static; 0 = off, only the moon's ambience).")]
        [Range(0f, 1f)] [SerializeField] private float _quietStaticGain;

        [Tooltip("Seconds (time constant) for the static to settle into or out of Quiet Hours.")]
        [Range(0.05f, 5f)] [SerializeField] private float _quietStaticFade = 1.2f;

        [Header("Radio-hop (between lit relay nodes; times match Gameplay's hop view)")]
        [Tooltip("Seconds for the radio to ease into static as the hop starts and the screen fades.")]
        [Range(0.1f, 5f)] [SerializeField] private float _hopOutTime = 0.8f;

        [Tooltip("Seconds of full dark (07 is moved) before the static starts to resolve.")]
        [Range(0f, 5f)] [SerializeField] private float _hopDarkTime = 0.4f;

        [Tooltip("Seconds for the static to resolve into the target node's radio as the view eases back in.")]
        [Range(0.1f, 5f)] [SerializeField] private float _hopInTime = 0.8f;

        [Tooltip("Extra static at the depth of the hop (0..1, on top of the clarity static).")]
        [Range(0f, 1f)] [SerializeField] private float _hopStaticBoost = 0.5f;

        [Tooltip("Music level at the depth of the hop (it filters away, not off).")]
        [Range(0f, 1f)] [SerializeField] private float _hopMusicGain = 0.15f;

        [Tooltip("Low-pass (Hz) the music closes to at the depth of the hop.")]
        [Range(100f, 22000f)] [SerializeField] private float _hopCutoff = 700f;

        [Header("Paused: listening in the cabin")]
        [Tooltip("Low-pass (Hz) the radio eases to while paused: a subtly closer, warmer set (22 kHz = unchanged).")]
        [Range(2000f, 22000f)] [SerializeField] private float _cabinCutoff = 7000f;

        [Tooltip("Music volume scale while paused (a touch closer; 1.12 is about +1 dB).")]
        [Range(1f, 1.5f)] [SerializeField] private float _cabinMusicGain = 1.12f;

        [Tooltip("Static volume scale while paused (the cabin shelters the set a little).")]
        [Range(0f, 1f)] [SerializeField] private float _cabinStaticGain = 0.7f;

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
        public float StationSwitchDuration => _stationSwitchDuration;
        public float SilentSwitchFade => _silentSwitchFade;
        public float QuietStaticGain => _quietStaticGain;
        public float QuietStaticFade => _quietStaticFade;
        public float HopOutTime => _hopOutTime;
        public float HopDarkTime => _hopDarkTime;
        public float HopInTime => _hopInTime;
        public float HopStaticBoost => _hopStaticBoost;
        public float HopMusicGain => _hopMusicGain;
        public float HopCutoff => _hopCutoff;
        public float CabinCutoff => _cabinCutoff;
        public float CabinMusicGain => _cabinMusicGain;
        public float CabinStaticGain => _cabinStaticGain;
        public float WakeMusicDelay => _wakeMusicDelay;
        public float WakeMusicFade => _wakeMusicFade;
        public float PlayerWakeMusicDelay => _playerWakeMusicDelay;
        public float PlayerWakeMusicFade => _playerWakeMusicFade;
        public float WakePowerTime => _wakePowerTime;
        public float WakeStaticBoost => _wakeStaticBoost;
    }
}
