using System;
using UnityEngine;
using Unity.Cinemachine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's wake through its own eye and the camera side of the title (docs/design/cutscenes.md §1, docs/features/M3-09
    /// Part A), the thin adapter around <see cref="OpeningScript"/>.
    /// <list type="bullet">
    /// <item>While the title is up (<see cref="IGameFlow"/>, <see cref="TitleShown"/>) 07 stays asleep. A fresh title
    /// holds the rig's lens shot on the closed lens, drifting after a while; over a save, the rig holds the wide shot
    /// of home.</item>
    /// <item><see cref="SessionStarted"/> with a fresh game plays the full opening once: from the title the lens camera
    /// pushes into the closed lens, and that black is O1. Inside the lens the camera sits at 07's lens socket and turns
    /// with the gaze, the picture goes through <see cref="ILensView"/>, and 07's head follows; then the camera leaves
    /// the lens toward the rig's lens shot (or straight to the chase frame when hurried) and the rig carries on.</item>
    /// <item>A loaded game plays the short wake: the title's frame eases back to the chase frame while 07's lid opens
    /// with one blink and it glances at Earth.</item>
    /// <item>In a game without a title (<see cref="IGameFlow.ShowsTitle"/> off: dev fixtures and test runs that drive
    /// 07 from the first frame) a session plays neither: 07 wakes on its own as it always did.</item>
    /// </list>
    /// Publishes <see cref="RoverWakeBeatReached"/> for every beat and holds 07 still (<see cref="IRoverRig"/>) only
    /// while the script says control is held. Pause freezes it. Without an <see cref="IGameFlow"/> (fixtures that drive
    /// 07 from the first frame) it stays out of the way and 07 wakes on its own as before.
    /// </summary>
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public sealed class OpeningDirector : MonoBehaviour, IGameSystem
    {
        /// <summary>The gaze request points this far (m) along the gaze: far enough to read as a direction.</summary>
        private const float GazeReach = 200f;

        /// <summary>Drive input below this (stick magnitude) is drift, not the player driving.</summary>
        private const float DriveEpsilon = 0.05f;

        /// <summary>Phase length or probe distance below this is treated as zero.</summary>
        private const float LengthEpsilon = 1e-4f;

        [Tooltip("Camera tuning (Assets/_Project/Data/Tuning/RoverCameraTuning.asset); the opening is its Opening block.")]
        [SerializeField] private RoverCameraTuning _tuning;

        [Tooltip("The chase camera rig: its lens shot, its title frames and its own Cinemachine camera.")]
        [SerializeField] private RoverCameraRig _rig;

        [Tooltip("07 itself: the drive it is asked for hurries the opening, even while it is held still.")]
        [SerializeField] private RoverController _controller;

        [Tooltip("07's body language: the wake's sleep, lid, head tilt, wing sigh and call sign.")]
        [SerializeField] private RoverBodyLanguage _body;

        [Tooltip("The lens camera: a Cinemachine camera with no body or aim, placed by this each frame it is live.")]
        [SerializeField] private CinemachineCamera _lensCamera;

        [Tooltip("The opening's lander (the first gaze target).")]
        [SerializeField] private Transform _lander;

        [Tooltip("The beacon on the lander's hull (the second gaze target).")]
        [SerializeField] private Transform _pilot;

        private readonly object _owner = new object();
        private readonly FaceInspectSightLine _sight = new FaceInspectSightLine();
        private OpeningScript _script;
        private LookSettings _look;
        private GameContext _context;
        private EventBus _events;
        private IRoverState _rover;
        private IRoverRig _roverRig;
        private IWorldLayout _world;
        private ILensView _lens;
        private ILookSettings _lookSettings;
        private IGameFlow _flow;
        private IDisposable[] _subscriptions;
        private Vector3 _pushFrom;
        private Quaternion _pushFromRotation;
        private float _pushFromFov;
        private Vector3 _pullOutFromOrigin;
        private Vector3 _pullOutFromEye;
        private Quaternion _pullOutFromGaze;
        private bool _hasPullOutFrom;
        private bool _titleHasSave;
        private bool _inTitle;
        private bool _paused;
        private bool _lensApplied;
        private bool _lensMissing;
        private bool _gazing;
        private bool _holding;
        private bool _tilting;
        private bool _blinkedInWake;
        private bool _initialized;

        /// <summary>The script, for tests and captures.</summary>
        public OpeningScript Script => _script;

        /// <summary>True while the lens camera has the view (pushing in, inside the lens, leaving it).</summary>
        public bool LensCameraLive => _lensCamera != null && _lensCamera.enabled;

        private Transform ScanLensOrigin => _roverRig.ScanOrigin != null ? _roverRig.ScanOrigin : _roverRig.TetherOrigin;

        public void Initialize(GameContext context)
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return;
            }

            _context = context;
            _events = context.Events;
            _rover = context.Get<IRoverState>();
            _roverRig = context.Get<IRoverRig>();
            _world = context.Get<IWorldLayout>();
            if (context.TryGet(out ITerrainQuery terrain))
            {
                _sight.SetTerrain(terrain);
            }

            context.TryGet(out _lookSettings);
            _script = new OpeningScript(_tuning);
            _look = new LookSettings(_tuning);
            _rig.SetWakeBody(_body);
            OpeningSettings settings = _tuning.Opening;
            CinemachineCamera chase = _rig.ChaseCamera;
            chase.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Always;
            _lensCamera.Priority = chase.Priority.Value + 1;
            LensSettings lens = _lensCamera.Lens;
            lens.NearClipPlane = settings.LensNearClip;
            lens.FieldOfView = settings.LensFov;
            _lensCamera.Lens = lens;
            _lensCamera.enabled = false;

            if (context.TryGet(out _flow))
            {
                _body.HoldAsleep();
                _inTitle = _flow.State == GameFlowState.Title;
            }

            _subscriptions = new[]
            {
                _events.Subscribe<TitleShown>(OnTitleShown),
                _events.Subscribe<SessionStarted>(OnSessionStarted),
                _events.Subscribe<NewGameRequested>(OnNewGameRequested),
                _events.Subscribe<PauseChanged>(OnPauseChanged),
            };
            _initialized = true;
        }

        private bool ValidateWiring()
        {
            bool ok = _tuning != null && _rig != null && _controller != null && _body != null && _lensCamera != null && _lander != null
                && _pilot != null;
            if (!ok)
            {
                Debug.LogError($"{nameof(OpeningDirector)}: tuning, rig, rover, body, lens camera or a gaze target " +
                    "(lander, pilot) is not assigned; rebuild the Main scene.", this);
            }

            return ok;
        }

        private void OnTitleShown(TitleShown shown)
        {
            _inTitle = true;
            _titleHasSave = shown.HasSave;
            _body.HoldAsleep();
            if (shown.HasSave)
            {
                _rig.HoldTitleFrame();
            }
            else
            {
                _rig.BeginWakeOpening(true);
            }
        }

        private void OnSessionStarted(SessionStarted started)
        {
            bool fromLensShot = _inTitle && _rig.Opening.IsHeld;
            _inTitle = false;
            if (_flow == null || !_flow.ShowsTitle)
            {
                _body.WakeOnItsOwn();
                return;
            }

            if (started.FreshGame)
            {
                _body.HoldOpeningScreenDark(true);
                BeginFull(fromLensShot);
                return;
            }

            _body.HoldOpeningScreenDark(false);
            _rig.ReleaseTitleFrame();
            _script.BeginShort();
            _body.BeginWake();
        }

        /// <summary>
        /// New Game that App answers with a reload (over a save, from the title or from play): the camera moves to
        /// the lens shot and pushes into the closed lens; App puts the save away and reloads under the black
        /// (<see cref="WakeBeat.PushedIn"/>). Over a fresh title App starts the session itself right after this, and
        /// that begins the opening instead.
        /// </summary>
        private void OnNewGameRequested(NewGameRequested request)
        {
            bool freshTitle = _inTitle && !_titleHasSave;
            if (_flow == null || !_flow.ShowsTitle || freshTitle || _script.Phase == OpeningPhase.PushIn)
            {
                return;
            }

            _script.Stop();
            _rig.ReleaseTitleFrame();
            BeginFull(true);
        }

        private void OnPauseChanged(PauseChanged pause)
        {
            _paused = pause.Paused;
        }

        /// <summary>
        /// The full opening. The rig holds its face-inspect wake shot underneath from the start, which keeps its wide
        /// shot and stargazing closed and is the frame the pull-out lands on.
        /// </summary>
        private void BeginFull(bool pushIn)
        {
            _blinkedInWake = false;
            _body.HoldOpeningScreenDark(true);
            SetTargets();
            _script.BeginFull(pushIn);
            _body.PoseOpeningHead(_script.Gaze.Yaw, _script.Gaze.Pitch, 0f, 1f);
            _rig.BeginWakeOpening(true);
            if (pushIn)
            {
                CameraState state = _rig.ChaseCamera.State;
                _pushFrom = state.GetFinalPosition();
                _pushFromRotation = state.GetFinalOrientation();
                _pushFromFov = state.Lens.FieldOfView;
            }
        }

        /// <summary>The gaze targets as (yaw, pitch) from 07's heading at its scan lens, fixed as the opening begins.</summary>
        private void SetTargets()
        {
            OpeningSettings settings = _tuning.Opening;
            Vector3 lens = ScanLensOrigin.position;
            Quaternion toLocal = Quaternion.Inverse(Heading());
            Vector3 lander = _lander.position + Vector3.up * settings.LanderLookHeight;
            Vector3 beacon = _world.PeakPosition + Vector3.up * settings.BeaconHeight;
            _script.SetTargets(LensGaze.Angles(toLocal * (lander - lens)), LensGaze.Angles(toLocal * (_pilot.position - lens)),
                LensGaze.Angles(toLocal * (beacon - lens)), LensGaze.Angles(toLocal * _world.EarthDirection));
        }

        private Quaternion Heading()
        {
            Vector3 forward = _rover.Rotation * Vector3.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(forward) : Quaternion.identity;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (!_initialized || _paused || deltaTime <= 0f)
            {
                return;
            }

            if (_inTitle)
            {
                _body.HoldAsleep();
            }

            bool drive = _controller.RequestedDrive.sqrMagnitude > DriveEpsilon * DriveEpsilon;
            _script.Step(deltaTime, ReadLook(deltaTime), drive);
            TakeBeats();
            if (_script.WantsRigEase && _rig.Opening.IsActive && !_rig.Opening.IsEasing)
            {
                _rig.Opening.EaseOut();
            }

            HoldControl(_script.ControlHeld);
            PoseBody();
            AimHead();
            PlaceLensCamera();
            ApplyLens();
        }

        /// <summary>The player's look this frame as gaze degrees (x right, y up), with their sensitivity and invert.</summary>
        private Vector2 ReadLook(float deltaTime)
        {
            if (_lookSettings != null)
            {
                _look.GentleCamera = _lookSettings.GentleCamera;
                _look.Sensitivity = _lookSettings.Sensitivity;
                _look.InvertY = _lookSettings.InvertY;
            }

            Vector2 orbit = _look.OrbitDegrees(_context.Input.LookDelta, _context.Input.LookRate, deltaTime);
            return new Vector2(orbit.x, -orbit.y);
        }

        private void TakeBeats()
        {
            while (_script.TryTakeBeat(out WakeBeat beat))
            {
                switch (beat)
                {
                    case WakeBeat.Crackle:
                        _blinkedInWake = false;
                        _hasPullOutFrom = false;
                        _body.BeginWake();
                        break;
                    case WakeBeat.PullingOut:
                        CapturePullOutStart();
                        _body.PoseWake(_script.Sleep, _script.BodyLid, _script.BodyLidWeight, _script.HeadTilt);
                        _rig.RefreshWakeFaceShot();
                        _rig.SyncWakeFacePose();
                        break;
                    case WakeBeat.PulledOut:
                        _rig.RefreshWakeFaceShot();
                        break;
                    case WakeBeat.Blinked:
                        _blinkedInWake = true;
                        _body.GreetWithBlink(_tuning.Opening.BlinkSigh);
                        break;
                    case WakeBeat.OverShoulder:
                        _rig.Opening.StartTurn();
                        break;
                    case WakeBeat.ChaseFrame:
                        _body.FinishWake();
                        break;
                }

                _events.Publish(new RoverWakeBeatReached(beat, _script.Hurried));
            }
        }

        private void CapturePullOutStart()
        {
            OpeningSettings settings = _tuning.Opening;
            _pullOutFromOrigin = ScanLensOrigin.position;
            _pullOutFromGaze = Heading() * Quaternion.Euler(-_script.Gaze.Pitch, _script.Gaze.Yaw, 0f);
            Vector3 gazeForward = _pullOutFromGaze * Vector3.forward;
            Vector3 desiredEye = _pullOutFromOrigin + gazeForward * settings.LensForward;
            float clearEyeDist = _sight.Clear(_pullOutFromOrigin, desiredEye, _tuning.FaceInspectProbeRadius);
            _pullOutFromEye = _pullOutFromOrigin + gazeForward * clearEyeDist;
            _hasPullOutFrom = true;
        }

        private void HoldControl(bool hold)
        {
            if (hold == _holding)
            {
                return;
            }

            _holding = hold;
            _roverRig.SetHoldStill(_owner, hold);
        }

        private void PoseBody()
        {
            if (_script.IsRunning)
            {
                _body.PoseWake(_script.Sleep, _script.BodyLid, _script.BodyLidWeight, _script.HeadTilt);
                OpeningPhase phase = _script.Phase;
                if (phase == OpeningPhase.PushIn || _script.InLens)
                {
                    _body.PoseOpeningHead(_script.Gaze.Yaw, _script.Gaze.Pitch, 0f, 1f);
                }
                else if (phase == OpeningPhase.PullOut)
                {
                    float u = _script.PhaseLength > LengthEpsilon
                        ? Mathf.Clamp01(_script.PhaseTime / _script.PhaseLength)
                        : 1f;
                    float pullT = WakeOpening.SmoothStepC2(0f, 1f, u);
                    if (_script.Hurried)
                    {
                        _body.PoseOpeningHead(_script.Gaze.Yaw, _script.Gaze.Pitch, 0f, 1f - pullT);
                    }
                    else
                    {
                        _body.PoseOpeningHead(
                            Mathf.Lerp(_script.Gaze.Yaw, _rig.WakeFaceNeckYaw, pullT),
                            Mathf.Lerp(_script.Gaze.Pitch, _tuning.FaceInspectHeadPitch, pullT),
                            0f,
                            1f);
                    }
                }
                else if (phase == OpeningPhase.BlinkShot)
                {
                    Vector2 earthGaze = LensGaze.Angles(Quaternion.Inverse(Heading()) * _world.EarthDirection);
                    float tiltShare = _tuning.Opening.BlinkTilt > LengthEpsilon
                        ? Mathf.Clamp01(Mathf.Abs(_script.HeadTilt) / _tuning.Opening.BlinkTilt)
                        : 0f;
                    float curiousYaw = _rig.WakeFaceNeckYaw + Mathf.Clamp(earthGaze.x * 0.18f, -10f, 10f) * tiltShare;
                    float curiousPitch = _tuning.FaceInspectHeadPitch + Mathf.Clamp(earthGaze.y * 0.12f, -3f, 6f) * tiltShare;
                    _body.PoseOpeningHead(curiousYaw, curiousPitch, _script.HeadTilt, 1f);
                }
                else if (phase == OpeningPhase.OverShoulder)
                {
                    float holdBlend = 1f - _rig.Opening.Turn;
                    _body.PoseOpeningHead(_rig.WakeFaceNeckYaw, _tuning.FaceInspectHeadPitch, _script.HeadTilt, holdBlend);
                }
                else
                {
                    _body.PoseOpeningHead(0f, 0f, 0f, 0f);
                }

                _tilting = true;
            }
            else if (_tilting)
            {
                _body.PoseWake(0f, 0f, 0f, _script.HeadTilt);
                _body.PoseOpeningHead(0f, 0f, 0f, 0f);
                _tilting = _script.TiltActive;
            }
        }

        private void AimHead()
        {
            Vector3 lens = ScanLensOrigin.position;
            bool gaze = _script.GazeHeld || _script.GlancingEarth;
            if (gaze)
            {
                Vector3 direction;
                if (_script.GazeHeld)
                {
                    direction = Heading() * LensGaze.Direction(_script.Gaze.Yaw, _script.Gaze.Pitch);
                }
                else if (_script.Phase == OpeningPhase.BlinkShot && !_blinkedInWake)
                {
                    direction = Heading() * Vector3.forward;
                }
                else
                {
                    direction = _world.EarthDirection;
                }

                _roverRig.SetGazeTarget(_owner, lens + direction * GazeReach, _tuning.Opening.GazePriority);
            }
            else if (_gazing)
            {
                _roverRig.ClearGazeTarget(_owner);
            }

            _gazing = gaze;
        }

        private void PlaceLensCamera()
        {
            OpeningPhase phase = _script.Phase;
            bool live = phase == OpeningPhase.PushIn || _script.InLens || phase == OpeningPhase.PullOut;
            if (!live)
            {
                if (_lensCamera.enabled)
                {
                    _lensCamera.enabled = false;
                }

                return;
            }

            OpeningSettings settings = _tuning.Opening;
            Vector3 lensOrigin = ScanLensOrigin.position;
            Quaternion gaze = Heading() * Quaternion.Euler(-_script.Gaze.Pitch, _script.Gaze.Yaw, 0f);
            Vector3 gazeForward = gaze * Vector3.forward;
            Vector3 desiredEye = lensOrigin + gazeForward * settings.LensForward;
            float clearEyeDist = _sight.Clear(lensOrigin, desiredEye, _tuning.FaceInspectProbeRadius);
            Vector3 eye = lensOrigin + gazeForward * clearEyeDist;
            Vector3 position = eye;
            Quaternion rotation = gaze;
            float fov = settings.LensFov;
            float nearClip = settings.LensNearClip;
            if (phase == OpeningPhase.PushIn)
            {
                float u = _script.PhaseLength > LengthEpsilon
                    ? Mathf.Clamp01(_script.PhaseTime / _script.PhaseLength)
                    : 1f;
                float t = WakeOpening.SmoothStepC2(0f, 1f, u);
                Vector3 desiredPos = Vector3.Lerp(_pushFrom, eye, t);
                Vector3 stepDir = desiredPos - _pushFrom;
                float stepDist = stepDir.magnitude;
                if (stepDist > LengthEpsilon)
                {
                    float clearStep = _sight.Clear(_pushFrom, desiredPos, _tuning.FaceInspectProbeRadius);
                    position = _pushFrom + (stepDir / stepDist) * clearStep;
                }
                else
                {
                    position = desiredPos;
                }

                Vector3 lookDir = lensOrigin - _pushFrom;
                Quaternion lookToLens = lookDir.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(lookDir) : _pushFromRotation;
                rotation = Quaternion.Slerp(_pushFromRotation, lookToLens, t);
                fov = Mathf.Lerp(_pushFromFov, settings.LensFov, t);
                nearClip = Mathf.Lerp(_tuning.FaceInspectNearClip, settings.LensNearClip, t);
            }
            else if (phase == OpeningPhase.PullOut)
            {
                if (!_hasPullOutFrom)
                {
                    CapturePullOutStart();
                }

                float u = _script.PhaseLength > LengthEpsilon
                    ? Mathf.Clamp01(_script.PhaseTime / _script.PhaseLength)
                    : 1f;
                float t = WakeOpening.SmoothStepC2(0f, 1f, u);
                Vector3 startEye = _pullOutFromEye;
                Vector3 startOrigin = _pullOutFromOrigin;
                Quaternion startGaze = _pullOutFromGaze;
                Vector3 startForward = startGaze * Vector3.forward;

                CameraState chase = _rig.ChaseCamera.State;
                FaceInspectShot wakeShot = default;
                bool hasWakeShot = !_script.Hurried && _rig.TryGetWakeFaceShot(out wakeShot);
                Vector3 end = hasWakeShot ? wakeShot.Eye : chase.GetFinalPosition();
                Quaternion endRot = hasWakeShot
                    ? Quaternion.LookRotation(wakeShot.Focus - wakeShot.Eye, Vector3.up)
                    : chase.GetFinalOrientation();
                float endFov = hasWakeShot ? wakeShot.VerticalFov : chase.Lens.FieldOfView;
                float endNearClip = hasWakeShot ? _tuning.FaceInspectNearClip : chase.Lens.NearClipPlane;

                Vector3 frontDir = _script.Hurried
                    ? (startForward + Vector3.up * settings.HurryPullOutUp).normalized
                    : (startForward + (Heading() * Vector3.forward)).normalized;
                Vector3 reachAnchor = _script.Hurried
                    ? startEye
                    : Vector3.Lerp(startEye, end, settings.PullOutReachAnchor);
                Vector3 reach = reachAnchor + frontDir * settings.PullOutReach;
                Vector3 desiredPos = Vector3.Lerp(Vector3.Lerp(startEye, reach, t), Vector3.Lerp(reach, end, t), t);
                Vector3 probeOrigin = hasWakeShot
                    ? Vector3.Lerp(startOrigin, wakeShot.Focus, t)
                    : startOrigin;
                Vector3 probeVec = desiredPos - probeOrigin;
                float probeDist = probeVec.magnitude;
                if (probeDist > LengthEpsilon)
                {
                    float clearDist = _sight.Clear(probeOrigin, desiredPos, _tuning.FaceInspectProbeRadius);
                    position = probeOrigin + (probeVec / probeDist) * clearDist;
                }
                else
                {
                    position = desiredPos;
                }

                Quaternion earth = Quaternion.LookRotation(_world.EarthDirection);
                Quaternion held = Quaternion.Slerp(startGaze, earth,
                    WakeOpening.SmoothStepC2(0f, settings.PullOutOntoEarth, u));
                float turnStart = settings.PullOutTurnStartShare * settings.PullOutOntoEarth;
                float turn = WakeOpening.SmoothStepC2(turnStart, 1f, u);
                rotation = Quaternion.Slerp(held, endRot, turn);
                fov = Mathf.Lerp(settings.LensFov, endFov, t);
                nearClip = Mathf.Lerp(settings.LensNearClip, endNearClip, t);
            }

            _lensCamera.transform.SetPositionAndRotation(position, rotation);
            LensSettings lensSettings = _lensCamera.Lens;
            lensSettings.FieldOfView = fov;
            lensSettings.NearClipPlane = nearClip;
            _lensCamera.Lens = lensSettings;
            if (!_lensCamera.enabled)
            {
                _lensCamera.enabled = true;
                _lensCamera.PreviousStateIsValid = false;
            }
        }

        private void ApplyLens()
        {
            if (!_script.LensActive)
            {
                if (_lensApplied)
                {
                    _lens.Release();
                    _lensApplied = false;
                }

                return;
            }

            if (_lens == null && !_context.TryGet(out _lens))
            {
                if (!_lensMissing)
                {
                    _lensMissing = true;
                    Debug.LogError($"{nameof(OpeningDirector)}: no ILensView is registered, so the opening plays " +
                        "without 07's lid and focus. Rebuild the Main scene (World's lens view).", this);
                }

                return;
            }

            LensViewState state = _script.LensState;
            OpeningPhase phase = _script.Phase;
            if (phase == OpeningPhase.PushIn || phase == OpeningPhase.PullOut)
            {
                float toLens = Vector3.Distance(_lensCamera.transform.position, ScanLensOrigin.position);
                state = new LensViewState(state.Lid01, toLens, state.Flicker01, state.Warmth01, state.SpeckOffset,
                    state.Readout01);
            }

            _lens.Apply(state);
            _lensApplied = true;
        }

        private void OnDestroy()
        {
            if (_subscriptions == null)
            {
                return;
            }

            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}
