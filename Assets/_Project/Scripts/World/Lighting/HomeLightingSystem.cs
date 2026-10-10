using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// Registers <see cref="IHomeLights"/> (docs/features/M3-16): follows <see cref="BasePowerChanged"/> and tells
    /// whoever drives home's lamps how lit each warm point is now. It drives World's own parts of the wake itself: the
    /// glass segments of Art's ground power cables (lit from the tower outward) and the tower's pilot blink while the
    /// base sleeps. Thin: the timing lives in <see cref="HomeLightStages"/>.
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

        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private HomeLightStages _stages;
        private IDisposable _powerChanged;
        private float[] _homeApplied;
        private float[] _bayApplied;
        private float[] _liftApplied;

        public void Initialize(GameContext context)
        {
            string problem = SetupProblem();
            if (problem != null)
            {
                Debug.LogError($"{nameof(HomeLightingSystem)}: {problem}", this);
                enabled = false;
                return;
            }

            _stages = new HomeLightStages(_settings.HomeLighting);
            _homeApplied = Unapplied(_homeCable.Length);
            _bayApplied = Unapplied(_bayCable.Length);
            _liftApplied = Unapplied(_liftCable.Length);
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

            return _pilot == null ? "The tower's pilot halo is not assigned." : null;
        }
    }
}
