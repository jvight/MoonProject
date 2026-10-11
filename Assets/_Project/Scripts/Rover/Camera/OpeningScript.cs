using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// The opening's conductor (docs/design/cutscenes.md §1), plain C# so every rule is testable: it walks the
    /// storyboard's rows O1-O13 and works out, each frame, how open 07's lid is, where the lens focuses, how the old
    /// electronics flicker, where the dust speck sits, where 07 looks (<see cref="LensGaze"/>), how asleep its body
    /// still is and when its head tilts. The beats it reaches queue up for <see cref="TryTakeBeat"/>.
    /// <list type="bullet">
    /// <item>Look input from O3 on steers the gaze; the scripted attention pauses and resumes a moment after the
    /// last look, from the next target not yet seen. Holding Earth in view on one's own skips ahead to O9.</item>
    /// <item>Drive input hurries: inside the lens the remaining lid and focus stages compress, then the camera goes
    /// straight to the chase frame; later it eases out at once. The blink always happens, and control is held only
    /// while 07 is still inside the lens (<see cref="ControlHeld"/>), at most the hurried lid's length after the
    /// first drive input.</item>
    /// <item>Continue plays the short wake instead: the lid opens in one move with one blink, then a glance at
    /// Earth that never holds control.</item>
    /// </list>
    /// The camera moves themselves (the lens camera, the rig's lens shot and its turn) are the adapter's; this says
    /// when (<see cref="PullOut01"/>, <see cref="WantsRigEase"/>).
    /// </summary>
    public sealed class OpeningScript
    {
        private const int BeatCapacity = 8;
        private const float LookEpsilon = 1e-4f;
        private const int Home = 0;
        private const int Pilot = 1;
        private const int Peak = 2;
        private const int Earth = 3;
        private const int TargetCount = 4;

        /// <summary>Share of the blink spent closing; the rest opens again, slower.</summary>
        private const float BlinkCloseShare = 0.4f;

        /// <summary>Share of the hunt where the focus reaches its nearest, then its overshoot.</summary>
        private const float HuntNearAt = 0.35f;
        private const float HuntFarAt = 0.7f;

        /// <summary>The hurried speck slides out this many times faster.</summary>
        private const float HurriedSpeckRate = 4f;

        private readonly RoverCameraTuning _tuning;
        private readonly OpeningSettings _settings;
        private readonly LensGaze _gaze;
        private readonly WakeBeat[] _beats = new WakeBeat[BeatCapacity];
        private readonly Vector2[] _targets = new Vector2[TargetCount];
        private readonly bool[] _seen = new bool[TargetCount];
        private int _beatHead;
        private int _beatCount;
        private float _phaseTime;
        private float _phaseLength;
        private float _lidStart;
        private float _focusStart;
        private float _sleepStart;
        private float _sinceLook;
        private float _earthHeld;
        private float _speckClock;
        private float _speckSpan;
        private float _blinkClock;
        private float _tiltClock;
        private float _glanceClock;
        private bool _blinked;
        private bool _farFocusLanded;

        public OpeningScript(RoverCameraTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _settings = tuning.Opening;
            _gaze = new LensGaze(_settings);
            Reset();
        }

        public OpeningPhase Phase { get; private set; }

        /// <summary>True from a begin until the chase frame (the short wake's glance may run on after it).</summary>
        public bool IsRunning => Phase != OpeningPhase.Idle;

        /// <summary>True once the player's drive input hurried the wake.</summary>
        public bool Hurried { get; private set; }

        /// <summary>Seconds since the opening (or the short wake) began, the push into the lens included.</summary>
        public float Elapsed { get; private set; }

        /// <summary>Seconds into the current phase, and how long it lasts.</summary>
        public float PhaseTime => _phaseTime;
        public float PhaseLength => _phaseLength;

        public LensGaze Gaze => _gaze;

        /// <summary>The lens view's openness: 07's lid inside the lens, the closing glass while pushing in.</summary>
        public float Lid01 { get; private set; }

        public float FocusDistance { get; private set; }

        public float Flicker01 { get; private set; }

        public float Warmth01 { get; private set; }

        public Vector2 SpeckOffset { get; private set; }

        /// <summary>The state to hand <see cref="ILensView"/> while <see cref="LensActive"/>.</summary>
        public LensViewState LensState => new LensViewState(Lid01, FocusDistance, Flicker01, Warmth01, SpeckOffset);

        /// <summary>True while the picture is seen through 07's eye (the lens view applies).</summary>
        public bool LensActive { get; private set; }

        /// <summary>True while the camera sits at the lens socket (O1-O9 and the hurried lid).</summary>
        public bool InLens => Phase >= OpeningPhase.Crackle && Phase <= OpeningPhase.HurryLid;

        /// <summary>The push into the closed lens, 0..1 eased (title -> O1).</summary>
        public float PushIn01 => Phase == OpeningPhase.PushIn ? Smoothing.SmoothStep(0f, _phaseLength, _phaseTime) : 0f;

        /// <summary>The camera's way out of the lens, 0..1 eased (O10).</summary>
        public float PullOut01 => Phase == OpeningPhase.PullOut ? Smoothing.SmoothStep(0f, _phaseLength, _phaseTime) : 0f;

        /// <summary>The rig's lens shot should ease out to the chase frame now (O13, or a hurry after the lens).</summary>
        public bool WantsRigEase { get; private set; }

        /// <summary>07's body: 1 asleep (head bowed, eye dark) .. 0 awake.</summary>
        public float Sleep { get; private set; }

        /// <summary>
        /// 07's own lid openness, and how much the wake holds it: shut while pushing in; fully open while the camera
        /// is inside the lens, where the lens view draws the lid and the real brow-shutter would cover the camera.
        /// </summary>
        public float BodyLid => Phase == OpeningPhase.PushIn ? 0f : InLens ? 1f : Lid01;
        public float BodyLidWeight { get; private set; }

        /// <summary>The O11 head tilt (deg); it eases back out even after the chase frame.</summary>
        public float HeadTilt { get; private set; }

        /// <summary>The head tilt is still easing in, holding or easing out.</summary>
        public bool TiltActive => _tiltClock >= 0f && _tiltClock < TiltSeconds;

        /// <summary>07's head should follow the gaze (inside the lens, and up at Earth until the turn).</summary>
        public bool GazeHeld { get; private set; }

        /// <summary>The short wake's glance at Earth is on.</summary>
        public bool GlancingEarth => _glanceClock >= 0f && _glanceClock < _settings.ShortGlanceSeconds;

        /// <summary>
        /// Control is held: while pushing in, inside the lens (the hurried lid included) and during the short wake's
        /// lid. Drive input inside the lens leaves at most the hurried lid's length.
        /// </summary>
        public bool ControlHeld => Phase == OpeningPhase.PushIn || InLens || Phase == OpeningPhase.ShortWake;

        /// <summary>
        /// Where the gaze targets are, as (yaw, pitch) deg from 07's heading at its lens: the lander, the tower's
        /// pilot, The Peak's beacon and Earth.
        /// </summary>
        public void SetTargets(Vector2 home, Vector2 pilot, Vector2 peak, Vector2 earth)
        {
            _targets[Home] = home;
            _targets[Pilot] = pilot;
            _targets[Peak] = peak;
            _targets[Earth] = earth;
        }

        /// <summary>A fresh game: the full opening, from the push into the closed lens when the title was showing.</summary>
        public void BeginFull(bool pushIn)
        {
            Reset();
            _gaze.Reset(0f, _settings.BowedPitch);
            Enter(pushIn ? OpeningPhase.PushIn : OpeningPhase.Crackle);
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

        /// <param name="look">The player's look this frame (deg).</param>
        /// <param name="drive">The player is giving drive input.</param>
        public void Step(float deltaTime, Vector2 look, bool drive)
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
                StepClocks(deltaTime);
                HeadTilt = Tilt();
                return;
            }

            Elapsed += deltaTime;
            if (drive)
            {
                Hurry();
            }

            bool steering = AcceptsLook && look.sqrMagnitude > LookEpsilon * LookEpsilon;
            bool wasPaused = _sinceLook < _settings.AttentionResume;
            if (steering)
            {
                _gaze.Steer(look);
                _sinceLook = 0f;
            }
            else
            {
                _sinceLook += deltaTime;
            }

            bool paused = _sinceLook < _settings.AttentionResume;
            bool gazing = IsGazePhase(Phase);
            if (!(gazing && paused))
            {
                _phaseTime += deltaTime;
            }

            TrackSeen(paused);
            if (gazing && wasPaused && !paused)
            {
                ResumeAttention();
            }

            CheckEarthFound(paused, deltaTime);
            if (!paused || !AcceptsLook)
            {
                AimScripted();
            }

            _gaze.Step(deltaTime);
            StepClocks(deltaTime);
            while (Phase != OpeningPhase.Idle && _phaseTime >= _phaseLength)
            {
                Advance();
            }

            Evaluate();
        }

        private bool AcceptsLook => Phase >= OpeningPhase.LidHalf && Phase <= OpeningPhase.GazeEarth;

        private static bool IsGazePhase(OpeningPhase phase)
        {
            return phase >= OpeningPhase.GazeHome && phase <= OpeningPhase.GazeEarth;
        }

        private void Reset()
        {
            Phase = OpeningPhase.Idle;
            Hurried = false;
            Elapsed = 0f;
            _beatHead = 0;
            _beatCount = 0;
            _phaseTime = 0f;
            _phaseLength = 0f;
            _sinceLook = float.MaxValue;
            _earthHeld = 0f;
            _speckClock = -1f;
            _blinkClock = -1f;
            _tiltClock = -1f;
            _glanceClock = -1f;
            _blinked = false;
            _farFocusLanded = false;
            WantsRigEase = false;
            for (int i = 0; i < TargetCount; i++)
            {
                _seen[i] = false;
            }

            _speckSpan = _settings.HuntSeconds + _settings.FlickerSeconds + _settings.GazeHomeSeconds
                + _settings.GazePilotSeconds + _settings.GazePeakSeconds + _settings.GazeEarthSeconds;
            Lid01 = 0f;
            FocusDistance = _settings.FocusBlur;
            Flicker01 = 0f;
            Warmth01 = 0f;
            SpeckOffset = _settings.SpeckStart;
            Sleep = 1f;
            BodyLidWeight = 0f;
            HeadTilt = 0f;
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
                    break;
                case OpeningPhase.LidCrack:
                    Publish(WakeBeat.LidCracked);
                    break;
                case OpeningPhase.LidHalf:
                    Publish(WakeBeat.LidHalf);
                    break;
                case OpeningPhase.Hunt:
                    _speckClock = 0f;
                    Publish(WakeBeat.FocusHunting);
                    break;
                case OpeningPhase.Flicker:
                    Publish(WakeBeat.FocusSettled);
                    Publish(WakeBeat.Flicker);
                    break;
                case OpeningPhase.GazeHome:
                    _seen[Home] = true;
                    Publish(WakeBeat.GazeHome);
                    break;
                case OpeningPhase.GazePilot:
                    _seen[Pilot] = true;
                    Publish(WakeBeat.GazePilot);
                    break;
                case OpeningPhase.GazePeak:
                    _seen[Peak] = true;
                    _farFocusLanded = false;
                    Publish(WakeBeat.GazePeak);
                    break;
                case OpeningPhase.GazeEarth:
                    _seen[Earth] = true;
                    Publish(WakeBeat.GazeEarth);
                    break;
                case OpeningPhase.PullOut:
                    Publish(WakeBeat.PullingOut);
                    break;
                case OpeningPhase.BlinkShot:
                    Publish(WakeBeat.PulledOut);
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
                case OpeningPhase.PushIn: return _settings.PushInSeconds;
                case OpeningPhase.Crackle: return _settings.CrackleSeconds;
                case OpeningPhase.LidCrack: return _settings.LidCrackSeconds;
                case OpeningPhase.LidHalf: return _settings.LidHalfSeconds;
                case OpeningPhase.Hunt: return _settings.HuntSeconds;
                case OpeningPhase.Flicker: return _settings.FlickerSeconds;
                case OpeningPhase.GazeHome: return _settings.GazeHomeSeconds;
                case OpeningPhase.GazePilot: return _settings.GazePilotSeconds;
                case OpeningPhase.GazePeak: return _settings.GazePeakSeconds;
                case OpeningPhase.GazeEarth: return _settings.GazeEarthSeconds;
                case OpeningPhase.HurryLid: return _settings.HurryLidSeconds;
                case OpeningPhase.PullOut: return Hurried ? _settings.HurryPullOutSeconds : _settings.PullOutSeconds;
                case OpeningPhase.BlinkShot: return _settings.BlinkShotSeconds;
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
                case OpeningPhase.PushIn:
                    Publish(WakeBeat.PushedIn);
                    Enter(OpeningPhase.Crackle);
                    break;
                case OpeningPhase.Crackle:
                    Enter(OpeningPhase.LidCrack);
                    break;
                case OpeningPhase.LidCrack:
                    Enter(OpeningPhase.LidHalf);
                    break;
                case OpeningPhase.LidHalf:
                    Enter(OpeningPhase.Hunt);
                    break;
                case OpeningPhase.Hunt:
                    Enter(OpeningPhase.Flicker);
                    break;
                case OpeningPhase.Flicker:
                    Enter(NextAttention(Home));
                    break;
                case OpeningPhase.GazeHome:
                    Enter(NextAttention(Pilot));
                    break;
                case OpeningPhase.GazePilot:
                    Enter(NextAttention(Peak));
                    break;
                case OpeningPhase.GazePeak:
                    Enter(OpeningPhase.GazeEarth);
                    break;
                case OpeningPhase.GazeEarth:
                case OpeningPhase.HurryLid:
                    Enter(OpeningPhase.PullOut);
                    break;
                case OpeningPhase.PullOut:
                    if (Hurried)
                    {
                        Finish();
                    }
                    else
                    {
                        Enter(OpeningPhase.BlinkShot);
                    }

                    break;
                case OpeningPhase.BlinkShot:
                    Enter(OpeningPhase.OverShoulder);
                    break;
                case OpeningPhase.OverShoulder:
                    Enter(OpeningPhase.ChaseEase);
                    break;
                default:
                    Finish();
                    break;
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
            Publish(WakeBeat.ChaseFrame);
        }

        /// <summary>The first target in the attention list from <paramref name="from"/> on that is not yet seen.</summary>
        private OpeningPhase NextAttention(int from)
        {
            for (int i = from; i < Earth; i++)
            {
                if (!_seen[i])
                {
                    return OpeningPhase.GazeHome + i;
                }
            }

            return OpeningPhase.GazeEarth;
        }

        private void ResumeAttention()
        {
            OpeningPhase next = NextAttention(Home);
            if (next != Phase)
            {
                Enter(next);
            }
        }

        /// <summary>A target counts as seen when the player's own look passes over it (the script's own looks count as
        /// they begin).</summary>
        private void TrackSeen(bool playerSteering)
        {
            if (!AcceptsLook || !playerSteering)
            {
                return;
            }

            for (int i = 0; i < TargetCount; i++)
            {
                _seen[i] |= _gaze.AngleTo(_targets[i].x, _targets[i].y) < _settings.SeenAngle;
            }
        }

        /// <summary>The player held Earth in view on their own: skip ahead to O9.</summary>
        private void CheckEarthFound(bool playerSteering, float deltaTime)
        {
            if (!AcceptsLook || Phase == OpeningPhase.GazeEarth || !playerSteering)
            {
                _earthHeld = 0f;
                return;
            }

            Vector2 earth = _targets[Earth];
            bool inView = _gaze.AngleTo(earth.x, earth.y) < _settings.EarthFoundAngle;
            _earthHeld = inView ? _earthHeld + deltaTime : 0f;
            if (_earthHeld >= _settings.EarthFoundSeconds)
            {
                _earthHeld = 0f;
                Enter(OpeningPhase.GazeEarth);
            }
        }

        private void AimScripted()
        {
            switch (Phase)
            {
                case OpeningPhase.PushIn:
                case OpeningPhase.Crackle:
                case OpeningPhase.LidCrack:
                case OpeningPhase.LidHalf:
                case OpeningPhase.Hunt:
                case OpeningPhase.Flicker:
                    _gaze.Aim(0f, _settings.BowedPitch);
                    break;
                case OpeningPhase.GazeHome:
                    Vector2 home = _targets[Home];
                    float unsure = _phaseTime >= _settings.HesitateAfter
                        ? -Mathf.Sign(home.x) * _settings.HesitateYaw
                        : 0f;
                    _gaze.Aim(home.x + unsure, home.y);
                    break;
                case OpeningPhase.GazePilot:
                    _gaze.Aim(_targets[Pilot].x, _targets[Pilot].y);
                    break;
                case OpeningPhase.GazePeak:
                    _gaze.Aim(_targets[Peak].x, _targets[Peak].y + _settings.PeakLift);
                    break;
                case OpeningPhase.GazeEarth:
                case OpeningPhase.PullOut:
                case OpeningPhase.BlinkShot:
                case OpeningPhase.OverShoulder:
                    if (!Hurried)
                    {
                        _gaze.Aim(_targets[Earth].x, _targets[Earth].y - _settings.EarthBelow);
                    }

                    break;
            }
        }

        /// <summary>Drive input: hurry whatever is left (once). The blink is never skipped.</summary>
        private void Hurry()
        {
            if (Hurried)
            {
                return;
            }

            switch (Phase)
            {
                case OpeningPhase.Crackle:
                case OpeningPhase.LidCrack:
                case OpeningPhase.LidHalf:
                case OpeningPhase.Hunt:
                case OpeningPhase.Flicker:
                case OpeningPhase.GazeHome:
                case OpeningPhase.GazePilot:
                case OpeningPhase.GazePeak:
                case OpeningPhase.GazeEarth:
                    Hurried = true;
                    WantsRigEase = true;
                    Enter(OpeningPhase.HurryLid);
                    break;
                case OpeningPhase.PullOut:
                    Hurried = true;
                    WantsRigEase = true;
                    float remaining = Mathf.Min(_phaseLength - _phaseTime, _settings.HurryPullOutSeconds);
                    if (!_blinked)
                    {
                        StartBlink();
                        remaining = Mathf.Max(remaining, _settings.BlinkSeconds);
                    }

                    _phaseLength = _phaseTime + remaining;
                    break;
                case OpeningPhase.BlinkShot:
                case OpeningPhase.OverShoulder:
                    Hurried = true;
                    if (!_blinked)
                    {
                        StartBlink();
                    }

                    Enter(OpeningPhase.ChaseEase);
                    break;
                case OpeningPhase.ChaseEase:
                    Hurried = true;
                    break;
            }
        }

        /// <summary>07's blink; the opening's (not the short wake's) brings the head tilt with it.</summary>
        private void StartBlink()
        {
            _blinked = true;
            _blinkClock = 0f;
            if (Phase == OpeningPhase.ShortWake)
            {
                return;
            }

            _tiltClock = 0f;
            Publish(WakeBeat.Blinked);
        }

        private float TiltSeconds => 2f * _settings.TiltEaseSeconds + _settings.TiltHoldSeconds;

        private void StepClocks(float deltaTime)
        {
            if (_speckClock >= 0f)
            {
                _speckClock += deltaTime * (Hurried ? HurriedSpeckRate : 1f);
            }

            if (_blinkClock >= 0f)
            {
                _blinkClock += deltaTime;
            }

            if (_tiltClock >= 0f)
            {
                _tiltClock += deltaTime;
            }

            if (_blinked)
            {
                return;
            }

            bool hurriedMidway = Phase == OpeningPhase.PullOut && Hurried && _phaseTime >= 0.5f * _phaseLength;
            bool onCue = Phase == OpeningPhase.BlinkShot && _phaseTime >= _settings.BlinkDelay;
            if (hurriedMidway || onCue)
            {
                StartBlink();
            }

            if (Phase == OpeningPhase.ShortWake && _phaseTime >= _settings.ShortLidSeconds)
            {
                StartBlink();
                _glanceClock = 0f;
            }
        }

        private void Evaluate()
        {
            HeadTilt = Tilt();
            if (Phase == OpeningPhase.Idle)
            {
                return;
            }

            float t = _phaseTime;
            float rise = _settings.LidRiseSeconds;
            float lid = _lidStart;
            float focus = _settings.FocusSettle;
            float sleep = Mathf.Lerp(_sleepStart, 0f, Smoothing.SmoothStep(0f, rise, t));
            Flicker01 = 0f;
            Warmth01 = 0f;
            BodyLidWeight = 1f;
            GazeHeld = InLens || (!Hurried && Phase >= OpeningPhase.PullOut && Phase <= OpeningPhase.OverShoulder);
            LensActive = InLens;
            switch (Phase)
            {
                case OpeningPhase.PushIn:
                    lid = 1f - Smoothing.SmoothStep(_settings.PushDarkFrom, 1f, t / _phaseLength);
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
                case OpeningPhase.Flicker:
                case OpeningPhase.GazeHome:
                case OpeningPhase.GazePilot:
                case OpeningPhase.GazePeak:
                    lid = Mathf.Lerp(_lidStart, _settings.LidRest, Smoothing.SmoothStep(0f, _settings.LidSettleSeconds, t));
                    focus = LogLerp(_focusStart, _settings.FocusSettle, Smoothing.SmoothStep(0f, rise, t));
                    sleep = Mathf.Lerp(_sleepStart, 0f,
                        Smoothing.SmoothStep(0f, _settings.FlickerSeconds + _settings.LidSettleSeconds, t));
                    if (Phase == OpeningPhase.Flicker)
                    {
                        Flicker01 = Dips(t);
                        Warmth01 = Bump(t, 0f, _phaseLength);
                    }
                    else if (Phase == OpeningPhase.GazePeak)
                    {
                        focus = Rehunt(t);
                    }

                    break;
                case OpeningPhase.GazeEarth:
                    lid = Mathf.Lerp(_lidStart, _settings.LidWonder, Smoothing.SmoothStep(0f, rise, t));
                    focus = LogLerp(_focusStart, _settings.FocusSettle, Smoothing.SmoothStep(0f, rise, t));
                    break;
                case OpeningPhase.HurryLid:
                    float hurry = Smoothing.SmoothStep(0f, _phaseLength, t);
                    lid = Mathf.Lerp(_lidStart, _settings.LidRest, hurry);
                    focus = LogLerp(_focusStart, _settings.FocusSettle, hurry);
                    sleep = Mathf.Lerp(_sleepStart, 0f, hurry);
                    break;
                case OpeningPhase.PullOut:
                    LensActive = t < _settings.LensReleaseAt * _phaseLength;
                    break;
                case OpeningPhase.BlinkShot:
                    lid = Mathf.Lerp(_lidStart, _settings.BlinkAfter,
                        Smoothing.SmoothStep(0f, _settings.BlinkDelay + _settings.BlinkSeconds, t));
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
            FocusDistance = focus;
            Sleep = sleep;
            SpeckOffset = _speckClock < 0f
                ? (Phase == OpeningPhase.PushIn ? _settings.SpeckEnd : _settings.SpeckStart)
                : Vector2.Lerp(_settings.SpeckStart, _settings.SpeckEnd, Smoothing.SmoothStep(0f, _speckSpan, _speckClock));
            if (Phase == OpeningPhase.GazePeak && !_farFocusLanded && t >= _settings.RehuntSeconds)
            {
                _farFocusLanded = true;
                Publish(WakeBeat.FarFocusSettled);
            }
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

        /// <summary>The O5 dips: two soft raised-cosine bumps, the second a little shallower.</summary>
        private float Dips(float t)
        {
            float dip = _settings.DipSeconds;
            return Mathf.Max(Bump(t, 0f, dip), _settings.SecondDip * Bump(t, dip + _settings.DipGap, dip));
        }

        private static float Bump(float t, float start, float length)
        {
            float u = (t - start) / length;
            if (u <= 0f || u >= 1f)
            {
                return 0f;
            }

            float s = Mathf.Sin(Mathf.PI * u);
            return s * s;
        }

        private float Rehunt(float t)
        {
            float u = t / _settings.RehuntSeconds;
            if (u >= 1f)
            {
                return _settings.FocusSettle;
            }

            return u < 0.5f
                ? LogLerp(_focusStart, _settings.RehuntNear, Smoothing.SmoothStep(0f, 0.5f, u))
                : LogLerp(_settings.RehuntNear, _settings.FocusSettle, Smoothing.SmoothStep(0.5f, 1f, u));
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

        private float Tilt()
        {
            if (_tiltClock < 0f)
            {
                return 0f;
            }

            float ease = _settings.TiltEaseSeconds;
            float hold = _settings.TiltHoldSeconds;
            return _settings.BlinkTilt * Smoothing.SmoothStep(0f, ease, _tiltClock)
                * (1f - Smoothing.SmoothStep(ease + hold, 2f * ease + hold, _tiltClock));
        }
    }
}
