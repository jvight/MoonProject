using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The sound of solitude (feel pillar 6, M3-10): how the radio fades out past its signal, how the room tone and
    /// 07's own small sounds come forward as everything else recedes, what counts as being alone, how stillness pulls
    /// the world back, and 07's lamp, servos and cooling ticks. Levels are in dB relative to each layer's own level at
    /// home. Created by the Audio/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class SoundscapeTuning : ScriptableObject
    {
        // Keeps ranges non-degenerate if an asset is edited into min >= max.
        private const float MinSpan = 0.01f;

        [Header("Distance: past the radio's signal")]
        [Tooltip("Metres past the edge of the radio's signal (clear radius + falloff, so tower upgrades move it out) " +
                 "over which the radio, static and all, fades to near-silence.")]
        [Range(10f, 1000f)] [SerializeField] private float _silenceWidth = 150f;

        [Tooltip("Radio level (music and static) once fully past the signal (dB): near-silence.")]
        [Range(-60f, 0f)] [SerializeField] private float _farRadioDb = -30f;

        [Tooltip("The basin's ambience bed far from home (dB): it recedes as the room tone takes over.")]
        [Range(-40f, 0f)] [SerializeField] private float _farBasinDb = -10f;

        [Header("Room tone: the space around 07")]
        [Tooltip("Room tone at home, under the radio and the basin bed (dB): barely there.")]
        [Range(-60f, 0f)] [SerializeField] private float _roomToneNearDb = -20f;

        [Tooltip("Room tone when 07 is fully alone (dB).")]
        [Range(-30f, 0f)] [SerializeField] private float _roomToneFarDb = -2f;

        [Tooltip("Room tone deep in Whispering Canyon (dB, added): the canyon's whisper is its own air there.")]
        [Range(-30f, 0f)] [SerializeField] private float _roomToneInCanyonDb = -6f;

        [Header("07's small sounds (lamp, servos, cooling ticks)")]
        [Tooltip("Their level at home (dB): masked by the radio anyway, they come forward to 0 dB when alone.")]
        [Range(-40f, 0f)] [SerializeField] private float _smallSoundsNearDb = -10f;

        [Header("What counts as alone")]
        [Tooltip("How much Quiet Hours counts as being alone (0..1): with the radio off, the room tone and 07's own " +
                 "sounds come forward even at home.")]
        [Range(0f, 1f)] [SerializeField] private float _quietHoursSolitude = 0.5f;

        [Tooltip("How much being inside Whispering Canyon counts as being alone (0..1).")]
        [Range(0f, 1f)] [SerializeField] private float _canyonSolitude = 0.8f;

        [Header("Quiet Hours")]
        [Tooltip("The basin bed swells this much (dB) when the radio leaves the moon to itself.")]
        [Range(0f, 6f)] [SerializeField] private float _quietHoursBasinDb = 2.6f;

        [Tooltip("Seconds (time constant) for the mix to settle into or out of Quiet Hours.")]
        [Range(0.1f, 10f)] [SerializeField] private float _quietHoursEase = 3f;

        [Header("Stillness (07 parked, no input)")]
        [Tooltip("Below this speed (m/s), grounded and without drive input, 07 counts as still.")]
        [Range(0.01f, 2f)] [SerializeField] private float _stillSpeed = 0.2f;

        [Tooltip("Drive input (steer or throttle, 0..1) at or below which the player counts as not driving.")]
        [Range(0f, 0.5f)] [SerializeField] private float _stillInput = 0.05f;

        [Tooltip("Seconds of stillness before the world starts to pull back (a short stop is not stillness).")]
        [Range(0f, 10f)] [SerializeField] private float _stillDelay = 1.5f;

        [Tooltip("Seconds (time constant) for the pull-back: ~95 % three time constants after the delay (~6 s).")]
        [Range(0.1f, 10f)] [SerializeField] private float _stillRiseTime = 1.5f;

        [Tooltip("Seconds (time constant) for the world to come back once 07 moves again: quick, never a snap.")]
        [Range(0.01f, 3f)] [SerializeField] private float _stillReleaseTime = 0.3f;

        [Tooltip("How far (dB) the world - radio, static, basin and canyon beds - pulls back when 07 is still. The " +
                 "room tone and 07's own sounds stay, so the space feels wider.")]
        [Range(-12f, 0f)] [SerializeField] private float _stillDuckDb = -4f;

        [Header("07's lamp")]
        [Tooltip("Lamp hum volume scale (very soft).")]
        [Range(0f, 1f)] [SerializeField] private float _lampVolume = 0.35f;

        [Tooltip("Seconds for the lamp hum to warm up after 07 wakes.")]
        [Range(0.01f, 10f)] [SerializeField] private float _lampFadeIn = 2f;

        [Header("Servos (steering, neck)")]
        [Tooltip("Steering speed (steer input per second) below which the steering servo is silent.")]
        [Range(0f, 5f)] [SerializeField] private float _steerDeadRate = 0.4f;

        [Tooltip("Steering speed (steer input per second) at which the steering servo whirs fully.")]
        [Range(0.1f, 20f)] [SerializeField] private float _steerFullRate = 3f;

        [Tooltip("Neck turning speed (degrees per second) below which the neck servo is silent (idle sway).")]
        [Range(0f, 200f)] [SerializeField] private float _neckDeadRate = 25f;

        [Tooltip("Neck turning speed (degrees per second) at which the neck servo whirs fully.")]
        [Range(1f, 720f)] [SerializeField] private float _neckFullRate = 160f;

        [Tooltip("Servo whirr volume scale at full speed.")]
        [Range(0f, 1f)] [SerializeField] private float _servoVolume = 0.5f;

        [Tooltip("Servo pitch when it just starts moving.")]
        [Range(0.25f, 2f)] [SerializeField] private float _servoMinPitch = 0.85f;

        [Tooltip("Servo pitch at full speed.")]
        [Range(0.25f, 2f)] [SerializeField] private float _servoMaxPitch = 1.25f;

        [Tooltip("Extra pitch of the neck servo (a smaller motor than the steering's).")]
        [Range(0.5f, 2f)] [SerializeField] private float _neckPitch = 1.35f;

        [Tooltip("Seconds (time constant) for a servo to spin up.")]
        [Range(0.01f, 1f)] [SerializeField] private float _servoAttack = 0.05f;

        [Tooltip("Seconds (time constant) for a servo to spin down.")]
        [Range(0.01f, 2f)] [SerializeField] private float _servoRelease = 0.15f;

        [Header("Cooling ticks after a drive")]
        [Tooltip("Seconds (time constant) for 07's metal to warm up at full effort.")]
        [Range(1f, 120f)] [SerializeField] private float _heatTime = 20f;

        [Tooltip("Seconds (time constant) for 07's metal to cool once stopped.")]
        [Range(1f, 120f)] [SerializeField] private float _coolTime = 30f;

        [Tooltip("Seconds after stopping before the first tick.")]
        [Range(0f, 20f)] [SerializeField] private float _tickDelay = 3f;

        [Tooltip("Seconds between ticks while still hot.")]
        [Range(0.1f, 10f)] [SerializeField] private float _tickMinGap = 0.8f;

        [Tooltip("Seconds between ticks when barely warm.")]
        [Range(0.2f, 20f)] [SerializeField] private float _tickMaxGap = 4f;

        [Tooltip("Random spread of each gap (0.5 = 50 % shorter to 50 % longer).")]
        [Range(0f, 0.9f)] [SerializeField] private float _tickGapJitter = 0.5f;

        [Tooltip("Heat (0..1) below which the metal has settled: no more ticks.")]
        [Range(0.01f, 0.9f)] [SerializeField] private float _tickMinHeat = 0.08f;

        [Tooltip("Tick volume scale when hot (cooler ticks are softer).")]
        [Range(0f, 1f)] [SerializeField] private float _tickVolume = 0.6f;

        public float SilenceWidth => _silenceWidth;
        public float FarRadioDb => _farRadioDb;
        public float FarBasinDb => _farBasinDb;
        public float RoomToneNearDb => _roomToneNearDb;
        public float RoomToneFarDb => _roomToneFarDb;
        public float RoomToneInCanyonDb => _roomToneInCanyonDb;
        public float SmallSoundsNearDb => _smallSoundsNearDb;
        public float QuietHoursSolitude => _quietHoursSolitude;
        public float CanyonSolitude => _canyonSolitude;
        public float QuietHoursBasinDb => _quietHoursBasinDb;
        public float QuietHoursEase => _quietHoursEase;
        public float StillSpeed => _stillSpeed;
        public float StillInput => _stillInput;
        public float StillDelay => _stillDelay;
        public float StillRiseTime => _stillRiseTime;
        public float StillReleaseTime => _stillReleaseTime;
        public float StillDuckDb => _stillDuckDb;
        public float LampVolume => _lampVolume;
        public float LampFadeIn => _lampFadeIn;
        public float SteerDeadRate => _steerDeadRate;
        public float SteerFullRate => Mathf.Max(_steerFullRate, _steerDeadRate + MinSpan);
        public float NeckDeadRate => _neckDeadRate;
        public float NeckFullRate => Mathf.Max(_neckFullRate, _neckDeadRate + MinSpan);
        public float ServoVolume => _servoVolume;
        public float ServoMinPitch => _servoMinPitch;
        public float ServoMaxPitch => _servoMaxPitch;
        public float NeckPitch => _neckPitch;
        public float ServoAttack => _servoAttack;
        public float ServoRelease => _servoRelease;
        public float HeatTime => _heatTime;
        public float CoolTime => _coolTime;
        public float TickDelay => _tickDelay;
        public float TickMinGap => _tickMinGap;
        public float TickMaxGap => Mathf.Max(_tickMaxGap, _tickMinGap + MinSpan);
        public float TickGapJitter => _tickGapJitter;
        public float TickMinHeat => _tickMinHeat;
        public float TickVolume => _tickVolume;
    }
}
