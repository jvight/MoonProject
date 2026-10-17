using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Tire tracks and surface-aware dust/debris. All four wheel sockets sample the ground for track ribbons while rear
    /// dust sockets drive the ballistic rooster-tail emitters; regolith throws dust and leaves tracks, while bare rock
    /// and metal break tracks, emit a few crisp ballistic chips/sparks, and expose a distinct surface sound hook.
    /// Ticked by <see cref="RoverController"/>.
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

        private int _cachedFlId;
        private GroundSurface _cachedFlSurface;
        private int _cachedFrId;
        private GroundSurface _cachedFrSurface;
        private int _cachedRlId;
        private GroundSurface _cachedRlSurface;
        private int _cachedRrId;
        private GroundSurface _cachedRrSurface;
        private int _cachedLandId;
        private GroundSurface _cachedLandSurface;

        private float _chipAccumLeft;
        private float _chipAccumRight;

        /// <summary>Most recent rolling ground surface sampled under the wheels.</summary>
        public GroundSurface LastSurface { get; private set; }

        /// <summary>Sound hook ID for <see cref="LastSurface"/> ("surface.regolith", "surface.rock", "surface.metal").</summary>
        public string LastSurfaceSoundHook => GroundModel.SurfaceSoundHook(LastSurface);

        /// <summary>Ground surface sampled on the most recent <see cref="RoverLanded"/> beat.</summary>
        public GroundSurface LastLandingSurface { get; private set; }

        /// <summary>Sound hook ID for <see cref="LastLandingSurface"/> ("surface.regolith", "surface.rock", "surface.metal").</summary>
        public string LastLandingSoundHook => GroundModel.SurfaceSoundHook(LastLandingSurface);

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
            float speedFactor = rolling
                ? Smoothing.SmoothStep(_tuning.DustMinSpeed, _tuning.DustFullSpeed, _rover.Speed)
                : 0f;
            float rate = _tuning.DustMaxRate * speedFactor;
            float time = Time.time;
            Quaternion rotation = _rover.Rotation;

            UpdateTrackOnly(_frontSocketLeft, _trackFrontLeft, rolling, rotation, time,
                ref _cachedFlId, ref _cachedFlSurface);
            UpdateTrackOnly(_frontSocketRight, _trackFrontRight, rolling, rotation, time,
                ref _cachedFrId, ref _cachedFrSurface);
            UpdateRearSide(_dustSocketLeft, _trackLeft, _dustLeft, rolling, rate, speedFactor, rotation, time,
                ref _cachedRlId, ref _cachedRlSurface, ref _chipAccumLeft);
            UpdateRearSide(_dustSocketRight, _trackRight, _dustRight, rolling, rate, speedFactor, rotation, time,
                ref _cachedRrId, ref _cachedRrSurface, ref _chipAccumRight);
        }

        private void UpdateTrackOnly(Transform socket, RoverTrackRenderer track, bool rolling, Quaternion rotation,
            float time, ref int cachedId, ref GroundSurface cachedSurface)
        {
            Vector3 up = rotation * Vector3.up;
            Vector3 position = socket.position;
            float probe = _tuning.TrackGroundProbe;
            if (rolling && Physics.Raycast(position + up * probe, -up, out RaycastHit hit, probe * 2f,
                    Layers.DriveableMask, QueryTriggerInteraction.Ignore))
            {
                GroundSurface surface = ClassifyHit(in hit, ref cachedId, ref cachedSurface);
                if (surface == GroundSurface.Regolith)
                {
                    track.Sample(hit.point, hit.normal, time);
                }
                else
                {
                    track.Break();
                }
            }
            else
            {
                track.Break();
            }

            track.Rebuild(time);
        }

        private void UpdateRearSide(Transform socket, RoverTrackRenderer track, ParticleSystem dust, bool rolling,
            float rate, float speedFactor, Quaternion rotation, float time, ref int cachedId,
            ref GroundSurface cachedSurface, ref float chipAccum)
        {
            Vector3 up = rotation * Vector3.up;
            Vector3 position = socket.position;
            float probe = _tuning.TrackGroundProbe;
            bool emitRegolithDust = false;
            if (rolling && Physics.Raycast(position + up * probe, -up, out RaycastHit hit, probe * 2f,
                    Layers.DriveableMask, QueryTriggerInteraction.Ignore))
            {
                position = hit.point;
                GroundSurface surface = ClassifyHit(in hit, ref cachedId, ref cachedSurface);
                LastSurface = surface;
                if (surface == GroundSurface.Regolith)
                {
                    emitRegolithDust = true;
                    chipAccum = 0f;
                    track.Sample(hit.point, hit.normal, time);
                }
                else
                {
                    track.Break();
                    if (speedFactor > 0f)
                    {
                        chipAccum += 9f * speedFactor * Time.deltaTime;
                        int chips = (int)chipAccum;
                        if (chips > 0)
                        {
                            chipAccum -= chips;
                            EmitRollingChips(dust, hit.point, hit.normal, rotation, surface, chips, speedFactor);
                        }
                    }
                }
            }
            else
            {
                chipAccum = 0f;
                track.Break();
            }

            track.Rebuild(time);

            dust.transform.SetPositionAndRotation(position, rotation);
            ParticleSystem.EmissionModule emission = dust.emission;
            emission.rateOverTimeMultiplier = emitRegolithDust ? rate : 0f;
        }

        private static GroundSurface ClassifyHit(in RaycastHit hit, ref int cachedId, ref GroundSurface cachedSurface)
        {
            int id = hit.colliderInstanceID;
            if (id != cachedId)
            {
                cachedId = id;
                cachedSurface = GroundModel.ClassifySurface(hit.collider);
            }

            return cachedSurface;
        }

        private static void EmitRollingChips(ParticleSystem emitter, Vector3 point, Vector3 normal, Quaternion rotation,
            GroundSurface surface, int count, float speedFactor)
        {
            Vector3 back = -(rotation * Vector3.forward);
            Vector3 right = rotation * Vector3.right;
            Color32 chipColor = surface == GroundSurface.Metal
                ? new Color32(255, 205, 130, 235)
                : new Color32(196, 198, 208, 220);

            for (int i = 0; i < count; i++)
            {
                float spread = ((i & 1) == 0 ? 1f : -1f) * (0.18f + 0.09f * (i % 3));
                Vector3 dir = (back * 0.82f + normal * 0.52f + right * spread).normalized;
                float speed = (1.6f + 1.2f * speedFactor) * (0.85f + 0.15f * (i % 2));
                var emitParams = new ParticleSystem.EmitParams
                {
                    position = point + normal * 0.03f,
                    velocity = dir * speed,
                    startSize = 0.034f + 0.008f * (i % 2),
                    startLifetime = 0.42f + 0.08f * (i % 2),
                    startColor = chipColor
                };
                emitter.Emit(emitParams, 1);
            }
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
            Vector3 up = _rover.Rotation * Vector3.up;
            float probe = _tuning.TrackGroundProbe;

            GroundSurface surface = _rover.ContactSurface;
            if (Physics.Raycast(landed.Position + up * probe, -up, out RaycastHit hit, probe * 2.5f,
                    Layers.DriveableMask, QueryTriggerInteraction.Ignore))
            {
                surface = ClassifyHit(in hit, ref _cachedLandId, ref _cachedLandSurface);
                normal = hit.normal;
            }

            LastLandingSurface = surface;
            LastSurface = surface;

            _landingDust.transform.SetPositionAndRotation(landed.Position,
                Quaternion.FromToRotation(Vector3.forward, normal));

            if (surface == GroundSurface.Regolith)
            {
                ParticleSystem.MainModule main = _landingDust.main;
                main.startSpeedMultiplier = Mathf.Lerp(_tuning.LandingMinSpeed, _tuning.LandingMaxSpeed, strength);
                main.startSizeMultiplier = Mathf.Lerp(_tuning.LandingMinSize, _tuning.LandingMaxSize, strength);
                int count = Mathf.RoundToInt(Mathf.Lerp(_tuning.LandingMinCount, _tuning.LandingMaxCount, strength));
                _landingDust.Emit(count);
            }
            else
            {
                int chipCount = Mathf.RoundToInt(Mathf.Lerp(6f, 12f, strength));
                EmitLandingChips(landed.Position, normal, surface, chipCount, strength);
            }
        }

        private void EmitLandingChips(Vector3 position, Vector3 normal, GroundSurface surface, int count,
            float strength)
        {
            Quaternion basis = Quaternion.FromToRotation(Vector3.up, normal);
            Color32 chipColor = surface == GroundSurface.Metal
                ? new Color32(255, 205, 130, 240)
                : new Color32(198, 200, 210, 225);

            float step = Mathf.PI * 2f / Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                float angle = i * step + (i & 1) * 0.18f;
                Vector3 radial = basis * new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                float elev = 0.42f + 0.14f * (i % 3);
                Vector3 dir = (radial + normal * elev).normalized;
                float speed = Mathf.Lerp(1.6f, 3.0f, strength) * (0.82f + 0.12f * (i % 3));
                var emitParams = new ParticleSystem.EmitParams
                {
                    position = position + radial * 0.35f + normal * 0.04f,
                    velocity = dir * speed,
                    startSize = 0.032f + 0.01f * (i % 3),
                    startLifetime = 0.4f + 0.08f * (i % 3),
                    startColor = chipColor
                };
                _landingDust.Emit(emitParams, 1);
            }
        }

        private void OnDestroy()
        {
            _landedSubscription?.Dispose();
        }
    }
}
