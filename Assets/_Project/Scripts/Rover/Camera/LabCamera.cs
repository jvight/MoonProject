using System;
using UnityEngine;
using Unity.Cinemachine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// The crew's lab through the camera (docs/features/M3-19 §6), with 07's ride pose on the belly lift:
    /// <list type="bullet">
    /// <item>As the platform starts up (<see cref="LiftRideChanged"/>), the platform holds 07 (<see cref="LiftRide"/>)
    /// and the lab camera takes the view from exactly where the chase camera stood, then follows 07 on a crane path in
    /// under the belly and up through the hatch to the ring rail, arriving as the platform settles.</item>
    /// <item>Inside there is no free camera: it rides the ring rail under the ceiling, opposite the station 07 faces,
    /// over its shoulder, and goes round the ring as the turntable turns (it follows 07's heading, so with the same
    /// easing). Looking at a station (<see cref="LabStationLooked"/>) eases it in to a three-quarter view of the
    /// station's face, with a narrower lens, and back out. The rail is inset from the walls and the near plane is
    /// close, so it never clips.</item>
    /// <item>On the way down the crane path reverses to where the chase camera now stands behind 07, which then takes
    /// the view back; the platform lets 07 go once it has set it down.</item>
    /// </list>
    /// Nothing allocates per frame.
    /// </summary>
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    public sealed class LabCamera : MonoBehaviour, IGameSystem
    {
        /// <summary>
        /// Share of the crane's way at which its look turns from 07's lens to where it is going: as it enters the
        /// shaft on the way up (from the ring on the way down, back toward 07 and then the chase view).
        /// </summary>
        private const float AimTurnStart = 0.5f;

        private enum Shot
        {
            Off,
            Up,
            Ring,
            Down,
        }

        [Tooltip("Camera tuning (Assets/_Project/Data/Tuning/RoverCameraTuning.asset); the lab is its Lab block.")]
        [SerializeField] private RoverCameraTuning _tuning;

        [Tooltip("The chase camera rig: its own Cinemachine camera and the view camera.")]
        [SerializeField] private RoverCameraRig _rig;

        [Tooltip("07 itself: the platform holds it while it rides.")]
        [SerializeField] private RoverController _rover;

        [Tooltip("The lab camera: a Cinemachine camera with no body or aim, placed by this each frame it is live.")]
        [SerializeField] private CinemachineCamera _camera;

        [Tooltip("BellyLift/Platform: rises along the shaft and turns as the turntable.")]
        [SerializeField] private Transform _platform;

        [Tooltip("BellyLift: on the ground at the shaft's axis.")]
        [SerializeField] private Transform _shaft;

        [Tooltip("Lab/RingRail: centred on the shaft at the rail's height.")]
        [SerializeField] private Transform _ringRail;

        [Tooltip("Lab/Station_<n> in LabStation order: each station's face, +Z out toward the shaft.")]
        [SerializeField] private Transform[] _stations = Array.Empty<Transform>();

        [Tooltip("The ring rail's radius (m), Art's LabRingRadius.")]
        [Min(0.5f)] [SerializeField] private float _ringRadius = 2.4f;

        private LiftRide _ride;
        private LabCameraSettings _settings;
        private IDisposable[] _subscriptions = Array.Empty<IDisposable>();
        private Shot _shot;
        private float _elapsed;
        private Vector3 _from;
        private Vector3 _fromAim;
        private float _stationHeight;
        private float _stationReach;
        private float _focus;
        private float _focusTarget;
        private LabStation _focusStation;
        private bool _initialized;

        /// <summary>True while the lab camera has the view (crane path, ring or on the way back).</summary>
        public bool Live => _shot != Shot.Off;

        /// <summary>True while it rides the ring rail in the lab.</summary>
        public bool OnRing => _shot == Shot.Ring;

        /// <summary>How far it has eased in to a station, 0..1.</summary>
        public float Focus => _focus;

        public void Initialize(GameContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            string problem = _tuning == null || _rig == null || _rover == null || _camera == null
                ? "tuning, the chase rig, 07 or the lab camera is not assigned"
                : _platform == null || _shaft == null || _ringRail == null || _ringRail.parent == null
                    ? "a belly lift or lab node is not assigned (docs/ARCHITECTURE.md \"Contract: the lab\")"
                    : _stations == null || _stations.Length != Enum.GetValues(typeof(LabStation)).Length
                      || Array.IndexOf(_stations, null) >= 0
                        ? "the lab's four station frames are not assigned"
                        : null;
            if (problem != null)
            {
                Debug.LogError($"{nameof(LabCamera)}: {problem}; rebuild the Main scene.", this);
                enabled = false;
                return;
            }

            _settings = _tuning.Lab;
            _ride = new LiftRide(_rover, _platform, _settings);
            CinemachineCamera chase = _rig.ChaseCamera;
            chase.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Always;
            _camera.Priority = chase.Priority.Value + 2;
            LensSettings lens = _camera.Lens;
            lens.NearClipPlane = _settings.NearClip;
            lens.FarClipPlane = _tuning.FarClip;
            lens.FieldOfView = _settings.Fov;
            _camera.Lens = lens;
            _camera.enabled = false;
            Vector3 axis = _ringRail.position;
            foreach (Transform station in _stations)
            {
                _stationHeight += station.position.y / _stations.Length;
                _stationReach += Flat(station.position - axis).magnitude / _stations.Length;
            }

            _subscriptions = new[]
            {
                context.Events.Subscribe<LiftRideChanged>(OnRide),
                context.Events.Subscribe<LabStationLooked>(OnLooked),
            };
            _initialized = true;
        }

        /// <summary>
        /// The ride starts up (the crane takes over from the chase view), starts down (the crane heads back) or has set
        /// 07 down (the platform lets it go).
        /// </summary>
        private void OnRide(LiftRideChanged ride)
        {
            if (ride.Riding)
            {
                _ride.Begin();
                Transform view = _rig.Camera.transform;
                Vector3 from = _shot == Shot.Off ? view.position : _camera.transform.position;
                Vector3 aim = from + (_shot == Shot.Off ? view.forward : _camera.transform.forward);
                Begin(_shot == Shot.Off ? Shot.Up : Shot.Down, from, aim);
                return;
            }

            if (!ride.Inside)
            {
                _ride.End();
            }
        }

        private void OnLooked(LabStationLooked looked)
        {
            _focusTarget = looked.Looking ? 1f : 0f;
            if (looked.Looking)
            {
                _focusStation = looked.Station;
            }
        }

        private void Begin(Shot shot, Vector3 from, Vector3 aim)
        {
            _shot = shot;
            _elapsed = 0f;
            _from = from;
            _fromAim = aim;
            _focus = 0f;
            _focusTarget = 0f;
            _camera.enabled = true;
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            if (!_initialized)
            {
                return;
            }

            _ride.Step(deltaTime);
            if (_shot == Shot.Off)
            {
                return;
            }

            _elapsed += deltaTime;
            float facing = _rover.Rotation.eulerAngles.y;
            Vector3 axis = _ringRail.position;
            Vector3 ring = LabShot.RingPosition(axis, axis.y, _ringRadius, facing, _settings);
            Vector3 ringAim = LabShot.RingAim(axis, _stationHeight, _stationReach, facing, _settings);
            Vector3 position;
            Vector3 aim;
            float fov = _settings.Fov;
            switch (_shot)
            {
                case Shot.Up:
                    float up = _elapsed / _settings.CraneSeconds;
                    position = Crane(_from, ring, LabShot.Ease(up));
                    aim = Vector3.Lerp(Lens(), ringAim, LabShot.Ease((up - AimTurnStart) / (1f - AimTurnStart)));
                    if (up >= 1f)
                    {
                        _shot = Shot.Ring;
                    }

                    break;
                case Shot.Down:
                    float down = _elapsed / _settings.CraneSeconds;
                    CameraState chase = _rig.ChaseCamera.State;
                    Vector3 home = chase.GetFinalPosition();
                    position = Crane(home, _from, 1f - LabShot.Ease(down));
                    Vector3 homeAim = home + chase.GetFinalOrientation() * Vector3.forward;
                    aim = Vector3.Lerp(Vector3.Lerp(_fromAim, Lens(), LabShot.Ease(down / (1f - AimTurnStart))),
                        homeAim, LabShot.Ease((down - AimTurnStart) / (1f - AimTurnStart)));
                    fov = Mathf.Lerp(_settings.Fov, _rig.Camera.fieldOfView, LabShot.Ease(down));
                    if (down >= 1f && !_ride.Holding)
                    {
                        _shot = Shot.Off;
                        _camera.enabled = false;
                        return;
                    }

                    break;
                default:
                    _focus = Mathf.MoveTowards(_focus, _focusTarget, deltaTime / _settings.FocusSeconds);
                    float weight = LabShot.Ease(_focus);
                    Transform station = _stations[(int)_focusStation];
                    Vector3 close = LabShot.FocusPosition(station.position, station.forward, station.right,
                        _settings);
                    position = Vector3.Lerp(ring, close, weight);
                    aim = Vector3.Lerp(ringAim, station.position, weight);
                    fov = Mathf.Lerp(_settings.Fov, _settings.FocusFov, weight);
                    break;
            }

            _camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(aim - position, Vector3.up));
            LensSettings lens = _camera.Lens;
            lens.FieldOfView = fov;
            _camera.Lens = lens;
        }

        /// <summary>
        /// The crane path from <paramref name="outside"/> (the chase view) to <paramref name="inside"/> (the ring) at
        /// <paramref name="t"/>: in under the belly toward the outside view's side, then up the shaft's gap beside
        /// the platform, through the open floor hatch.
        /// </summary>
        private Vector3 Crane(Vector3 outside, Vector3 inside, float t)
        {
            Vector3 axis = _shaft.position;
            Vector3 under = LabShot.UnderBelly(axis, outside, _settings);
            float cabinFloor = _ringRail.parent.position.y;
            Vector3 entry = LabShot.Shaft(axis, cabinFloor - _settings.ShaftEntryDepth, inside, _settings);
            Vector3 floor = LabShot.Shaft(axis, cabinFloor + _settings.CabinRise, inside, _settings);
            return LabShot.Crane(outside, under, entry, floor, inside, t);
        }

        /// <summary>07's lens, a little above: what the crane follows.</summary>
        private Vector3 Lens()
        {
            return _rover.TetherOrigin.position + Vector3.up * _settings.FollowAim;
        }

        private static Vector3 Flat(Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
        }

        private void OnDestroy()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            if (_initialized)
            {
                _ride.End();
            }
        }
    }
}
