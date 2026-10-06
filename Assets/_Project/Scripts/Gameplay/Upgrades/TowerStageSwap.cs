using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The upgrade moment of the radio tower over time t (seconds since the purchase): the beacon flares while the
    /// old stage sinks a little, then the new stage replaces it at the flare's peak and grows in with a soft settle
    /// as the flare fades. Pure curves.
    /// </summary>
    public static class TowerStageSwap
    {
        public static bool Swapped(float t, float flareDuration)
        {
            return t >= flareDuration;
        }

        /// <summary>Scale of the old stage before the swap.</summary>
        public static float Sink(float t, float flareDuration, float sinkTo)
        {
            return Mathf.Lerp(1f, sinkTo, Ease.InOutSine(t / flareDuration));
        }

        /// <summary>Scale of the new stage after the swap.</summary>
        public static float Grow(float t, float flareDuration, float growDuration, float from, float overshoot)
        {
            if (t < flareDuration)
            {
                return from;
            }

            return Mathf.LerpUnclamped(from, 1f, Ease.OutBack((t - flareDuration) / growDuration, overshoot));
        }

        /// <summary>0..1 flare: rises over the flare, fades over the growth.</summary>
        public static float Flare(float t, float flareDuration, float growDuration)
        {
            if (t < 0f)
            {
                return 0f;
            }

            return t < flareDuration ? Ease.OutCubic(t / flareDuration)
                : 1f - Ease.InOutSine((t - flareDuration) / growDuration);
        }

        public static bool Done(float t, float flareDuration, float growDuration)
        {
            return t >= flareDuration + growDuration;
        }

        /// <summary>
        /// Stage shown at <paramref name="level"/>: the first stage until level 1, then one per level.
        /// </summary>
        public static int StageFor(int level, int stageCount)
        {
            return Mathf.Clamp(level - 1, 0, stageCount - 1);
        }
    }
}
