using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's wake through its own eye (docs/design/cutscenes.md §1) and the title's camera (docs/features/M3-09): how
    /// long each beat holds, how the lid, the focus and the old electronics move, how heavy the neck is, how the camera
    /// leaves the lens and how the hurry compresses it all. The beats themselves are fixed by the storyboard; only the
    /// holds are tuned here. Untouched, the opening must stay at or under 45 s.
    /// </summary>
    [Serializable]
    public sealed class OpeningSettings
    {
        [Header("Holds (s), one per storyboard row")]
        [Tooltip("O1: pure black while 07's systems thump on and the static swells.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _crackleSeconds = 0.8f;

        [Tooltip("O2: the lid's first stage, a warm slit, and its stall.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _lidCrackSeconds = 0.9f;

        [Tooltip("O3: the lid's second stage, up to half open with a shudder.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _lidHalfSeconds = 0.9f;

        [Tooltip("O4: the lens hunts for focus, too near, too far, then settles.")]
        [Range(1f, 6f)]
        [SerializeField] private float _huntSeconds = 1.4f;

        [Tooltip("O10: the camera eases backward out of the lens to the front face-inspect reveal shot.")]
        [Range(1f, 8f)]
        [SerializeField] private float _pullOutSeconds = 1.8f;

        [Tooltip("O12: the ease round over 07's shoulder toward Earth, before the chase frame takes over.")]
        [Range(1f, 6f)]
        [SerializeField] private float _overShoulderSeconds = 1.4f;

        [Header("Lid (0 shut, 0.7 rest, 1 fully open)")]
        [Tooltip("Openness of the lid's first stage (O2).")]
        [Range(0.02f, 0.3f)]
        [SerializeField] private float _lidCrack = 0.2f;

        [Tooltip("Openness of the lid's second stage (O3).")]
        [Range(0.2f, 0.6f)]
        [SerializeField] private float _lidHalf = 0.5f;

        [Tooltip("07's half-lidded rest (O5 on).")]
        [Range(0.5f, 0.9f)]
        [SerializeField] private float _lidRest = 0.7f;

        [Tooltip("Seconds each lid stage takes to rise before it stalls.")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _lidRiseSeconds = 0.65f;

        [Tooltip("Seconds the lid takes to settle from its second stage to rest (from O4's end, through O5).")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _lidSettleSeconds = 0.9f;

        [Tooltip("Size of the second stage's shudder (lid openness).")]
        [Range(0f, 0.05f)]
        [SerializeField] private float _shudderAmount = 0.02f;

        [Tooltip("Soft wobbles in the shudder; slow enough to stay under 3 per second.")]
        [Range(1, 5)]
        [SerializeField] private int _shudderWobbles = 3;

        [Tooltip("The O11 blink's length (s): wonder down to its lowest, then back up to rest.")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float _blinkSeconds = 0.55f;

        [Tooltip("Lowest openness in the O11 blink.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _blinkLow = 0.25f;

        [Header("Focus (m)")]
        [Tooltip("The lens' blurred start (O2, O3): the world mush, the speck too.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _focusBlur = 2.5f;

        [Tooltip("The near end of the hunt: past the dock pad's edge to the glass, where the speck is sharp.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _focusNear = 0.1f;

        [Tooltip("How far the hunt overshoots before it swings back.")]
        [Range(50f, 2000f)]
        [SerializeField] private float _focusOvershoot = 400f;

        [Tooltip("Where the focus settles, 'infinity' for this lens.")]
        [Range(20f, 500f)]
        [SerializeField] private float _focusSettle = 80f;

        [Header("Boot readout (O4)")]
        [Tooltip("Seconds the boot readout shows in the lens, from the start of the flicker; it is never shown when "
            + "the player hurried the wake.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _readoutSeconds = 1.8f;

        [Tooltip("Seconds the readout takes to fade in, and again to fade out.")]
        [Range(0.1f, 1.5f)]
        [SerializeField] private float _readoutFade = 0.4f;

        [Header("Dust speck (viewport units from the frame's centre)")]
        [Tooltip("Where the speck sits on the glass when the lens first hunts: left of centre, low enough to show "
            + "under the half-open lid.")]
        [SerializeField] private Vector2 _speckStart = new Vector2(-0.2f, 0f);

        [Tooltip("Where it has slid to by the pull-out: down and out of the frame.")]
        [SerializeField] private Vector2 _speckEnd = new Vector2(-0.34f, -0.7f);

        [Header("Gaze (deg, relative to 07's heading)")]
        [Tooltip("Gaze pitch while 07's head is still bowed (O1-O5): a little below level, so the sky and the lander's " +
                 "beacon show through the lid's slit instead of only the ground and the cable.")]
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

        [Tooltip("O9: how far (deg) the gaze rests below Earth, so Earth sits just above the frame's centre.")]
        [Range(0f, 12f)]
        [SerializeField] private float _earthBelow = 4f;

        [Header("Lens camera")]
        [Tooltip("The POV camera sits this far (m) in front of the scan lens origin, clear of the cheek glass and visor.")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _lensForward = 0.44f;

        [Tooltip("Near clip (m) of the lens camera.")]
        [Range(0.01f, 0.3f)]
        [SerializeField] private float _lensNearClip = 0.22f;

        [Tooltip("Vertical field of view (deg) through 07's eye: a little wider than the chase camera's.")]
        [Range(30f, 90f)]
        [SerializeField] private float _lensFov = 58f;

        [Tooltip("How far (m) the camera travels forward out of the lens before it swings round to face 07.")]
        [Range(0f, 2f)]
        [SerializeField] private float _pullOutReach = 0.7f;

        [Tooltip("Share (0..1) along the line from the lens to the front reveal shot where the pull-out's reach "
            + "control point is anchored.")]
        [Range(0f, 1f)]
        [SerializeField] private float _pullOutReachAnchor = 0.5f;

        [Tooltip("Share of the pull-out over which the view settles from the gaze onto Earth itself.")]
        [Range(0.02f, 0.4f)]
        [SerializeField] private float _pullOutOntoEarth = 0.15f;

        [Tooltip("Share of Pull Out Onto Earth at which the pull-out camera begins turning smoothly toward 07.")]
        [Range(0f, 1f)]
        [SerializeField] private float _pullOutTurnStartShare = 0.5f;

        [Tooltip("Share of the pull-out after which the lens view lets go (the glass has passed).")]
        [Range(0.05f, 0.6f)]
        [SerializeField] private float _lensReleaseAt = 0.2f;

        [Header("07's body at the antenna check")]
        [Tooltip("Strength of the solar wing's sigh as 07 blinks.")]
        [Range(0f, 1f)]
        [SerializeField] private float _blinkSigh = 0.5f;

        [Header("The power-on self-test after the lens stages")]
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
        [Tooltip("Seconds New Game takes to fade the title's picture to black before O1 begins; it runs with the " +
                 "title's own switch-off.")]
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

        public float CrackleSeconds => _crackleSeconds;
        public float LidCrackSeconds => _lidCrackSeconds;
        public float LidHalfSeconds => _lidHalfSeconds;
        public float HuntSeconds => _huntSeconds;
        public float PullOutSeconds => _pullOutSeconds;
        public float OverShoulderSeconds => _overShoulderSeconds;
        public float LidCrack => _lidCrack;
        public float LidHalf => Mathf.Max(_lidHalf, _lidCrack);
        public float LidRest => _lidRest;
        public float LidRiseSeconds => _lidRiseSeconds;
        public float LidSettleSeconds => _lidSettleSeconds;
        public float ShudderAmount => _shudderAmount;
        public int ShudderWobbles => _shudderWobbles;
        public float BlinkSeconds => _blinkSeconds;
        public float BlinkLow => _blinkLow;
        public float FocusBlur => _focusBlur;
        public float FocusNear => _focusNear;
        public float FocusOvershoot => _focusOvershoot;
        public float FocusSettle => _focusSettle;
        public float ReadoutSeconds => _readoutSeconds;
        public float ReadoutFade => Mathf.Min(_readoutFade, 0.5f * _readoutSeconds);
        public Vector2 SpeckStart => _speckStart;
        public Vector2 SpeckEnd => _speckEnd;
        public float BowedPitch => _bowedPitch;
        public float YawLimit => _yawLimit;
        public float PitchDownLimit => _pitchDownLimit;
        public float PitchUpLimit => _pitchUpLimit;
        public float NeckFrequency => _neckFrequency;
        public float EarthBelow => _earthBelow;
        public float LensForward => _lensForward;
        public float LensNearClip => _lensNearClip;
        public float LensFov => _lensFov;
        public float PullOutReach => _pullOutReach;
        public float PullOutReachAnchor => _pullOutReachAnchor;
        public float PullOutOntoEarth => _pullOutOntoEarth;
        public float PullOutTurnStartShare => _pullOutTurnStartShare;
        public float LensReleaseAt => _lensReleaseAt;
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
