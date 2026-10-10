using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Maps the base's power stage to how lit each of home's warm points is (0..1, linear glow scale). A fresh game is
    /// Asleep: only the dock glows. When a stage arrives, its warm points wake one by one, eased; a restored stage
    /// (on load) is applied at once. Plain logic, allocation-free: poll <see cref="Level"/> every frame.
    /// </summary>
    public sealed class HomeLightStages
    {
        private static readonly int LightCount = Enum.GetValues(typeof(HomeLight)).Length;

        private readonly HomeLightingSettings _settings;
        private BasePowerStage _stage = BasePowerStage.Asleep;
        private BasePowerStage _wakingStage = BasePowerStage.Asleep;
        private float _wokeAt = float.NegativeInfinity;

        public HomeLightStages(HomeLightingSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public BasePowerStage Stage => _stage;

        /// <summary>The stage each warm point wakes with.</summary>
        public static BasePowerStage WakesWith(HomeLight light)
        {
            switch (light)
            {
                case HomeLight.Dock:
                    return BasePowerStage.Asleep;
                case HomeLight.Windows:
                case HomeLight.PorchLamps:
                case HomeLight.Halo:
                    return BasePowerStage.Home;
                case HomeLight.BayLamps:
                    return BasePowerStage.Bay;
                case HomeLight.LiftLamps:
                    return BasePowerStage.Lift;
                default:
                    throw new ArgumentOutOfRangeException(nameof(light), light, null);
            }
        }

        /// <summary>
        /// Power reached <paramref name="stage"/> at <paramref name="time"/> (seconds). Restored, or a stage that is
        /// not a step up, applies at once with no wake moment.
        /// </summary>
        public void SetStage(BasePowerStage stage, bool restored, float time)
        {
            bool wakes = !restored && stage > _stage;
            _stage = stage;
            _wakingStage = wakes ? stage : BasePowerStage.Asleep;
            _wokeAt = wakes ? time : float.NegativeInfinity;
        }

        /// <summary>How lit <paramref name="light"/> is at <paramref name="time"/>: 0 dark, 1 fully lit.</summary>
        public float Level(HomeLight light, float time)
        {
            BasePowerStage needs = WakesWith(light);
            if (needs > _stage)
            {
                return 0f;
            }

            if (needs != _wakingStage || needs == BasePowerStage.Asleep)
            {
                return 1f;
            }

            float delay = OrderInStage(light) * _settings.WakeStagger;
            return Mathf.SmoothStep(0f, 1f, (time - _wokeAt - delay) / _settings.WakeFade);
        }

        private static int OrderInStage(HomeLight light)
        {
            int order = 0;
            for (int i = 0; i < LightCount && (HomeLight)i != light; i++)
            {
                order += WakesWith((HomeLight)i) == WakesWith(light) ? 1 : 0;
            }

            return order;
        }
    }
}
