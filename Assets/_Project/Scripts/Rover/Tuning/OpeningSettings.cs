using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's wake and the title's camera (docs/features/M3-09): how 07's lid rests and blinks, how heavy its neck is,
    /// how its lamps power up, the fade to black from the title and the short wake; the power-on self-test seen on
    /// its face is <see cref="Post"/>. Untouched, the opening must stay at or under 45 s.
    /// </summary>
    [Serializable]
    public sealed class OpeningSettings
    {
        [Header("Lid (0 shut, 0.7 rest, 1 fully open)")]
        [Tooltip("07's half-lidded rest once its eyes are open.")]
        [Range(0.5f, 0.9f)]
        [SerializeField] private float _lidRest = 0.7f;

        [Tooltip("The wake's blink length (s): down to its lowest, then back up to rest.")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float _blinkSeconds = 0.55f;

        [Tooltip("Lowest openness in the wake's blink.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _blinkLow = 0.25f;

        [Header("Gaze (deg, relative to 07's heading)")]
        [Tooltip("Gaze pitch while 07 still sleeps, before its eyes open: a little below level, the head bowed.")]
        [Range(-30f, 10f)]
        [SerializeField] private float _bowedPitch = -2f;

        [Tooltip("Soft yaw limit either way.")]
        [Range(20f, 120f)]
        [SerializeField] private float _yawLimit = 70f;

        [Tooltip("Lowest pitch of the soft limit.")]
        [Range(-45f, 0f)]
        [SerializeField] private float _pitchDownLimit = -15f;

        [Tooltip("Highest pitch of the soft limit.")]
        [Range(10f, 80f)]
        [SerializeField] private float _pitchUpLimit = 50f;

        [Tooltip("Neck response (Hz): low and critically damped, an old neck.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _neckFrequency = 0.6f;

        [Header("07's body at the antenna check")]
        [Tooltip("Strength of the solar wing's sigh as 07 blinks.")]
        [Range(0f, 1f)]
        [SerializeField] private float _blinkSigh = 0.5f;

        [Header("The power-on self-test, seen on 07's face")]
        [SerializeField] private PostSettings _post = new PostSettings();

        [Header("Lamps power up as 07 opens its eyes")]
        [Tooltip("How many uneven flickers the lamps make before the dark gap.")]
        [Range(2, 4)]
        [SerializeField] private int _lampFlickerCount = 3;

        [Tooltip("Seconds all the flickers take together.")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _lampFlickerSeconds = 0.9f;

        [Tooltip("Seconds of dark between the last flicker and the ramp.")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _lampDarkGapSeconds = 0.35f;

        [Tooltip("Seconds the lamps take to ramp smoothly to full.")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _lampRampSeconds = 1.1f;

        [Header("Title and the fade to black")]
        [Tooltip("Seconds New Game takes to fade the title's picture to black before the wake begins; it runs with " +
                 "the title's own switch-off.")]
        [Range(0.3f, 2f)]
        [SerializeField] private float _fadeOutSeconds = 0.6f;

        [Tooltip("Share of the fade-out after which the picture starts to darken to black.")]
        [Range(0.2f, 0.95f)]
        [SerializeField] private float _fadeOutFrom = 0.1f;

        [Tooltip("Seconds untouched on the title before the camera starts to drift.")]
        [Range(5f, 60f)]
        [SerializeField] private float _titleDriftDelay = 20f;

        [Tooltip("How far (deg) the title's drift swings round the lens shot, either way.")]
        [Range(0f, 10f)]
        [SerializeField] private float _titleDriftYaw = 8f;

        [Tooltip("Seconds of one full swing of the title's drift.")]
        [Range(10f, 120f)]
        [SerializeField] private float _titleDriftPeriod = 48f;

        [Tooltip("Seconds the drift takes to ease in, and to ease back when a choice is made (at most 1 s back).")]
        [Range(0.3f, 6f)]
        [SerializeField] private float _titleDriftEase = 3f;

        [Tooltip("Over a save, the title frames 07 from behind, looking toward the lander; standing nearer than this " +
                 "(m) to it, the frame keeps 07's own heading instead.")]
        [Min(0f)]
        [SerializeField] private float _titleFrameMinLanderDistance = 12f;

        [Header("Short wake (Continue)")]
        [Tooltip("Seconds 07's lid takes to open in one eased move.")]
        [Range(0.3f, 2f)]
        [SerializeField] private float _shortLidSeconds = 0.8f;

        [Tooltip("Seconds 07 glances at Earth after the lid opens; never holds control.")]
        [Range(0f, 4f)]
        [SerializeField] private float _shortGlanceSeconds = 2f;

        [Tooltip("Gaze priority of the opening's look requests (above gameplay's ordinary glances).")]
        [Range(0, 100)]
        [SerializeField] private int _gazePriority = 50;

        public float LidRest => _lidRest;
        public float BlinkSeconds => _blinkSeconds;
        public float BlinkLow => _blinkLow;
        public float BowedPitch => _bowedPitch;
        public float YawLimit => _yawLimit;
        public float PitchDownLimit => _pitchDownLimit;
        public float PitchUpLimit => _pitchUpLimit;
        public float NeckFrequency => _neckFrequency;
        public float BlinkSigh => _blinkSigh;
        public PostSettings Post => _post;
        public int LampFlickerCount => _lampFlickerCount;
        public float LampFlickerSeconds => _lampFlickerSeconds;
        public float LampDarkGapSeconds => _lampDarkGapSeconds;
        public float LampRampSeconds => _lampRampSeconds;
        public float FadeOutSeconds => _fadeOutSeconds;
        public float FadeOutFrom => _fadeOutFrom;

        public float TitleFrameMinLanderDistance => _titleFrameMinLanderDistance;
        public float TitleDriftDelay => _titleDriftDelay;
        public float TitleDriftYaw => _titleDriftYaw;
        public float TitleDriftPeriod => _titleDriftPeriod;
        public float TitleDriftEase => _titleDriftEase;
        public float ShortLidSeconds => _shortLidSeconds;
        public float ShortGlanceSeconds => _shortGlanceSeconds;
        public int GazePriority => _gazePriority;
    }
}
