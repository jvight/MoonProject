using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Tire tracks and ballistic regolith dust. All four wheel sockets sample the ground for track ribbons while rear
    /// dust sockets drive the rooster-tail emitters; dust rate follows speed while grounded, boosting when accelerating
    /// or turning hard and dropping to zero when still. A towed load on <see cref="ITowLine"/> kicks up matching
    /// ballistic skid dust under its hitch while sliding across the ground. A ring of dust bursts out on every
    /// announced <see cref="RoverLanded"/>, scaled by impact. Ticked by <see cref="RoverController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverWheelFx : MonoBehaviour
    {
        private const float WheelTrackHalfWidth = 0.64f;
        private const float LifetimeMinFactor = 0.80f;
        private const float SpeedMinFactor = 0.75f;
        private const float SizeMinFactor = 0.70f;
        private const float MinDeltaTime = 1e-5f;
        private const float MinDirectionSqr = 1e-6f;

        [Tooltip("Wheel effects tuning (Assets/_Project/Data/Tuning/RoverFxTuning.asset).")]
        [SerializeField] private RoverFxTuning _tuning;

        [Tooltip("RoverModel 'Wheel_FL' (front-left wheel contact).")]
        [SerializeField] private Transform _frontSocketLeft;

        [Tooltip("RoverModel 'Wheel_FR' (front-right wheel contact).")]
        [SerializeField] private Transform _frontSocketRight;

        [Tooltip("RoverModel 'DustSocket_L' (rear-left wheel contact).")]
        [SerializeField] private Transform _dustSocketLeft;

        [Tooltip("RoverModel 'DustSocket_R' (rear-right wheel contact).")]
        [SerializeField] private Transform _dustSocketRight;

        [SerializeField] private RoverTrackRenderer _trackFrontLeft;
        [SerializeField] private RoverTrackRenderer _trackFrontRight;
        [SerializeField] private RoverTrackRenderer _trackLeft;
        [SerializeField] private RoverTrackRenderer _trackRight;

        [Tooltip("Rolling dust emitter for the left side (world simulation space).")]
        [SerializeField] private ParticleSystem _dustLeft;

        [Tooltip("Rolling dust emitter for the right side (world simulation space).")]
        [SerializeField] private ParticleSystem _dustRight;

        [Tooltip("Ballistic skid dust emitter under a towed load (world simulation space).")]
        [SerializeField] private ParticleSystem _towDust;

        [Tooltip("Landing ring emitter (cone splash in its local XY plane, world simulation space, no automatic emission).")]
        [SerializeField] private ParticleSystem _landingDust;

        private GameContext _context;
        private RoverController _rover;
        private ITowLine _towLine;
        private Vector3 _lastTowHitch;
        private float _lastTowSpeed;
        private bool _wasTowing;
        private IDisposable _landedSubscription;

        public RoverTrackRenderer TrackFrontLeft => _trackFrontLeft;
        public RoverTrackRenderer TrackFrontRight => _trackFrontRight;
        public RoverTrackRenderer TrackLeft => _trackLeft;
        public RoverTrackRenderer TrackRight => _trackRight;
        public ParticleSystem TowDust => _towDust;

        /// <summary>
        /// Pure calculation of per-side wheel dust emission rate (grains/s): zero when ungrounded or still, scaling
        /// with speed or pivot scrub and boosted by longitudinal acceleration and hard turning.
        /// </summary>
        public static float ComputeWheelDustRate(RoverFxTuning tuning, bool rolling, float speed,
            float yawRateDegPerSec, float longitudinalAcceleration)
        {
            if (tuning == null || !rolling)
            {
                return 0f;
            }

            float scrubSpeed = Mathf.Abs(yawRateDegPerSec) * Mathf.Deg2Rad * WheelTrackHalfWidth;
            float effectiveSpeed = Mathf.Max(speed, scrubSpeed);
            float baseFactor = Smoothing.SmoothStep(tuning.DustMinSpeed, tuning.DustFullSpeed, effectiveSpeed);
            if (baseFactor <= 0f)
            {
                return 0f;
            }

            float accel01 = Smoothing.SmoothStep(0f, tuning.DustAccelFull, Mathf.Abs(longitudinalAcceleration));
            float turn01 = Smoothing.SmoothStep(0f, tuning.DustTurnFullYawRate, Mathf.Abs(yawRateDegPerSec));
            float boost = 1f + tuning.DustAccelBoost * accel01 + tuning.DustTurnBoost * turn01;
            return tuning.DustMaxRate * baseFactor * boost;
        }

        /// <summary>
        /// Pure calculation of ballistic skid dust emission rate (grains/s) under a towed load: zero when not towing,
        /// airborne, or still, scaling with ground sliding speed and boosted by longitudinal acceleration.
        /// </summary>
        public static float ComputeTowSkidDustRate(RoverFxTuning tuning, bool towingAndGrounded, float speed,
            float acceleration)
        {
            if (tuning == null || !towingAndGrounded)
            {
                return 0f;
            }

            float baseFactor = Smoothing.SmoothStep(tuning.DustMinSpeed, tuning.DustFullSpeed, speed);
            if (baseFactor <= 0f)
            {
                return 0f;
            }

            float accel01 = Smoothing.SmoothStep(0f, tuning.DustAccelFull, Mathf.Abs(acceleration));
            float boost = 1f + tuning.DustAccelBoost * accel01;
            return tuning.TowSkidDustMaxRate * baseFactor * boost;
        }

        /// <summary>Validates the wiring and subscribes to landings. Returns false (and logs) when broken.</summary>
        public bool Initialize(GameContext context, RoverController rover)
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return false;
            }

            _context = context;
            _rover = rover;
            _trackFrontLeft.Initialize(_tuning);
            _trackFrontRight.Initialize(_tuning);
            _trackLeft.Initialize(_tuning);
            _trackRight.Initialize(_tuning);
            ConfigureRolling(_dustLeft);
            ConfigureRolling(_dustRight);
            if (_towDust != null)
            {
                ConfigureRolling(_towDust);
            }

            Configure(_landingDust, _tuning.LandingLifetime, _tuning.LandingDrag, _tuning.LandingOpacity);
            _landedSubscription = context.Events.Subscribe<RoverLanded>(OnLanded);
            return true;
        }

        private bool ValidateWiring()
        {
            return Require(_tuning != null, "RoverFxTuning is not assigned.")
                & Require(_frontSocketLeft != null && _frontSocketRight != null, "Front wheel sockets are not assigned.")
                & Require(_dustSocketLeft != null && _dustSocketRight != null, "Dust sockets are not assigned.")
                & Require(_trackFrontLeft != null && _trackFrontRight != null, "Front track renderers are not assigned.")
                & Require(_trackLeft != null && _trackRight != null, "Track renderers are not assigned.")
                & Require(_dustLeft != null && _dustRight != null, "Dust emitters are not assigned.")
                & Require(_landingDust != null, "Landing dust emitter is not assigned.");
        }

        private bool Require(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"{nameof(RoverWheelFx)}: {message}", this);
            }

            return condition;
        }

        private void ConfigureRolling(ParticleSystem system)
        {
            Configure(system, _tuning.DustLifetime, _tuning.DustDrag, _tuning.DustOpacity);
            ParticleSystem.MainModule main = system.main;
            main.startSize = new ParticleSystem.MinMaxCurve(SizeMinFactor * _tuning.DustSize, _tuning.DustSize);
            main.startSpeed = new ParticleSystem.MinMaxCurve(SpeedMinFactor * _tuning.DustSpeed, _tuning.DustSpeed);
        }

        /// <summary>Applies the tuned lifetime, drag, warm-grey ground tint, opacity and lunar gravity.</summary>
        private void Configure(ParticleSystem system, float lifetime, float drag, float opacity)
        {
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(LifetimeMinFactor * lifetime, lifetime);
            main.gravityModifierMultiplier = _tuning.DustGravity;
            main.startColor = new ParticleSystem.MinMaxGradient(
                WithAlpha(_tuning.DustShadowColor, opacity),
                WithAlpha(_tuning.DustColor, opacity));

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = false;

            ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
            limit.enabled = drag > 0f;
            limit.dragMultiplier = drag;

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = false;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTimeMultiplier = 0f;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public void Tick()
        {
            bool rolling = _rover.IsGrounded && _rover.HasGroundContact;
            float rate = ComputeWheelDustRate(
                _tuning,
                rolling,
                _rover.Speed,
                _rover.YawRate,
                _rover.LocalAcceleration.z);
            float time = Time.time;
            Quaternion rotation = _rover.Rotation;

            UpdateTrackOnly(_frontSocketLeft, _trackFrontLeft, rolling, rotation, time);
            UpdateTrackOnly(_frontSocketRight, _trackFrontRight, rolling, rotation, time);
            UpdateSide(_dustSocketLeft, _trackLeft, _dustLeft, rolling, rate, rotation, time);
            UpdateSide(_dustSocketRight, _trackRight, _dustRight, rolling, rate, rotation, time);
            UpdateTowSkidDust();
        }

        private void UpdateTrackOnly(Transform socket, RoverTrackRenderer track, bool rolling, Quaternion rotation,
            float time)
        {
            Vector3 up = rotation * Vector3.up;
            Vector3 position = socket.position;
            float probe = _tuning.TrackGroundProbe;
            if (rolling && Physics.Raycast(position + up * probe, -up, out RaycastHit hit, probe * 2f,
                    Layers.DriveableMask, QueryTriggerInteraction.Ignore))
            {
                track.Sample(hit.point, hit.normal, time);
            }
            else
            {
                track.Break();
            }

            track.Rebuild(time);
        }

        private void UpdateSide(Transform socket, RoverTrackRenderer track, ParticleSystem dust, bool rolling,
            float rate, Quaternion rotation, float time)
        {
            Vector3 up = rotation * Vector3.up;
            Vector3 position = socket.position;
            float probe = _tuning.TrackGroundProbe;
            bool onGround = false;
            if (rolling && Physics.Raycast(position + up * probe, -up, out RaycastHit hit, probe * 2f,
                    Layers.DriveableMask, QueryTriggerInteraction.Ignore))
            {
                onGround = true;
                position = hit.point;
                track.Sample(hit.point, hit.normal, time);
            }
            else
            {
                track.Break();
            }

            track.Rebuild(time);

            dust.transform.SetPositionAndRotation(position, rotation);
            ParticleSystem.EmissionModule emission = dust.emission;
            emission.rateOverTimeMultiplier = onGround ? rate : 0f;
        }

        private void UpdateTowSkidDust()
        {
            if (_towDust == null)
            {
                return;
            }

            if (_towLine == null && _context != null)
            {
                _context.TryGet(out _towLine);
            }

            ParticleSystem.EmissionModule emission = _towDust.emission;
            if (_towLine == null || !_towLine.IsTowing)
            {
                _wasTowing = false;
                _lastTowSpeed = 0f;
                emission.rateOverTimeMultiplier = 0f;
                return;
            }

            Vector3 hitch = _towLine.LoadHitch;
            if (!_wasTowing)
            {
                _wasTowing = true;
                _lastTowHitch = hitch;
                _lastTowSpeed = 0f;
                emission.rateOverTimeMultiplier = 0f;
                return;
            }

            Vector3 delta = hitch - _lastTowHitch;
            _lastTowHitch = hitch;
            delta.y = 0f;
            float dt = Time.deltaTime;
            float speed = dt > MinDeltaTime ? delta.magnitude / dt : 0f;
            float accel = dt > MinDeltaTime ? Mathf.Abs(speed - _lastTowSpeed) / dt : 0f;
            _lastTowSpeed = speed;

            float probe = _tuning.TrackGroundProbe;
            if (Physics.Raycast(hitch + Vector3.up * probe, Vector3.down, out RaycastHit hit, probe * 2f,
                    Layers.DriveableMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 forward = delta.sqrMagnitude > MinDirectionSqr ? delta.normalized : _rover.transform.forward;
                Vector3 tangent = Vector3.ProjectOnPlane(forward, hit.normal);
                Quaternion towRotation = tangent.sqrMagnitude > MinDirectionSqr
                    ? Quaternion.LookRotation(tangent.normalized, hit.normal)
                    : _rover.Rotation;
                _towDust.transform.SetPositionAndRotation(hit.point, towRotation);
                emission.rateOverTimeMultiplier = ComputeTowSkidDustRate(_tuning, true, speed, accel);
            }
            else
            {
                emission.rateOverTimeMultiplier = 0f;
            }
        }

        /// <summary>Ends all four tire tracks where they are, so no ribbon stretches across a placement.</summary>
        public void Break()
        {
            _wasTowing = false;
            _lastTowSpeed = 0f;
            _trackFrontLeft.Break();
            _trackFrontRight.Break();
            _trackLeft.Break();
            _trackRight.Break();
        }

        /// <summary>A small soft puff of dust at <paramref name="position"/>, for coat that falls off 07.</summary>
        public void Puff(Vector3 position, Vector3 normal, int count)
        {
            _landingDust.transform.SetPositionAndRotation(position, Quaternion.FromToRotation(Vector3.forward, normal));
            ParticleSystem.MainModule main = _landingDust.main;
            main.startSpeedMultiplier = _tuning.LandingMinSpeed;
            main.startSizeMultiplier = _tuning.LandingMinSize;
            _landingDust.Emit(count);
        }

        private void OnLanded(RoverLanded landed)
        {
            if (!enabled || landed.ImpactSpeed < _tuning.LandingMinImpact)
            {
                return;
            }

            float impact = Mathf.InverseLerp(_tuning.LandingMinImpact, _tuning.LandingFullImpact, landed.ImpactSpeed);
            float flight = Mathf.InverseLerp(_tuning.LandingRingAirTimeFrom, _tuning.LandingRingAirTimeFull,
                landed.AirTime);
            float strength = Mathf.Max(impact, flight);
            Vector3 normal = _rover.GroundNormal;
            _landingDust.transform.SetPositionAndRotation(landed.Position,
                Quaternion.FromToRotation(Vector3.forward, normal));

            ParticleSystem.MainModule main = _landingDust.main;
            main.startSpeedMultiplier = Mathf.Lerp(_tuning.LandingMinSpeed, _tuning.LandingMaxSpeed, strength);
            main.startSizeMultiplier = Mathf.Lerp(_tuning.LandingMinSize, _tuning.LandingMaxSize, strength);
            int count = Mathf.RoundToInt(Mathf.Lerp(_tuning.LandingMinCount, _tuning.LandingMaxCount, strength));
            _landingDust.Emit(count);
        }

        private void OnDestroy()
        {
            _landedSubscription?.Dispose();
        }
    }
}
