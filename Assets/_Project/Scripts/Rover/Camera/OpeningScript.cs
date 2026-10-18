using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// The New Game wake as 07's power-on self-test, plain C# so every rule is testable. It walks the lens stages
    /// (black, the lid's slit, half open, the focus hunt), the camera's pull-out to the three-quarter front shot, the
    /// checks of <see cref="PostSequencer"/> and the release toward the chase frame, and works out each frame how open
    /// 07's lid is, where the lens focuses, where the dust speck sits, where 07 looks (<see cref="LensGaze"/>) and how
    /// asleep its body still is. The beats it reaches queue up for <see cref="TryTakeBeat"/>; the lines of the test
    /// (carrier, resume and agenda during the lens stages, then each check) queue up for
    /// <see cref="TryTakePostEvent"/>.
    /// <list type="bullet">
    /// <item>Not skippable: only time moves it, and 07 is held (<see cref="ControlHeld"/>) until the chase frame.</item>
    /// <item>Continue plays the short wake instead: the lid opens in one move with one blink, then a glance at
    /// Earth that never holds control.</item>
    /// </list>
    /// The camera moves themselves (the lens camera, the rig's wake shot and its turn) are the adapter's; this says
    /// when (<see cref="WantsRigEase"/>).
    /// </summary>
    public sealed class OpeningScript
    {
        private const int BeatCapacity = 8;
        private const int PostEventCapacity = 32;

        /// <summary>Share of the blink spent closing; the rest opens again, slower.</summary>
        private const float BlinkCloseShare = 0.4f;

        /// <summary>Share of the hunt where the focus reaches its nearest, then its overshoot.</summary>
        private const float HuntNearAt = 0.35f;
        private const float HuntFarAt = 0.7f;

        private readonly RoverCameraTuning _tuning;
        private readonly OpeningSettings _settings;
        private readonly LensGaze _gaze;
        private readonly PostSequencer _post;
        private readonly WakeBeat[] _beats = new WakeBeat[BeatCapacity];
        private readonly PostEvent[] _postEvents = new PostEvent[PostEventCapacity];
        private int _beatHead;
        private int _beatCount;
        private int _postHead;
        private int _postCount;
        private float _phaseTime;
        private float _phaseLength;
        private float _lidStart;
        private float _focusStart;
        private float _sleepStart;
        private float _speckClock;
        private float _speckSpan;
        private float _blinkClock;
        private float _readoutClock;
        private float _glanceClock;
        private Vector2 _earth;
        private bool _blinked;

        public OpeningScript(RoverCameraTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _settings = tuning.Opening;
            _gaze = new LensGaze(_settings);
            Lamps = new LampPowerUp(_settings);
            Motions = new PostMotions(_settings.Post);
            _post = new PostSequencer(_settings.Post, Lamps);
            Reset();
        }

        /// <summary>07's lamps: dark while it sleeps, then the lamps check's flicker and ramp.</summary>
        public LampPowerUp Lamps { get; }

        /// <summary>What 07 does in each check, as functions of <see cref="PostProgress01"/>.</summary>
        public PostMotions Motions { get; }

        public OpeningPhase Phase { get; private set; }

        /// <summary>True from a begin until the chase frame (the short wake's glance may run on after it).</summary>
        public bool IsRunning => Phase != OpeningPhase.Idle;

        /// <summary>Seconds since the opening (or the short wake) began, the fade to black included.</summary>
        public float Elapsed { get; private set; }

        /// <summary>Seconds into the current phase, and how long it lasts.</summary>
        public float PhaseTime => _phaseTime;
        public float PhaseLength => _phaseLength;

        public LensGaze Gaze => _gaze;

        /// <summary>The check under way while <see cref="OpeningPhase.Post"/> runs.</summary>
        public BootCheckStep PostStep => _post.Current;

        /// <summary>How far through the current check, 0..1.</summary>
        public float PostProgress01 => _post.StepProgress01;

        /// <summary>The lens view's openness: 07's lid inside the lens, the picture fading to black before O1.</summary>
        public float Lid01 { get; private set; }

        public float FocusDistance { get; private set; }

        /// <summary>The old electronics' exposure dip; the self-test has none.</summary>
        public float Flicker01 => 0f;

        /// <summary>The brief warm tint; the self-test has none.</summary>
        public float Warmth01 => 0f;

        public Vector2 SpeckOffset { get; private set; }

        /// <summary>07's boot readout in the lens (O4), 0..1.</summary>
        public float Readout01 { get; private set; }

        /// <summary>The state to hand <see cref="ILensView"/> while <see cref="LensActive"/>.</summary>
        public LensViewState LensState =>
            new LensViewState(Lid01, FocusDistance, Flicker01, Warmth01, SpeckOffset, Readout01);

        /// <summary>True while the picture is seen through 07's eye (the lens view applies).</summary>
        public bool LensActive { get; private set; }

        /// <summary>True while the camera sits at the lens socket (O1-O4).</summary>
        public bool InLens => Phase >= OpeningPhase.Crackle && Phase <= OpeningPhase.Hunt;

        /// <summary>The rig's wake shot should ease out to the chase frame now.</summary>
        public bool WantsRigEase { get; private set; }

        /// <summary>07's body: 1 asleep (head bowed, eye dark) .. 0 awake.</summary>
        public float Sleep { get; private set; }

        /// <summary>
        /// 07's own lid openness, and how much the wake holds it: shut while the picture fades; fully open while the
        /// camera is inside the lens, where the lens view draws the lid and the real brow-shutter would cover the camera.
        /// </summary>
        public float BodyLid => Phase == OpeningPhase.FadeOut ? 0f : InLens ? 1f : Lid01;
        public float BodyLidWeight { get; private set; }

        /// <summary>07's head should follow the gaze (inside the lens, and during the pull-out and the checks).</summary>
        public bool GazeHeld { get; private set; }

        /// <summary>The short wake's glance at Earth is on.</summary>
        public bool GlancingEarth => _glanceClock >= 0f && _glanceClock < _settings.ShortGlanceSeconds;

        /// <summary>
        /// Control is held from the fade to black until the camera begins to settle into the chase frame (and through
        /// the short wake): the self-test cannot be skipped.
        /// </summary>
        public bool ControlHeld => Phase != OpeningPhase.Idle && Phase != OpeningPhase.ChaseEase;

        /// <summary>Where Earth is as (yaw, pitch) deg from 07's heading at its lens: the head looks there from the pull-out on.</summary>
        public void SetEarthTarget(Vector2 earth)
        {
            _earth = earth;
        }

        /// <summary>A fresh game: the full self-test, from the fade to black when the title was showing.</summary>
        public void BeginFull(bool fromTitle)
        {
            Reset();
            _gaze.Reset(0f, _settings.BowedPitch);
            Enter(fromTitle ? OpeningPhase.FadeOut : OpeningPhase.Crackle);
            Evaluate();
        }

        /// <summary>A loaded game: the short wake.</summary>
        public void BeginShort()
        {
            Reset();
            Enter(OpeningPhase.ShortWake);
            Evaluate();
        }

        /// <summary>Stops everything at once (a scene going away).</summary>
        public void Stop()
        {
            Reset();
        }

        /// <summary>Takes the oldest beat reached and not yet taken.</summary>
        public bool TryTakeBeat(out WakeBeat beat)
        {
            if (_beatCount == 0)
            {
                beat = default;
                return false;
            }

            beat = _beats[_beatHead];
            _beatHead = (_beatHead + 1) % BeatCapacity;
            _beatCount--;
            return true;
        }

        /// <summary>Takes the oldest line of the self-test reached and not yet taken.</summary>
        public bool TryTakePostEvent(out PostEvent postEvent)
        {
            if (_postCount == 0)
            {
                postEvent = default;
                return false;
            }

            postEvent = _postEvents[_postHead];
            _postHead = (_postHead + 1) % PostEventCapacity;
            _postCount--;
            return true;
        }

        public void Step(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (_glanceClock >= 0f)
            {
                _glanceClock += deltaTime;
            }

            if (Phase == OpeningPhase.Idle)
            {
                return;
            }

            Elapsed += deltaTime;
            _phaseTime += deltaTime;
            AimScripted();
            _gaze.Step(deltaTime);
            StepClocks(deltaTime);
            if (Phase == OpeningPhase.Post)
            {
                StepPost(deltaTime);
            }

            while (Phase != OpeningPhase.Idle && Phase != OpeningPhase.Post && _phaseTime >= _phaseLength)
            {
                Advance();
            }

            Evaluate();
        }

        private void Reset()
        {
            Phase = OpeningPhase.Idle;
            Elapsed = 0f;
            _beatHead = 0;
            _beatCount = 0;
            _postHead = 0;
            _postCount = 0;
            _phaseTime = 0f;
            _phaseLength = 0f;
            _speckClock = -1f;
            _blinkClock = -1f;
            _readoutClock = -1f;
            _glanceClock = -1f;
            _blinked = false;
            WantsRigEase = false;
            _speckSpan = _settings.HuntSeconds + _settings.PullOutSeconds;
            Lid01 = 0f;
            FocusDistance = _settings.FocusBlur;
            SpeckOffset = _settings.SpeckStart;
            Sleep = 1f;
            BodyLidWeight = 0f;
            GazeHeld = false;
            LensActive = false;
        }

        private void Publish(WakeBeat beat)
        {
            if (_beatCount == BeatCapacity)
            {
                _beatHead = (_beatHead + 1) % BeatCapacity;
                _beatCount--;
            }

            _beats[(_beatHead + _beatCount) % BeatCapacity] = beat;
            _beatCount++;
        }

        private void PublishPost(PostEventKind kind, BootCheckStep step, BootCheckResult result)
        {
            if (_postCount == PostEventCapacity)
            {
                _postHead = (_postHead + 1) % PostEventCapacity;
                _postCount--;
            }

            _postEvents[(_postHead + _postCount) % PostEventCapacity] = new PostEvent(kind, step, result);
            _postCount++;
        }

        private void Enter(OpeningPhase phase)
        {
            Phase = phase;
            _phaseTime = 0f;
            _phaseLength = Length(phase);
            _lidStart = Lid01;
            _focusStart = FocusDistance;
            _sleepStart = Sleep;
            switch (phase)
            {
                case OpeningPhase.Crackle:
                    Publish(WakeBeat.Crackle);
                    PublishPost(PostEventKind.Started, BootCheckStep.Carrier, BootCheckResult.Info);
                    break;
                case OpeningPhase.LidCrack:
                    Publish(WakeBeat.LidCracked);
                    PublishPost(PostEventKind.Started, BootCheckStep.Resume, BootCheckResult.Info);
                    break;
                case OpeningPhase.LidHalf:
                    Publish(WakeBeat.LidHalf);
                    break;
                case OpeningPhase.Hunt:
                    _speckClock = 0f;
                    _readoutClock = 0f;
                    Publish(WakeBeat.FocusHunting);
                    PublishPost(PostEventKind.Started, BootCheckStep.Agenda, BootCheckResult.Info);
                    break;
                case OpeningPhase.PullOut:
                    Publish(WakeBeat.FocusSettled);
                    Publish(WakeBeat.PullingOut);
                    break;
                case OpeningPhase.Post:
                    Publish(WakeBeat.PulledOut);
                    _post.Begin();
                    DrainPost();
                    break;
                case OpeningPhase.OverShoulder:
                    Publish(WakeBeat.OverShoulder);
                    break;
                case OpeningPhase.ChaseEase:
                    WantsRigEase = true;
                    break;
                case OpeningPhase.ShortWake:
                    Publish(WakeBeat.Stirred);
                    break;
            }
        }

        private float Length(OpeningPhase phase)
        {
            switch (phase)
            {
                case OpeningPhase.FadeOut: return _settings.FadeOutSeconds;
                case OpeningPhase.Crackle: return _settings.CrackleSeconds;
                case OpeningPhase.LidCrack: return _settings.LidCrackSeconds;
                case OpeningPhase.LidHalf: return _settings.LidHalfSeconds;
                case OpeningPhase.Hunt: return _settings.HuntSeconds;
                case OpeningPhase.PullOut: return _settings.PullOutSeconds;
                case OpeningPhase.Post: return _post.TotalSeconds;
                case OpeningPhase.OverShoulder: return _settings.OverShoulderSeconds;
                case OpeningPhase.ChaseEase: return _tuning.WakeEaseSeconds;
                case OpeningPhase.ShortWake: return _settings.ShortLidSeconds + _settings.BlinkSeconds;
                default: return 0f;
            }
        }

        private void Advance()
        {
            switch (Phase)
            {
                case OpeningPhase.FadeOut:
                    Publish(WakeBeat.FadedOut);
                    Enter(OpeningPhase.Crackle);
                    break;
                case OpeningPhase.Crackle:
                    PublishPost(PostEventKind.Completed, BootCheckStep.Carrier, BootCheckResult.Info);
                    Enter(OpeningPhase.LidCrack);
                    break;
                case OpeningPhase.LidCrack:
                    Enter(OpeningPhase.LidHalf);
                    break;
                case OpeningPhase.LidHalf:
                    PublishPost(PostEventKind.Completed, BootCheckStep.Resume, BootCheckResult.Info);
                    Enter(OpeningPhase.Hunt);
                    break;
                case OpeningPhase.Hunt:
                    PublishPost(PostEventKind.Completed, BootCheckStep.Agenda, BootCheckResult.Info);
                    Enter(OpeningPhase.PullOut);
                    break;
                case OpeningPhase.PullOut:
                    Enter(OpeningPhase.Post);
                    break;
                case OpeningPhase.OverShoulder:
                    Enter(OpeningPhase.ChaseEase);
                    break;
                default:
                    Finish();
                    break;
            }
        }

        private void StepPost(float deltaTime)
        {
            _post.Step(deltaTime);
            DrainPost();
            if (_post.IsFinished)
            {
                Enter(OpeningPhase.OverShoulder);
            }
        }

        private void DrainPost()
        {
            while (_post.TryTake(out PostEvent postEvent))
            {
                PublishPost(postEvent.Kind, postEvent.Step, postEvent.Result);
            }
        }

        private void Finish()
        {
            Phase = OpeningPhase.Idle;
            _phaseTime = 0f;
            _phaseLength = 0f;
            LensActive = false;
            GazeHeld = false;
            BodyLidWeight = 0f;
            Sleep = 0f;
            Readout01 = 0f;
            Publish(WakeBeat.ChaseFrame);
        }

        private void AimScripted()
        {
            switch (Phase)
            {
                case OpeningPhase.FadeOut:
                case OpeningPhase.Crackle:
                case OpeningPhase.LidCrack:
                case OpeningPhase.LidHalf:
                case OpeningPhase.Hunt:
                    _gaze.Aim(0f, _settings.BowedPitch);
                    break;
                case OpeningPhase.PullOut:
                case OpeningPhase.Post:
                case OpeningPhase.OverShoulder:
                    _gaze.Aim(_earth.x, _earth.y - _settings.EarthBelow);
                    break;
            }
        }

        private void StepClocks(float deltaTime)
        {
            if (_speckClock >= 0f)
            {
                _speckClock += deltaTime;
            }

            if (_blinkClock >= 0f)
            {
                _blinkClock += deltaTime;
            }

            if (_readoutClock >= 0f)
            {
                _readoutClock += deltaTime;
            }

            if (_blinked)
            {
                return;
            }

            if (Phase == OpeningPhase.ShortWake && _phaseTime >= _settings.ShortLidSeconds)
            {
                _blinked = true;
                _blinkClock = 0f;
                _glanceClock = 0f;
            }
        }

        private void Evaluate()
        {
            if (Phase == OpeningPhase.Idle)
            {
                return;
            }

            float t = _phaseTime;
            float rise = _settings.LidRiseSeconds;
            float lid = _lidStart;
            float focus = _settings.FocusSettle;
            float sleep = Mathf.Lerp(_sleepStart, 0f, Smoothing.SmoothStep(0f, rise, t));
            BodyLidWeight = 1f;
            GazeHeld = InLens || Phase >= OpeningPhase.PullOut && Phase <= OpeningPhase.OverShoulder;
            LensActive = InLens;
            switch (Phase)
            {
                case OpeningPhase.FadeOut:
                    lid = 1f - Smoothing.SmoothStep(_settings.FadeOutFrom, 1f, t / _phaseLength);
                    sleep = 1f;
                    LensActive = lid < 1f;
                    break;
                case OpeningPhase.Crackle:
                    lid = 0f;
                    focus = _settings.FocusBlur;
                    sleep = 1f;
                    break;
                case OpeningPhase.LidCrack:
                    lid = Mathf.Lerp(_lidStart, _settings.LidCrack, Smoothing.SmoothStep(0f, rise, t));
                    focus = _settings.FocusBlur;
                    sleep = 1f;
                    break;
                case OpeningPhase.LidHalf:
                    lid = Mathf.Lerp(_lidStart, _settings.LidHalf, Smoothing.SmoothStep(0f, rise, t)) + Shudder(t);
                    focus = _settings.FocusBlur;
                    sleep = 1f;
                    break;
                case OpeningPhase.Hunt:
                    focus = Keyframes(t / _phaseLength, _focusStart, _settings.FocusNear, _settings.FocusOvershoot,
                        _settings.FocusSettle);
                    sleep = 1f;
                    break;
                case OpeningPhase.PullOut:
                    lid = Mathf.Lerp(_lidStart, _settings.LidRest, Smoothing.SmoothStep(0f, _settings.LidSettleSeconds, t));
                    LensActive = t < _settings.LensReleaseAt * _phaseLength;
                    break;
                case OpeningPhase.Post:
                case OpeningPhase.OverShoulder:
                    lid = _settings.LidRest;
                    break;
                case OpeningPhase.ChaseEase:
                    float ease = Smoothing.SmoothStep(0f, _phaseLength, t);
                    lid = Mathf.Lerp(_lidStart, _settings.LidRest, ease);
                    BodyLidWeight = 1f - ease;
                    break;
                case OpeningPhase.ShortWake:
                    float open = Smoothing.SmoothStep(0f, _settings.ShortLidSeconds, t);
                    lid = Mathf.Lerp(0f, _settings.LidRest, open);
                    sleep = Mathf.Lerp(_sleepStart, 0f, open);
                    break;
            }

            Lid01 = Mathf.Lerp(Mathf.Clamp01(lid), _settings.BlinkLow, BlinkDip());
            Readout01 = Readout();
            FocusDistance = focus;
            Sleep = sleep;
            SpeckOffset = _speckClock < 0f
                ? _settings.SpeckStart
                : Vector2.Lerp(_settings.SpeckStart, _settings.SpeckEnd, Smoothing.SmoothStep(0f, _speckSpan, _speckClock));
        }

        private float Shudder(float t)
        {
            float rise = _settings.LidRiseSeconds;
            if (t >= rise)
            {
                return 0f;
            }

            float u = t / rise;
            return _settings.ShudderAmount * Mathf.Sin(2f * Mathf.PI * _settings.ShudderWobbles * u)
                * Mathf.Sin(Mathf.PI * u);
        }

        /// <summary>The hunt: from where it was, too near, overshooting far, settling.</summary>
        private static float Keyframes(float u, float start, float near, float far, float settle)
        {
            if (u < HuntNearAt)
            {
                return LogLerp(start, near, Smoothing.SmoothStep(0f, HuntNearAt, u));
            }

            if (u < HuntFarAt)
            {
                return LogLerp(near, far, Smoothing.SmoothStep(HuntNearAt, HuntFarAt, u));
            }

            return LogLerp(far, settle, Smoothing.SmoothStep(HuntFarAt, 1f, u));
        }

        /// <summary>Interpolates focus distances evenly in their logarithm, the way a focus ring feels.</summary>
        private static float LogLerp(float from, float to, float t)
        {
            return Mathf.Exp(Mathf.Lerp(Mathf.Log(Mathf.Max(from, 1e-3f)), Mathf.Log(Mathf.Max(to, 1e-3f)), t));
        }

        private float Readout()
        {
            if (_readoutClock < 0f || !InLens)
            {
                return 0f;
            }

            float fade = _settings.ReadoutFade;
            float length = _settings.ReadoutSeconds;
            return Smoothing.SmoothStep(0f, fade, _readoutClock)
                * (1f - Smoothing.SmoothStep(length - fade, length, _readoutClock));
        }

        private float BlinkDip()
        {
            if (_blinkClock < 0f || _blinkClock >= _settings.BlinkSeconds)
            {
                return 0f;
            }

            float u = _blinkClock / _settings.BlinkSeconds;
            return u < BlinkCloseShare
                ? Smoothing.SmoothStep(0f, BlinkCloseShare, u)
                : 1f - Smoothing.SmoothStep(BlinkCloseShare, 1f, u);
        }
    }
}
