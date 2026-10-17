using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Maps the base's power stage to how lit each of home's warm points is (0..1, linear glow scale). A fresh game is
    /// Asleep: only the dock glows and the tower's pilot blinks. When a stage arrives, power runs along its ground
    /// cable from the tower, then its warm points wake one by one, eased; a restored stage (on load) is applied at
    /// once. The first time 07 comes near the tower, a slow light previews each dark cable in turn (lander, bay, chart;
    /// docs/features/M3-24 B) and fades. Plain logic, allocation-free: poll every frame.
    /// </summary>
    public sealed class HomeLightStages
    {
        private static readonly int LightCount = Enum.GetValues(typeof(HomeLight)).Length;

        // The first warm point each stage after Asleep wakes: the grade warms a step with each of them.
        private static readonly HomeLight[] StageLeads =
        {
            HomeLight.Windows, HomeLight.BayLamps, HomeLight.ChartTable,
        };

        private readonly HomeLightingSettings _settings;
        private BasePowerStage _stage = BasePowerStage.Asleep;
        private BasePowerStage _wakingStage = BasePowerStage.Asleep;
        private float _wokeAt = float.NegativeInfinity;
        private float _previewAt = float.NegativeInfinity;

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
                case HomeLight.LiftLamps:
                case HomeLight.LabLamps:
                    return BasePowerStage.Home;
                case HomeLight.BayLamps:
                    return BasePowerStage.Bay;
                case HomeLight.ChartTable:
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

        /// <summary>07 came near the tower for the first time: its cables' preview starts at <paramref name="time"/>.</summary>
        public void Preview(float time)
        {
            _previewAt = time;
        }

        /// <summary>
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
            if (light != HomeLight.Windows)
            {
                return Mathf.SmoothStep(0f, 1f, (time - _wokeAt - delay) / _settings.WakeFade);
            }

            // The windows wake one by one across the fade: each step eases up on its own.
            int steps = _settings.WindowSteps;
            float level = 0f;
            for (int step = 0; step < steps; step++)
            {
                float at = delay + _settings.WakeFade * step / steps;
                level += Mathf.SmoothStep(0f, 1f, (time - _wokeAt - at) / _settings.WindowStepEase);
            }

            return level / steps;
        }

        /// <summary>
        /// How lit segment <paramref name="segment"/> of <paramref name="count"/> is on the ground cable that carries
        /// <paramref name="stage"/>'s power (segment 0 at the tower).
        /// </summary>
        public float CableLevel(BasePowerStage stage, int segment, int count, float time)
        {
            return Mathf.Max(PowerLevel(stage, segment, count, time), PreviewLevel(stage, segment, count, time));
        }

        private float PowerLevel(BasePowerStage stage, int segment, int count, float time)
        {
            if (stage > _stage)
            {
                return 0f;
            }

            float steady = _settings.CableSteady;
            if (stage != _wakingStage)
            {
                return steady;
            }

            // A bright pulse races from the tower to home: each bead flares as it passes, then settles to a steady
            // soft glow that shows home is connected.
            float since = time - _wokeAt - _settings.CableTravel * segment / Mathf.Max(1, count);
            if (since <= 0f)
            {
                return 0f;
            }

            float rise = Mathf.SmoothStep(0f, 1f, since / _settings.CableSegmentFade);
            float settle = (since - _settings.CableSegmentFade) / _settings.CablePulseSeconds;
            float flare = 1f - Mathf.SmoothStep(0f, 1f, settle);
            return rise * (steady + (_settings.CablePulsePeak - steady) * flare);
        }

        /// <summary>
        /// Where the power pulse is on the cable of the stage now waking: <paramref name="progress"/> runs 0 at the
        /// tower to 1 at home over the cable travel; false when no pulse is travelling (or it has faded at home).
        /// </summary>
        public bool CablePulse(float time, out BasePowerStage stage, out float progress)
        {
            stage = _wakingStage;
            progress = (time - _wokeAt) / _settings.CableTravel;
            float fadeShare = _settings.CablePulseSeconds / _settings.CableTravel;
            return _wakingStage != BasePowerStage.Asleep && progress >= 0f && progress < 1f + fadeShare;
        }

        /// <summary>
        /// How many of the windows' steps have begun lighting at <paramref name="time"/>: 0..WindowSteps. A restored
        /// or earlier stage counts as all of them.
        /// </summary>
        public int WindowStepsLit(float time)
        {
            if (WakesWith(HomeLight.Windows) > _stage)
            {
                return 0;
            }

            if (_wakingStage != WakesWith(HomeLight.Windows))
            {
                return _settings.WindowSteps;
            }

            float since = time - _wokeAt - _settings.CableTravel
                - OrderInStage(HomeLight.Windows) * _settings.WakeStagger;
            int lit = Mathf.FloorToInt(since / (_settings.WakeFade / _settings.WindowSteps)) + 1;
            return Mathf.Clamp(lit, 0, _settings.WindowSteps);
        }

        /// <summary>
        /// The preview's glow on a segment: it brightens as the slow light reaches it and lingers a moment behind it.
        /// The cables run in power order (Home, Bay, Chart), one after another.
        /// </summary>
        private float PreviewLevel(BasePowerStage stage, int segment, int count, float time)
        {
            int order = (int)stage - (int)BasePowerStage.Home;
            float arrives = _previewAt + order * _settings.PreviewStagger
                + _settings.PreviewTravel * segment / Mathf.Max(1, count);
            float since = time - arrives;
            if (since <= 0f || since >= _settings.PreviewRise + _settings.PreviewTail)
            {
                return 0f;
            }

            float rise = Mathf.SmoothStep(0f, 1f, since / _settings.PreviewRise);
            float fall = 1f - Mathf.SmoothStep(0f, 1f, (since - _settings.PreviewRise) / _settings.PreviewTail);
            return _settings.PreviewPeak * Mathf.Min(rise, fall);
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
