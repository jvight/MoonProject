using System;
using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.Rover
{
    /// <summary>
    /// The floaty drone camera: places a follow target above 07 facing its heading, feeds look input and the
    /// <see cref="CameraOrbit"/> into a Cinemachine 3 rig (OrbitalFollow locked to the target's yaw + RotationComposer,
    /// with Decollider and Deoccluder keeping it out of the terrain), widens the FOV with speed and dips softly on
    /// landings. Reads the rover only through <see cref="IRoverState"/>, so it must initialise after the rover.
    /// Slow, skippable camera moments frame 07 with the beam while digging, a surfacing relic, a relay mast's lamp as
    /// it lights, or the base after an upgrade (needs <see cref="IWorldLayout"/>). When 07 has rested a while
    /// (<see cref="IRoverStillness"/>) with nothing going on, the camera drifts out to the lonely
    /// <see cref="WideShot"/>, composed against the analytic terrain (<see cref="ITerrainQuery"/>), and publishes
    /// <see cref="RoverWideShotChanged"/> as it opens and hands back. Camera moments, a leap, the tether and
    /// interactions take precedence over it. When 07 is placed somewhere else (<see cref="RoverPlaced"/>)
    /// everything snaps behind it, no ease. Registers itself as <see cref="IViewCamera"/> (gameplay aims from its
    /// centre ray, UI projects with it) and <see cref="ILookSettings"/> (the UI applies the player's sensitivity and
    /// invert-Y).
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class RoverCameraRig : MonoBehaviour, IGameSystem, IViewCamera, ILookSettings
    {
        /// <summary>Look input below this (deg per frame) is sensor noise, not the player looking around.</summary>
        private const float LookEpsilon = 1e-4f;

        /// <summary>Subjects nearer than this (m, ground plane) have no meaningful direction to turn to.</summary>
        private const float MinSubjectDistance = 0.5f;

        /// <summary>Below this horizontal speed (m/s) the travel direction is too noisy to judge slopes.</summary>
        private const float DescentMinSpeed = 0.5f;

        /// <summary>Smallest share of the orbit a moment leaves, so adopting the wide yaw never divides by 0.</summary>
        private const float MinOrbitShare = 0.05f;

        [Tooltip("Camera tuning (Assets/_Project/Data/Tuning/RoverCameraTuning.asset).")]
        [SerializeField] private RoverCameraTuning _tuning;

        [Tooltip("Follow/look-at target this rig moves above the rover each frame.")]
        [SerializeField] private Transform _target;

        [Tooltip("The Unity camera driven by the CinemachineBrain (what the player sees).")]
        [SerializeField] private Camera _viewCamera;

        [SerializeField] private CinemachineCamera _camera;
        [SerializeField] private CinemachineOrbitalFollow _orbit;
        [SerializeField] private CinemachineRotationComposer _composer;
        [SerializeField] private CinemachineDecollider _decollider;
        [SerializeField] private CinemachineDeoccluder _deoccluder;
        [SerializeField] private RoverCameraBump _bump;

        private readonly CameraMoment _moment = new CameraMoment();
        private WideShot _wide;
        private bool _wideSteersYaw;
        private bool _tethered;
        private bool _paused;
        private float _momentYaw;
        private float _momentLift;
        private float _momentPullBack;
        private Vector3 _momentShift;
        private bool _leapHeld;
        private bool _leapAirborne;
        private IRoverState _rover;
        private IWorldLayout _world;
        private IRoverStillness _stillness;
        private ITerrainQuery _terrain;
        private EventBus _events;
        private InputReader _input;
        private CameraOrbit _orbitState;
        private LookSettings _look;
        private DampedSpring _bumpSpring;
        private IDisposable[] _subscriptions;
        private bool _initialized;

        public CameraOrbit Orbit => _orbitState;

        /// <summary>The lonely wide shot's state (quiet time, weight, frame), for tests and tooling.</summary>
        public WideShot WideShot => _wide;

        /// <summary>How far the current camera moment has eased in, 0..1 (for tests and tooling).</summary>
        public float MomentWeight => _moment.Weight;

        public Camera Camera => _viewCamera;

        public float Sensitivity
        {
            get => _look.Sensitivity;
            set => _look.Sensitivity = value;
        }

        public bool InvertY
        {
            get => _look.InvertY;
            set => _look.InvertY = value;
        }

        public void Initialize(GameContext context)
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return;
            }

            _rover = context.Get<IRoverState>();
            _world = context.Get<IWorldLayout>();
            _stillness = context.Get<IRoverStillness>();
            _terrain = context.Get<ITerrainQuery>();
            _events = context.Events;
            _input = context.Input;
            _orbitState = new CameraOrbit(_tuning);
            _wide = new WideShot(_tuning.WideShot);
            _look = new LookSettings(_tuning);
            ApplyCinemachineSettings();
            _subscriptions = new[]
            {
                context.Events.Subscribe<RoverLanded>(OnLanded),
                context.Events.Subscribe<ExcavationStarted>(OnExcavationStarted),
                context.Events.Subscribe<ExcavationStopped>(OnExcavationStopped),
                context.Events.Subscribe<RelicSurfaced>(OnRelicSurfaced),
                context.Events.Subscribe<RelayRestored>(OnRelayRestored),
                context.Events.Subscribe<UpgradePurchased>(OnUpgradePurchased),
                context.Events.Subscribe<RoverJumped>(OnJumped),
                context.Events.Subscribe<TetherAttached>(OnTetherAttached),
                context.Events.Subscribe<TetherReleased>(OnTetherReleased),
                context.Events.Subscribe<PauseChanged>(OnPauseChanged),
                context.Events.Subscribe<UiCue>(OnUiCue),
                context.Events.Subscribe<BellCued>(OnBellCued),
                context.Events.Subscribe<RoverPlaced>(OnPlaced),
            };
            context.Register<IViewCamera>(this);
            context.Register<ILookSettings>(this);
            _initialized = true;
            Snap();
        }

        private bool ValidateWiring()
        {
            bool ok = _tuning != null && _target != null && _viewCamera != null && _camera != null && _orbit != null
                && _composer != null && _decollider != null && _deoccluder != null && _bump != null;
            if (!ok)
            {
                Debug.LogError(
                    $"{nameof(RoverCameraRig)}: tuning, target, view camera or a Cinemachine component is missing.",
                    this);
            }

            return ok;
        }

        private void ApplyCinemachineSettings()
        {
            _camera.Follow = _target;
            _camera.LookAt = _target;
            _camera.Lens.NearClipPlane = _tuning.NearClip;
            _camera.Lens.FarClipPlane = _tuning.FarClip;

            _orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            _orbit.Radius = _tuning.Distance;
            TrackerSettings tracker = _orbit.TrackerSettings;
            tracker.BindingMode = BindingMode.LockToTargetWithWorldUp;
            tracker.PositionDamping = _tuning.PositionDamping;
            tracker.AngularDampingMode = AngularDampingMode.Euler;
            tracker.RotationDamping = new Vector3(0f, _tuning.YawDamping, 0f);
            _orbit.TrackerSettings = tracker;
            _orbit.HorizontalAxis.Range = new Vector2(-180f, 180f);
            _orbit.HorizontalAxis.Wrap = true;
            _orbit.HorizontalAxis.Center = 0f;
            _orbit.HorizontalAxis.Recentering.Enabled = false;
            _orbit.VerticalAxis.Range = new Vector2(_tuning.MinPitch, _tuning.MaxPitch);
            _orbit.VerticalAxis.Wrap = false;
            _orbit.VerticalAxis.Center = _tuning.DefaultPitch;
            _orbit.VerticalAxis.Recentering.Enabled = false;
            _orbit.RadialAxis.Recentering.Enabled = false;

            _composer.Damping = _tuning.AimDamping;
            _composer.CenterOnActivate = false;
            ScreenComposerSettings composition = _composer.Composition;
            composition.ScreenPosition = _tuning.ScreenPosition;
            _composer.Composition = composition;

            int terrain = Layers.GroundMask;
            int obstacles = Layers.GroundMask | Layers.PropMask;
            _decollider.CameraRadius = _tuning.CameraRadius;
            _decollider.TerrainResolution.Enabled = true;
            _decollider.TerrainResolution.TerrainLayers = terrain;
            _decollider.TerrainResolution.MaximumRaycast = _tuning.TerrainProbe;
            _decollider.TerrainResolution.Damping = _tuning.AvoidanceDamping;
            _decollider.Decollision.Enabled = true;
            _decollider.Decollision.ObstacleLayers = Layers.PropMask;
            _decollider.Decollision.Damping = _tuning.AvoidanceDamping;

            _deoccluder.CollideAgainst = obstacles;
            _deoccluder.TransparentLayers = 0;
            _deoccluder.AvoidObstacles.Enabled = true;
            _deoccluder.AvoidObstacles.CameraRadius = _tuning.CameraRadius;
            _deoccluder.AvoidObstacles.Strategy =
                CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            _deoccluder.AvoidObstacles.Damping = _tuning.AvoidanceDamping;
            _deoccluder.AvoidObstacles.DampingWhenOccluded = _tuning.OcclusionDamping;
            _deoccluder.AvoidObstacles.SmoothingTime = _tuning.OcclusionSmoothing;
        }

        /// <summary>Puts the camera straight behind the rover with no damping (spawn, placement).</summary>
        public void Snap()
        {
            _orbitState.Reset();
            _bumpSpring.Reset(0f);
            PlaceTarget();
            ApplyOrbit();
            _camera.PreviousStateIsValid = false;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (!_initialized || deltaTime <= 0f)
            {
                return;
            }

            Vector2 look = ReadLook(deltaTime);
            if (look.sqrMagnitude > LookEpsilon * LookEpsilon)
            {
                _moment.Cancel(_tuning.MomentCancelEaseOut);
            }

            ReleaseLeapOnTouchdown();
            _moment.Step(deltaTime);
            StepWideShot(deltaTime);
            StepMomentFraming(deltaTime);
            PlaceTarget();

            Vector3 velocity = _rover.Velocity;
            float horizontal = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
            float descent = horizontal > DescentMinSpeed ? Mathf.Atan2(-velocity.y, horizontal) * Mathf.Rad2Deg : 0f;
            _orbitState.Step(look, _rover.Speed, _rover.NormalizedSpeed, descent, _rover.IsGrounded,
                deltaTime);

            _bumpSpring.Step(0f, _tuning.BumpFrequency, _tuning.BumpDamping, deltaTime);
            ApplyOrbit();
        }

        /// <summary>
        /// The current moment's framing as offsets from the normal chase camera (yaw swing, look shift, lift,
        /// pull-back), eased by the moment's weight and smoothed again so one moment flowing into the next (dig into
        /// relic) never jumps. With no moment they all settle to zero.
        /// </summary>
        private void StepMomentFraming(float deltaTime)
        {
            float yaw = 0f;
            float lift = 0f;
            float pullBack = 0f;
            Vector3 shift = Vector3.zero;
            float weight = _moment.Weight;
            if (weight > 0f)
            {
                CameraMomentSettings moment = _moment.Settings;
                Vector3 rover = _rover.Position;
                Vector3 toSubject = _moment.Subject - rover;
                float distance = Mathf.Sqrt(toSubject.x * toSubject.x + toSubject.z * toSubject.z);
                float reach = weight * CameraMoment.FocusReach(moment, distance);
                if (distance > MinSubjectDistance)
                {
                    float roverYaw = RoverYaw();
                    float subjectYaw = Mathf.Atan2(toSubject.x, toSubject.z) * Mathf.Rad2Deg;
                    yaw = Mathf.DeltaAngle(roverYaw, CameraMoment.BlendYaw(moment, roverYaw, subjectYaw, reach));
                }

                Vector3 follow = rover + Vector3.up * _tuning.TargetHeight;
                shift = CameraMoment.LookPoint(moment, follow, _moment.Subject, reach) - follow;
                lift = moment.Lift * weight;
                pullBack = moment.PullBack * weight;
            }

            float halfLife = _tuning.MomentBlendHalfLife;
            _momentYaw = Smoothing.Damp(_momentYaw, yaw, halfLife, deltaTime);
            _momentShift = Smoothing.Damp(_momentShift, shift, halfLife, deltaTime);
            _momentLift = Smoothing.Damp(_momentLift, lift, halfLife, deltaTime);
            _momentPullBack = Smoothing.Damp(_momentPullBack, pullBack, halfLife, deltaTime);
        }

        private float RoverYaw()
        {
            Vector3 forward = _rover.Rotation * Vector3.forward;
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        /// <summary>Follow point above 07 facing its heading, offset by the current moment's framing.</summary>
        private void PlaceTarget()
        {
            Vector3 point = _rover.Position + Vector3.up * _tuning.TargetHeight + _momentShift;
            _target.SetPositionAndRotation(point, Quaternion.Euler(0f, RoverYaw() + _momentYaw, 0f));
        }

        private Vector2 ReadLook(float deltaTime)
        {
            return _look.OrbitDegrees(_input.LookDelta, _input.LookRate, deltaTime);
        }

        /// <summary>
        /// The chase orbit (player look, recentering, moments) blended toward the wide shot's frame and its breathing
        /// by the wide shot's weight; at weight 0 it is exactly the chase camera.
        /// </summary>
        private void ApplyOrbit()
        {
            float yaw = _orbitState.YawOffset * (1f - _moment.Weight);
            float elevation = _orbitState.Elevation + _momentLift;
            float radius = _tuning.Distance * (1f + _momentPullBack);
            float fov = _orbitState.FieldOfView;
            Vector2 screen = _tuning.ScreenPosition;
            float wide = _wide.Weight;
            if (wide > 0f)
            {
                WideShotSettings settings = _tuning.WideShot;
                WideShotFrame frame = _wide.Frame;
                float time = _wide.BreathSeconds;
                if (_wideSteersYaw)
                {
                    float sway = WideShotComposer.Breath(settings.BreathYaw, settings.BreathYawPeriod, time);
                    float wideYaw = Mathf.DeltaAngle(RoverYaw() + _momentYaw, frame.Yaw + sway);
                    yaw += Mathf.DeltaAngle(yaw, wideYaw) * wide;
                }

                float rise = WideShotComposer.Breath(settings.BreathElevation, settings.BreathElevationPeriod, time);
                float drift = WideShotComposer.Breath(settings.BreathDistance, settings.BreathDistancePeriod, time);
                elevation = Mathf.Lerp(elevation, frame.Elevation + rise, wide);
                radius = Mathf.Lerp(radius, frame.Distance * (1f + drift), wide);
                fov = Mathf.Lerp(fov, _tuning.BaseFov + settings.FovWiden, wide);
                screen.y = Mathf.Lerp(screen.y, frame.ScreenY, wide);
            }

            _orbit.HorizontalAxis.Value = yaw;
            _orbit.VerticalAxis.Value = Mathf.Clamp(elevation, _tuning.MinPitch, _tuning.MaxPitch);
            _orbit.Radius = radius;
            _camera.Lens.FieldOfView = fov;
            ScreenComposerSettings composition = _composer.Composition;
            composition.ScreenPosition = screen;
            _composer.Composition = composition;
            _bump.Offset = Vector3.up * _bumpSpring.Value;
        }

        /// <summary>
        /// Counts 07's rest toward the wide shot (a moment, a leap or the tether keep it closed; the pause menu only
        /// delays it) and opens or hands it back when it says so.
        /// </summary>
        private void StepWideShot(float deltaTime)
        {
            bool busy = _moment.IsActive || _leapHeld || _tethered;
            switch (_wide.Step(_stillness.StillSeconds, busy, _paused, deltaTime))
            {
                case WideShotCue.Open:
                    OpenWideShot();
                    break;
                case WideShotCue.HandBack:
                    HandBackWideShot();
                    break;
            }
        }

        /// <summary>
        /// Composes the wide frame from where the camera looks now, turned gently toward Earth or The Peak (or, shut in
        /// by a canyon, closer and looking along the way) and fitted to the terrain. 07 is resting, so the frame is
        /// solved once and holds.
        /// </summary>
        private void OpenWideShot()
        {
            WideShotSettings settings = _tuning.WideShot;
            Vector3 follow = _rover.Position + Vector3.up * _tuning.TargetHeight;
            float viewYaw = WideShotComposer.Bearing(_viewCamera.transform.forward);
            float earthYaw = WideShotComposer.Bearing(_world.EarthDirection);
            float peakYaw = WideShotComposer.Bearing(_world.PeakPosition - follow);
            float swing = WideShotComposer.SubjectSwing(settings, viewYaw, earthYaw, peakYaw);
            float shutIn = WideShotComposer.ShutIn(settings, _terrain, follow);
            _wide.Open(WideShotComposer.Solve(settings, _terrain, follow, viewYaw, swing, shutIn));
            _wideSteersYaw = true;
            _events.Publish(new RoverWideShotChanged(true));
        }

        /// <summary>
        /// Eases back to the chase camera. The view keeps the yaw the wide shot turned to (the orbit adopts it), so
        /// only distance, height and lens come back: the quickest return that never swings the world round.
        /// </summary>
        private void HandBackWideShot()
        {
            if (_wideSteersYaw)
            {
                float share = Mathf.Max(1f - _moment.Weight, MinOrbitShare);
                _orbitState.AdoptYaw(_orbit.HorizontalAxis.Value / share);
                _wideSteersYaw = false;
            }

            _wide.HandBack();
            _events.Publish(new RoverWideShotChanged(false));
        }

        private void OnTetherAttached(TetherAttached attached)
        {
            _tethered = true;
        }

        private void OnTetherReleased(TetherReleased released)
        {
            _tethered = false;
        }

        /// <summary>
        /// 07 jumped somewhere else while the view was dark: the wide shot and any moment end at once and the camera
        /// snaps behind 07, so nothing lerps across from the old spot.
        /// </summary>
        private void OnPlaced(RoverPlaced placed)
        {
            bool wasWide = _wide.IsOpen;
            _wide.Close();
            _wideSteersYaw = false;
            _moment.Clear();
            _leapHeld = false;
            _leapAirborne = false;
            _momentYaw = 0f;
            _momentLift = 0f;
            _momentPullBack = 0f;
            _momentShift = Vector3.zero;
            Snap();
            if (wasWide)
            {
                _events.Publish(new RoverWideShotChanged(false));
            }
        }

        private void OnPauseChanged(PauseChanged pause)
        {
            _paused = pause.Paused;
        }

        /// <summary>A card to read holds the wide shot back; holding an upgrade's confirm is an interaction.</summary>
        private void OnUiCue(UiCue cue)
        {
            if (cue.Kind == UiCueKind.CardShown)
            {
                _wide.HoldBack(_tuning.WideShot.CardQuietSeconds);
            }
            else if (cue.Kind == UiCueKind.HoldFill)
            {
                Interact();
            }
        }

        private void OnBellCued(BellCued cue)
        {
            if (cue.Cue == BellCue.DialTurned)
            {
                Interact();
            }
        }

        /// <summary>The player did something at a station or at Bell: hand back and stay close for a while.</summary>
        private void Interact()
        {
            _wide.HoldBack(_tuning.WideShot.InteractionQuietSeconds);
            if (_wide.IsOpen)
            {
                HandBackWideShot();
            }
        }

        private void OnExcavationStarted(ExcavationStarted excavation)
        {
            Vector3 rising = excavation.Position + Vector3.up * _tuning.TargetHeight;
            _moment.Start(_tuning.DigMoment, rising, true);
        }

        /// <summary>
        /// The beam stopped: a held dig moment eases back. When the relic completed, RelicSurfaced follows in the same
        /// frame and its moment continues from the dig's framing.
        /// </summary>
        private void OnExcavationStopped(ExcavationStopped excavation)
        {
            _moment.Release();
        }

        private void OnRelicSurfaced(RelicSurfaced relic)
        {
            _moment.Start(_tuning.RelicMoment, relic.Position, false);
        }

        /// <summary>The mast 07 restored lights up: a slow look up at it and its lamp against the sky.</summary>
        private void OnRelayRestored(RelayRestored relay)
        {
            _moment.Start(_tuning.RelayMoment, relay.Position, false);
        }

        /// <summary>A real leap (not a hop) lifts the camera and looks ahead to the landing until 07 is down.</summary>
        private void OnJumped(RoverJumped jumped)
        {
            if (jumped.Strength < _tuning.LeapMomentMinStrength)
            {
                return;
            }

            Vector3 velocity = _rover.Velocity;
            Vector3 ahead = new Vector3(velocity.x, 0f, velocity.z) * _tuning.LeapLookAhead;
            _moment.Start(_tuning.LeapMoment, _rover.Position + ahead, true);
            _leapHeld = true;
            _leapAirborne = false;
        }

        private void ReleaseLeapOnTouchdown()
        {
            if (!_leapHeld)
            {
                return;
            }

            _leapAirborne |= !_rover.IsGrounded;
            if (_leapAirborne && _rover.IsGrounded)
            {
                _moment.Release();
                _leapHeld = false;
            }
        }

        private void OnUpgradePurchased(UpgradePurchased upgrade)
        {
            _moment.Start(_tuning.UpgradeMoment, _world.BasePosition + Vector3.up * _tuning.TargetHeight, false);
        }

        private void OnLanded(RoverLanded landed)
        {
            _bumpSpring.AddVelocity(-Mathf.Min(landed.ImpactSpeed * _tuning.BumpPerImpact, _tuning.MaxBumpKick));
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
