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
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class RoverCameraRig : MonoBehaviour, IGameSystem
    {
        /// <summary>Below this horizontal speed (m/s) the travel direction is too noisy to judge slopes.</summary>
        private const float DescentMinSpeed = 0.5f;

        [Tooltip("Camera tuning (Assets/_Project/Data/Tuning/RoverCameraTuning.asset).")]
        [SerializeField] private RoverCameraTuning _tuning;

        [Tooltip("Follow/look-at target this rig moves above the rover each frame.")]
        [SerializeField] private Transform _target;

        [SerializeField] private CinemachineCamera _camera;
        [SerializeField] private CinemachineOrbitalFollow _orbit;
        [SerializeField] private CinemachineRotationComposer _composer;
        [SerializeField] private CinemachineDecollider _decollider;
        [SerializeField] private CinemachineDeoccluder _deoccluder;
        [SerializeField] private RoverCameraBump _bump;

        private IRoverState _rover;
        private InputReader _input;
        private CameraOrbit _orbitState;
        private DampedSpring _bumpSpring;
        private IDisposable _landedSubscription;
        private bool _initialized;

        public CameraOrbit Orbit => _orbitState;

        public void Initialize(GameContext context)
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return;
            }

            _rover = context.Get<IRoverState>();
            _input = context.Input;
            _orbitState = new CameraOrbit(_tuning);
            ApplyCinemachineSettings();
            _landedSubscription = context.Events.Subscribe<RoverLanded>(OnLanded);
            _initialized = true;
            Snap();
        }

        private bool ValidateWiring()
        {
            bool ok = _tuning != null && _target != null && _camera != null && _orbit != null && _composer != null
                && _decollider != null && _deoccluder != null && _bump != null;
            if (!ok)
            {
                Debug.LogError($"{nameof(RoverCameraRig)}: tuning, target or a Cinemachine component is not assigned.",
                    this);
            }

            return ok;
        }

        private void ApplyCinemachineSettings()
        {
            _camera.Follow = _target;
            _camera.LookAt = _target;

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

        /// <summary>Puts the camera straight behind the rover with no damping (spawn only).</summary>
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

            PlaceTarget();

            Vector3 velocity = _rover.Velocity;
            float horizontal = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
            float descent = horizontal > DescentMinSpeed ? Mathf.Atan2(-velocity.y, horizontal) * Mathf.Rad2Deg : 0f;
            _orbitState.Step(ReadLook(deltaTime), _rover.Speed, _rover.NormalizedSpeed, descent, _rover.IsGrounded,
                deltaTime);

            _bumpSpring.Step(0f, _tuning.BumpFrequency, _tuning.BumpDamping, deltaTime);
            ApplyOrbit();
        }

        private void PlaceTarget()
        {
            Vector3 forward = _rover.Rotation * Vector3.forward;
            float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            _target.SetPositionAndRotation(_rover.Position + Vector3.up * _tuning.TargetHeight,
                Quaternion.Euler(0f, yaw, 0f));
        }

        /// <summary>Mouse pixels are already per-frame; the stick is a rate. Up tilts the view up.</summary>
        private Vector2 ReadLook(float deltaTime)
        {
            Vector2 look = _input.LookDelta * _tuning.MouseSensitivity
                + _input.LookRate * (_tuning.StickRate * deltaTime);
            float vertical = _tuning.InvertY ? look.y : -look.y;
            return new Vector2(look.x, vertical);
        }

        private void ApplyOrbit()
        {
            _orbit.HorizontalAxis.Value = _orbitState.YawOffset;
            _orbit.VerticalAxis.Value = _orbitState.Elevation;
            _camera.Lens.FieldOfView = _orbitState.FieldOfView;
            _bump.Offset = Vector3.up * _bumpSpring.Value;
        }

        private void OnLanded(RoverLanded landed)
        {
            _bumpSpring.AddVelocity(-Mathf.Min(landed.ImpactSpeed * _tuning.BumpPerImpact, _tuning.MaxBumpKick));
        }

        private void OnDestroy()
        {
            _landedSubscription?.Dispose();
        }
    }
}
