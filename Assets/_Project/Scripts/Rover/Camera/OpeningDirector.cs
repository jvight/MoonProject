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

        [Tooltip("Home's lander (the first gaze target: its dark windows).")]
        [SerializeField] private Transform _lander;

        [Tooltip("The radio tower's pilot light (the second gaze target).")]
        [SerializeField] private Transform _pilot;

        private readonly object _owner = new object();
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
        private bool _titleHasSave;
        private bool _inTitle;
        private bool _paused;
        private bool _lensApplied;
        private bool _lensMissing;
        private bool _gazing;
        private bool _holding;
        private bool _tilting;
        private bool _initialized;

        /// <summary>The script, for tests and captures.</summary>
        public OpeningScript Script => _script;

        /// <summary>True while the lens camera has the view (pushing in, inside the lens, leaving it).</summary>
        public bool LensCameraLive => _lensCamera != null && _lensCamera.enabled;

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
            context.TryGet(out _lookSettings);
            _script = new OpeningScript(_tuning);
            _look = new LookSettings(_tuning);
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
                BeginFull(fromLensShot);
                return;
            }

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
        /// The full opening. The rig holds its lens shot underneath from the start, which keeps its wide shot and
        /// stargazing closed and is the frame the pull-out lands on.
        /// </summary>
        private void BeginFull(bool pushIn)
        {
            SetTargets();
            _rig.BeginWakeOpening(true);
            if (pushIn)
            {
                CameraState state = _rig.ChaseCamera.State;
                _pushFrom = state.GetFinalPosition();
                _pushFromRotation = state.GetFinalOrientation();
                _pushFromFov = state.Lens.FieldOfView;
            }

            _script.BeginFull(pushIn);
        }

        /// <summary>The gaze targets as (yaw, pitch) from 07's heading at its lens, fixed as the opening begins.</summary>
        private void SetTargets()
        {
            OpeningSettings settings = _tuning.Opening;
            Vector3 lens = _roverRig.TetherOrigin.position;
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
                        _body.BeginWake();
                        break;
                    case WakeBeat.Blinked:
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
                _tilting = true;
            }
            else if (_tilting)
            {
                _body.PoseWake(0f, 0f, 0f, _script.HeadTilt);
                _tilting = _script.TiltActive;
            }
        }

        private void AimHead()
        {
            Vector3 lens = _roverRig.TetherOrigin.position;
            bool gaze = _script.GazeHeld || _script.GlancingEarth;
            if (gaze)
            {
                Vector3 direction = _script.GazeHeld
                    ? Heading() * LensGaze.Direction(_script.Gaze.Yaw, _script.Gaze.Pitch)
                    : _world.EarthDirection;
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
            Quaternion gaze = Heading() * Quaternion.Euler(-_script.Gaze.Pitch, _script.Gaze.Yaw, 0f);
            Vector3 eye = _roverRig.TetherOrigin.position + gaze * Vector3.forward * settings.LensForward;
            Vector3 position = eye;
            Quaternion rotation = gaze;
            float fov = settings.LensFov;
            if (phase == OpeningPhase.PushIn)
            {
                float t = _script.PushIn01;
                Vector3 lens = _roverRig.TetherOrigin.position;
                position = Vector3.Lerp(_pushFrom, lens, t);
                rotation = Quaternion.Slerp(_pushFromRotation, Quaternion.LookRotation(lens - _pushFrom), t);
                fov = Mathf.Lerp(_pushFromFov, settings.LensFov, t);
            }
            else if (phase == OpeningPhase.PullOut)
            {
                float t = _script.PullOut01;
                CameraState chase = _rig.ChaseCamera.State;
                Vector3 reach = eye + gaze * Vector3.forward * settings.PullOutReach;
                Vector3 end = chase.GetFinalPosition();
                position = Vector3.Lerp(Vector3.Lerp(eye, reach, t), Vector3.Lerp(reach, end, t), t);
                Quaternion earth = Quaternion.LookRotation(_world.EarthDirection);
                Quaternion held = Quaternion.Slerp(gaze, earth, Smoothing.SmoothStep(0f, settings.PullOutOntoEarth, t));
                float turn = Smoothing.SmoothStep(settings.PullOutEarthHeld, 1f, t);
                rotation = Quaternion.Slerp(held, chase.GetFinalOrientation(), turn);
                fov = Mathf.Lerp(settings.LensFov, chase.Lens.FieldOfView, t);
            }

            _lensCamera.transform.SetPositionAndRotation(position, rotation);
            LensSettings lensSettings = _lensCamera.Lens;
            lensSettings.FieldOfView = fov;
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
                float toLens = Vector3.Distance(_lensCamera.transform.position, _roverRig.TetherOrigin.position);
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
