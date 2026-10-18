using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Animates the chest-mounted cutter nozzle aperture on <c>CutterOrigin</c>, built at edit time by
    /// <c>RoverPrefabBuilder</c>:
    /// <list type="bullet">
    /// <item>Idle: 6 iris blades sit closed over the lens face, the <c>R1_LampAmber</c> core is at rest, and the spill
    /// point light is off (0 intensity).</item>
    /// <item>Charge (<see cref="CutChargeStaged"/> stage 0..N): the iris blades twist and retract outward, the core
    /// expands and its <c>_EmissionStrength</c> rises, and the warm spill light ramps up across the nozzle rim.</item>
    /// <item>Fire (<see cref="FeelKind.Cut"/> <see cref="FeelPhase.Act"/>): the iris snaps fully open, the core pulses
    /// at peak emission, and the spill light flickers on the chest chassis.</item>
    /// <item>Release / Cancel / Impact: the iris closes and the core and spill light cool smoothly back to rest.</item>
    /// </list>
    /// Zero allocations per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CutterNozzleDevice : MonoBehaviour, IDisposable
    {
        private const float FullCircleDegrees = 360f;
        private const float TwoPi = Mathf.PI * 2f;

        /// <summary>How far the iris flinches open (0..1) at a full flash.</summary>
        private const float IgnitionIris = 0.45f;

        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        [Tooltip("Rover effect tuning (Assets/_Project/Data/Tuning/RoverFxTuning.asset), 'Cutter nozzle'.")]
        [SerializeField] private RoverFxTuning _tuning;

        [Tooltip("The 6 mechanical iris blades parented under CutterOrigin.")]
        [SerializeField] private Transform[] _blades = Array.Empty<Transform>();

        [Tooltip("The hot core disc transform parented under CutterOrigin.")]
        [SerializeField] private Transform _core;

        [Tooltip("The hot core renderer on R1_LampAmber, driven through _EmissionStrength.")]
        [SerializeField] private Renderer _coreRenderer;

        [Tooltip("Warm spill point light parented under CutterOrigin (no shadows).")]
        [SerializeField] private Light _spillLight;

        private MaterialPropertyBlock _block;
        private IDisposable[] _subscriptions;

        private bool _online;
        private float _flash;
        private bool _charging;
        private bool _firing;
        private float _chargeElapsed;
        private float _chargeDuration;
        private float _irisOpen01;
        private float _coreGlow01;
        private float _phase;

        /// <summary>
        /// An ignition attempt's flash, 0..1: the core glows and the spill light kicks, as if the nozzle sparked. Set by
        /// the self-test's cutter check; 0 leaves the device to its own state.
        /// </summary>
        public void SetFlash(float flash01)
        {
            _flash = Mathf.Clamp01(flash01);
            if (_block != null)
            {
                ApplyVisuals();
            }
        }

        /// <summary>Normalized [0..1] iris opening (0 = closed, 1 = fully retracted).</summary>
        public float IrisOpen01 => _irisOpen01;

        /// <summary>Normalized [0..1] core glow level.</summary>
        public float CoreGlow01 => _coreGlow01;

        /// <summary>Current spill point-light intensity at the nozzle.</summary>
        public float SpillLightIntensity => _spillLight != null ? _spillLight.intensity : 0f;

        /// <summary>The nozzle spill point light.</summary>
        public Light SpillLight => _spillLight;

        /// <summary>True while the cutter is charging up.</summary>
        public bool IsCharging => _charging;

        /// <summary>True while the cutter beam is actively firing.</summary>
        public bool IsFiring => _firing;

        public bool Initialize(GameContext context)
        {
            bool ready = Initialize(context != null ? context.Events : null);
            if (ready && context.TryGet(out IRoverSystems systems))
            {
                _online = systems.StateOf(RoverSystem.Cutter) == RoverSystemState.Ok;
                ApplyVisuals();
            }

            return ready;
        }

        public bool Initialize(EventBus events)
        {
            if (!ValidateWiring(events))
            {
                enabled = false;
                return false;
            }

            DisposeSubscriptions();
            _block = new MaterialPropertyBlock();
            _subscriptions = new[]
            {
                events.Subscribe<CutChargeStaged>(OnChargeStarted),
                events.Subscribe<FeelBeat>(OnFeelBeat),
                events.Subscribe<RoverSystemChanged>(OnSystemChanged),
            };
            _charging = false;
            _firing = false;
            _chargeElapsed = 0f;
            _chargeDuration = _tuning.CutterNozzle.DefaultChargeDuration;
            _irisOpen01 = 0f;
            _coreGlow01 = 0f;
            _phase = 0f;
            ApplyVisuals();
            return true;
        }

        private bool ValidateWiring(EventBus events)
        {
            bool ok = Require(events != null, "EventBus is not assigned.")
                & Require(_tuning != null, "RoverFxTuning is not assigned.")
                & Require(_core != null, "Nozzle hot core transform is not assigned.")
                & Require(_coreRenderer != null, "Nozzle hot core renderer is not assigned.")
                & Require(_spillLight != null, "Nozzle spill light is not assigned.")
                & Require(_blades != null && _blades.Length == RoverModelNodes.NozzleBladeCount,
                    $"Exactly {RoverModelNodes.NozzleBladeCount} iris blades are required.");
            for (int i = 0; _blades != null && i < _blades.Length; i++)
            {
                ok &= Require(_blades[i] != null, $"Iris blade {i} is not assigned.");
            }

            return ok;
        }

        private bool Require(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"{nameof(CutterNozzleDevice)}: {message}", this);
            }

            return condition;
        }

        public void Tick(float deltaTime, float time)
        {
            if (_block == null)
            {
                return;
            }

            CutterNozzleSettings settings = _tuning.CutterNozzle;
            _phase = time;

            if (_firing)
            {
                _irisOpen01 = Smoothing.Damp(_irisOpen01, 1f, settings.FireOpenHalfLife, deltaTime);
                _coreGlow01 = Smoothing.Damp(_coreGlow01, 1f, settings.FireOpenHalfLife, deltaTime);
            }
            else if (_charging)
            {
                _chargeElapsed += deltaTime;
                float duration = Mathf.Max(settings.MinChargeDuration, _chargeDuration);
                float t = Mathf.Clamp01(_chargeElapsed / duration);
                float eased = 1f - (1f - t) * (1f - t);
                float targetOpen = Mathf.Lerp(settings.ChargeInitialIrisOpen, settings.ChargeFullIrisOpen, eased);
                float targetGlow = Mathf.Lerp(settings.ChargeInitialCoreGlow, settings.ChargeFullCoreGlow, t);
                _irisOpen01 = Mathf.Max(_irisOpen01, targetOpen);
                _coreGlow01 = Mathf.Max(_coreGlow01, targetGlow);
            }
            else
            {
                _irisOpen01 = Smoothing.Damp(_irisOpen01, 0f, settings.ReleaseHalfLife, deltaTime);
                _coreGlow01 = Smoothing.Damp(_coreGlow01, RestGlow(settings), settings.ReleaseHalfLife, deltaTime);
            }

            ApplyVisuals();
        }

        private void OnChargeStarted(CutChargeStaged staged)
        {
            CutterNozzleSettings settings = _tuning.CutterNozzle;
            if (staged.Stage == 0)
            {
                _charging = true;
                _firing = false;
                _chargeElapsed = 0f;
                int stages = Mathf.Max(1, staged.StagesNeeded);
                _chargeDuration = staged.StageSeconds > 0f
                    ? staged.StageSeconds * stages
                    : settings.DefaultChargeDuration;
                _irisOpen01 = Mathf.Max(_irisOpen01, settings.ChargeInitialIrisOpen);
                _coreGlow01 = Mathf.Max(_coreGlow01, settings.ChargeInitialCoreGlow);
                ApplyVisuals();
            }
            else if (_charging)
            {
                _irisOpen01 = Mathf.Max(_irisOpen01, settings.ChargeFullIrisOpen);
                _coreGlow01 = Mathf.Max(_coreGlow01, settings.ChargeFullCoreGlow);
                ApplyVisuals();
            }
        }

        private void OnSystemChanged(RoverSystemChanged changed)
        {
            if (changed.System == RoverSystem.Cutter)
            {
                _online = changed.State == RoverSystemState.Ok;
            }
        }

        /// <summary>The idle glow: dark while the cutter is offline, a small steady pilot light once it works.</summary>
        private float RestGlow(CutterNozzleSettings settings)
        {
            return _online ? settings.PilotCoreGlow : 0f;
        }

        private void OnFeelBeat(FeelBeat beat)
        {
            if (beat.Kind != FeelKind.Cut)
            {
                return;
            }

            switch (beat.Phase)
            {
                case FeelPhase.Act:
                    _charging = false;
                    _firing = true;
                    _irisOpen01 = 1f;
                    _coreGlow01 = 1f;
                    ApplyVisuals();
                    break;
                case FeelPhase.Cancel:
                case FeelPhase.Impact:
                    _charging = false;
                    _firing = false;
                    break;
            }
        }

        /// <summary>07 was placed: return the nozzle immediately to its closed, unlit rest state.</summary>
        public void Reset()
        {
            _charging = false;
            _firing = false;
            _chargeElapsed = 0f;
            _irisOpen01 = 0f;
            _coreGlow01 = 0f;
            if (_block != null)
            {
                _coreGlow01 = RestGlow(_tuning.CutterNozzle);
                ApplyVisuals();
            }
        }

        private void ApplyVisuals()
        {
            CutterNozzleSettings settings = _tuning.CutterNozzle;
            float iris = Mathf.Max(_irisOpen01, _flash * IgnitionIris);
            float glow = Mathf.Max(_coreGlow01, _flash);
            float radius = Mathf.Lerp(settings.BladeClosedRadius, settings.BladeOpenRadius, iris);
            float twist = settings.BladeAngleOffset + settings.BladeOpenTwist * iris;
            int count = _blades.Length;
            for (int i = 0; i < count; i++)
            {
                float baseAngle = i * (FullCircleDegrees / count);
                float rad = baseAngle * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
                Transform blade = _blades[i];
                blade.localPosition = radial * radius + new Vector3(0f, 0f, settings.BladeForwardOffset);
                blade.localRotation = Quaternion.Euler(0f, 0f, baseAngle + twist);
            }

            float pulse = _firing
                ? 1f + settings.FireCorePulseAmplitude * Mathf.Sin(_phase * settings.FireCorePulseRate * TwoPi)
                : 1f;
            float coreDiameter = Mathf.Lerp(settings.CoreRestDiameter, settings.CoreHotDiameter, glow) * pulse;
            _core.localPosition = new Vector3(0f, 0f, settings.CoreForwardOffset);
            _core.localScale = new Vector3(coreDiameter, coreDiameter, 1f);

            float emissionStrength = glow * settings.CorePeakEmission;
            _block.SetFloat(EmissionStrengthId, emissionStrength);
            _coreRenderer.SetPropertyBlock(_block);

            float targetSpill;
            if (_firing)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(_phase * settings.FireFlickerRate * TwoPi);
                targetSpill = settings.FireBaseSpillIntensity + settings.FireFlickerAmplitude * wave;
            }
            else if (_charging)
            {
                targetSpill = Mathf.Lerp(settings.ChargeMinSpillIntensity, settings.ChargeMaxSpillIntensity, glow);
            }
            else
            {
                targetSpill = glow * settings.ReleaseSpillIntensity;
            }

            _spillLight.range = settings.SpillRange;
            _spillLight.intensity = Mathf.Max(targetSpill, _flash * settings.FireBaseSpillIntensity);
        }

        public void Dispose()
        {
            DisposeSubscriptions();
        }

        private void OnDestroy()
        {
            DisposeSubscriptions();
        }

        private void DisposeSubscriptions()
        {
            if (_subscriptions == null)
            {
                return;
            }

            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i]?.Dispose();
            }

            _subscriptions = null;
        }
    }
}
