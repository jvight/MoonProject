using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.Rover
{
    /// <summary>
    /// The floaty drone camera: places a follow target above 07 facing its heading, feeds look input, the recenter
    /// button and the <see cref="CameraOrbit"/> into a Cinemachine 3 rig (OrbitalFollow locked to the target's yaw with
    /// no rotation damping, so the orbit's world bearing is exactly what the player sees while 07 turns under it, +
    /// RotationComposer, with Decollider and Deoccluder keeping it out of the terrain), widens the FOV with speed and
    /// dips softly on landings; the player's Gentle camera stills both and slows every blend. Reads the rover only
    /// through <see cref="IRoverState"/>, so it must initialise after the rover.
    /// Slow, skippable camera moments frame 07 with the beam while digging, a surfacing relic, a relay mast's lamp as
    /// it lights, the base after an upgrade (needs <see cref="IWorldLayout"/>), or 07 itself from low and round the
    /// side as a gift or a port part settles onto it (<see cref="PartFitted"/>, or a gift's Install beat). In Kenji's
    /// Rover Bay
    /// (<see cref="IRoverBay"/>, resolved once Gameplay registers it) a held moment fixes the view in through the open
    /// front, under the crane rail (<see cref="BayFramingSettings"/>); while the bay fits a piece
    /// (<see cref="IBayFitView"/>) it eases in to a close shot of the arm setting it on, or low under 07's belly for
    /// the floor arm, and back out as the arms fold away (<see cref="BayShotComposer"/>). When 07 has
    /// rested a while (<see cref="IRoverStillness"/>) with nothing going on, the camera drifts out to the lonely
    /// <see cref="WideShot"/>, composed against the analytic terrain (<see cref="ITerrainQuery"/>), and publishes
    /// <see cref="RoverWideShotChanged"/> as it opens and hands back. Camera moments, a leap, the tether, the radio-hop
    /// list, the bay and interactions take precedence over it. When 07 is placed somewhere else
    /// (<see cref="RoverPlaced"/>) everything snaps behind it, no ease. Registers itself as <see cref="IViewCamera"/>
    /// (gameplay aims from its centre ray, UI projects with it) and <see cref="ILookSettings"/> (the UI applies the
    /// player's sensitivity and invert-Y).
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class RoverCameraRig : MonoBehaviour, IGameSystem, IViewCamera, ILookSettings, ISkyGaze
    {
        /// <summary>Look input below this (deg per frame) is sensor noise, not the player looking around.</summary>
        private const float LookEpsilon = 1e-4f;

        /// <summary>Drive input below this (stick magnitude) is drift, not the player driving.</summary>
        private const float DriveEpsilon = 0.05f;

        /// <summary>Furthest the composer may put 07 from the frame's centre (viewport, either way).</summary>
        private const float MaxScreenOffset = 0.45f;

        /// <summary>Subjects nearer than this (m, ground plane) have no meaningful direction to turn to.</summary>
        private const float MinSubjectDistance = 0.5f;

        /// <summary>Below this horizontal speed (m/s) the travel direction is too noisy to judge slopes.</summary>
        private const float DescentMinSpeed = 0.5f;

        /// <summary>Smallest share of the orbit a moment leaves, so adopting the wide yaw never divides by 0.</summary>
        private const float MinOrbitShare = 0.05f;

        /// <summary>Blend weight below this is settled enough to treat a framed shot or face-inspect mode as inactive.</summary>
        private const float ShotWeightEpsilon = 0.01f;

        /// <summary>Blend margin inside (0, 1) where the wake face turn actively probes for terrain clearance.</summary>
        private const float WakeProbeWeightEpsilon = 0.001f;

        /// <summary>Yaw difference (deg) close enough to 180 deg to pick a deterministic turn side.</summary>
        private const float OppositeYawThreshold = 179.9f;

        /// <summary>Squared length above which a reported ground normal is valid rather than zero.</summary>
        private const float ValidGroundNormalSqr = 0.5f;

        /// <summary>Steps either way the evening frame turns, and raises, to find a clear view.</summary>
        private const int HeldShotTurns = 2;
        private const int HeldShotLifts = 3;

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
        private bool _hopListOpen;
        private bool _paused;
        private bool _crawling;
        private float _crawl;
        private float _freeLook;
        private float _momentYaw;
        private float _momentLift;
        private float _momentPullBack;
        private float _framedRoverYaw;
        private float _targetYaw;
        private Vector3 _dampedTargetPosition;
        private Vector3 _momentShift;
        private bool _leapHeld;
        private bool _leapAirborne;
        private IRoverState _rover;
        private IWorldLayout _world;
        private IRoverStillness _stillness;
        private GameContext _context;
        private IRoverBay _bay;
        private float _bayRail;
        private bool _bayBroken;
        private bool _inBay;
        private bool _bayDismissed;
        private bool _giftViewPending;
        private RoverKitPiece _giftViewPiece;
        private IBayFitView _fit;
        private readonly PropSightLine _sight = new PropSightLine();
        private readonly HeldShot _held = new HeldShot();
        private bool _onLift;
        private bool _evening;
        private BeamFraming _beam;
        private bool _charging;
        private float _chargingSeconds;
        private bool _chargeAsks;
        private BayShot _shot;
        private bool _shotSolved;
        private bool _hasShot;
        private float _shotEase;
        private float _shotWeight;
        private readonly FaceInspectSightLine _faceSight = new FaceInspectSightLine();
        private RoverBodyLanguage _faceInspectBody;
        private FaceInspectShot _faceInspectShot;
        private FaceInspectShot _wakeFaceShot;
        private bool _hasWakeFaceShot;
        private bool _faceInspectOpen;
        private bool _faceInspectHasShot;
        private float _faceInspectEase;
        private float _faceInspectWeight;
        private float _openingDampingFade = 1f;
        private float _wallCompressionTarget;
        private float _wallCompression;
        private Renderer[] _headRenderers;
        private bool _headHidden;
        private float _appliedDampingShare = -1f;
        private float _appliedCameraRadius = -1f;
        private ITerrainQuery _terrain;
        private EventBus _events;
        private InputReader _input;
        private CameraOrbit _orbitState;
        private Stargaze _stargaze;
        private WakeOpening _opening;
        private float _wakeYaw;
        private float _wakeEarthYaw;
        private float _wakeEarthScreenY;
        private float _wakeLift;
        private LookSettings _look;
        private DampedSpring _bumpSpring;
        private IDisposable[] _subscriptions;
        private bool _initialized;

        public CameraOrbit Orbit => _orbitState;

        /// <summary>The fresh game's first shot on 07's lens (inactive on a loaded save).</summary>
        public WakeOpening Opening => _opening;

        /// <summary>The rig's own Cinemachine camera: the opening's lens camera leaves the lens toward its pose.</summary>
        public CinemachineCamera ChaseCamera => _camera;

        /// <summary>The dock's evening, when it is open or easing (capture sessions).</summary>
        public HeldShot Held => _held;

        public float SkyLift => _stargaze != null ? Mathf.Max(_stargaze.Lift, _wakeLift) : 0f;

        public float EarthTurn => _opening != null ? _opening.Turn * _wakeLift : 0f;

        /// <summary>The lonely wide shot's state (quiet time, weight, frame), for tests and tooling.</summary>
        public WideShot WideShot => _wide;

        /// <summary>How far the current camera moment has eased in, 0..1 (for tests and tooling).</summary>
        public float MomentWeight => _moment.Weight;

        /// <summary>How far the close shot of a bay fitting has eased in, 0..1 (for tests and tooling).</summary>
        public float FittingShotWeight => _shotWeight;

        /// <summary>How far the CRT face-inspect shot has eased in, 0..1 (for tests and tooling).</summary>
        public float FaceInspectWeight => _faceInspectWeight;

        /// <summary>True while the CRT face-inspect camera mode is open.</summary>
        public bool IsFaceInspectOpen => _faceInspectOpen;

        /// <summary>Neck yaw offset (deg) solved for the opening's front CRT reveal shot.</summary>
        public float WakeFaceNeckYaw => _hasWakeFaceShot ? _wakeFaceShot.NeckYawOffset : 0f;

        /// <summary>The close shot composed for the bay fitting under way, if any (for tests and tooling).</summary>
        public bool TryGetFittingShot(out BayShot shot)
        {
            shot = _shot;
            return _shotSolved && _hasShot;
        }

        /// <summary>The close shot composed for 07's CRT face screen, if any (for tests and tooling).</summary>
        public bool TryGetFaceInspectShot(out FaceInspectShot shot)
        {
            shot = _faceInspectShot;
            return _faceInspectHasShot;
        }

        /// <summary>The front face-inspect shot composed for the opening's CRT reveal, if any (for tests and tooling).</summary>
        public bool TryGetWakeFaceShot(out FaceInspectShot shot)
        {
            shot = _wakeFaceShot;
            return _hasWakeFaceShot;
        }

        /// <summary>Assigns 07's <see cref="RoverBodyLanguage"/> so the wake opening can compose its front CRT reveal shot.</summary>
        public void SetWakeBody(RoverBodyLanguage bodyLanguage)
        {
            if (bodyLanguage != null)
            {
                _faceInspectBody = bodyLanguage;
                CacheHeadRenderers();
            }
        }

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

        public bool GentleCamera
        {
            get => _look.GentleCamera;
            set
            {
                _look.GentleCamera = value;
                ApplyComfort();
            }
        }

        public void Initialize(GameContext context)
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return;
            }

            _context = context;
            _rover = context.Get<IRoverState>();
            _world = context.Get<IWorldLayout>();
            _stillness = context.Get<IRoverStillness>();
            _fit = context.Get<IBayFitView>();
            _terrain = context.Get<ITerrainQuery>();
            _events = context.Events;
            _input = context.Input;
            _orbitState = new CameraOrbit(_tuning);
            _stargaze = new Stargaze(_tuning);
            _opening = new WakeOpening(_tuning);
            _wide = new WideShot(_tuning.WideShot);
            _look = new LookSettings(_tuning);
            _beam = new BeamFraming(_tuning);
            if (_faceInspectBody == null && _rover is Component roverComponent)
            {
                _faceInspectBody = roverComponent.GetComponentInChildren<RoverBodyLanguage>(true);
            }

            CacheHeadRenderers();
            ApplyComfort();
            ApplyCinemachineSettings();
            _subscriptions = new[]
            {
                context.Events.Subscribe<RoverLanded>(OnLanded),
                context.Events.Subscribe<ExcavationStarted>(OnExcavationStarted),
                context.Events.Subscribe<ExcavationStopped>(OnExcavationStopped),
                context.Events.Subscribe<RelicSurfaced>(OnRelicSurfaced),
                context.Events.Subscribe<RelayRestored>(OnRelayRestored),
                context.Events.Subscribe<LadderLookChanged>(OnLadderLookChanged),
                context.Events.Subscribe<FeelBeat>(OnFeelBeat),
                context.Events.Subscribe<CutChargeStaged>(OnChargeStaged),
                context.Events.Subscribe<LiftRideChanged>(OnLiftRideChanged),
                context.Events.Subscribe<DockEveningOpened>(OnDockEveningOpened),
                context.Events.Subscribe<UpgradePurchased>(OnUpgradePurchased),
                context.Events.Subscribe<RoverJumped>(OnJumped),
                context.Events.Subscribe<TetherAttached>(OnTetherAttached),
                context.Events.Subscribe<TetherReleased>(OnTetherReleased),
                context.Events.Subscribe<PauseChanged>(OnPauseChanged),
                context.Events.Subscribe<UiCue>(OnUiCue),
                context.Events.Subscribe<BellCued>(OnBellCued),
                context.Events.Subscribe<RadioHopListChanged>(OnHopListChanged),
                context.Events.Subscribe<RoverPlaced>(OnPlaced),
                context.Events.Subscribe<PartFitted>(OnPartFitted),
                context.Events.Subscribe<LowPowerChanged>(OnLowPowerChanged),
                context.Events.Subscribe<ChargingChanged>(OnChargingChanged),
            };
            context.Register<IViewCamera>(this);
            context.Register<ILookSettings>(this);
            context.Register<ISkyGaze>(this);
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
            tracker.RotationDamping = Vector3.zero;
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
            _deoccluder.AvoidObstacles.CameraRadius = Mathf.Min(_tuning.CameraRadius, _tuning.FaceInspectProbeRadius);
            _deoccluder.AvoidObstacles.Strategy =
                CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            _deoccluder.AvoidObstacles.Damping = _tuning.AvoidanceDamping;
            _deoccluder.AvoidObstacles.DampingWhenOccluded = _tuning.OcclusionDamping;
            _deoccluder.AvoidObstacles.SmoothingTime = _tuning.OcclusionSmoothing;
        }

        /// <summary>
        /// The comfort floors of the player's Gentle camera (or the usual ones): the orbit's recentering and lens, the
        /// shortest blend of every framed shot, and the wide shot's breathing.
        /// </summary>
        private void ApplyComfort()
        {
            bool gentle = _look.GentleCamera;
            float blend = gentle ? _tuning.GentleMinShotBlend : _tuning.MinShotBlend;
            _orbitState.Gentle = gentle;
            _moment.MinBlend = blend;
            _wide.MinBlend = blend;
            _held.MinBlend = blend;
            _wide.BreathRate = gentle ? _tuning.GentleBreathRate : 1f;
        }

        /// <summary>Puts the camera straight behind the rover with no damping (spawn, placement).</summary>
        public void Snap()
        {
            _orbitState.Reset(RoverYaw());
            if (_stargaze.Reset())
            {
                _events.Publish(new StargazingChanged(false));
            }

            _bumpSpring.Reset(0f);
            StepWallCompression(0f, true);
            PlaceTarget();
            StepFreeLookDamping(0f);
            ApplyOrbit();
            _camera.PreviousStateIsValid = false;
        }

        /// <summary>Updates the chase camera pose immediately after refreshing the wake face shot.</summary>
        public void SyncWakeFacePose()
        {
            PlaceTarget();
            StepFreeLookDamping(0f);
            ApplyOrbit();
            _camera.PreviousStateIsValid = false;
        }

        private void CacheHeadRenderers()
        {
            if (_headRenderers != null || _faceInspectBody == null || _faceInspectBody.NeckTransform == null)
            {
                return;
            }

            _headRenderers = _faceInspectBody.NeckTransform.GetComponentsInChildren<Renderer>(true);
        }

        private void SetHeadHidden(bool hidden)
        {
            if (hidden == _headHidden)
            {
                return;
            }

            _headHidden = hidden;
            CacheHeadRenderers();
            if (_headRenderers == null)
            {
                return;
            }

            ShadowCastingMode mode = hidden ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            for (int i = 0; i < _headRenderers.Length; i++)
            {
                Renderer r = _headRenderers[i];
                if (r != null && r.shadowCastingMode != ShadowCastingMode.Off)
                {
                    r.shadowCastingMode = mode;
                }
            }
        }

        private void StepWallCompression(float deltaTime, bool snap)
        {
            bool suppress = _inBay || _faceInspectOpen || _faceInspectWeight > ShotWeightEpsilon || _opening.IsActive
                || _wide.Weight > ShotWeightEpsilon || _held.Weight > ShotWeightEpsilon
                || _shotWeight > ShotWeightEpsilon;
            if (suppress)
            {
                _wallCompressionTarget = snap ? 0f : Smoothing.Damp(_wallCompressionTarget, 0f, _tuning.WallLiftHalfLife, deltaTime);
                _wallCompression = snap ? 0f : Smoothing.Damp(_wallCompression, _wallCompressionTarget, _tuning.WallLiftHalfLife, deltaTime);
                SetHeadHidden(false);
                return;
            }

            Vector3 follow = _rover.Position + Vector3.up * _tuning.TargetHeight;
            float worldYaw = RoverYaw() + _momentYaw + _orbitState.YawOffset * (1f - _moment.Weight);
            float pitchRad = Mathf.Clamp(_orbitState.Elevation + _momentLift, _tuning.MinPitch, _tuning.MaxPitch) * Mathf.Deg2Rad;
            Vector3 back = Quaternion.Euler(0f, worldYaw, 0f) * Vector3.back;
            float radius = _tuning.Distance * (1f + _momentPullBack);
            Vector3 desiredEye = follow + (back * Mathf.Cos(pitchRad) + Vector3.up * Mathf.Sin(pitchRad)) * radius;
            float rayReach = SightReach(follow, desiredEye, _tuning.CameraRadius);
            float effectiveDist = rayReach;
            if (!snap && _camera.PreviousStateIsValid && _viewCamera != null)
            {
                float liveDist = Vector3.Distance(_viewCamera.transform.position, _rover.Position);
                effectiveDist = Mathf.Min(rayReach, liveDist);
            }

            float wanted = Mathf.InverseLerp(_tuning.WallLiftStartDistance, _tuning.WallLiftFullDistance, effectiveDist);
            if (snap)
            {
                _wallCompressionTarget = wanted;
                _wallCompression = wanted;
            }
            else
            {
                _wallCompressionTarget = Smoothing.Damp(_wallCompressionTarget, wanted, _tuning.WallLiftHalfLife, deltaTime);
                _wallCompression = Smoothing.Damp(_wallCompression, _wallCompressionTarget, _tuning.WallLiftHalfLife, deltaTime);
            }

            float fadeThreshold = _headHidden
                ? _tuning.HeadFadeStartDistance + _tuning.HeadFadeHysteresis
                : _tuning.HeadFadeStartDistance;
            SetHeadHidden(effectiveDist < fadeThreshold);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (!_initialized || deltaTime <= 0f)
            {
                return;
            }

            if (_giftViewPending)
            {
                _giftViewPending = false;
                StartGiftView(_giftViewPiece);
            }

            // On the belly lift or in face-inspect mode, looking round must not stir this orbit.
            Vector2 look = _onLift || _faceInspectOpen ? Vector2.zero : ReadLook(deltaTime);
            bool recenter = !_onLift && !_faceInspectOpen && _input.RecenterPressed;
            bool viewInput = recenter || look.sqrMagnitude > LookEpsilon * LookEpsilon;
            if (viewInput)
            {
                _moment.Cancel(_tuning.MomentCancelEaseOut);
                _bayDismissed |= _inBay;
            }

            bool driving = _rover.DriveInput.sqrMagnitude > DriveEpsilon * DriveEpsilon;
            if (_beam.Framing.IsActive && (viewInput || driving))
            {
                _beam.Framing.End(_tuning.CutFrameHandBackSeconds);
            }

            if (driving && IsTowerMoment)
            {
                _moment.Cancel(_tuning.MomentCancelEaseOut);
            }

            if (recenter && _wide.IsOpen)
            {
                HandBackWideShot();
            }

            StepWakeOpening(viewInput, deltaTime);
            ReleaseLeapOnTouchdown();
            StepBay();
            StepShot(deltaTime);
            StepFaceInspect(deltaTime);
            _moment.Step(deltaTime);
            StepWideShot(deltaTime);
            StepHeldShot(viewInput, deltaTime);
            StepMomentFraming(deltaTime);
            _beam.Framing.Step(deltaTime);
            _orbitState.Step(OrbitInput(look, recenter), deltaTime);
            StepWallCompression(deltaTime, false);
            StepFreeLookDamping(deltaTime);
            PlaceTarget(deltaTime, false);
            StepStargaze(deltaTime);

            _bumpSpring.Step(0f, _tuning.BumpFrequency, _tuning.BumpDamping, deltaTime);
            _crawl = Smoothing.Damp(_crawl, _crawling ? 1f : 0f, _tuning.CrawlHalfLife, deltaTime);
            ApplyOrbit();
        }

        /// <summary>
        /// A view of the player's own keeps up with 07 (light position and aim damping), so 07 stays put on screen
        /// and the rendered heading holds while 07 drives and turns under it; the floaty lag comes back as the camera
        /// follows 07 again. Eased both ways, so the switch never jolts. Scales down to zero during face-inspect mode
        /// and the wake opening so the camera tracks scripted trajectories with zero residual lag.
        /// </summary>
        private void StepFreeLookDamping(float deltaTime)
        {
            float target = _orbitState.IsFollowing ? 0f : 1f;
            _freeLook = deltaTime > 0f
                ? Smoothing.Damp(_freeLook, target, _tuning.FreeLookDampingHalfLife, deltaTime)
                : target;
            bool zeroDamping = _faceInspectOpen || _opening.IsActive;
            _openingDampingFade = zeroDamping
                ? 0f
                : (deltaTime > 0f
                    ? Smoothing.Damp(_openingDampingFade, 1f, _tuning.OpeningDampingFadeHalfLife, deltaTime)
                    : 1f);
            float inspectFade = Mathf.Min(1f - _faceInspectWeight, _openingDampingFade);
            float bodyShare = Mathf.Min(_tuning.FreeLookDampingShare, _tuning.FreeLookBodyDampingShare);
            float share = Mathf.Lerp(1f, bodyShare, _freeLook) * inspectFade;
            if (Mathf.Approximately(share, _appliedDampingShare))
            {
                return;
            }

            _appliedDampingShare = share;
            TrackerSettings tracker = _orbit.TrackerSettings;
            tracker.PositionDamping = _tuning.PositionDamping * share;
            _orbit.TrackerSettings = tracker;
            _composer.Damping = _tuning.AimDamping * share;
        }

        /// <summary>
        /// This frame's look and 07's motion for the orbit. It recenters by itself only while nothing else frames the
        /// view: no moment, wide shot, evening or bay close shot, the opening, the lift or the crawl home.
        /// </summary>
        private CameraOrbitInput OrbitInput(Vector2 look, bool recenter)
        {
            Vector3 velocity = _rover.Velocity;
            float horizontal = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
            float roverYaw = RoverYaw();
            Vector3 heading = Quaternion.Euler(0f, roverYaw, 0f) * Vector3.forward;
            float forwardSpeed = velocity.x * heading.x + velocity.z * heading.z;
            float descent;
            if (_rover.IsGrounded)
            {
                float velocityDescent = horizontal > DescentMinSpeed
                    ? Mathf.Atan2(-velocity.y, horizontal) * Mathf.Rad2Deg
                    : 0f;
                Vector3 normal = _rover.GroundNormal;
                float normalDownhill = normal.sqrMagnitude > ValidGroundNormalSqr
                    ? Mathf.Asin(Mathf.Clamp(Vector3.Dot(heading, normal), -1f, 1f)) * Mathf.Rad2Deg
                    : 0f;
                Vector3 chassisForward = _rover.Rotation * Vector3.forward;
                float chassisDownhill = Mathf.Asin(Mathf.Clamp(-chassisForward.y, -1f, 1f)) * Mathf.Rad2Deg;
                float terrainDownhill = Mathf.Max(normalDownhill, chassisDownhill);
                float slopeDescent = terrainDownhill > _tuning.DownhillSlopeMinAngle && forwardSpeed >= -DescentMinSpeed
                    ? terrainDownhill * _tuning.StoppedDownhillShare
                    : 0f;
                descent = Mathf.Max(velocityDescent, slopeDescent);
            }
            else
            {
                float airSpeed = Mathf.Max(horizontal, _tuning.AirbornePitchRefSpeed);
                descent = Mathf.Atan2(-velocity.y, airSpeed) * Mathf.Rad2Deg;
            }

            bool mayRecenter = !_moment.IsActive && _wide.Weight <= 0f && _held.Weight <= 0f && _shotWeight <= 0f
                && !_faceInspectOpen && _faceInspectWeight <= 0f && !_opening.IsActive && !_onLift && !_crawling;
            return new CameraOrbitInput(look, _input.LookRate.sqrMagnitude > 0f, recenter, roverYaw, forwardSpeed,
                _rover.DriveInput.y, _rover.NormalizedSpeed, descent, _rover.IsGrounded, mayRecenter);
        }

        /// <summary>
        /// The current moment's framing as offsets from the normal chase camera (yaw swing, look shift, lift,
        /// pull-back), eased by the moment's weight and smoothed again so one moment flowing into the next (dig into
        /// relic) never jumps. The yaw swing gives back at once the share of any turn of 07's that the moment holds
        /// toward its subject, so a view fixed on the world (the bay's) stays put while the turntable turns 07. With
        /// no moment they all settle to zero.
        /// </summary>
        private void StepMomentFraming(float deltaTime)
        {
            float yaw = 0f;
            float lift = 0f;
            float pullBack = 0f;
            Vector3 shift = Vector3.zero;
            float weight = _moment.Weight;
            float roverYaw = RoverYaw();
            float turned = Mathf.DeltaAngle(_framedRoverYaw, roverYaw);
            _framedRoverYaw = roverYaw;
            if (weight > 0f)
            {
                CameraMomentSettings moment = _moment.Settings;
                Vector3 rover = _rover.Position;
                Vector3 toSubject = _moment.Subject - rover;
                float distance = Mathf.Sqrt(toSubject.x * toSubject.x + toSubject.z * toSubject.z);
                float reach = weight * CameraMoment.FocusReach(moment, distance);
                if (distance > MinSubjectDistance)
                {
                    float subjectYaw = Mathf.Atan2(toSubject.x, toSubject.z) * Mathf.Rad2Deg;
                    yaw = Mathf.DeltaAngle(roverYaw, CameraMoment.BlendYaw(moment, roverYaw, subjectYaw, reach));
                    _momentYaw -= turned * moment.YawShare * Mathf.Clamp01(reach);
                }

                Vector3 follow = rover + Vector3.up * _tuning.TargetHeight;
                shift = CameraMoment.LookPoint(moment, follow, _moment.Subject, reach) - follow;
                lift = moment.Lift * weight;
                pullBack = moment.PullBack * weight;
                if (_look.GentleCamera && ReferenceEquals(moment, _tuning.LeapMoment))
                {
                    pullBack *= _tuning.GentleLeapPullBack;
                }
            }

            float halfLife = _tuning.MomentBlendHalfLife;
            _momentYaw = Smoothing.Damp(_momentYaw, _momentYaw + Mathf.DeltaAngle(_momentYaw, yaw), halfLife,
                deltaTime);
            _momentShift = Smoothing.Damp(_momentShift, shift, halfLife, deltaTime);
            _momentLift = Smoothing.Damp(_momentLift, lift, halfLife, deltaTime);
            _momentPullBack = Smoothing.Damp(_momentPullBack, pullBack, halfLife, deltaTime);
        }

        /// <summary>The camera moment running now is the bay's view.</summary>
        private bool IsBayMoment => _moment.IsActive && ReferenceEquals(_moment.Settings, _tuning.BayMoment);

        /// <summary>
        /// 07 in the bay: the held bay view, unless the player has looked away since 07 drove in or another moment is
        /// playing (the view comes back after it). Out of the bay: it hands back to the chase camera.
        /// </summary>
        private void StepBay()
        {
            if (!ResolveBay())
            {
                return;
            }

            BayFramingSettings settings = _tuning.BayFraming;
            Vector3 offset = _rover.Position - _bay.TurntablePosition;
            float reach = _inBay ? settings.Radius + settings.Hysteresis : settings.Radius;
            _inBay = offset.x * offset.x + offset.z * offset.z <= reach * reach;
            if (!_inBay)
            {
                _bayDismissed = false;
                if (IsBayMoment)
                {
                    _moment.Release();
                }

                return;
            }

            if (!_bayDismissed && (!_moment.IsActive || (IsBayMoment && !_moment.IsHeld)))
            {
                Vector3 into = Vector3.ProjectOnPlane(_bay.TurntableRotation * Vector3.forward, Vector3.up).normalized;
                Vector3 subject = _bay.TurntablePosition + into * settings.SubjectDistance
                    + Vector3.up * _tuning.TargetHeight;
                _moment.Start(_tuning.BayMoment, subject, true);
            }
        }

        /// <summary>
        /// The bay and the height of its crane rail (its lowest arm shoulder), once Gameplay has registered it; a bay
        /// without arms breaks the art contract and is logged once and left alone.
        /// </summary>
        private bool ResolveBay()
        {
            if (_bay != null)
            {
                return true;
            }

            if (_bayBroken || !_context.TryGet(out IRoverBay bay))
            {
                return false;
            }

            float rail = float.MaxValue;
            for (int i = 0; i < bay.ArmCount; i++)
            {
                Transform shoulder = bay.GetArmJoint(i, RoverBayJoint.Yaw);
                if (shoulder == null)
                {
                    rail = float.NaN;
                    break;
                }

                rail = Mathf.Min(rail, shoulder.position.y);
            }

            if (bay.ArmCount < 1 || float.IsNaN(rail))
            {
                Debug.LogError($"{nameof(RoverCameraRig)}: the {nameof(IRoverBay)} has no arms or an arm without its "
                    + $"Yaw shoulder ({BayArm.Contract}); the bay view is off.", this);
                _bayBroken = true;
                return false;
            }

            _bay = bay;
            _bayRail = rail;
            return true;
        }

        /// <summary>
        /// The highest orbit elevation (deg) at <paramref name="radius"/> that keeps the camera the rail clearance
        /// below the bay's crane rail, so it looks in under the roof.
        /// </summary>
        private float BayCeiling(float follow, float radius)
        {
            return Mathf.Asin(Mathf.Clamp((BayTop - follow) / radius, -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>The highest the camera may stand in the bay (world y): the rail clearance under the rail.</summary>
        private float BayTop => _bayRail - _tuning.BayFraming.RailClearance;

        /// <summary>Where the bay's view, fully eased in, stands for 07 parked for the fitting (world).</summary>
        private Vector3 BayEye()
        {
            Vector3 follow = _fit.Centre + Vector3.up * _tuning.TargetHeight;
            float radius = _tuning.Distance * (1f + _tuning.BayMoment.PullBack);
            float pitch = Mathf.Min(_tuning.BayFraming.Elevation, BayCeiling(follow.y, radius)) * Mathf.Deg2Rad;
            Vector3 front = Vector3.ProjectOnPlane(_fit.Front, Vector3.up).normalized;
            return follow + (front * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch)) * radius;
        }

        /// <summary>
        /// The close shot of a bay fitting: composed once as the fitting begins in the bay's view, eased in while the
        /// arms work and out as they fold away (or as soon as looking round dismisses the bay's view).
        /// </summary>
        private void StepShot(float deltaTime)
        {
            BayShotSettings settings = _tuning.BayFraming.Shot;
            if (!_fit.Active)
            {
                _shotSolved = false;
            }
            else if (!_shotSolved && _bay != null && IsBayMoment)
            {
                _shotSolved = true;
                _hasShot = BayShotComposer.TrySolve(settings, _fit, BayEye(), BayTop, _sight, out _shot);
            }

            bool wanted = _shotSolved && _hasShot && _fit.Working && IsBayMoment && !_bayDismissed;
            float seconds = Mathf.Max(wanted ? settings.EaseIn : settings.EaseOut, _moment.MinBlend);
            _shotEase = Mathf.MoveTowards(_shotEase, wanted ? 1f : 0f, deltaTime / seconds);
            _shotWeight = Smoothing.SmoothStep(0f, 1f, _shotEase);
            ApplyCameraRadius(Mathf.Lerp(_tuning.CameraRadius, settings.CameraRadius, _shotWeight));
        }

        /// <summary>The clearance Cinemachine keeps from scenery (smaller in the bay's close shot).</summary>
        private void ApplyCameraRadius(float radius)
        {
            if (Mathf.Approximately(radius, _appliedCameraRadius))
            {
                return;
            }

            _decollider.CameraRadius = radius;
            _deoccluder.AvoidObstacles.CameraRadius = Mathf.Min(radius, _tuning.FaceInspectProbeRadius);
            _appliedCameraRadius = radius;
        }

        /// <summary>
        /// Opens or closes the close-up face-inspect camera mode on 07's CRT <c>Screen</c> (FACE-INV). Solves terrain
        /// and rock occlusion in order (<c>Default -&gt; Left -&gt; Right -&gt; Closer</c>) and directs
        /// <paramref name="bodyLanguage"/> to lift 07's head (<c>+10 deg</c>) and turn its neck (<c>+-35 deg</c> max)
        /// toward the solved camera bearing while suppressing idle head noise.
        /// </summary>
        public void SetFaceInspect(bool open, RoverBodyLanguage bodyLanguage)
        {
            _faceInspectOpen = open;
            if (bodyLanguage != null)
            {
                _faceInspectBody = bodyLanguage;
                CacheHeadRenderers();
            }

            if (open)
            {
                SetHeadHidden(false);
                if (_wide != null && _wide.IsOpen)
                {
                    HandBackWideShot();
                }

                _moment.Clear();
                _giftViewPending = false;
                SolveFaceInspectShot();
                if (_faceInspectBody != null && _faceInspectHasShot)
                {
                    _faceInspectBody.SetScreenInspect(
                        true,
                        _faceInspectShot.NeckYawOffset,
                        _tuning.FaceInspectHeadPitch,
                        counterRollShare: _tuning.FaceInspectHeadCounterRollShare,
                        counterRollMaxDeg: _tuning.FaceInspectHeadCounterRollMax);
                }
            }
            else if (_faceInspectBody != null)
            {
                _faceInspectBody.SetScreenInspect(false, 0f, 0f);
            }
        }

        private void SolveFaceInspectShot()
        {
            if (_faceInspectBody == null || _rover == null)
            {
                return;
            }

            _faceInspectBody.SetScreenInspectCounterRoll(
                _tuning.FaceInspectHeadCounterRollShare,
                _tuning.FaceInspectHeadCounterRollMax);
            _faceInspectBody.EvaluateScreenPose(0f, _tuning.FaceInspectHeadPitch,
                out Vector3 baseScreenPos, out Quaternion baseScreenRot);
            Vector3 neckPivot = _faceInspectBody.NeckTransform != null
                ? _faceInspectBody.NeckTransform.position
                : baseScreenPos;
            FaceInspectShot solved = FaceInspectComposer.Solve(
                neckPivot,
                _rover.Rotation,
                baseScreenPos,
                baseScreenRot,
                _tuning.FaceInspectScreenHeight,
                _tuning.FaceInspectHeightFill,
                _tuning.FaceInspectDistance,
                _tuning.FaceInspectMinDistance,
                _tuning.FaceInspectLookOffsetY,
                _tuning.FaceInspectSideYaw,
                _tuning.FaceInspectProbeRadius,
                _faceSight);
            _faceInspectBody.EvaluateScreenPose(solved.NeckYawOffset, _tuning.FaceInspectHeadPitch,
                out Vector3 exactScreenPos, out Quaternion exactScreenRot);
            FaceInspectComposer.ComposePose(exactScreenPos, exactScreenRot, _tuning.FaceInspectLookOffsetY,
                solved.Distance, out Vector3 exactFocus, out Vector3 exactEye);
            _faceInspectShot = new FaceInspectShot(
                exactFocus,
                exactEye,
                solved.Distance,
                solved.VerticalFov,
                solved.NeckYawOffset,
                solved.Choice,
                exactScreenRot * Vector3.up);
            _faceInspectHasShot = true;
        }

        private void StepFaceInspect(float deltaTime)
        {
            if (_faceInspectOpen && _faceInspectBody != null && _rover != null)
            {
                if (_rover.Speed > 0.05f || !_faceInspectHasShot)
                {
                    SolveFaceInspectShot();
                    _faceInspectBody.SetScreenInspect(
                        true,
                        _faceInspectShot.NeckYawOffset,
                        _tuning.FaceInspectHeadPitch,
                        counterRollShare: _tuning.FaceInspectHeadCounterRollShare,
                        counterRollMaxDeg: _tuning.FaceInspectHeadCounterRollMax);
                }
                else
                {
                    _faceInspectBody.EvaluateScreenPose(_faceInspectShot.NeckYawOffset, _tuning.FaceInspectHeadPitch,
                        out Vector3 screenPos, out Quaternion screenRot);
                    FaceInspectComposer.ComposePose(screenPos, screenRot, _tuning.FaceInspectLookOffsetY,
                        _faceInspectShot.Distance, out Vector3 focus, out Vector3 eye);
                    _faceInspectShot = new FaceInspectShot(focus, eye, _faceInspectShot.Distance,
                        _faceInspectShot.VerticalFov, _faceInspectShot.NeckYawOffset, _faceInspectShot.Choice,
                        screenRot * Vector3.up);
                }
            }

            float duration = _faceInspectOpen ? _tuning.FaceInspectOpenSeconds : _tuning.FaceInspectCloseSeconds;
            float seconds = _look != null && _look.GentleCamera
                ? Mathf.Max(duration * 1.35f, 0.75f)
                : Mathf.Max(0.05f, duration);
            _faceInspectEase = Mathf.MoveTowards(_faceInspectEase, _faceInspectOpen ? 1f : 0f, deltaTime / seconds);
            _faceInspectWeight = Smoothing.SmoothStep(0f, 1f, _faceInspectEase);

            bool cineAvoidance = !_faceInspectOpen && _faceInspectWeight < ShotWeightEpsilon && !_opening.IsActive;
            if (_decollider.TerrainResolution.Enabled != cineAvoidance)
            {
                _decollider.TerrainResolution.Enabled = cineAvoidance;
                _decollider.Decollision.Enabled = cineAvoidance;
                _deoccluder.AvoidObstacles.Enabled = cineAvoidance;
            }
        }

        private void BlendFaceInspect(ref float orbitYaw, ref float elevation, ref float radius, ref float fov,
            ref Vector2 screen)
        {
            Vector3 target = _target.position;
            Vector3 focus = Vector3.Lerp(target, _faceInspectShot.Focus, _faceInspectWeight);
            Vector3 shotLine = _faceInspectShot.Focus - _faceInspectShot.Eye;
            float shotRadius = Mathf.Max(_tuning.FaceInspectMinRadius, shotLine.magnitude);
            float shotElevation = Mathf.Asin(Mathf.Clamp(-shotLine.y / shotRadius, -1f, 1f)) * Mathf.Rad2Deg;
            float shotYaw = Mathf.Atan2(shotLine.x, shotLine.z) * Mathf.Rad2Deg;

            float currentYaw = _targetYaw + orbitYaw;
            float deltaYaw = Mathf.DeltaAngle(currentYaw, shotYaw);
            if (Mathf.Abs(deltaYaw) > OppositeYawThreshold)
            {
                deltaYaw = 180f;
            }

            _targetYaw = currentYaw + deltaYaw * _faceInspectWeight;
            orbitYaw = 0f;
            elevation = Mathf.Lerp(elevation, shotElevation, _faceInspectWeight);
            radius = Mathf.Lerp(radius, shotRadius, _faceInspectWeight);
            fov = Mathf.Lerp(fov, _faceInspectShot.VerticalFov, _faceInspectWeight);
            screen = Vector2.Lerp(screen, Vector2.zero, _faceInspectWeight);
            _target.SetPositionAndRotation(focus, Quaternion.Euler(0f, _targetYaw, 0f));
        }

        /// <summary>
        /// Blends the orbit's pose toward the fitting shot by its weight along straight lines (where the camera stands
        /// and where it looks), so it comes in through the bay's open front rather than swinging round through a wall,
        /// and hands the result back to the orbit as a target, a yaw, an elevation and a distance.
        /// </summary>
        private void BlendShot(ref float orbitYaw, ref float elevation, ref float radius)
        {
            Vector3 target = _target.position;
            float pitch = Mathf.Clamp(elevation, _tuning.MinPitch, _tuning.MaxPitch) * Mathf.Deg2Rad;
            Vector3 back = Quaternion.Euler(0f, _targetYaw + orbitYaw, 0f) * Vector3.back;
            Vector3 eye = target + (back * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch)) * radius;
            Vector3 focus = Vector3.Lerp(target, _shot.Focus, _shotWeight);
            Vector3 line = focus - Vector3.Lerp(eye, _shot.Eye, _shotWeight);
            radius = line.magnitude;
            elevation = Mathf.Asin(Mathf.Clamp(-line.y / radius, -1f, 1f)) * Mathf.Rad2Deg;
            _targetYaw = Mathf.Atan2(line.x, line.z) * Mathf.Rad2Deg;
            orbitYaw = 0f;
            _target.SetPositionAndRotation(focus, Quaternion.Euler(0f, _targetYaw, 0f));
        }

        private float RoverYaw()
        {
            Vector3 forward = _rover.Rotation * Vector3.forward;
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        /// <summary>Follow point above 07 facing its heading, offset by the current moment's framing.</summary>
        private void PlaceTarget(float deltaTime = 0f, bool snap = true)
        {
            float targetHeight = _tuning.TargetHeight + _wallCompression * _tuning.WallMaxTargetLift;
            Vector3 point = _rover.Position + Vector3.up * targetHeight + _momentShift;
            float cut = _beam.Framing.Weight;
            if (cut > 0f)
            {
                point = Vector3.Lerp(point, BeamMiddle(), cut);
            }

            float inspectFade = Mathf.Min(1f - _faceInspectWeight, _openingDampingFade);
            float freeTargetShare = Mathf.Max(0f, _tuning.FreeLookDampingShare - _tuning.FreeLookBodyDampingShare)
                * _freeLook * inspectFade;
            if (snap || deltaTime <= 0f || freeTargetShare <= LookEpsilon)
            {
                _dampedTargetPosition = point;
            }
            else
            {
                Vector3 halfLife = _tuning.PositionDamping * (freeTargetShare * _tuning.FreeLookTargetHalfLifeScale);
                _dampedTargetPosition.x = Smoothing.Damp(_dampedTargetPosition.x, point.x, halfLife.x, deltaTime);
                _dampedTargetPosition.y = Smoothing.Damp(_dampedTargetPosition.y, point.y, halfLife.y, deltaTime);
                _dampedTargetPosition.z = Smoothing.Damp(_dampedTargetPosition.z, point.z, halfLife.z, deltaTime);
            }

            _targetYaw = RoverYaw() + _momentYaw;
            _target.SetPositionAndRotation(_dampedTargetPosition, Quaternion.Euler(0f, _targetYaw, 0f));
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
            float elevation = _orbitState.Elevation + _momentLift + _wallCompression * _tuning.WallMaxPitchLift
                - _crawl * _tuning.CrawlLower;
            float radius = _tuning.Distance * (1f + _momentPullBack) * (1f + _crawl * _tuning.CrawlPullBack);
            if (IsBayMoment)
            {
                float follow = _rover.Position.y + _tuning.TargetHeight + _momentShift.y;
                float bay = Mathf.Min(_tuning.BayFraming.Elevation, BayCeiling(follow, radius));
                elevation = Mathf.Lerp(elevation, bay, _moment.Weight);
            }

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

            float framed = _held.Weight;
            if (framed > 0f)
            {
                BlendHeldShot(framed, ref yaw, ref elevation, ref radius, ref fov, ref screen);
            }

            float cutFrame = _beam.Framing.Weight;
            if (cutFrame > 0f)
            {
                BlendCutFraming(cutFrame, ref yaw, ref elevation, ref radius);
            }

            float wake = _opening.Weight;
            float wakeFaceWeight = 0f;
            if (wake > 0f)
            {
                float turn = _opening.Turn;
                if (_hasWakeFaceShot)
                {
                    wakeFaceWeight = wake * (1f - turn);
                    Vector3 shotLine = _wakeFaceShot.Focus - _wakeFaceShot.Eye;
                    float faceRadius = Mathf.Max(_tuning.FaceInspectMinRadius, shotLine.magnitude);
                    float faceElevation = Mathf.Asin(Mathf.Clamp(-shotLine.y / faceRadius, -1f, 1f)) * Mathf.Rad2Deg;
                    float faceWorldYaw = Mathf.Atan2(shotLine.x, shotLine.z) * Mathf.Rad2Deg;
                    float earthYaw = yaw + Mathf.DeltaAngle(yaw, _wakeEarthYaw);
                    float deltaEarthToFace = Mathf.DeltaAngle(_wakeEarthYaw, faceWorldYaw - _targetYaw);
                    if (Mathf.Abs(deltaEarthToFace) > OppositeYawThreshold)
                    {
                        deltaEarthToFace = _wakeYaw < 0f ? -180f : 180f;
                    }

                    float faceYaw = earthYaw + deltaEarthToFace;
                    float wakeShotYaw = Mathf.Lerp(faceYaw, earthYaw, turn) + _opening.DriftYaw;
                    float wakeShotElevation = Mathf.Lerp(faceElevation, _tuning.WakeEarthElevation, turn);
                    float wakeShotRadius = Mathf.Lerp(faceRadius, _tuning.WakeEarthDistance, turn);
                    float wakeShotFov = Mathf.Lerp(_wakeFaceShot.VerticalFov, fov, turn);
                    Vector2 wakeShotScreen = Vector2.Lerp(Vector2.zero, new Vector2(screen.x, _wakeEarthScreenY), turn);
                    Vector3 wakeTargetPos = Vector3.Lerp(_wakeFaceShot.Focus, _target.position, turn);

                    Vector3 focus = Vector3.Lerp(_target.position, wakeTargetPos, wake);
                    yaw = Mathf.Lerp(yaw, wakeShotYaw, wake);
                    elevation = Mathf.Lerp(elevation, wakeShotElevation, wake);
                    radius = Mathf.Lerp(radius, wakeShotRadius, wake);
                    fov = Mathf.Lerp(fov, wakeShotFov, wake);
                    screen = Vector2.Lerp(screen, wakeShotScreen, wake);

                    if (wakeFaceWeight > WakeProbeWeightEpsilon && wakeFaceWeight < 1f - WakeProbeWeightEpsilon)
                    {
                        float pitchRad = Mathf.Clamp(elevation, _tuning.MinPitch, _tuning.MaxPitch) * Mathf.Deg2Rad;
                        Vector3 back = Quaternion.Euler(0f, _targetYaw + yaw, 0f) * Vector3.back;
                        Vector3 candidateEye = focus + (back * Mathf.Cos(pitchRad) + Vector3.up * Mathf.Sin(pitchRad)) * radius;
                        float clear = _faceSight.Clear(focus, candidateEye, _tuning.FaceInspectProbeRadius);
                        if (clear < radius)
                        {
                            radius = Mathf.Max(_tuning.FaceInspectMinDistance, clear);
                        }
                    }

                    _target.position = focus;
                }
                else
                {
                    float shotYaw = _wakeYaw + Mathf.DeltaAngle(_wakeYaw, _wakeEarthYaw) * turn + _opening.DriftYaw;
                    yaw += Mathf.DeltaAngle(yaw, shotYaw) * wake;
                    elevation = Mathf.Lerp(elevation,
                        Mathf.Lerp(_tuning.WakeElevation, _tuning.WakeEarthElevation, turn), wake);
                    radius = Mathf.Lerp(radius, Mathf.Lerp(_tuning.WakeDistance, _tuning.WakeEarthDistance, turn), wake);
                    screen.y = Mathf.Lerp(screen.y, Mathf.Lerp(_tuning.WakeScreenY, _wakeEarthScreenY, turn), wake);
                }
            }

            if (_shotWeight > 0f)
            {
                BlendShot(ref yaw, ref elevation, ref radius);
            }

            if (_faceInspectWeight > 0f && _faceInspectHasShot)
            {
                BlendFaceInspect(ref yaw, ref elevation, ref radius, ref fov, ref screen);
            }

            float effectiveInspectWeight = Mathf.Max(_faceInspectWeight, wakeFaceWeight);
            float orbitShare = 1f - effectiveInspectWeight;
            _orbit.HorizontalAxis.Value = yaw;
            elevation = Mathf.Lerp(Mathf.Clamp(elevation, _tuning.MinPitch, _tuning.MaxPitch), elevation, effectiveInspectWeight);
            float floor = LowOrbit.Floor(radius, _tuning.TargetHeight, _tuning.LowestCameraHeight) * orbitShare;
            float held = Mathf.Lerp(Mathf.Max(elevation, floor), elevation, effectiveInspectWeight);
            _orbit.VerticalAxis.Value = held;
            float tilt = Mathf.Min(held - elevation, _tuning.MaxLookUpTilt) * orbitShare;
            _composer.TargetOffset = Vector3.up * (LowOrbit.AimLift(floor, tilt, radius) * orbitShare);
            _orbit.Radius = radius;
            _camera.Lens.FieldOfView = fov;
            _camera.Lens.NearClipPlane = Mathf.Lerp(_tuning.NearClip, _tuning.FaceInspectNearClip, effectiveInspectWeight);
            ScreenComposerSettings composition = _composer.Composition;
            composition.ScreenPosition = screen;
            _composer.Composition = composition;
            _bump.Offset = Vector3.up * (_bumpSpring.Value * orbitShare);
            _bump.FaceInspectWeight = _faceInspectHasShot ? _faceInspectWeight : 0f;
            _bump.FaceInspectEye = _faceInspectShot.Eye;
            _bump.FaceInspectRotation = _faceInspectShot.Rotation;
            _bump.FaceInspectUp = _faceInspectShot.ScreenUp;
        }

        /// <summary>
        /// Counts 07's rest toward the wide shot (a moment, a leap or the tether keep it closed; the pause menu only
        /// delays it) and opens or hands it back when it says so. A charge asks for it once, after its charging delay.
        /// </summary>
        private void StepWideShot(float deltaTime)
        {
            bool busy = _moment.IsActive || _leapHeld || _tethered || _hopListOpen || _inBay || _stargaze.LookUp > 0f
                || _opening.IsActive || _held.IsOpen || _onLift || _faceInspectOpen || _faceInspectWeight > 0f;
            _chargingSeconds = _charging ? _chargingSeconds + deltaTime : 0f;
            bool asked = _chargeAsks && _chargingSeconds >= _tuning.WideShot.ChargingDelay;
            switch (_wide.Step(_stillness.StillSeconds, busy, _paused, asked, deltaTime))
            {
                case WideShotCue.Open:
                    _chargeAsks = false;
                    OpenWideShot();
                    break;
                case WideShotCue.HandBack:
                    HandBackWideShot();
                    break;
            }
        }

        /// <summary>
        /// The lens shot on 07's eye (M3-16 P5, cutscenes §1): the fresh title holds it on the closed lens and the
        /// opening's pull-out lands on it (<paramref name="held"/>); then it turns round over 07's shoulder to Earth
        /// and eases into the chase frame. Started by the opening only, so a loaded save never replays it.
        /// </summary>
        public void BeginWakeOpening(bool held)
        {
            SetHeadHidden(false);
            if (_opening.IsActive)
            {
                PlaceTarget();
                FrameWakeShots();
                StepFreeLookDamping(0f);
                ApplyOrbit();
                _camera.PreviousStateIsValid = false;
                return;
            }

            _opening.Begin(held);
            _wakeLift = 0f;
            PlaceTarget();
            FrameWakeShots();
            StepFreeLookDamping(0f);
            ApplyOrbit();
            _camera.PreviousStateIsValid = false;
        }

        /// <summary>
        /// Re-solves the front CRT reveal shot using <see cref="FaceInspectComposer.Solve"/> and its sphere-cast
        /// occlusion fallback (<see cref="FaceInspectSightLine"/>) at 07's current dock position and rotation.
        /// </summary>
        public void RefreshWakeFaceShot()
        {
            if (_faceInspectBody == null || _rover == null)
            {
                _hasWakeFaceShot = false;
                return;
            }

            _faceInspectBody.EvaluateScreenPose(0f, _tuning.FaceInspectHeadPitch,
                out Vector3 baseScreenPos, out Quaternion baseScreenRot);
            Vector3 neckPivot = _faceInspectBody.NeckTransform != null
                ? _faceInspectBody.NeckTransform.position
                : baseScreenPos;
            _wakeFaceShot = FaceInspectComposer.Solve(
                neckPivot,
                _rover.Rotation,
                baseScreenPos,
                baseScreenRot,
                _tuning.FaceInspectScreenHeight,
                _tuning.FaceInspectHeightFill,
                _tuning.FaceInspectDistance,
                _tuning.FaceInspectMinDistance,
                _tuning.FaceInspectLookOffsetY,
                _tuning.FaceInspectSideYaw,
                _tuning.FaceInspectProbeRadius,
                _faceSight);
            _hasWakeFaceShot = true;
        }

        /// <summary>
        /// The two beats' framing, fixed once 07 rests on its dock. Beat 1 frames 07's front CRT face using the
        /// face-inspect composer; beat 2 looks along Earth's bearing from behind 07 (the orbit looks along the target's
        /// yaw plus its axis value), raised so Earth sits at the tuned height in the frame.
        /// </summary>
        private void FrameWakeShots()
        {
            Vector3 earth = _world.EarthDirection;
            float bearing = WideShotComposer.Bearing(earth);
            float heading = RoverYaw();
            float positive = Mathf.Abs(Mathf.DeltaAngle(heading - _tuning.WakeYaw, bearing));
            float negative = Mathf.Abs(Mathf.DeltaAngle(heading + _tuning.WakeYaw, bearing));
            _wakeYaw = positive <= negative ? _tuning.WakeYaw : -_tuning.WakeYaw;
            _wakeEarthYaw = Mathf.DeltaAngle(_targetYaw, bearing);
            float floor = LowOrbit.Floor(_tuning.WakeEarthDistance, _tuning.TargetHeight, _tuning.LowestCameraHeight);
            float elevation = Mathf.Max(_tuning.WakeEarthElevation, floor);
            float earthElevation = Mathf.Asin(Mathf.Clamp(earth.y, -1f, 1f)) * Mathf.Rad2Deg;
            _wakeEarthScreenY = Mathf.Clamp(WakeOpening.EarthScreenY(earthElevation, elevation,
                _tuning.WakeEarthHeight, _orbitState.FieldOfView), -MaxScreenOffset, MaxScreenOffset);
            RefreshWakeFaceShot();
        }

        /// <summary>
        /// The title over a save (M3-09 Part A): the high, wide shot of home from behind 07 on its dock, the dock's
        /// evening framing, held until <see cref="ReleaseTitleFrame"/>.
        /// </summary>
        public void HoldTitleFrame()
        {
            Vector3 follow = _rover.Position + Vector3.up * _tuning.TargetHeight;
            float home = WideShotComposer.Bearing(_world.BasePosition - _rover.Position);
            OpenHeldShot(_tuning.EveningShot, follow, home, 0f);
        }

        /// <summary>Continue: the title's wide frame eases back into the chase frame.</summary>
        public void ReleaseTitleFrame()
        {
            if (_held.IsOpen)
            {
                CloseHeldShot();
            }
        }

        /// <summary>
        /// Holds the first shot until any input or the hold runs out; 07 lifts its gaze with it once the shot is no
        /// longer held (asleep on the title and inside the lens, its head stays its own).
        /// </summary>
        /// <param name="viewInput">The player looked round or pressed the recenter button this frame.</param>
        private void StepWakeOpening(bool viewInput, float deltaTime)
        {
            bool input = viewInput || _rover.DriveInput.sqrMagnitude > DriveEpsilon * DriveEpsilon;
            _opening.Step(input, deltaTime);
            float target = _opening.IsHeld ? 0f : _opening.Weight;
            float halfLife = target > _wakeLift ? _tuning.StargazeRiseHalfLife : _tuning.StargazeFallHalfLife;
            _wakeLift = Smoothing.Damp(_wakeLift, target, halfLife, deltaTime);
        }

        /// <summary>
        /// Looking up with 07 at rest: 07 joins the gaze, and after a rest the stargazing beat begins (published so UI
        /// and audio can quiet down); drive input or the view dropping ends it.
        /// </summary>
        private void StepStargaze(float deltaTime)
        {
            bool moving = _rover.DriveInput.sqrMagnitude > DriveEpsilon * DriveEpsilon
                || _rover.Speed > _tuning.StargazeMaxSpeed;
            bool free = !_moment.IsActive && _wide.Weight <= 0f && _shotWeight <= 0f && !_inBay && !_tethered
                && !_leapHeld && !_opening.IsActive && _held.Weight <= 0f && !_faceInspectOpen && _faceInspectWeight <= 0f;
            if (_stargaze.Step(_orbitState.Elevation, _stillness.StillSeconds, moving, free, deltaTime))
            {
                _events.Publish(new StargazingChanged(_stargaze.IsActive));
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
        /// Eases back to the chase camera. The view keeps the bearing the wide shot turned to (the orbit adopts it),
        /// so only distance, height and lens come back: the quickest return that never swings the world round.
        /// </summary>
        private void HandBackWideShot()
        {
            if (_wideSteersYaw)
            {
                AdoptViewBearing();
                _wideSteersYaw = false;
            }

            _wide.HandBack();
            _events.Publish(new RoverWideShotChanged(false));
        }

        /// <summary>
        /// The dock's evening, blended like the wide shot: toward its world yaw, elevation, distance, lens and 07's
        /// place on screen, breathing slowly once open.
        /// </summary>
        private void BlendHeldShot(float weight, ref float yaw, ref float elevation, ref float radius, ref float fov,
            ref Vector2 screen)
        {
            WideShotSettings settings = _held.Settings;
            WideShotFrame frame = _held.Frame;
            float time = _held.Seconds;
            float sway = WideShotComposer.Breath(settings.BreathYaw, settings.BreathYawPeriod, time);
            float heldYaw = Mathf.DeltaAngle(RoverYaw() + _momentYaw, frame.Yaw + sway);
            yaw += Mathf.DeltaAngle(yaw, heldYaw) * weight;
            float rise = WideShotComposer.Breath(settings.BreathElevation, settings.BreathElevationPeriod, time);
            float drift = WideShotComposer.Breath(settings.BreathDistance, settings.BreathDistancePeriod, time);
            elevation = Mathf.Lerp(elevation, frame.Elevation + rise, weight);
            radius = Mathf.Lerp(radius, frame.Distance * (1f + drift), weight);
            fov = Mathf.Lerp(fov, _tuning.BaseFov + settings.FovWiden, weight);
            screen.y = Mathf.Lerp(screen.y, frame.ScreenY, weight);
        }

        /// <summary>
        /// The dock's evening ends on any drive or view input or once its hold has run; while 07 charges it holds at
        /// least as long as the fill, up to the evening's fill hold (a longer fill goes on in the chase frame).
        /// </summary>
        /// <param name="viewInput">The player looked round or pressed the recenter button this frame.</param>
        private void StepHeldShot(bool viewInput, float deltaTime)
        {
            bool input = viewInput || _rover.DriveInput.sqrMagnitude > DriveEpsilon * DriveEpsilon;
            bool fillHolds = _evening && _charging && _held.Seconds < _tuning.EveningFillHoldMax;
            if ((_held.HoldRanOut && !fillHolds) || (_evening && input))
            {
                CloseHeldShot();
            }

            _held.Step(deltaTime);
        }

        /// <summary>
        /// The belly lift carries 07 from the moment it starts up until it has set 07 down again (docs/features/M3-19):
        /// the lab camera has the view meanwhile, so the lonely wide shot stays away.
        /// </summary>
        private void OnLiftRideChanged(LiftRideChanged ride)
        {
            _onLift = ride.Riding || ride.Inside;
        }

        /// <summary>
        /// 07 parked at its dock after a trip (M3-16 P6): the camera eases out behind 07 to a wide shot of home's newly
        /// lit lamps beyond it, holds, then eases back to the chase frame.
        /// </summary>
        private void OnDockEveningOpened(DockEveningOpened evening)
        {
            Vector3 follow = _rover.Position + Vector3.up * _tuning.TargetHeight;
            float home = WideShotComposer.Bearing(_world.BasePosition - evening.DockPosition);
            OpenHeldShot(_tuning.EveningShot, follow, home, _tuning.EveningHoldSeconds);
            _evening = true;
        }

        /// <summary>
        /// Eases out to the frame along <paramref name="yaw"/>, fitted to the ground and clear of solid scenery (see
        /// <see cref="ClearHeldFrame"/>).
        /// </summary>
        private void OpenHeldShot(WideShotSettings settings, Vector3 follow, float yaw, float holdSeconds)
        {
            if (_wide.IsOpen)
            {
                HandBackWideShot();
            }

            bool wasOpen = _held.IsOpen;
            _chargeAsks = false;
            _held.Open(settings, ClearHeldFrame(settings, follow, yaw), holdSeconds);
            _evening = false;
            if (!wasOpen)
            {
                _events.Publish(new RoverWideShotChanged(true));
            }
        }

        /// <summary>
        /// The frame fitted to the ground (it rises, then comes closer), tried along the yaw asked and then turned a
        /// step either way and raised toward the settings' highest elevation until nothing solid (the lander, the bay)
        /// lies between 07 and the camera. If no try is clear, the one that sees farthest, brought in short of what
        /// blocks it.
        /// </summary>
        private WideShotFrame ClearHeldFrame(WideShotSettings settings, Vector3 follow, float yaw)
        {
            float clearance = _tuning.HeldShotClearance;
            WideShotFrame best = default;
            float bestReach = -1f;
            for (int turn = 0; turn <= 2 * HeldShotTurns; turn++)
            {
                float offset = (turn + 1) / 2 * (turn % 2 == 0 ? -1f : 1f) * _tuning.HeldShotYawStep;
                WideShotFrame fit = WideShotComposer.Fit(settings, _terrain, follow, yaw + offset, 0f);
                for (int lift = 0; lift <= HeldShotLifts; lift++)
                {
                    float elevation = Mathf.Max(fit.Elevation,
                        Mathf.Lerp(settings.Elevation, settings.MaxElevation, lift / (float)HeldShotLifts));
                    float pitch = elevation * Mathf.Deg2Rad;
                    Vector3 behind = Vector3.up * Mathf.Sin(pitch)
                        - WideShotComposer.Direction(fit.Yaw) * Mathf.Cos(pitch);
                    float reach = SightReach(follow, follow + behind * fit.Distance, clearance);
                    var frame = new WideShotFrame(fit.Yaw, elevation, Mathf.Min(fit.Distance, reach - clearance),
                        fit.ScreenY);
                    if (reach >= fit.Distance)
                    {
                        return frame;
                    }

                    if (reach > bestReach)
                    {
                        bestReach = reach;
                        best = frame;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// How far (m) from 07 toward <paramref name="eye"/> the view is clear, cast both ways: out from 07 and in from
        /// the camera as the deoccluder does, since 07 can rest inside a hollow collider (under the lander's deck)
        /// whose faces a cast from inside never meets.
        /// </summary>
        private float SightReach(Vector3 follow, Vector3 eye, float radius)
        {
            float length = Vector3.Distance(follow, eye);
            float inward = _sight.Clear(eye, follow, radius);
            float outward = _sight.Clear(follow, eye, radius);
            return inward >= length ? outward : Mathf.Min(outward, length - inward);
        }

        /// <summary>Eases back to the chase camera keeping the shot's bearing, as the wide shot does.</summary>
        private void CloseHeldShot()
        {
            AdoptViewBearing();
            _held.Close();
            _evening = false;
            _events.Publish(new RoverWideShotChanged(false));
        }

        /// <summary>
        /// Hands the bearing the view looks along now back to the player's orbit (the orbit's share of the yaw, undone
        /// by the moment's weight), so control returns where the shot left the view.
        /// </summary>
        private void AdoptViewBearing()
        {
            float share = Mathf.Max(1f - _moment.Weight, MinOrbitShare);
            _orbitState.AdoptBearing(RoverYaw() + _orbit.HorizontalAxis.Value / share);
        }

        private void OnTetherAttached(TetherAttached attached)
        {
            _tethered = true;
        }

        private void OnTetherReleased(TetherReleased released)
        {
            _tethered = false;
        }

        private void OnHopListChanged(RadioHopListChanged list)
        {
            _hopListOpen = list.Open;
        }

        /// <summary>
        /// 07 jumped somewhere else while the view was dark: the wide shot and any moment end at once and the camera
        /// snaps behind 07, so nothing lerps across from the old spot.
        /// </summary>
        /// <summary>
        /// 07 crawls home on its reserve (docs/features/M3-20): the chase eases wider and a touch lower, a quiet frame;
        /// a loaded crawl starts framed.
        /// </summary>
        /// <summary>
        /// A charge starting asks for the wide shot after its delay, unless a held shot (the dock's evening) already
        /// has the view.
        /// </summary>
        private void OnChargingChanged(ChargingChanged charging)
        {
            _charging = charging.Charging;
            _chargeAsks = _charging && !_held.IsOpen;
        }

        private void OnLowPowerChanged(LowPowerChanged lowPower)
        {
            _crawling = lowPower.LowPower;
            if (lowPower.Restored)
            {
                _crawl = _crawling ? 1f : 0f;
            }
        }

        private void OnPlaced(RoverPlaced placed)
        {
            bool wasWide = _wide.IsOpen || _held.IsOpen;
            _wide.Close();
            _held.Reset();
            _evening = false;
            _wideSteersYaw = false;
            _moment.Clear();
            _giftViewPending = false;
            _inBay = false;
            _bayDismissed = false;
            _shotSolved = false;
            _shotEase = 0f;
            _shotWeight = 0f;
            _faceInspectOpen = false;
            _faceInspectHasShot = false;
            _faceInspectEase = 0f;
            _faceInspectWeight = 0f;
            if (_faceInspectBody != null)
            {
                _faceInspectBody.SetScreenInspect(false, 0f, 0f);
            }

            _leapHeld = false;
            _leapAirborne = false;
            _momentYaw = 0f;
            _momentLift = 0f;
            _momentPullBack = 0f;
            _momentShift = Vector3.zero;
            _beam.Framing.Reset();
            SetHeadHidden(false);
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

        /// <summary>
        /// 07's first look up the crew's ladder, then at the lift (M3-16 "Each place"): one held moment, low, looking
        /// up, that eases from the ladder across to the lift as 07's gaze moves and lets go once the look ends.
        /// </summary>
        private void OnLadderLookChanged(LadderLookChanged look)
        {
            if (look.Looking)
            {
                _moment.Start(_tuning.LadderMoment, look.Subject, true);
            }
            else if (_moment.IsActive && ReferenceEquals(_moment.Settings, _tuning.LadderMoment))
            {
                _moment.Release();
            }
        }

        /// <summary>The charge-up's stages: <see cref="BeamFraming"/> decides whether the cut pushes in.</summary>
        private void OnChargeStaged(CutChargeStaged staged)
        {
            _beam.OnStaged(staged, _rover.Position, _rover.Rotation * Vector3.forward, _look.GentleCamera);
        }

        /// <summary>The tower coming online, and the beam's beats for <see cref="BeamFraming"/> (the cut's push-in
        /// and the dig's shoulder framing; look or drive input hands either back at once).</summary>
        private void OnFeelBeat(FeelBeat beat)
        {
            if (beat.Kind == FeelKind.Build)
            {
                OnTowerBuilt(beat);
                return;
            }

            if (beat.Kind == FeelKind.Install && beat.Phase == FeelPhase.Anticipate && beat.Strength < 1f)
            {
                OnGiftAnticipated();
                return;
            }

            _beam.OnBeat(beat, _rover.Position, _rover.Rotation * Vector3.forward, _look.GentleCamera);
        }

        /// <summary>The camera moment running now is the radio tower coming online.</summary>
        private bool IsTowerMoment => _moment.IsActive && (ReferenceEquals(_moment.Settings, _tuning.TowerMoment)
            || ReferenceEquals(_moment.Settings, _tuning.GentleTowerMoment));

        /// <summary>
        /// The radio tower comes online (docs/design/feel-pass.md §8.2): the camera eases round to frame the tower
        /// with 07 in front (a gentler turn with the Gentle camera), held until the beat's last Follow, then eases
        /// back. Gameplay publishes it on a level-up only, never on a load.
        /// </summary>
        private void OnTowerBuilt(FeelBeat beat)
        {
            if (beat.Phase == FeelPhase.Anticipate)
            {
                CameraMomentSettings moment = _look.GentleCamera ? _tuning.GentleTowerMoment : _tuning.TowerMoment;
                _moment.Start(moment, beat.Position, true);
            }
            else if (beat.Phase == FeelPhase.Follow && beat.Strength >= 1f && IsTowerMoment)
            {
                _moment.Release();
            }
        }

        /// <summary>The middle of the beam, between 07's emitter (its follow point) and the cut point.</summary>
        private Vector3 BeamMiddle()
        {
            Vector3 emitter = _rover.Position + Vector3.up * _tuning.TargetHeight;
            return Vector3.Lerp(emitter, _beam.Framing.CutPoint, 0.5f);
        }

        /// <summary>
        /// The orbit toward the over-the-shoulder eye, looking at the beam's middle (where the target goes): the
        /// emitter, the beam and the cut point all in view, never rolled.
        /// </summary>
        private void BlendCutFraming(float weight, ref float yaw, ref float elevation, ref float radius)
        {
            if (_beam.IsDig)
            {
                BlendDigFraming(weight, ref yaw, ref elevation, ref radius);
                return;
            }

            CutFraming.PushIn(weight, _tuning.CutFramePushIn, _tuning.CutFrameLower, ref elevation, ref radius);
            Vector3 emitter = _rover.Position + Vector3.up * _tuning.TargetHeight;
            Vector3 line = BeamMiddle() - _beam.Framing.Eye(emitter, _tuning.CutFrameBack, _tuning.CutFrameSide, 0f);
            line.y = 0f;
            if (line.magnitude < MinSubjectDistance)
            {
                return;
            }

            float frameYaw = Mathf.DeltaAngle(RoverYaw() + _momentYaw, WideShotComposer.Bearing(line));
            yaw += Mathf.DeltaAngle(yaw, frameYaw) * weight;
        }

        /// <summary>The dig's framing: the orbit toward the shoulder eye, a little higher, looking at the beam's middle.
        /// </summary>
        private void BlendDigFraming(float weight, ref float yaw, ref float elevation, ref float radius)
        {
            float share = _look.GentleCamera ? _tuning.DigFrameGentleShare : 1f;
            Vector3 emitter = _rover.Position + Vector3.up * _tuning.TargetHeight;
            Vector3 eye = _beam.Framing.Eye(emitter, _tuning.CutFrameBack, _tuning.CutFrameSide * share,
                _tuning.DigFrameRise * share);
            Vector3 line = BeamMiddle() - eye;
            float distance = line.magnitude;
            if (distance < MinSubjectDistance)
            {
                return;
            }

            float frameYaw = Mathf.DeltaAngle(RoverYaw() + _momentYaw, WideShotComposer.Bearing(line));
            yaw += Mathf.DeltaAngle(yaw, frameYaw) * weight;
            elevation = Mathf.Lerp(elevation, Mathf.Asin(Mathf.Clamp(-line.y / distance, -1f, 1f)) * Mathf.Rad2Deg,
                weight);
            radius = Mathf.Lerp(radius, distance, weight);
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

        /// <summary>
        /// A base upgrade (the radio tower) takes in the base; kit for 07 itself is fitted inside the bay's view.
        /// </summary>
        private void OnUpgradePurchased(UpgradePurchased upgrade)
        {
            if (RoverKitPieces.IsRoverUpgrade(upgrade.UpgradeId))
            {
                return;
            }

            _moment.Start(_tuning.UpgradeMoment, _world.BasePosition + Vector3.up * _tuning.TargetHeight, false);
        }

        /// <summary>
        /// A part fitted: a gift or a port part gets the gift view (taken this frame); bay kit is fitted inside the
        /// bay's view, which the fitting brings back if the player had looked away.
        /// </summary>
        private void OnPartFitted(PartFitted fitted)
        {
            if (!fitted.Gift && !RoverKitPieces.IsPortPiece(fitted.Part))
            {
                _bayDismissed = false;
                return;
            }

            _giftViewPending = true;
            _giftViewPiece = fitted.Part;
        }

        /// <summary>A gift's soft moment with no part behind it (Bell's paint): its view, unless a part claimed it.</summary>
        private void OnGiftAnticipated()
        {
            if (!_giftViewPending)
            {
                _giftViewPending = true;
                _giftViewPiece = RoverKitPiece.FreshPaint;
            }
        }

        /// <summary>
        /// The gift view: the camera eases low and round to the side the piece faces (gift views), holds while it
        /// settles, then returns.
        /// </summary>
        private void StartGiftView(RoverKitPiece piece)
        {
            KitViewSettings views = _tuning.KitViews;
            float bearing = RoverYaw() + views.Bearing(piece);
            Vector3 toward = Quaternion.Euler(0f, bearing, 0f) * Vector3.forward;
            Vector3 subject = _rover.Position + toward * views.SubjectDistance;
            _moment.Start(_tuning.GiftMoment, subject, false);
        }

        /// <summary>A soft dip on landing; the Gentle camera stays level.</summary>
        private void OnLanded(RoverLanded landed)
        {
            if (_look.GentleCamera)
            {
                return;
            }

            _bumpSpring.AddVelocity(-Mathf.Min(landed.ImpactSpeed * _tuning.BumpPerImpact, _tuning.MaxBumpKick));
        }

        private void OnDestroy()
        {
            SetHeadHidden(false);
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
