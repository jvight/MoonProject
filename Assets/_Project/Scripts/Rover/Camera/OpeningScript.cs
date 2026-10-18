using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// The New Game wake as 07's power-on self-test, plain C# so every rule is testable. The whole wake is one calm shot
    /// of 07's face: black, the picture opens on 07 asleep, its face screen flickers on and its eyes blink awake, the
    /// lines of <see cref="PostSequencer"/> print on the face while each part does its check, then a short dip to black
    /// hands over to the chase frame. It works out each frame how clear the picture is, how far the camera has crept
    /// in, how open 07's lid is, where 07 looks (<see cref="LensGaze"/>) and how asleep its body still is. The beats it
    /// reaches queue up for <see cref="TryTakeBeat"/>; the self-test's lines queue up for
    /// <see cref="TryTakePostEvent"/>.
    /// <list type="bullet">
    /// <item>Not skippable: only time moves it, and 07 is held (<see cref="ControlHeld"/>) until the chase frame.</item>
    /// <item>Continue plays the short wake instead: the lid opens in one move with one blink, then a glance at
    /// Earth that never holds control.</item>
    /// </list>
    /// The camera itself is the adapter's; this says when the face shot is live (<see cref="ShotLive"/>).
    /// </summary>
    public sealed class OpeningScript
    {
        private const int BeatCapacity = 8;
        private const int PostEventCapacity = 64;

        /// <summary>Share of the blink spent closing; the rest opens again, slower.</summary>
        private const float BlinkCloseShare = 0.4f;

        /// <summary>The handover's dip is darkest, and the chase frame takes over, at this share of it.</summary>
        public const float HandoverCutShare = 0.5f;

        private readonly OpeningSettings _settings;
        private readonly PostSettings _postSettings;
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
        private float _pushClock;
        private float _blinkClock;
        private float _glanceClock;
        private Vector2 _face;
        private bool _blinked;

        public OpeningScript(RoverCameraTuning tuning)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            _settings = tuning.Opening;
            _postSettings = _settings.Post;
            _gaze = new LensGaze(_settings);
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

        public LensGaze Gaze => _gaze;

        /// <summary>The check under way while <see cref="OpeningPhase.Post"/> runs.</summary>
        public BootCheckStep PostStep => _post.Current;

        /// <summary>How far through the current check, 0..1.</summary>
        public float PostProgress01 => _post.StepProgress01;

        /// <summary>How clear the picture is: 0 black, 1 the world as it is.</summary>
        public float Picture01 { get; private set; }

        /// <summary>True while the picture is dimmed toward black (the fades, the black and the handover's dip).</summary>
        public bool PictureDimmed => Phase != OpeningPhase.Idle && Phase != OpeningPhase.ShortWake && Picture01 < 1f;

        /// <summary>
        /// True while the camera holds the one shot of 07's face: from the black until the middle of the handover's
        /// dip, where the chase frame takes over in the dark.
        /// </summary>
        public bool ShotLive
        {
            get
            {
                switch (Phase)
                {
                    case OpeningPhase.Dark:
                    case OpeningPhase.FadeIn:
                    case OpeningPhase.EyesOpen:
                    case OpeningPhase.Post:
                        return true;
                    case OpeningPhase.Handover:
                        return _phaseTime < HandoverCutShare * _phaseLength;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// How far (0..1) through the shot's slow push-in toward the face: still while 07 wakes, then over
        /// <see cref="PostSettings.PushInSeconds"/> from the first line on the face.
        /// </summary>
        public float Push01 => Mathf.Clamp01(_pushClock / _postSettings.PushInSeconds);

        /// <summary>07's body: 1 asleep (head bowed, eye dark) .. 0 awake.</summary>
        public float Sleep { get; private set; }

        /// <summary>07's own lid openness, and how much the wake holds it.</summary>
        public float BodyLid { get; private set; }
        public float BodyLidWeight { get; private set; }

        /// <summary>07's head should follow the gaze (from the black until the chase frame takes over).</summary>
        public bool GazeHeld { get; private set; }

        /// <summary>The short wake's glance at Earth is on.</summary>
        public bool GlancingEarth => _glanceClock >= 0f && _glanceClock < _settings.ShortGlanceSeconds;

        /// <summary>Control is held from the fade to black until the chase frame (and through the short wake).</summary>
        public bool ControlHeld => Phase != OpeningPhase.Idle;

        private float EyesOpenSeconds => _postSettings.EyesBootSeconds + _postSettings.EyesHoldSeconds;

        /// <summary>
        /// Where the camera is as (yaw, pitch) deg from 07's heading at its face: the head lifts toward it as the eyes
        /// open and holds there through the checks.
        /// </summary>
        public void SetFaceTarget(Vector2 face)
        {
            _face = face;
        }

        /// <summary>A fresh game: the full self-test, from the fade to black when the title was showing.</summary>
        public void BeginFull(bool fromTitle)
        {
            Reset();
            _gaze.Reset(0f, _settings.BowedPitch);
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
            if (Phase == OpeningPhase.Post)
            {
                _pushClock += deltaTime;
            }

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
            _pushClock = 0f;
            _blinkClock = -1f;
            _glanceClock = -1f;
            _blinked = false;
            Picture01 = 1f;
            Sleep = 1f;
            BodyLid = 0f;
            BodyLidWeight = 0f;
            GazeHeld = false;
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
                case OpeningPhase.FadeIn:
                    Publish(WakeBeat.PictureOpens);
                    break;
                case OpeningPhase.EyesOpen:
                    Publish(WakeBeat.EyesOpen);
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
                case OpeningPhase.FadeIn: return _postSettings.FadeInSeconds;
                case OpeningPhase.EyesOpen: return EyesOpenSeconds;
                case OpeningPhase.Post: return _post.TotalSeconds;
                case OpeningPhase.Handover: return _postSettings.HandoverSeconds;
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
                    Enter(OpeningPhase.FadeIn);
                    break;
                case OpeningPhase.FadeIn:
                    Enter(OpeningPhase.EyesOpen);
                    break;
                case OpeningPhase.EyesOpen:
                    Enter(OpeningPhase.Post);
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
                Enter(OpeningPhase.Handover);
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
            GazeHeld = false;
            BodyLidWeight = 0f;
            Sleep = 0f;
            Publish(WakeBeat.ChaseFrame);
        }

        private void AimScripted()
        {
            switch (Phase)
            {
                case OpeningPhase.FadeOut:
                case OpeningPhase.Dark:
                case OpeningPhase.FadeIn:
                    _gaze.Aim(0f, _settings.BowedPitch);
                    break;
                case OpeningPhase.EyesOpen:
                case OpeningPhase.Post:
                case OpeningPhase.Handover:
                    _gaze.Aim(_face.x, _face.y);
                    break;
            }
        }

        private void StepClocks(float deltaTime)
        {
            if (_blinkClock >= 0f)
            {
                _blinkClock += deltaTime;
            }

            if (_blinked)
            {
                return;
            }

            if (Phase == OpeningPhase.EyesOpen && _phaseTime >= _postSettings.EyesBootSeconds)
            {
                _blinked = true;
                _blinkClock = 0f;
                Publish(WakeBeat.Blinked);
            }
            else if (Phase == OpeningPhase.ShortWake && _phaseTime >= _settings.ShortLidSeconds)
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
            float u = _phaseLength > 1e-4f ? Mathf.Clamp01(t / _phaseLength) : 1f;
            float picture = 1f;
            float sleep = 0f;
            float lid = _settings.LidRest;
            BodyLidWeight = 1f;
            GazeHeld = ShotLive;
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
                case OpeningPhase.FadeIn:
                    picture = WakeOpening.SmoothStepC2(0f, 1f, u);
                    sleep = 1f;
                    lid = 0f;
                    break;
                case OpeningPhase.EyesOpen:
                    float wake = Smoothing.SmoothStep(0f, _postSettings.EyesBootSeconds, t);
                    sleep = 1f - wake;
                    lid = _settings.LidRest * wake;
                    break;
                case OpeningPhase.Handover:
                    float dip = u < HandoverCutShare
                        ? Smoothing.SmoothStep(0f, HandoverCutShare, u)
                        : 1f - Smoothing.SmoothStep(HandoverCutShare, 1f, u);
                    picture = 1f - dip;
                    BodyLidWeight = ShotLive ? 1f : 0f;
                    break;
                case OpeningPhase.ShortWake:
                    float open = Smoothing.SmoothStep(0f, _settings.ShortLidSeconds, t);
                    lid = _settings.LidRest * open;
                    sleep = 1f - open;
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
