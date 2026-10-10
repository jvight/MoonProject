using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// Registers <see cref="IHomeLights"/> (docs/features/M3-16): follows <see cref="BasePowerChanged"/> and tells
    /// whoever drives home's lamps how lit each warm point is now. Thin: the timing lives in
    /// <see cref="HomeLightStages"/>.
    /// </summary>
    public sealed class HomeLightingSystem : MonoBehaviour, IGameSystem, IHomeLights
    {
        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its Home Lighting block.")]
        [SerializeField] private WorldSettings _settings;

        private HomeLightStages _stages;
        private IDisposable _powerChanged;

        public void Initialize(GameContext context)
        {
            if (_settings == null)
            {
                Debug.LogError($"{nameof(HomeLightingSystem)}: World Settings is not assigned.", this);
                enabled = false;
                return;
            }

            _stages = new HomeLightStages(_settings.HomeLighting);
            _powerChanged = context.Events.Subscribe<BasePowerChanged>(OnPowerChanged);
            context.Register<IHomeLights>(this);
        }

        public float Level(HomeLight light)
        {
            return _stages.Level(light, Time.time);
        }

        private void OnPowerChanged(BasePowerChanged change)
        {
            _stages.SetStage(change.Stage, change.Restored, Time.time);
        }

        private void OnDestroy()
        {
            _powerChanged?.Dispose();
        }
    }
}
