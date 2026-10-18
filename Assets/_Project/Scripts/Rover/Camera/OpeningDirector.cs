using System;
using UnityEngine;
using Unity.Cinemachine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's wake and the camera side of the title (docs/design/opening-v2.md, the boot readout), the thin adapter around
    /// <see cref="OpeningScript"/>.
    /// <list type="bullet">
    /// <item>While the title is up (<see cref="IGameFlow"/>, <see cref="TitleShown"/>) 07 stays asleep. A fresh title
    /// holds the rig's lens shot on the closed lens, drifting after a while; over a save, the rig holds the wide shot
    /// of 07 where it was left (<see cref="TitleFrame"/>).</item>
    /// <item><see cref="SessionStarted"/> with a fresh game plays the full opening once: from the title the picture
    /// fades to black where the camera is (no camera move). In the black the shot camera takes the one fixed shot of
    /// 07's face (<see cref="PostShot"/>) and holds it: the picture opens, the face screen flickers on, the eyes blink
    /// awake and the self-test prints on the face while each part does its check. A short dip to black
    /// (<see cref="ILensView"/> darkens the picture) hands over to the chase frame in the dark.</item>
    /// <item>A loaded game plays the short wake: the title's frame eases back to the chase frame while 07's lid opens
    /// with one blink and it glances at Earth.</item>
    /// <item>In a game without a title (<see cref="IGameFlow.ShowsTitle"/> off: dev fixtures and test runs that drive
    /// 07 from the first frame) a session plays neither: 07 wakes on its own as it always did.</item>
    /// </list>
    /// Publishes <see cref="RoverWakeBeatReached"/> for every beat and holds 07 still (<see cref="IRoverRig"/>) only
    /// while the script says control is held. Pause freezes it.
    /// </summary>
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public sealed class OpeningDirector : MonoBehaviour, IGameSystem
    {
        /// <summary>The gaze request points this far (m) along the gaze: far enough to read as a direction.</summary>
        private const float GazeReach = 200f;

        /// <summary>Frames the brain keeps its cut blend after the shot camera goes live or lets go.</summary>
        private const int CutFrameCount = 2;

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

        [Tooltip("07 itself: the cutter's flashes and the dust puffs of the checks.")]
        [SerializeField] private RoverController _controller;

        [Tooltip("07's body language: the wake's sleep, lid, head tilt, wing sigh and call sign.")]
        [SerializeField] private RoverBodyLanguage _body;

        [Tooltip("The shot camera: a Cinemachine camera with no body or aim, placed by this each frame it is live.")]
        [SerializeField] private CinemachineCamera _lensCamera;

        [Tooltip("The amber beacon on the lander's hull: 07's head glances at it at the start.")]
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
        private Vector3 _shotFace;
        private Vector3 _shotBody;
        private float _shotHeading;
        private bool _shotWasLive;
        private bool _rigReleased;
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
            _shot = new PostShot(settings.Post, _tuning.FaceInspectScreenHeight);
            _lights = new PostLights(transform, settings.Post);
            _dust = new PostDust(settings.Post);
            _brain = _rig.Camera != null ? _rig.Camera.GetComponent<CinemachineBrain>() : null;
            CinemachineCamera chase = _rig.ChaseCamera;
            chase.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Always;
            _lensCamera.Priority = chase.Priority.Value + 1;
            LensSettings lens = _lensCamera.Lens;
            lens.NearClipPlane = _tuning.FaceInspectNearClip;
            lens.FieldOfView = settings.Post.ShotFov;
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
                Debug.LogError($"{nameof(OpeningDirector)}: tuning, rig, rover, body, shot camera or the lander's " +
                    "beacon is not assigned; rebuild the Main scene.", this);
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
                _rig.BeginWakeOpening();
            }
        }

        private void OnSessionStarted(SessionStarted started)
        {
            bool fromLensShot = _inTitle && _rig.Opening.IsActive;
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
        /// The full opening. The rig holds its wake shot underneath from the start, which keeps its wide shot and
        /// stargazing closed until the handover lets it go.
        /// </summary>
        private void BeginFull(bool fromTitle)
        {
            _body.HoldOpeningScreenDark(true);
            SetShot();
            _script.BeginFull(fromTitle);
            _wakeWaitsForBlack = fromTitle;
            if (fromTitle)
            {
                _rig.Opening.FreezeDrift();
            }
            else
            {
                StartWakeShot();
            }
        }

        /// <summary>
        /// Puts 07's head where the black needs it and the rig's wake shot underneath. From the title this waits until
        /// the picture has faded to black, so nothing moves in view while the title goes.
        /// </summary>
        private void StartWakeShot()
        {
            _body.PoseOpeningHead(_script.Gaze.Yaw, _script.Gaze.Pitch, 0f, 1f);
            _rig.BeginWakeOpening();
        }

        /// <summary>
        /// Fixes the one shot as the opening begins (07's creep and shiver must not move the camera): aimed at where
        /// 07's face will be once its head has turned into the shot, which is where the script tells the head to look.
        /// </summary>
        private void SetShot()
        {
            _shotBody = _rover.Position;
            _shotHeading = HeadingDegrees();
            _rigReleased = false;
            Vector2 face = _shot.FaceAngles(_shotHeading);
            _body.EvaluateScreenPose(face.x, face.y, out _shotFace, out _);
            _script.SetFaceTarget(face);
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
            _body.SetLampPower(_lamps.Level * PostMotions.Lamps(_script.Phase == OpeningPhase.Perform ? _script.Perform01 : 0f));

            _script.Step(deltaTime);
            TakeBeats();
            TakePostEvents();
            HoldControl(_script.ControlHeld);
            PoseBody();
            ApplyPostMotion(deltaTime);
            AimHead();
            PlaceShotCamera();
            ApplyPicture();
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
                        _body.BeginWake();
                        if (_wakeWaitsForBlack)
                        {
                            _wakeWaitsForBlack = false;
                            StartWakeShot();
                        }

                        break;
                    case WakeBeat.ChaseFrame:
                        _body.FinishWake();
                        break;
                }

                _events.Publish(new RoverWakeBeatReached(beat));
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
                    case PostEventKind.Blink:
                        _body.BlinkFace();
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
            if (_script.GazeHeld)
            {
                PoseHead();
            }
            else
            {
                _body.PoseOpeningHead(0f, 0f, 0f, 0f);
            }
        }

        /// <summary>
        /// The head follows the gaze (bowed in the black, then into the shot), sweeps and nods in its own check, glances
        /// toward the beacon at the end of the ready performance and is handed back to 07 as the camera moves behind
        /// it: the body never turns.
        /// </summary>
        private void PoseHead()
        {
            float yaw = _script.Gaze.Yaw;
            float pitch = _script.Gaze.Pitch;
            if (_script.Phase == OpeningPhase.Post && _script.PostStep == BootCheckStep.Head)
            {
                Vector2 head = _script.Motions.Head(_script.PostProgress01);
                yaw += head.x;
                pitch += head.y;
            }

            float turn = PostMotions.BeaconTurn(_script.Perform01);
            if (turn > 0f)
            {
                PostSettings post = _tuning.Opening.Post;
                Vector2 beacon = LensGaze.Angles(Quaternion.Inverse(Heading()) * (_beacon.position - ScanLensOrigin.position));
                yaw += turn * Mathf.Clamp(Mathf.DeltaAngle(yaw, beacon.x), -post.BeaconGlanceYaw, post.BeaconGlanceYaw);
                pitch += turn * Mathf.Clamp(beacon.y - pitch, -post.BeaconGlancePitch, post.BeaconGlancePitch);
            }

            float hold = _script.Phase == OpeningPhase.MoveBehind
                ? 1f - WakeOpening.SmoothStepC2(0f, 1f, _script.PhaseShare)
                : 1f;
            _body.PoseOpeningHead(yaw, pitch, 0f, hold);
        }

        /// <summary>
        /// What each check does on 07 beyond its head: the wheels' twitches, the motor's shudder and lurch, the
        /// cutter's flashes and turret kicks, the wing's jerk, the antenna's rise (held through the later checks and
        /// the ready performance, then lowered as the camera moves behind) and the performance's dust shiver.
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
                }

                if (step > BootCheckStep.Antenna)
                {
                    antennaTarget = motions.Antenna(1f);
                }
            }
            else if (_script.Phase >= OpeningPhase.Ready && _script.Phase <= OpeningPhase.Perform)
            {
                antennaTarget = _script.Motions.Antenna(1f);
                motion.Shudder = PostMotions.Shiver(_script.Perform01) * _tuning.Opening.Post.ShudderMetres * ShiverScale;
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

        /// <summary>
        /// The wake's camera, fixed where 07 stood as it began: the face shot (kept clear of the terrain) through the
        /// checks and the ready hold, then the pull-back to the wide shot, the performance held wide, and the move round
        /// to the chase frame. The rig is let go behind 07 as the pull-back starts, so the chase frame has settled
        /// before the camera moves toward it; at the end the shot camera hands over on the very pose the chase frame has.
        /// </summary>
        private void PlaceShotCamera()
        {
            if (!_script.ShotLive)
            {
                if (_shotWasLive)
                {
                    _shotWasLive = false;
                    ReleaseRig();
                }

                if (_lensCamera.enabled)
                {
                    CutBlend();
                    _lensCamera.enabled = false;
                }

                return;
            }

            OpeningPhase phase = _script.Phase;
            if (phase >= OpeningPhase.PullBack)
            {
                ReleaseRig();
            }

            ShotPose face = FaceShot();
            ShotPose pose;
            switch (phase)
            {
                case OpeningPhase.PullBack:
                    pose = PostShot.PullBack(face, _shot.Wide(_shotBody, _shotHeading), _script.PhaseShare);
                    break;
                case OpeningPhase.Perform:
                    pose = _shot.Wide(_shotBody, _shotHeading);
                    break;
                case OpeningPhase.MoveBehind:
                    CameraState chase = _rig.ChaseCamera.State;
                    var chasePose = new ShotPose(chase.GetFinalPosition(), chase.GetFinalOrientation(), chase.Lens.FieldOfView);
                    pose = PostShot.Round(_shot.Wide(_shotBody, _shotHeading), chasePose, _shotBody, _script.PhaseShare);
                    break;
                default:
                    pose = face;
                    break;
            }

            _lensCamera.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            LensSettings lensSettings = _lensCamera.Lens;
            lensSettings.FieldOfView = pose.Fov;
            lensSettings.NearClipPlane = _tuning.FaceInspectNearClip;
            _lensCamera.Lens = lensSettings;
            _shotWasLive = true;
            if (!_lensCamera.enabled)
            {
                CutBlend();
                _lensCamera.enabled = true;
                _lensCamera.PreviousStateIsValid = false;
            }
        }

        private void ApplyPostDust()
        {
            PostSettings post = _tuning.Opening.Post;
            float progress = _script.Phase == OpeningPhase.Perform ? _script.Perform01 : _script.PostProgress01;
            _dust.Update(_script.Phase, _script.PostStep, progress);
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
            float share = _script.PhaseShare;
            float sinceFaceOn = phase == OpeningPhase.EyesOpen ? _script.PhaseTime : post.FillRampSeconds;
            Transform screen = _body.ScreenTransform;
            _lights.Apply(_rover.Position, _world.EarthDirection, screen.position, screen.forward,
                PostLighting.Rim(phase, share), PostLighting.Fill(phase, share, sinceFaceOn, post.FillRampSeconds));
        }

        /// <summary>The face shot at its push-in, kept clear of the terrain between the face and the camera.</summary>
        private ShotPose FaceShot()
        {
            ShotPose pose = _shot.At(_shotFace, _shotHeading, _script.Push01);
            Vector3 toCamera = pose.Position - _shotFace;
            float length = toCamera.magnitude;
            if (length <= LengthEpsilon)
            {
                return pose;
            }

            float clear = _sight.Clear(_shotFace, pose.Position, _tuning.FaceInspectProbeRadius);
            return new ShotPose(_shotFace + toCamera / length * clear, pose.Rotation, pose.Fov);
        }

        /// <summary>Lets the rig go behind 07, once per opening.</summary>
        private void ReleaseRig()
        {
            if (_rigReleased)
            {
                return;
            }

            _rigReleased = true;
            _rig.EndWakeOpening();
        }

        private float HeadingDegrees()
        {
            Vector3 forward = _rover.Rotation * Vector3.forward;
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// The shot camera takes and gives back the picture in one cut: the brain's default blend (a 2 s ease) would
        /// otherwise show a view between the shot and the chase frame.
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

        /// <summary>The fades and the handover's dip: the lens view darkens the picture, kept sharp on 07.</summary>
        private void ApplyPicture()
        {
            if (!_script.PictureDimmed)
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
                    Debug.LogError($"{nameof(OpeningDirector)}: no ILensView is registered, so the opening's fades " +
                        "cannot darken the picture. Rebuild the Main scene (World's lens view).", this);
                }

                return;
            }

            float toLens = Vector3.Distance(_rig.Camera.transform.position, ScanLensOrigin.position);
            _lens.Apply(new LensViewState(_script.Picture01, toLens));
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
