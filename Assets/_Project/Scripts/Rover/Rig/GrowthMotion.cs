using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The growth parts' motion as pure functions of the seconds since their event (<see cref="GrowthSettings"/>).
    /// </summary>
    public static class GrowthMotion
    {
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
    }
}
