using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How the base's machines sound as they work for 07 (M3-14): the hopper feed, the Rover Bay's arms and turntable,
    /// the radio tower's service port and the charging dock. Created by the Audio/Tuning builder; runtime code only
    /// reads it.
    /// </summary>
    public sealed class StationAudioTuning : ScriptableObject
    {
        // Keeps ranges non-degenerate if an asset is edited into min >= max.
        private const float MinSpan = 0.01f;

        [Header("Hopper feed")]
        [Tooltip("Seconds for the feeding beam's hum to come in.")]
        [Range(0.01f, 2f)] [SerializeField] private float _feedFadeIn = 0.2f;

        [Tooltip("Seconds for the hum to go as the last bundle drops in.")]
        [Range(0.01f, 2f)] [SerializeField] private float _feedFadeOut = 0.4f;

        [Tooltip("Volume scale of the feeding beam's hum.")]
        [Range(0f, 1f)] [SerializeField] private float _feedVolume = 0.7f;

        [Header("The bay's arms")]
        [Tooltip("Tip speed (m/s) below which an arm's servo is silent.")]
        [Range(0f, 2f)] [SerializeField] private float _armDeadSpeed = 0.05f;

        [Tooltip("Tip speed (m/s) at which an arm's servo whirs fully.")]
        [Range(0.05f, 5f)] [SerializeField] private float _armFullSpeed = 0.8f;

        [Tooltip("A tip moving faster than this (m/s) was placed, not moved: no whirr.")]
        [Range(1f, 100f)] [SerializeField] private float _armPlacedSpeed = 10f;

        [Tooltip("Seconds (time constant) for an arm's servo to spin up.")]
        [Range(0.01f, 1f)] [SerializeField] private float _armAttack = 0.08f;

        [Tooltip("Seconds (time constant) for an arm's servo to spin down.")]
        [Range(0.01f, 2f)] [SerializeField] private float _armRelease = 0.2f;

        [Tooltip("Volume scale of an arm's servo at full speed.")]
        [Range(0f, 1f)] [SerializeField] private float _armVolume = 0.7f;

        [Tooltip("Servo pitch as an arm starts to move.")]
        [Range(0.25f, 2f)] [SerializeField] private float _armMinPitch = 0.85f;

        [Tooltip("Servo pitch at full speed.")]
        [Range(0.25f, 2f)] [SerializeField] private float _armMaxPitch = 1.15f;

        [Tooltip("Seconds an arm must have moved before coming to rest gives a hydraulic sigh (not after a twitch).")]
        [Range(0f, 5f)] [SerializeField] private float _sighAfter = 0.4f;

        [Tooltip("Servo level (0..1) above which an arm counts as moving, for the sigh.")]
        [Range(0.05f, 1f)] [SerializeField] private float _sighMoving = 0.3f;

        [Tooltip("Servo level (0..1) below which a moving arm has come to rest and sighs.")]
        [Range(0f, 0.5f)] [SerializeField] private float _sighResting = 0.05f;

        [Header("The turntable")]
        [Tooltip("Turning speed (degrees per second) below which the turntable is silent.")]
        [Range(0f, 30f)] [SerializeField] private float _turnDeadRate = 2f;

        [Tooltip("Turning speed (degrees per second) at which the turntable rumbles fully.")]
        [Range(1f, 180f)] [SerializeField] private float _turnFullRate = 30f;

        [Tooltip("Volume scale of the turntable's rumble.")]
        [Range(0f, 1f)] [SerializeField] private float _turnVolume = 0.8f;

        [Tooltip("Rumble pitch at the slowest turn.")]
        [Range(0.25f, 2f)] [SerializeField] private float _turnMinPitch = 0.9f;

        [Tooltip("Rumble pitch at full speed.")]
        [Range(0.25f, 2f)] [SerializeField] private float _turnMaxPitch = 1.1f;

        [Header("The tower's stitch")]
        [Tooltip("Seconds for the stitching to come in as the beam starts.")]
        [Range(0.01f, 2f)] [SerializeField] private float _stitchFadeIn = 0.3f;

        [Tooltip("Seconds for the stitching to fade as the hatch closes.")]
        [Range(0.01f, 2f)] [SerializeField] private float _stitchFadeOut = 0.5f;

        [Tooltip("Semitones the stitching rises as the new section goes up (5 = a fourth: in key).")]
        [Range(0f, 12f)] [SerializeField] private float _stitchRise = 5f;

        [Tooltip("Seconds for the stitching to reach the top of its rise (it holds there).")]
        [Range(0.1f, 10f)] [SerializeField] private float _stitchRiseTime = 3f;

        [Tooltip("Volume scale of the tower's stitching.")]
        [Range(0f, 1f)] [SerializeField] private float _stitchVolume = 0.8f;

        [Header("The charging dock")]
        [Tooltip("Seconds for the charging hum to come in once docked.")]
        [Range(0.1f, 5f)] [SerializeField] private float _chargeFadeIn = 1.5f;

        [Tooltip("Seconds docked before 07 is full: the hum settles away and the 'full' tone plays.")]
        [Range(1f, 120f)] [SerializeField] private float _chargeSeconds = 20f;

        [Tooltip("Seconds for the hum to fade once full or undocked.")]
        [Range(0.1f, 5f)] [SerializeField] private float _chargeFadeOut = 2f;

        [Tooltip("Volume scale of the charging hum (very soft).")]
        [Range(0f, 1f)] [SerializeField] private float _chargeVolume = 0.5f;

        public float FeedFadeIn => _feedFadeIn;
        public float FeedFadeOut => _feedFadeOut;
        public float FeedVolume => _feedVolume;
        public float ArmDeadSpeed => _armDeadSpeed;
        public float ArmFullSpeed => Mathf.Max(_armFullSpeed, _armDeadSpeed + MinSpan);
        public float ArmPlacedSpeed => _armPlacedSpeed;
        public float ArmAttack => _armAttack;
        public float ArmRelease => _armRelease;
        public float ArmVolume => _armVolume;
        public float ArmMinPitch => _armMinPitch;
        public float ArmMaxPitch => _armMaxPitch;
        public float SighAfter => _sighAfter;
        public float SighMoving => _sighMoving;
        public float SighResting => _sighResting;
        public float TurnDeadRate => _turnDeadRate;
        public float TurnFullRate => Mathf.Max(_turnFullRate, _turnDeadRate + MinSpan);
        public float TurnVolume => _turnVolume;
        public float TurnMinPitch => _turnMinPitch;
        public float TurnMaxPitch => _turnMaxPitch;
        public float StitchFadeIn => _stitchFadeIn;
        public float StitchFadeOut => _stitchFadeOut;
        public float StitchRise => _stitchRise;
        public float StitchRiseTime => _stitchRiseTime;
        public float StitchVolume => _stitchVolume;
        public float ChargeFadeIn => _chargeFadeIn;
        public float ChargeSeconds => _chargeSeconds;
        public float ChargeFadeOut => _chargeFadeOut;
        public float ChargeVolume => _chargeVolume;
    }
}
