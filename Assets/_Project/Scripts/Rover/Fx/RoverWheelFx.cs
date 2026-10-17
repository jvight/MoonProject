using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Tire tracks and ballistic regolith dust. All four wheel sockets sample the ground for track ribbons while rear
    /// dust sockets drive the rooster-tail emitters; dust rate follows speed while grounded. A ring of dust bursts out
    /// on every announced <see cref="RoverLanded"/>, scaled by impact. Ticked by <see cref="RoverController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverWheelFx : MonoBehaviour
    {
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

        [Tooltip("Landing ring emitter (cone splash in its local XY plane, world simulation space, no automatic emission).")]
        [SerializeField] private ParticleSystem _landingDust;

        private RoverController _rover;
        private IDisposable _landedSubscription;

        public RoverTrackRenderer TrackFrontLeft => _trackFrontLeft;
        public RoverTrackRenderer TrackFrontRight => _trackFrontRight;
        public RoverTrackRenderer TrackLeft => _trackLeft;
        public RoverTrackRenderer TrackRight => _trackRight;

        /// <summary>Validates the wiring and subscribes to landings. Returns false (and logs) when broken.</summary>
        public bool Initialize(GameContext context, RoverController rover)
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return false;
            }

            _rover = rover;
            _trackFrontLeft.Initialize(_tuning);
            _trackFrontRight.Initialize(_tuning);
            _trackLeft.Initialize(_tuning);
            _trackRight.Initialize(_tuning);
            Configure(_dustLeft, _tuning.DustLifetime, _tuning.DustDrag, _tuning.DustOpacity);
            Configure(_dustRight, _tuning.DustLifetime, _tuning.DustDrag, _tuning.DustOpacity);
            Configure(_landingDust, _tuning.LandingLifetime, _tuning.LandingDrag, _tuning.LandingOpacity);
            ParticleSystem.MainModule dustLeft = _dustLeft.main;
            dustLeft.startSizeMultiplier = _tuning.DustSize;
            dustLeft.startSpeedMultiplier = _tuning.DustSpeed;
            ParticleSystem.MainModule dustRight = _dustRight.main;
            dustRight.startSizeMultiplier = _tuning.DustSize;
            dustRight.startSpeedMultiplier = _tuning.DustSpeed;
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

        /// <summary>Applies the tuned lifetime, drag, opacity and gravity (once, at start-up).</summary>
        private void Configure(ParticleSystem system, float lifetime, float drag, float opacity)
        {
            ParticleSystem.MainModule main = system.main;
            main.startLifetimeMultiplier = lifetime;
            main.gravityModifierMultiplier = _tuning.DustGravity;
            ParticleSystem.MinMaxGradient color = main.startColor;
            color.colorMin = WithAlpha(color.colorMin, opacity);
            color.colorMax = WithAlpha(color.colorMax, opacity);
            main.startColor = color;
            ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
            limit.enabled = drag > 0f;
            limit.dragMultiplier = drag;
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
            float rate = rolling
                ? _tuning.DustMaxRate * Smoothing.SmoothStep(_tuning.DustMinSpeed, _tuning.DustFullSpeed, _rover.Speed)
                : 0f;
            float time = Time.time;
            Quaternion rotation = _rover.Rotation;

            UpdateTrackOnly(_frontSocketLeft, _trackFrontLeft, rolling, rotation, time);
            UpdateTrackOnly(_frontSocketRight, _trackFrontRight, rolling, rotation, time);
            UpdateSide(_dustSocketLeft, _trackLeft, _dustLeft, rolling, rate, rotation, time);
            UpdateSide(_dustSocketRight, _trackRight, _dustRight, rolling, rate, rotation, time);
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

        /// <summary>Ends all four tire tracks where they are, so no ribbon stretches across a placement.</summary>
        public void Break()
        {
            _trackFrontLeft.Break();
            _trackFrontRight.Break();
            _trackLeft.Break();
            _trackRight.Break();
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
