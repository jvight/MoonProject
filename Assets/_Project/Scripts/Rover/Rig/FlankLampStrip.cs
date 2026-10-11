using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// What each flank lamp shows for a battery (docs/features/M3-20 "How 07 shows it"), as a pure function: a full
    /// cell is lit, the cell being drawn glows by what is left of it and breathes, an empty cell is dark glass; while
    /// 07 crawls the first lamp breathes faintly on the sealed reserve.
    /// </summary>
    public static class FlankLampStrip
    {
        /// <summary>
        /// Glow of lamp <paramref name="socket"/> with <paramref name="charge"/> cells of charge, at
        /// <paramref name="time"/> seconds (for the breath).
        /// </summary>
        public static float Level(FlankLampSettings settings, int socket, float charge, bool lowPower, float time)
        {
            if (lowPower)
            {
                return socket == 0 ? settings.ReserveGlow * Breath(settings, time) : 0f;
            }

            float left = Mathf.Clamp01(charge - socket);
            if (left >= 1f)
            {
                return settings.LitGlow;
            }

            return settings.LitGlow * left * Breath(settings, time);
        }

        /// <summary>1 at the top of a breath, 1 - depth at its bottom.</summary>
        public static float Breath(FlankLampSettings settings, float time)
        {
            float wave = 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * time / settings.BreathPeriod);
            return 1f - settings.BreathDepth * (1f - wave);
        }
    }
}
