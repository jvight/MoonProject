using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// Registers <see cref="IHomeLights"/> (docs/features/M3-16): follows <see cref="BasePowerChanged"/> and tells
    /// whoever drives home's lamps how lit each warm point is now. It drives World's own parts of the wake itself: the
    /// glass segments of Art's ground power cables (lit from the tower outward), the tower's pilot blink while the
    /// base sleeps, the warm light pools on the ground under home's warm points and home's grade, which warms a step
    /// with each stage. Thin: the timing lives in <see cref="HomeLightStages"/>.
    /// </summary>
    public sealed class HomeLightingSystem : MonoBehaviour, IGameSystem, IHomeLights
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its Home Lighting block.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("Glass segments of the lander's PowerCable_Home, ordered out from the tower (Glow_0..n).")]
        [SerializeField] private Renderer[] _homeCable = new Renderer[0];

        [Tooltip("Glass segments of the lander's PowerCable_Bay, ordered out from the tower.")]
        [SerializeField] private Renderer[] _bayCable = new Renderer[0];

        [Tooltip("Glass segments of the lander's PowerCable_Lift, ordered out from the tower.")]
        [SerializeField] private Renderer[] _liftCable = new Renderer[0];

        [Tooltip("The pilot light's halo at the top of the radio tower (LofiBeaconHalo).")]
        [SerializeField] private Renderer _pilot;

        [Tooltip("Global Volume with home's warm grade (Generated/World/HomeGradeProfile); its weight follows the " +
            "power stages.")]
        [SerializeField] private Volume _homeGrade;

        [Tooltip("The warm pools' point lights, one per Home Lighting pool, in the same order (the scene build hangs " +
            "them from their anchors in the base).")]
        [SerializeField] private Light[] _pools = new Light[0];

        private MaterialPropertyBlock _block;
        private HomeLightStages _stages;
        private IDisposable _powerChanged;
        private float[] _homeApplied;
        private float[] _bayApplied;
        private float[] _liftApplied;
        private float _gradeApplied = -1f;
        private float[] _poolsApplied;

        public void Initialize(GameContext context)
        {
            string problem = SetupProblem();
            if (problem != null)
            {
                Debug.LogError($"{nameof(HomeLightingSystem)}: {problem}", this);
                enabled = false;
                return;
            }

            // Unity refuses to create a property block while it constructs a MonoBehaviour (a field initializer).
            _block = new MaterialPropertyBlock();
            _stages = new HomeLightStages(_settings.HomeLighting);
            _homeApplied = Unapplied(_homeCable.Length);
            _bayApplied = Unapplied(_bayCable.Length);
            _liftApplied = Unapplied(_liftCable.Length);
            _poolsApplied = Unapplied(_pools.Length);
            SetUpPools();
            _powerChanged = context.Events.Subscribe<BasePowerChanged>(OnPowerChanged);
            context.Register<IHomeLights>(this);
        }

        public float Level(HomeLight light)
        {
            return _stages.Level(light, Time.time);
        }

        private void Update()
        {
            if (_stages == null)
            {
                return;
            }

            float time = Time.time;
            ApplyCable(BasePowerStage.Home, _homeCable, _homeApplied, time);
            ApplyCable(BasePowerStage.Bay, _bayCable, _bayApplied, time);
            ApplyCable(BasePowerStage.Lift, _liftCable, _liftApplied, time);

            HomeLightingSettings tuning = _settings.HomeLighting;
            float pilot = _stages.PilotLevel(time);
            _pilot.enabled = pilot > 0f;
            _block.Clear();
            _block.SetFloat(IntensityId, pilot * tuning.PilotIntensity);
            _block.SetFloat(RadiusId, tuning.PilotRadius);
            _pilot.SetPropertyBlock(_block);
            ApplyPools(tuning, time);
            float warmth = _stages.GradeWarmth(time);
            if (!Mathf.Approximately(warmth, _gradeApplied))
            {
                _gradeApplied = warmth;
                _homeGrade.weight = warmth;
            }
        }

        private void ApplyCable(BasePowerStage stage, Renderer[] segments, float[] applied, float time)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                float level = _stages.CableLevel(stage, i, segments.Length, time);
                if (Mathf.Approximately(level, applied[i]))
                {
                    continue;
                }

                applied[i] = level;
                _block.Clear();
                _block.SetVector(EmissionColorId, new Vector4(level, level, level, 1f));
                segments[i].SetPropertyBlock(_block);
            }
        }

        private void SetUpPools()
        {
            HomeLightingSettings tuning = _settings.HomeLighting;
            for (int i = 0; i < _pools.Length; i++)
            {
                LightPoolTuning pool = tuning.Pools[i];
                Light light = _pools[i];
                light.type = LightType.Point;
                light.color = tuning.PoolColor;
                light.range = pool.Range;
                light.shadows = LightShadows.None;
                light.intensity = 0f;
                light.enabled = false;
                light.transform.localPosition = pool.Offset;
            }
        }

        private void ApplyPools(HomeLightingSettings tuning, float time)
        {
            for (int i = 0; i < _pools.Length; i++)
            {
                LightPoolTuning pool = tuning.Pools[i];
                float level = _stages.Level(pool.WakesWith, time);
                if (Mathf.Approximately(level, _poolsApplied[i]))
                {
                    continue;
                }

                _poolsApplied[i] = level;
                _pools[i].intensity = level * pool.Intensity;
                _pools[i].enabled = level > 0f;
            }
        }

        private void OnPowerChanged(BasePowerChanged change)
        {
            _stages.SetStage(change.Stage, change.Restored, Time.time);
        }

        private void OnDestroy()
        {
            _powerChanged?.Dispose();
        }

        private static float[] Unapplied(int count)
        {
            var applied = new float[count];
            for (int i = 0; i < count; i++)
            {
                applied[i] = -1f;
            }

            return applied;
        }

        private static bool HasEmpty(Renderer[] segments)
        {
            if (segments == null || segments.Length == 0)
            {
                return true;
            }

            foreach (Renderer segment in segments)
            {
                if (segment == null)
                {
                    return true;
                }
            }

            return false;
        }

        private string SetupProblem()
        {
            if (_settings == null)
            {
                return "World Settings is not assigned.";
            }

            if (HasEmpty(_homeCable) || HasEmpty(_bayCable) || HasEmpty(_liftCable))
            {
                return "Home, Bay and Lift cable segments must be assigned (the scene build wires Art's Glow_ nodes).";
            }

            if (_pilot == null)
            {
                return "The tower's pilot halo is not assigned.";
            }

            if (_pools == null || _pools.Length != _settings.HomeLighting.Pools.Count)
            {
                return $"Light pools must match Home Lighting's {_settings.HomeLighting.Pools.Count} pools (the " +
                    "scene build hangs them from the base).";
            }

            foreach (Light pool in _pools)
            {
                if (pool == null)
                {
                    return "A light pool is missing.";
                }
            }

            return _homeGrade == null ? "Home's grade Volume is not assigned." : null;
        }
    }
}
