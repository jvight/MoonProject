using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The growth parts' motion as pure functions of the seconds since their event (<see cref="GrowthSettings"/>).
    /// </summary>
    public static class GrowthMotion
    {
        /// <summary>Share of the headlamp fit duration spent on the eased unfold-and-slide before the clamp click.</summary>
        private const float HeadlampClampShare = 0.8f;

        /// <summary>Initial filament warmth when the headlamp pods first appear so the lenses catch warm right away.</summary>
        private const float HeadlampInitialWarmth = 0.35f;

        /// <summary>Metres the antenna's second segment is out of its mast <paramref name="seconds"/> after the event.</summary>
        public static float AntennaOut(GrowthSettings settings, float seconds)
        {
            if (seconds <= 0f)
            {
                return 0f;
            }

            if (seconds < settings.AntennaSeconds)
            {
                return settings.AntennaExtendMetres * Smoothing.SmoothStep(0f, 1f, seconds / settings.AntennaSeconds);
            }

            float settle = Mathf.Clamp01((seconds - settings.AntennaSeconds) / settings.AntennaSettleSeconds);
            float give = -settings.AntennaSettleMetres * Mathf.Sin(settle * Mathf.PI * 2f) * (1f - settle) * (1f - settle);
            return settings.AntennaExtendMetres + give;
        }

        /// <summary>The motion has finished: the segment rests fully out.</summary>
        public static bool AntennaDone(GrowthSettings settings, float seconds)
        {
            return seconds >= settings.AntennaSeconds + settings.AntennaSettleSeconds;
        }

        /// <summary>
        /// Metres the igniter cartridge sits out of its socket <paramref name="seconds"/> after it is fitted: negative
        /// at the click, when it presses a little past home.
        /// </summary>
        public static float IgniterOut(GrowthSettings settings, float seconds)
        {
            if (seconds <= 0f)
            {
                return settings.IgniterSeatMetres;
            }

            if (seconds < settings.IgniterSeatSeconds)
            {
                float u = seconds / settings.IgniterSeatSeconds;
                float eased = 1f - (1f - u) * (1f - u) * (1f - u);
                return settings.IgniterSeatMetres * (1f - eased);
            }

            float click = (seconds - settings.IgniterSeatSeconds) / settings.IgniterClickSeconds;
            return click >= 1f ? 0f : -settings.IgniterClickMetres * Mathf.Sin(click * Mathf.PI);
        }

        /// <summary>The cartridge has seated and rests at home.</summary>
        public static bool IgniterDone(GrowthSettings settings, float seconds)
        {
            return seconds >= settings.IgniterSeatSeconds + settings.IgniterClickSeconds;
        }

        /// <summary>
        /// Metres each headlamp pod housing (or flank cell pod) sits outboard of its bracket <paramref name="seconds"/>
        /// after it is fitted: eases inward over the clamp phase and presses slightly past home at the final click.
        /// </summary>
        public static float HeadlampOut(GrowthSettings settings, float seconds)
        {
            if (seconds <= 0f)
            {
                return settings.HeadlampClampMetres;
            }

            float total = Mathf.Max(0.01f, settings.HeadlampFitSeconds);
            float clampSeconds = total * HeadlampClampShare;
            if (seconds < clampSeconds)
            {
                float u = seconds / clampSeconds;
                float eased = Smoothing.SmoothStep(0f, 1f, u);
                return settings.HeadlampClampMetres * (1f - eased);
            }

            if (seconds < total)
            {
                float clickSeconds = Mathf.Max(0.001f, total - clampSeconds);
                float click = (seconds - clampSeconds) / clickSeconds;
                return -settings.HeadlampClickMetres * Mathf.Sin(click * Mathf.PI);
            }

            return 0f;
        }

        /// <summary>
        /// Degrees each headlamp pod housing sits unfolded outward on its bracket <paramref name="seconds"/> after it
        /// is fitted, easing shut to 0 before the clamp click.
        /// </summary>
        public static float HeadlampOpenDegrees(GrowthSettings settings, float seconds)
        {
            if (seconds <= 0f)
            {
                return settings.HeadlampUnfoldDegrees;
            }

            float clampSeconds = Mathf.Max(0.01f, settings.HeadlampFitSeconds) * HeadlampClampShare;
            if (seconds < clampSeconds)
            {
                float u = seconds / clampSeconds;
                float eased = Smoothing.SmoothStep(0f, 1f, u);
                return settings.HeadlampUnfoldDegrees * (1f - eased);
            }

            return 0f;
        }

        /// <summary>
        /// Warmth 0..1 of the pod lenses <paramref name="seconds"/> after live fit: catches warm immediately and
        /// swells to 1 as the pods clamp home.
        /// </summary>
        public static float HeadlampWarmth01(GrowthSettings settings, float seconds)
        {
            if (seconds <= 0f)
            {
                return HeadlampInitialWarmth;
            }

            float total = Mathf.Max(0.01f, settings.HeadlampFitSeconds);
            if (seconds >= total)
            {
                return 1f;
            }

            return Mathf.Lerp(HeadlampInitialWarmth, 1f, Smoothing.SmoothStep(0f, 1f, seconds / total));
        }

        /// <summary>The headlamp (or cell pod) unfold-and-clamp motion has finished and rests at home.</summary>
        public static bool HeadlampDone(GrowthSettings settings, float seconds)
        {
            return seconds >= settings.HeadlampFitSeconds;
        }
    }
}
