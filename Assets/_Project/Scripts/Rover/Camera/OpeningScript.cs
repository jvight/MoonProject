using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// The New Game wake as 07's power-on self-test, plain C# so every rule is testable. From the title's fade and a
    /// moment of black the picture opens straight on the close-up of 07's face (the inventory's framing) and the lines
    /// of <see cref="PostSequencer"/> print there while each part does its check. Then the readout holds its success
    /// and 07's eyes come back to the face, the camera eases back and out to a wide shot, 07 performs that it is ready
    /// (a double blink, the headlamps clicked off and on, a glance at the beacon), and the camera eases
    /// round to the chase frame. It works out each frame how clear the picture is, how open 07's lid is and how asleep
    /// its body still is. The beats it reaches queue up for <see cref="TryTakeBeat"/>; the self-test's lines and the
    /// performance's sounds and blinks queue up for <see cref="TryTakePostEvent"/>.
    /// <list type="bullet">
    /// <item>A key can fast-forward the self-test's lines (<see cref="FastForward"/>), never on their first frame;
    /// nothing else skips, and 07 is held (<see cref="ControlHeld"/>) until the chase frame.</item>
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

        /// <summary>How clear the picture is: 0 black, 1 the world as it is.</summary>
        public float Picture01 { get; private set; }

        /// <summary>True while the picture is dimmed toward black (the fade from the title, the black, the opening).</summary>
        public bool PictureDimmed => Phase != OpeningPhase.Idle && Phase != OpeningPhase.ShortWake && Picture01 < 1f;

        /// <summary>True while the opening's camera has the view: from the black until the chase frame.</summary>
        public bool ShotLive => Phase >= OpeningPhase.Dark && Phase <= OpeningPhase.MoveBehind;

        /// <summary>07's body: 1 asleep (head bowed, eye dark) .. 0 awake.</summary>
        public float Sleep { get; private set; }

        /// <summary>07's own lid openness, and how much the wake holds it.</summary>
        public float BodyLid { get; private set; }
        public float BodyLidWeight { get; private set; }

        /// <summary>The short wake's glance at Earth is on.</summary>
        public bool GlancingEarth => _glanceClock >= 0f && _glanceClock < _settings.ShortGlanceSeconds;

        /// <summary>How fast the self-test's lines run: the fast-forward's scale once a key sped them up, else 1.</summary>
        public float PostTimeScale => _fastForward && Phase == OpeningPhase.Post ? _postSettings.FastForwardScale : 1f;

        /// <summary>Control is held from the fade to black until the chase frame (and through the short wake).</summary>
        public bool ControlHeld => Phase != OpeningPhase.Idle;

        /// <summary>A fresh game: the full self-test, from the fade to black when the title was showing.</summary>
        public void BeginFull(bool fromTitle)
        {
            Reset();
            Enter(fromTitle ? OpeningPhase.FadeOut : OpeningPhase.Dark);
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
        /// A key asks to fast-forward the remaining lines: granted once, while they print and only after
        /// <see cref="PostSettings.FastForwardAfterSeconds"/> of them, so no press skips the wake on its first frame.
        /// The lines then run <see cref="PostSettings.FastForwardScale"/> times faster, each still announced in order.
        /// </summary>
        public bool FastForward()
        {
            if (Phase != OpeningPhase.Post || _fastForward || _phaseTime < _postSettings.FastForwardAfterSeconds)
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
            StepClocks(deltaTime);
            if (Phase == OpeningPhase.Post)
            {
                StepPost(phaseDelta);
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
            _blinkClock = -1f;
            _glanceClock = -1f;
            _blinked = false;
            _eyesBack = false;
            _fastForward = false;
            _performPulsesSent = 0;
            _performBlinksSent = 0;
            Picture01 = 1f;
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
                case OpeningPhase.Dark:
                    Publish(WakeBeat.Crackle);
                    break;
                case OpeningPhase.Post:
                    Publish(WakeBeat.ChecksBegin);
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
                case OpeningPhase.Dark: return _postSettings.DarkSeconds;
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
                case OpeningPhase.Dark:
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
                Enter(OpeningPhase.Ready);
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
            Picture01 = 1f;
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
            float picture = 1f;
            float sleep = 0f;
            float lid = _settings.LidRest;
            BodyLidWeight = 1f;
            switch (Phase)
            {
                case OpeningPhase.FadeOut:
                    picture = 1f - Smoothing.SmoothStep(_settings.FadeOutFrom, 1f, u);
                    sleep = 1f;
                    lid = 0f;
                    break;
                case OpeningPhase.Dark:
                    picture = 0f;
                    sleep = 1f;
                    lid = 0f;
                    break;
                case OpeningPhase.Post:
                    float open = WakeOpening.SmoothStepC2(0f, _postSettings.FadeInSeconds, t);
                    picture = open;
                    sleep = 1f - open;
                    lid = _settings.LidRest * open;
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

            Picture01 = picture;
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
