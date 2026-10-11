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
        [SerializeField] private float _crackleSeconds = 3f;

        [Tooltip("O2: the lid's first stage, a warm slit, and its stall.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _lidCrackSeconds = 2.5f;

        [Tooltip("O3: the lid's second stage, up to half open with a shudder.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _lidHalfSeconds = 2.5f;

        [Tooltip("O4: the lens hunts for focus, too near, too far, then settles.")]
        [Range(1f, 6f)]
        [SerializeField] private float _huntSeconds = 3.5f;

        [Tooltip("O5: the old electronics' two dips, roll band and warm lift.")]
        [Range(0.8f, 3f)]
        [SerializeField] private float _flickerSeconds = 1f;

        [Tooltip("O6: the look at the sleeping home.")]
        [Range(1f, 8f)]
        [SerializeField] private float _gazeHomeSeconds = 4f;

        [Tooltip("O7: the look at the tower's pilot; long enough to hold through one of its blinks.")]
        [Range(1f, 8f)]
        [SerializeField] private float _gazePilotSeconds = 3.5f;

        [Tooltip("O8: the look across the basin at The Peak's beacon.")]
        [Range(1f, 8f)]
        [SerializeField] private float _gazePeakSeconds = 4f;

        [Tooltip("O9: the look up at Earth, the longest hold.")]
        [Range(1f, 10f)]
        [SerializeField] private float _gazeEarthSeconds = 5f;

        [Tooltip("O10: the camera eases backward out of the lens to the front three-quarter shot.")]
        [Range(1f, 8f)]
        [SerializeField] private float _pullOutSeconds = 4f;

        [Tooltip("O11: the lens shot holds for the blink, the head tilt, the wing's sigh and the call sign.")]
        [Range(1f, 6f)]
        [SerializeField] private float _blinkShotSeconds = 3.5f;

        [Tooltip("O12: the ease round over 07's shoulder toward Earth, before the chase frame takes over.")]
        [Range(1f, 6f)]
        [SerializeField] private float _overShoulderSeconds = 3f;

        [Header("Lid (0 shut, 0.7 rest, 1 fully open)")]
        [Tooltip("Openness of the lid's first stage (O2).")]
        [Range(0.02f, 0.3f)]
        [SerializeField] private float _lidCrack = 0.12f;

        [Tooltip("Openness of the lid's second stage (O3).")]
        [Range(0.2f, 0.6f)]
        [SerializeField] private float _lidHalf = 0.4f;

        [Tooltip("07's half-lidded rest (O5 on).")]
        [Range(0.5f, 0.9f)]
        [SerializeField] private float _lidRest = 0.7f;

        [Tooltip("Wonder at Earth (O9, O10).")]
        [Range(0.7f, 1f)]
        [SerializeField] private float _lidWonder = 0.85f;

        [Tooltip("Seconds each lid stage takes to rise before it stalls.")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _lidRiseSeconds = 1.2f;

        [Tooltip("Seconds the lid takes to settle from its second stage to rest (from O4's end, through O5).")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _lidSettleSeconds = 1.6f;

        [Tooltip("Size of the second stage's shudder (lid openness).")]
        [Range(0f, 0.05f)]
        [SerializeField] private float _shudderAmount = 0.02f;

        [Tooltip("Soft wobbles in the shudder; slow enough to stay under 3 per second.")]
        [Range(1, 5)]
        [SerializeField] private int _shudderWobbles = 3;

        [Tooltip("The O11 blink: seconds into the lens shot it starts.")]
        [Range(0f, 2f)]
        [SerializeField] private float _blinkDelay = 0.4f;

        [Tooltip("The O11 blink's length (s): wonder down to its lowest, then back up to rest.")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float _blinkSeconds = 0.7f;

        [Tooltip("Lowest openness in the O11 blink.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _blinkLow = 0.25f;

        [Tooltip("Openness the O11 blink comes back up to.")]
        [Range(0.5f, 0.9f)]
        [SerializeField] private float _blinkAfter = 0.75f;

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

        [Tooltip("The O8 re-hunt dips this near before it finds the far distance again.")]
        [Range(5f, 100f)]
        [SerializeField] private float _rehuntNear = 25f;

        [Tooltip("Seconds of the O8 re-hunt (from the start of the look at The Peak).")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _rehuntSeconds = 1.4f;

        [Header("Old electronics (O5)")]
        [Tooltip("Length (s) of each soft exposure dip. Photosensitivity: long enough that nothing strobes at 3 Hz.")]
        [Range(0.34f, 0.8f)]
        [SerializeField] private float _dipSeconds = 0.4f;

        [Tooltip("Gap (s) between the two dips.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _dipGap = 0.12f;

        [Tooltip("Depth of the second dip relative to the first.")]
        [Range(0f, 1f)]
        [SerializeField] private float _secondDip = 0.8f;

        [Header("Boot readout (O5, GDD v3 §8)")]
        [Tooltip("Seconds the boot readout shows in the lens, from the start of the flicker; it is never shown when "
            + "the player hurried the wake.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _readoutSeconds = 2f;

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
        [Tooltip("Gaze pitch while 07's head is still bowed (O1-O5): down at its dock.")]
        [Range(-30f, 0f)]
        [SerializeField] private float _bowedPitch = -15f;

        [Tooltip("Soft yaw limit either way.")]
        [Range(20f, 120f)]
        [SerializeField] private float _yawLimit = 70f;

        [Tooltip("Lowest pitch of the soft limit.")]
        [Range(-45f, 0f)]
        [SerializeField] private float _pitchDownLimit = -15f;

        [Tooltip("Highest pitch of the soft limit.")]
        [Range(10f, 80f)]
        [SerializeField] private float _pitchUpLimit = 50f;

        [Tooltip("Degrees short of a limit where the player's look starts to meet resistance.")]
        [Range(1f, 40f)]
        [SerializeField] private float _softZone = 18f;

        [Tooltip("Neck response (Hz): low and critically damped, an old neck.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _neckFrequency = 0.45f;

        [Tooltip("Share of the player's look input the heavy neck follows.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _lookShare = 0.6f;

        [Tooltip("Seconds after the player's last look before the scripted attention resumes.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _attentionResume = 2f;

        [Tooltip("Within this many degrees of Earth the player has found it on their own.")]
        [Range(2f, 25f)]
        [SerializeField] private float _earthFoundAngle = 10f;

        [Tooltip("Seconds the player must hold Earth in view to skip ahead to O9.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _earthFoundSeconds = 1f;

        [Tooltip("Within this many degrees of a target the player has seen it, so the attention skips it.")]
        [Range(2f, 30f)]
        [SerializeField] private float _seenAngle = 12f;

        [Tooltip("O6: seconds into the look at home before the gaze drifts back, unsure.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _hesitateAfter = 1.8f;

        [Tooltip("O6: how far (deg) the unsure gaze drifts back toward centre.")]
        [Range(0f, 10f)]
        [SerializeField] private float _hesitateYaw = 3f;

        [Tooltip("O8: the tiny lift of the head (deg) as the gaze reaches The Peak.")]
        [Range(0f, 5f)]
        [SerializeField] private float _peakLift = 1f;

        [Tooltip("O9: how far (deg) the gaze rests below Earth, so Earth sits just above the frame's centre.")]
        [Range(0f, 12f)]
        [SerializeField] private float _earthBelow = 4f;

        [Tooltip("Height (m) above the lander's base the gaze looks at: its dark windows.")]
        [Range(0f, 10f)]
        [SerializeField] private float _landerLookHeight = 3f;

        [Tooltip("Height (m) of The Peak's red beacon above its summit (IWorldLayout.PeakPosition).")]
        [Range(0f, 30f)]
        [SerializeField] private float _beaconHeight = 6f;

        [Header("Lens camera")]
        [Tooltip("The POV camera sits this far (m) in front of the lens centre, clear of the glass.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _lensForward = 0.15f;

        [Tooltip("Near clip (m) of the lens camera.")]
        [Range(0.01f, 0.3f)]
        [SerializeField] private float _lensNearClip = 0.02f;

        [Tooltip("Vertical field of view (deg) through 07's eye: a little wider than the chase camera's.")]
        [Range(30f, 90f)]
        [SerializeField] private float _lensFov = 58f;

        [Tooltip("How far (m) the camera travels forward out of the lens before it swings round to face 07.")]
        [Range(0f, 2f)]
        [SerializeField] private float _pullOutReach = 0.7f;

        [Tooltip("Share of the pull-out over which the view settles from the gaze onto Earth itself.")]
        [Range(0.02f, 0.4f)]
        [SerializeField] private float _pullOutOntoEarth = 0.15f;

        [Tooltip("Share of the pull-out Earth stays the subject before the view turns round to 07: the turn then "
            + "sweeps the sky between Earth and the lens, never the empty ground to the side.")]
        [Range(0.1f, 0.8f)]
        [SerializeField] private float _pullOutEarthHeld = 0.4f;

        [Tooltip("Share of the pull-out after which the lens view lets go (the glass has passed).")]
        [Range(0.05f, 0.6f)]
        [SerializeField] private float _lensReleaseAt = 0.2f;

        [Header("Hurry (any drive input)")]
        [Tooltip("Seconds the remaining lid and focus stages compress into; control is held this long at most.")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float _hurryLidSeconds = 1.2f;

        [Tooltip("Seconds the hurried camera takes from the lens straight to the chase frame.")]
        [Range(1f, 2.8f)]
        [SerializeField] private float _hurryPullOutSeconds = 2.5f;

        [Header("07's body in the blink (O11)")]
        [Tooltip("Head tilt (deg) to one side as 07 blinks: a question.")]
        [Range(0f, 20f)]
        [SerializeField] private float _blinkTilt = 8f;

        [Tooltip("Seconds the tilt eases in, and again out.")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _tiltEaseSeconds = 0.6f;

        [Tooltip("Seconds the tilt holds between easing in and out.")]
        [Range(0f, 4f)]
        [SerializeField] private float _tiltHoldSeconds = 1.6f;

        [Tooltip("Strength of the solar wing's sigh as 07 blinks.")]
        [Range(0f, 1f)]
        [SerializeField] private float _blinkSigh = 0.5f;

        [Header("Title and the push into the lens")]
        [Tooltip("Seconds New Game takes to push from the title's lens shot into the closed lens (the reverse of O10).")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _pushInSeconds = 2f;

        [Tooltip("Share of the push after which the lens view darkens to black.")]
        [Range(0.2f, 0.95f)]
        [SerializeField] private float _pushDarkFrom = 0.6f;

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
        public float FlickerSeconds => Mathf.Max(_flickerSeconds, 2f * _dipSeconds + _dipGap);
        public float GazeHomeSeconds => _gazeHomeSeconds;
        public float GazePilotSeconds => _gazePilotSeconds;
        public float GazePeakSeconds => Mathf.Max(_gazePeakSeconds, _rehuntSeconds);
        public float GazeEarthSeconds => _gazeEarthSeconds;
        public float PullOutSeconds => _pullOutSeconds;
        public float BlinkShotSeconds => Mathf.Max(_blinkShotSeconds, _blinkDelay + _blinkSeconds);
        public float OverShoulderSeconds => _overShoulderSeconds;
        public float LidCrack => _lidCrack;
        public float LidHalf => Mathf.Max(_lidHalf, _lidCrack);
        public float LidRest => _lidRest;
        public float LidWonder => Mathf.Max(_lidWonder, _lidRest);
        public float LidRiseSeconds => _lidRiseSeconds;
        public float LidSettleSeconds => _lidSettleSeconds;
        public float ShudderAmount => _shudderAmount;
        public int ShudderWobbles => _shudderWobbles;
        public float BlinkDelay => _blinkDelay;
        public float BlinkSeconds => _blinkSeconds;
        public float BlinkLow => _blinkLow;
        public float BlinkAfter => _blinkAfter;
        public float FocusBlur => _focusBlur;
        public float FocusNear => _focusNear;
        public float FocusOvershoot => _focusOvershoot;
        public float FocusSettle => _focusSettle;
        public float RehuntNear => _rehuntNear;
        public float RehuntSeconds => _rehuntSeconds;
        public float DipSeconds => _dipSeconds;
        public float DipGap => _dipGap;
        public float SecondDip => _secondDip;
        public float ReadoutSeconds => _readoutSeconds;
        public float ReadoutFade => Mathf.Min(_readoutFade, 0.5f * _readoutSeconds);
        public Vector2 SpeckStart => _speckStart;
        public Vector2 SpeckEnd => _speckEnd;
        public float BowedPitch => _bowedPitch;
        public float YawLimit => _yawLimit;
        public float PitchDownLimit => _pitchDownLimit;
        public float PitchUpLimit => _pitchUpLimit;
        public float SoftZone => _softZone;
        public float NeckFrequency => _neckFrequency;
        public float LookShare => _lookShare;
        public float AttentionResume => _attentionResume;
        public float EarthFoundAngle => _earthFoundAngle;
        public float EarthFoundSeconds => _earthFoundSeconds;
        public float SeenAngle => _seenAngle;
        public float HesitateAfter => _hesitateAfter;
        public float HesitateYaw => _hesitateYaw;
        public float PeakLift => _peakLift;
        public float EarthBelow => _earthBelow;
        public float LanderLookHeight => _landerLookHeight;
        public float BeaconHeight => _beaconHeight;
        public float LensForward => _lensForward;
        public float LensNearClip => _lensNearClip;
        public float LensFov => _lensFov;
        public float PullOutReach => _pullOutReach;
        public float PullOutOntoEarth => _pullOutOntoEarth;
        public float PullOutEarthHeld => Mathf.Max(_pullOutEarthHeld, _pullOutOntoEarth);
        public float LensReleaseAt => _lensReleaseAt;
        public float HurryLidSeconds => _hurryLidSeconds;
        public float HurryPullOutSeconds => _hurryPullOutSeconds;
        public float BlinkTilt => _blinkTilt;
        public float TiltEaseSeconds => _tiltEaseSeconds;
        public float TiltHoldSeconds => _tiltHoldSeconds;
        public float BlinkSigh => _blinkSigh;
        public float PushInSeconds => _pushInSeconds;
        public float PushDarkFrom => _pushDarkFrom;
        public float TitleDriftDelay => _titleDriftDelay;
        public float TitleDriftYaw => _titleDriftYaw;
        public float TitleDriftPeriod => _titleDriftPeriod;
        public float TitleDriftEase => _titleDriftEase;
        public float ShortLidSeconds => _shortLidSeconds;
        public float ShortGlanceSeconds => _shortGlanceSeconds;
        public int GazePriority => _gazePriority;
    }
}
