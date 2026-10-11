using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Maps the base's power stage to how lit each of home's warm points is (0..1, linear glow scale). A fresh game is
    /// Asleep: only the dock glows and the tower's pilot blinks. When a stage arrives, power runs along its ground
    /// cable from the tower, then its warm points wake one by one, eased; a restored stage (on load) is applied at
    /// once. Plain logic, allocation-free: poll every frame.
    /// </summary>
    public sealed class HomeLightStages
    {
        private static readonly int LightCount = Enum.GetValues(typeof(HomeLight)).Length;

        // The first warm point each stage after Asleep wakes: the grade warms a step with each of them.
        private static readonly HomeLight[] StageLeads = { HomeLight.Windows, HomeLight.BayLamps, HomeLight.LiftLamps };

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
                    return BasePowerStage.Chart;
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

            float delay = _settings.CableTravel + OrderInStage(light) * _settings.WakeStagger;
            return Mathf.SmoothStep(0f, 1f, (time - _wokeAt - delay) / _settings.WakeFade);
        }

        /// <summary>
        /// How lit segment <paramref name="segment"/> of <paramref name="count"/> is on the ground cable that carries
        /// <paramref name="stage"/>'s power (segment 0 at the tower).
        /// </summary>
        public float CableLevel(BasePowerStage stage, int segment, int count, float time)
        {
            if (stage > _stage)
            {
                return 0f;
            }

            if (stage != _wakingStage)
            {
                return 1f;
            }

            float arrives = _settings.CableTravel * segment / Mathf.Max(1, count);
            return Mathf.SmoothStep(0f, 1f, (time - _wokeAt - arrives) / _settings.CableSegmentFade);
        }

        /// <summary>
        /// How far home's grade has warmed at <paramref name="time"/>: 0 asleep, 1 with the whole base awake. Each
        /// stage adds an equal step, eased in with the first warm point it wakes.
        /// </summary>
        public float GradeWarmth(float time)
        {
            float warmth = 0f;
            foreach (HomeLight lead in StageLeads)
            {
                warmth += Level(lead, time);
            }

            return warmth / StageLeads.Length;
        }

        /// <summary>
        /// The tower's top light (0..1): a slow, short blink while the base sleeps, easing into a steady warm glow as
        /// home's windows wake, so home stays a readable warm dot from far across the basin.
        /// </summary>
        public float PilotLevel(float time)
        {
            float wave = 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * time / _settings.PilotPeriod);
            float blink = Mathf.Pow(wave, _settings.PilotSharpness);
            return Mathf.Lerp(blink, _settings.TopLightLevel, Level(HomeLight.Windows, time));
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
