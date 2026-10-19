using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// The New Game wake as 07's power-on self-test, plain C# so every rule is testable. The camera settles onto the
    /// close-up of 07's dark face (the inventory's framing): one push-in from a fresh title, or, over a save, a cover
    /// App reloads under and then the dark face itself. The tube wakes, the maker's mark draws itself, a field profile
    /// loads (<see cref="PostEventKind.FaceBegan"/>), and the lines of <see cref="PostSequencer"/> print there while
    /// each part does its check. Then the readout holds its success and 07's eyes come back to the face, the camera
    /// eases back and out to a wide shot, 07 performs that it is ready (a double blink, the headlamps clicked off and
    /// on, a glance at the beacon), and the camera eases round to the chase frame. It works out each frame how open
    /// 07's lid is and how asleep its body still is. The beats it reaches queue up for <see cref="TryTakeBeat"/>; the
    /// face's parts, the self-test's lines and the performance's sounds and blinks queue up for
    /// <see cref="TryTakePostEvent"/>.
    /// <list type="bullet">
    /// <item>A key can fast-forward the face's boot and the self-test's lines (<see cref="FastForward"/>), never in
    /// their first moments; nothing else skips, and 07 is held (<see cref="ControlHeld"/>) until the chase frame.</item>
    /// <item>Continue plays the short wake instead: the lid opens in one move with one blink, then a glance at
    /// Earth that never holds control.</item>
    /// </list>
    /// The camera and the head are the adapter's; this says when the opening's shot is live (<see cref="ShotLive"/>).
    /// </summary>
    public sealed class OpeningScript
    {
        private const int BeatCapacity = 8;
        private const int PostEventCapacity = 64;

        /// <summary>Share of the blink spent closing; the rest opens again, slower.</summary>
        private const float BlinkCloseShare = 0.4f;

        private readonly OpeningSettings _settings;
        private readonly PostSettings _postSettings;
        private readonly PostSequencer _post;
        private readonly WakeBeat[] _beats = new WakeBeat[BeatCapacity];
        private readonly PostEvent[] _postEvents = new PostEvent[PostEventCapacity];
        private int _beatHead;
        private int _beatCount;
        private int _postHead;
        private int _postCount;
        private float _phaseTime;
        private float _phaseLength;
        private float _blinkClock;
        private float _glanceClock;
        private bool _blinked;
        private bool _eyesBack;
        private bool _fastForward;
        private float _screenOnSeconds;
        private int _performPulsesSent;
        private int _performBlinksSent;

        public OpeningScript(RoverCameraTuning tuning)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            _settings = tuning.Opening;
            _postSettings = _settings.Post;
            Lamps = new LampPowerUp(_settings);
            Motions = new PostMotions(_postSettings);
            _post = new PostSequencer(_postSettings, Lamps);
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

        /// <summary>How far (0..1) through the current phase; 1 when it has no length.</summary>
        public float PhaseShare => _phaseLength > 1e-4f ? Mathf.Clamp01(_phaseTime / _phaseLength) : 1f;

        /// <summary>The check under way while <see cref="OpeningPhase.Post"/> runs.</summary>
        public BootCheckStep PostStep => _post.Current;

        /// <summary>How far through the current check, 0..1.</summary>
        public float PostProgress01 => _post.StepProgress01;

        /// <summary>How far (0..1) through the ready performance: 0 before it, 1 after it.</summary>
        public float Perform01 => Phase == OpeningPhase.Perform ? PhaseShare : Phase == OpeningPhase.MoveBehind ? 1f : 0f;

        /// <summary>True while the opening's camera has the view: from the push-in (or the dark face) to the chase frame.</summary>
        public bool ShotLive => Phase >= OpeningPhase.PushIn && Phase <= OpeningPhase.MoveBehind;

        /// <summary>Self-test seconds since 07's tube woke (0 before; fast-forwarded seconds count at their scale).</summary>
        public float ScreenOnSeconds => _screenOnSeconds;

        /// <summary>07's body: 1 asleep (head bowed, eye dark) .. 0 awake.</summary>
        public float Sleep { get; private set; }

        /// <summary>07's own lid openness, and how much the wake holds it.</summary>
        public float BodyLid { get; private set; }
        public float BodyLidWeight { get; private set; }

        /// <summary>The short wake's glance at Earth is on.</summary>
        public bool GlancingEarth => _glanceClock >= 0f && _glanceClock < _settings.ShortGlanceSeconds;

        /// <summary>
        /// How fast the face's boot and the self-test's lines run: the fast-forward's scale once a key sped them up,
        /// else 1.
        /// </summary>
        public float PostTimeScale => _fastForward && InSelfTest ? _postSettings.FastForwardScale : 1f;

        /// <summary>From the tube waking to the last line: what a fast-forward speeds up.</summary>
        private bool InSelfTest => Phase >= OpeningPhase.PowerOn && Phase <= OpeningPhase.Post;

        /// <summary>Control is held from the fade to black until the chase frame (and through the short wake).</summary>
        public bool ControlHeld => Phase != OpeningPhase.Idle;

        /// <summary>A fresh game: the full self-test, begun as <paramref name="start"/> says.</summary>
        public void BeginFull(OpeningStart start)
        {
            Reset();
            Enter(start == OpeningStart.PushIn ? OpeningPhase.PushIn
                : start == OpeningStart.Cover ? OpeningPhase.FadeOut
                : OpeningPhase.Dark);
            Evaluate();
        }

        /// <summary>A loaded game: the short wake.</summary>
        public void BeginShort()
        {
            Reset();
            Enter(OpeningPhase.ShortWake);
            Evaluate();
        }

        /// <summary>
        /// A key asks to fast-forward the rest of the self-test (the face's boot and the lines): granted once, from
        /// <see cref="PostSettings.FastForwardAfterSeconds"/> after the tube wakes, so no press skips the wake on its
        /// first frame. The rest then runs <see cref="PostSettings.FastForwardScale"/> times faster, each part and line
        /// still announced in order.
        /// </summary>
        public bool FastForward()
        {
            if (!InSelfTest || _fastForward || _screenOnSeconds < _postSettings.FastForwardAfterSeconds)
            {
                return false;
            }

            _fastForward = true;
            return true;
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
            float phaseDelta = deltaTime * PostTimeScale;
            _phaseTime += phaseDelta;
            if (Phase >= OpeningPhase.PowerOn)
            {
                _screenOnSeconds += phaseDelta;
            }

            StepClocks(deltaTime);
            if (Phase == OpeningPhase.Post)
            {
                StepPost(phaseDelta);
            }

            while (Phase != OpeningPhase.Idle && Phase != OpeningPhase.Post && _phaseTime >= _phaseLength)
            {
                float leftover = (_phaseTime - _phaseLength) / PostTimeScale;
                Advance();
                CarryInto(leftover);
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
            _blinkClock = -1f;
            _glanceClock = -1f;
            _blinked = false;
            _eyesBack = false;
            _fastForward = false;
            _screenOnSeconds = 0f;
            _performPulsesSent = 0;
            _performBlinksSent = 0;
            Sleep = 1f;
            BodyLid = 0f;
            BodyLidWeight = 0f;
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

        private void PublishPost(PostEvent postEvent)
        {
            if (_postCount == PostEventCapacity)
            {
                _postHead = (_postHead + 1) % PostEventCapacity;
                _postCount--;
            }

            _postEvents[(_postHead + _postCount) % PostEventCapacity] = postEvent;
            _postCount++;
        }

        private void Enter(OpeningPhase phase)
        {
            Phase = phase;
            _phaseTime = 0f;
            _phaseLength = Length(phase);
            switch (phase)
            {
                case OpeningPhase.PushIn:
                case OpeningPhase.Dark:
                    Publish(WakeBeat.Crackle);
                    break;
                case OpeningPhase.PowerOn:
                    Publish(WakeBeat.ChecksBegin);
                    PublishFace(BootFacePart.PowerOn);
                    break;
                case OpeningPhase.Logo:
                    PublishFace(BootFacePart.Logo);
                    break;
                case OpeningPhase.Load:
                    PublishFace(BootFacePart.Load);
                    break;
                case OpeningPhase.Post:
                    _post.Begin();
                    DrainPost();
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
                case OpeningPhase.PushIn: return _postSettings.PushInSeconds;
                case OpeningPhase.Dark: return _postSettings.DarkSeconds;
                case OpeningPhase.PowerOn: return _postSettings.PowerOnSeconds;
                case OpeningPhase.Logo: return _postSettings.LogoSeconds;
                case OpeningPhase.Load: return _postSettings.LoadSeconds;
                case OpeningPhase.Post: return _post.TotalSeconds;
                case OpeningPhase.Ready: return _postSettings.ReadySeconds;
                case OpeningPhase.PullBack: return _postSettings.PullBackSeconds;
                case OpeningPhase.Perform: return _postSettings.PerformSeconds;
                case OpeningPhase.MoveBehind: return _postSettings.MoveBehindSeconds;
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
                    Enter(OpeningPhase.Dark);
                    break;
                case OpeningPhase.PushIn:
                case OpeningPhase.Dark:
                    Enter(OpeningPhase.PowerOn);
                    break;
                case OpeningPhase.PowerOn:
                    Enter(OpeningPhase.Logo);
                    break;
                case OpeningPhase.Logo:
                    Enter(OpeningPhase.Load);
                    break;
                case OpeningPhase.Load:
                    Enter(OpeningPhase.Post);
                    break;
                case OpeningPhase.Ready:
                    Enter(OpeningPhase.PullBack);
                    break;
                case OpeningPhase.PullBack:
                    Enter(OpeningPhase.Perform);
                    break;
                case OpeningPhase.Perform:
                    StepPerform();
                    Enter(OpeningPhase.MoveBehind);
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
                float leftover = Math.Max(0f, _phaseTime - _phaseLength) / PostTimeScale;
                Enter(OpeningPhase.Ready);
                CarryInto(leftover);
            }
        }

        /// <summary>
        /// Starts the phase just entered <paramref name="seconds"/> (unscaled) in: what the frame had left after the
        /// last phase ended. Dropping it would add up to a frame at every change, so a slow frame rate would stretch
        /// the wake past its tuned length.
        /// </summary>
        private void CarryInto(float seconds)
        {
            if (Phase == OpeningPhase.Idle || seconds <= 0f)
            {
                return;
            }

            float delta = seconds * PostTimeScale;
            _phaseTime += delta;
            if (Phase == OpeningPhase.Post)
            {
                StepPost(delta);
            }
        }

        /// <summary>The ready performance's blinks and sounds reached by now, in order.</summary>
        private void StepPerform()
        {
            float p = PhaseShare;
            while (_performBlinksSent < PostMotions.BlinkCount && PostMotions.BlinkAt(_performBlinksSent) <= p)
            {
                PublishPost(new PostEvent(PostEventKind.Blink, BootCheckStep.Begin, BootCheckResult.Info,
                    _performBlinksSent));
                _performBlinksSent++;
            }

            while (_performPulsesSent < PostMotions.PerformPulseCount)
            {
                PostMotions.PulseMark mark = PostMotions.PerformPulse(_performPulsesSent);
                if (mark.Share > p)
                {
                    break;
                }

                PublishPost(new PostEvent(PostEventKind.Pulse, BootCheckStep.Begin, BootCheckResult.Info, mark.Index));
                _performPulsesSent++;
            }
        }

        private void PublishFace(BootFacePart part)
        {
            PublishPost(new PostEvent(PostEventKind.FaceBegan, BootCheckStep.Carrier, BootCheckResult.Info, (int)part,
                seconds: _phaseLength));
        }

        private void DrainPost()
        {
            while (_post.TryTake(out PostEvent postEvent))
            {
                PublishPost(postEvent);
            }
        }

        private void Finish()
        {
            Phase = OpeningPhase.Idle;
            _phaseTime = 0f;
            _phaseLength = 0f;
            BodyLidWeight = 0f;
            Sleep = 0f;
            Publish(WakeBeat.ChaseFrame);
        }

        private void StepClocks(float deltaTime)
        {
            if (_blinkClock >= 0f)
            {
                _blinkClock += deltaTime;
            }

            if (Phase == OpeningPhase.Ready && !_eyesBack && _phaseTime >= _postSettings.ReadyEyesSeconds)
            {
                _eyesBack = true;
                Publish(WakeBeat.EyesOpen);
            }

            if (Phase == OpeningPhase.Perform)
            {
                StepPerform();
            }

            if (!_blinked && Phase == OpeningPhase.ShortWake && _phaseTime >= _settings.ShortLidSeconds)
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
            float u = PhaseShare;
            float sleep = 0f;
            float lid = _settings.LidRest;
            BodyLidWeight = 1f;
            switch (Phase)
            {
                case OpeningPhase.FadeOut:
                    sleep = 1f;
                    lid = 0f;
                    break;
                case OpeningPhase.PushIn:
                case OpeningPhase.Dark:
                    float lift = WakeOpening.SmoothStepC2(0f, 1f, u);
                    sleep = 1f - lift;
                    lid = _settings.LidRest * lift;
                    break;
                case OpeningPhase.MoveBehind:
                    BodyLidWeight = 1f - WakeOpening.SmoothStepC2(0f, 1f, u);
                    break;
                case OpeningPhase.ShortWake:
                    float shortOpen = Smoothing.SmoothStep(0f, _settings.ShortLidSeconds, t);
                    lid = _settings.LidRest * shortOpen;
                    sleep = 1f - shortOpen;
                    break;
            }

            Sleep = sleep;
            BodyLid = Mathf.Lerp(Mathf.Clamp01(lid), _settings.BlinkLow, BlinkDip());
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
