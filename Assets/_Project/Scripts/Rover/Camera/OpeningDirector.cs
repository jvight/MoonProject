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
    /// of 07 where it was left (<see cref="TitleFrame"/>).</item>
    /// <item><see cref="SessionStarted"/> with a fresh game plays the full opening once: from the title the picture
    /// fades to black where the camera is (no camera move), and the wake starts from that black as O1. Inside the lens the camera sits at 07's lens socket and turns
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

        /// <summary>Frames the brain keeps its cut blend after the lens camera goes live.</summary>
        private const int CutFrameCount = 2;

        /// <summary>The head turns this far (deg) at most toward the beacon, and lifts this far.</summary>
        /// <summary>The camera looks from, and its terrain probe starts at, this height (m) above 07's ground point.</summary>
        private const float ProbeLift = 1f;

        private const float BeaconYawLimit = 70f;
        private const float BeaconPitchLimit = 12f;

        /// <summary>The start's dust shiver shakes the chassis this many times the motor's shudder.</summary>
        private const float ShiverScale = 1.5f;

        /// <summary>Half-life (s) of the antenna's rise and fall.</summary>
        private const float AntennaHalfLife = 0.15f;

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

        [Tooltip("The amber beacon on the lander's hull (the second gaze target: the warm point that leads).")]
        [SerializeField] private Transform _beacon;

        private readonly object _owner = new object();
        private readonly FaceInspectSightLine _sight = new FaceInspectSightLine();
        private bool _wakeWaitsForBlack;
        private float _antennaLift;
        private CinemachineBrain _brain;
        private CinemachineBlendDefinition _savedBlend;
        private int _cutFrames;
        private OpeningScript _script;
        private LampPowerUp _lamps;
        private PostShot _shot;
        private PostLights _lights;
        private PostDust _dust;
        private GameContext _context;
        private EventBus _events;
        private IRoverState _rover;
        private IRoverRig _roverRig;
        private IWorldLayout _world;
        private ILensView _lens;
        private IGameFlow _flow;
        private IDisposable[] _subscriptions;
        private Vector3 _pullOutFromOrigin;
        private Vector3 _pullOutFromEye;
        private Quaternion _pullOutFromGaze;
        private bool _hasPullOutFrom;
        private bool _titleHasSave;
        private bool _inTitle;
        private bool _paused;
        private bool _lensApplied;
        private bool _lensWarmed;
        private bool _lensMissing;
        private bool _gazing;
        private bool _holding;
        private bool _initialized;

        /// <summary>The script, for tests and captures.</summary>
        public OpeningScript Script => _script;

        /// <summary>True while the lens camera has the view (inside the lens, leaving it).</summary>
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

            _script = new OpeningScript(_tuning);
            _rig.SetWakeBody(_body);
            OpeningSettings settings = _tuning.Opening;
            _lamps = _script.Lamps;
            _shot = new PostShot(settings.Post);
            _lights = new PostLights(transform, settings.Post);
            _dust = new PostDust(settings.Post);
            _brain = _rig.Camera != null ? _rig.Camera.GetComponent<CinemachineBrain>() : null;
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
            bool ok = _tuning != null && _rig != null && _controller != null && _body != null && _lensCamera != null && _beacon != null;
            if (!ok)
            {
                Debug.LogError($"{nameof(OpeningDirector)}: tuning, rig, rover, body, lens camera or a gaze target " +
                    "(the lander's beacon) is not assigned; rebuild the Main scene.", this);
            }

            return ok;
        }

        private void OnTitleShown(TitleShown shown)
        {
            _inTitle = true;
            _titleHasSave = shown.HasSave;
            _body.HoldAsleep();
            _lamps.HoldDark();
            WarmLens();
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
            WarmLens();
            _inTitle = false;
            if (_flow == null || !_flow.ShowsTitle)
            {
                _body.WakeOnItsOwn();
                _lamps.Release();
                return;
            }

            _lamps.HoldDark();

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
        /// New Game that App answers with a reload (over a save, from the title or from play): the picture fades to
        /// black where the camera is; App puts the save away and reloads under the black
        /// (<see cref="WakeBeat.FadedOut"/>). Over a fresh title App starts the session itself right after this, and
        /// that begins the opening instead.
        /// </summary>
        private void OnNewGameRequested(NewGameRequested request)
        {
            bool freshTitle = _inTitle && !_titleHasSave;
            if (_flow == null || !_flow.ShowsTitle || freshTitle || _script.Phase == OpeningPhase.FadeOut)
            {
                return;
            }

            _script.Stop();
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
        private void BeginFull(bool fromTitle)
        {
            _body.HoldOpeningScreenDark(true);
            SetTargets();
            _script.BeginFull(fromTitle);
            _wakeWaitsForBlack = fromTitle;
            if (fromTitle)
            {
                _rig.Opening.FreezeDrift();
            }

            if (!fromTitle)
            {
                StartWakeShot();
            }
        }

        /// <summary>
        /// Puts 07's head and the rig's wake shot where O1 needs them. From the title this waits until the picture has
        /// faded to black, so nothing moves in view while the title goes.
        /// </summary>
        private void StartWakeShot()
        {
            _body.PoseOpeningHead(_script.Gaze.Yaw, _script.Gaze.Pitch, 0f, 1f);
            _rig.BeginWakeOpening(true);
        }

        /// <summary>Where Earth is as (yaw, pitch) from 07's heading at its scan lens, fixed as the opening begins.</summary>
        private void SetTargets()
        {
            Quaternion toLocal = Quaternion.Inverse(Heading());
            _script.SetEarthTarget(LensGaze.Angles(toLocal * _world.EarthDirection));
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

            if (_cutFrames > 0 && --_cutFrames == 0)
            {
                _brain.DefaultBlend = _savedBlend;
            }

            _lamps.Step(deltaTime);
            _body.SetLampPower(_lamps.Level);

            _script.Step(deltaTime);
            // The lines first: the carrier is announced before O1's beat, while the screen is still black.
            TakePostEvents();
            TakeBeats();
            if (_script.WantsRigEase && _rig.Opening.IsActive && !_rig.Opening.IsEasing)
            {
                _rig.Opening.EaseOut();
            }

            HoldControl(_script.ControlHeld);
            PoseBody();
            ApplyPostMotion(deltaTime);
            AimHead();
            PlaceLensCamera();
            ApplyLens();
            ApplyPostLights();
            ApplyPostDust();
        }

        private void TakeBeats()
        {
            while (_script.TryTakeBeat(out WakeBeat beat))
            {
                switch (beat)
                {
                    case WakeBeat.Crackle:
                        _hasPullOutFrom = false;
                        _body.BeginWake();
                        if (_wakeWaitsForBlack)
                        {
                            _wakeWaitsForBlack = false;
                            StartWakeShot();
                        }

                        break;
                    case WakeBeat.PullingOut:
                        CapturePullOutStart();
                        _body.PoseWake(_script.Sleep, _script.BodyLid, _script.BodyLidWeight, 0f);
                        _rig.RefreshWakeFaceShot();
                        _rig.SyncWakeFacePose();
                        break;
                    case WakeBeat.PulledOut:
                        _rig.RefreshWakeFaceShot();
                        break;
                    case WakeBeat.OverShoulder:
                        _rig.Opening.StartTurn();
                        break;
                    case WakeBeat.ChaseFrame:
                        _body.FinishWake();
                        break;
                }

                _events.Publish(new RoverWakeBeatReached(beat, false));
            }
        }

        /// <summary>
        /// Publishes the self-test's lines as Core events (UI prints them, Audio voices them) and starts each check's
        /// own motion on 07.
        /// </summary>
        private void TakePostEvents()
        {
            while (_script.TryTakePostEvent(out PostEvent postEvent))
            {
                switch (postEvent.Kind)
                {
                    case PostEventKind.Started:
                        StartCheck(postEvent.Step);
                        _events.Publish(new BootCheckStarted(postEvent.Step));
                        break;
                    case PostEventKind.Completed:
                        _events.Publish(new BootCheckCompleted(postEvent.Step, postEvent.Result));
                        break;
                    case PostEventKind.Pulse:
                        _events.Publish(new BootCheckPulse(postEvent.Step, postEvent.Index, postEvent.Progress01));
                        break;
                    case PostEventKind.Finished:
                        _events.Publish(new BootCheckFinished());
                        break;
                }
            }
        }

        /// <summary>What 07 does as each check begins.</summary>
        private void StartCheck(BootCheckStep step)
        {
            switch (step)
            {
                case BootCheckStep.Lamps:
                    _lamps.Start();
                    break;
                case BootCheckStep.Antenna:
                    _body.GreetWithBlink(_tuning.Opening.BlinkSigh);
                    break;
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
            if (!_script.IsRunning)
            {
                return;
            }

            _body.PoseWake(_script.Sleep, _script.BodyLid, _script.BodyLidWeight, 0f);
            OpeningPhase phase = _script.Phase;
            if (_script.InLens)
            {
                _body.PoseOpeningHead(_script.Gaze.Yaw, _script.Gaze.Pitch, 0f, 1f);
            }
            else if (phase == OpeningPhase.PullOut)
            {
                float u = _script.PhaseLength > LengthEpsilon
                    ? Mathf.Clamp01(_script.PhaseTime / _script.PhaseLength)
                    : 1f;
                float pullT = WakeOpening.SmoothStepC2(0f, 1f, u);
                _body.PoseOpeningHead(
                    Mathf.Lerp(_script.Gaze.Yaw, _rig.WakeFaceNeckYaw, pullT),
                    Mathf.Lerp(_script.Gaze.Pitch, _tuning.FaceInspectHeadPitch, pullT),
                    0f,
                    1f);
            }
            else if (phase == OpeningPhase.Post)
            {
                PoseHeadForCheck();
            }
            else if (phase == OpeningPhase.OverShoulder)
            {
                float holdBlend = 1f - _rig.Opening.Turn;
                _body.PoseOpeningHead(_rig.WakeFaceNeckYaw, _tuning.FaceInspectHeadPitch, 0f, holdBlend);
            }
            else
            {
                _body.PoseOpeningHead(0f, 0f, 0f, 0f);
            }
        }

        /// <summary>The head in the checks: held to the front shot, sweeping and nodding at its check, turning to the beacon at the start.</summary>
        private void PoseHeadForCheck()
        {
            float yaw = _rig.WakeFaceNeckYaw;
            float pitch = _tuning.FaceInspectHeadPitch;
            BootCheckStep step = _script.PostStep;
            float progress = _script.PostProgress01;
            if (step == BootCheckStep.Head)
            {
                Vector2 head = _script.Motions.Head(progress);
                yaw += head.x;
                pitch += head.y;
            }
            else if (step == BootCheckStep.Begin)
            {
                Vector2 beacon = LensGaze.Angles(Quaternion.Inverse(Heading()) * (_beacon.position - ScanLensOrigin.position));
                float turn = PostMotions.BeaconTurn(progress);
                yaw = Mathf.Lerp(yaw, Mathf.Clamp(beacon.x, -BeaconYawLimit, BeaconYawLimit), turn);
                pitch = Mathf.Lerp(pitch, Mathf.Clamp(beacon.y, -BeaconPitchLimit, BeaconPitchLimit), turn);
            }

            _body.PoseOpeningHead(yaw, pitch, 0f, 1f);
        }

        /// <summary>
        /// What each check does on 07 beyond its head: the wheels' twitches, the motor's shudder and lurch, the
        /// cutter's flashes and turret kicks, the wing's jerk, the antenna's rise (held through the later checks and
        /// then lowered) and the start's dust shiver.
        /// </summary>
        private void ApplyPostMotion(float deltaTime)
        {
            var motion = new PostBodyMotion();
            float wing = -1f;
            float turret = 0f;
            float flash = 0f;
            float antennaTarget = 0f;
            float eyeFocus = 0f;
            if (_script.Phase == OpeningPhase.Post)
            {
                PostMotions motions = _script.Motions;
                BootCheckStep step = _script.PostStep;
                float p = _script.PostProgress01;
                switch (step)
                {
                    case BootCheckStep.Lens:
                        eyeFocus = PostMotions.LensFocus(p);
                        break;
                    case BootCheckStep.Wheels:
                        motion.Wheel0 = motions.Wheel(0, p);
                        motion.Wheel1 = motions.Wheel(1, p);
                        motion.Wheel2 = motions.Wheel(2, p);
                        motion.Wheel3 = motions.Wheel(3, p);
                        break;
                    case BootCheckStep.Motor:
                        motion.Shudder = motions.Shudder(p);
                        motion.Lurch = motions.Lurch(p);
                        break;
                    case BootCheckStep.Cutter:
                        turret = motions.Turret(p);
                        flash = motions.CutterFlash(p);
                        break;
                    case BootCheckStep.SolarWing:
                        wing = motions.Wing(p);
                        break;
                    case BootCheckStep.Antenna:
                        antennaTarget = motions.Antenna(p);
                        break;
                    case BootCheckStep.Begin:
                        motion.Shudder = PostMotions.Shiver(p) * _tuning.Opening.Post.ShudderMetres * ShiverScale;
                        break;
                }

                if (step > BootCheckStep.Antenna)
                {
                    antennaTarget = motions.Antenna(1f);
                }
            }

            _antennaLift = Smoothing.Damp(_antennaLift, antennaTarget, AntennaHalfLife, deltaTime);
            _body.SetPostPose(wing, _antennaLift, turret, eyeFocus);
            _body.SetPostMotion(motion);
            _controller.NozzleDevice.SetFlash(flash);
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
            bool live = _script.InLens || phase == OpeningPhase.PullOut || phase == OpeningPhase.Post
                || phase == OpeningPhase.OverShoulder || phase == OpeningPhase.ChaseEase;
            if (!live)
            {
                if (_lensCamera.enabled)
                {
                    CutBlend();
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
            if (phase == OpeningPhase.PullOut)
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

                ShotPose front = _shot.Base(_rover.Position, HeadingDegrees());
                Vector3 end = front.Position;
                Quaternion endRot = front.Rotation;
                float endFov = front.Fov;
                float endNearClip = _tuning.FaceInspectNearClip;

                Vector3 frontDir = (startForward + (Heading() * Vector3.forward)).normalized;
                Vector3 reachAnchor = Vector3.Lerp(startEye, end, settings.PullOutReachAnchor);
                Vector3 reach = reachAnchor + frontDir * settings.PullOutReach;
                Vector3 desiredPos = Vector3.Lerp(Vector3.Lerp(startEye, reach, t), Vector3.Lerp(reach, end, t), t);
                Vector3 probeOrigin = Vector3.Lerp(startOrigin, _rover.Position + Vector3.up * ProbeLift, t);
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

            else if (phase == OpeningPhase.Post || phase == OpeningPhase.OverShoulder || phase == OpeningPhase.ChaseEase)
            {
                ShotPose pose = ChecksShot(phase);
                Vector3 anchor = _rover.Position + Vector3.up * ProbeLift;
                Vector3 toCamera = pose.Position - anchor;
                float length = toCamera.magnitude;
                position = length > LengthEpsilon
                    ? anchor + toCamera / length * _sight.Clear(anchor, pose.Position, _tuning.FaceInspectProbeRadius)
                    : pose.Position;
                rotation = pose.Rotation;
                fov = pose.Fov;
                nearClip = _tuning.FaceInspectNearClip;
            }

            _lensCamera.transform.SetPositionAndRotation(position, rotation);
            LensSettings lensSettings = _lensCamera.Lens;
            lensSettings.FieldOfView = fov;
            lensSettings.NearClipPlane = nearClip;
            _lensCamera.Lens = lensSettings;
            if (!_lensCamera.enabled)
            {
                CutBlend();
                _lensCamera.enabled = true;
                _lensCamera.PreviousStateIsValid = false;
            }
        }

        /// <summary>
        /// The camera's pose after the pull-out: the three-quarter front shot easing toward each part as it is checked;
        /// then over the shoulder toward the beacon, and finally into the chase frame.
        /// </summary>
        private ShotPose ChecksShot(OpeningPhase phase)
        {
            Vector3 body = _rover.Position;
            float heading = HeadingDegrees();
            if (phase == OpeningPhase.Post)
            {
                return _script.PostStep == BootCheckStep.Begin
                    ? _shot.AtBegin(body, heading, _beacon.position, _script.PostProgress01)
                    : _shot.At(body, heading, _script.PostStep, _script.PostProgress01);
            }

            float u = _script.PhaseLength > LengthEpsilon ? Mathf.Clamp01(_script.PhaseTime / _script.PhaseLength) : 1f;
            float t = WakeOpening.SmoothStepC2(0f, 1f, u);
            ShotPose over = _shot.OverShoulder(body, heading, _beacon.position);
            if (phase == OpeningPhase.OverShoulder)
            {
                return over;
            }

            CameraState chase = _rig.ChaseCamera.State;
            var chasePose = new ShotPose(chase.GetFinalPosition(), chase.GetFinalOrientation(), chase.Lens.FieldOfView);
            return PostShot.Blend(over, chasePose, t);
        }

        private void ApplyPostDust()
        {
            PostSettings post = _tuning.Opening.Post;
            _dust.Update(_script.Phase, _script.PostStep, _script.PostProgress01);
            _body.SetPostDust(_dust, post.DustCoatAmount);
            while (_dust.TryTakePuff(out DustZone zone))
            {
                Vector3 anchor = _body.DustAnchor(zone);
                _controller.DustPuff(new Vector3(anchor.x, _rover.Position.y, anchor.z), post.DustPuffGrains);
            }
        }

        private void ApplyPostLights()
        {
            OpeningPhase phase = _script.Phase;
            PostSettings post = _tuning.Opening.Post;
            float share = _script.PhaseLength > LengthEpsilon ? _script.PhaseTime / _script.PhaseLength : 1f;
            float secondsIn = phase == OpeningPhase.Post ? _script.PhaseTime : post.FillRampSeconds;
            Transform screen = _body.ScreenTransform;
            _lights.Apply(_rover.Position, _world.EarthDirection, screen.position, screen.forward,
                PostLighting.Rim(phase, share), PostLighting.Fill(phase, share, secondsIn, post.FillRampSeconds));
        }

        private float HeadingDegrees()
        {
            Vector3 forward = _rover.Rotation * Vector3.forward;
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// The lens camera takes the picture in one cut: the brain's default blend (a 2 s ease) would otherwise mix the
        /// chase camera into it for the first seconds of the wake, showing a view between the two.
        /// </summary>
        private void CutBlend()
        {
            if (_brain == null)
            {
                return;
            }

            if (_cutFrames == 0)
            {
                _savedBlend = _brain.DefaultBlend;
            }

            _brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            _cutFrames = CutFrameCount;
        }

        private void RestoreBlend()
        {
            if (_brain != null && _cutFrames > 0)
            {
                _brain.DefaultBlend = _savedBlend;
            }

            _cutFrames = 0;
        }

        private void OnDisable()
        {
            RestoreBlend();
        }

        /// <summary>Has the lens view build itself now (on the title, or in a new game's black), once.</summary>
        private void WarmLens()
        {
            if (!_lensWarmed && (_lens != null || _context.TryGet(out _lens)))
            {
                _lens.Warm();
                _lensWarmed = true;
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
            if (phase == OpeningPhase.FadeOut || phase == OpeningPhase.PullOut)
            {
                // The fade runs on the camera the player already sees; the pull-out on the lens camera.
                Vector3 eyePosition = phase == OpeningPhase.FadeOut
                    ? _rig.Camera.transform.position
                    : _lensCamera.transform.position;
                float toLens = Vector3.Distance(eyePosition, ScanLensOrigin.position);
                state = new LensViewState(state.Lid01, toLens, state.Flicker01, state.Warmth01, state.SpeckOffset);
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
