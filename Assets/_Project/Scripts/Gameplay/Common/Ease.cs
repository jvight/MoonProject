using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Easing curves on 0..1 (inputs are clamped). Everything that moves in gameplay eases through these, so nothing
    /// starts or stops on a frame-one snap.
    /// </summary>
    public static class Ease
    {
        public static float InOutSine(float t)
        {
            return 0.5f - 0.5f * Mathf.Cos(Mathf.PI * Mathf.Clamp01(t));
        }

        public static float OutQuad(float t)
        {
            float u = 1f - Mathf.Clamp01(t);
            return 1f - u * u;
        }

        /// <summary>Inverse of <see cref="OutQuad"/>: the t at which OutQuad reaches <paramref name="value"/>.</summary>
        public static float InverseOutQuad(float value)
        {
            return 1f - Mathf.Sqrt(1f - Mathf.Clamp01(value));
        }

        public static float OutCubic(float t)
        {
            float u = 1f - Mathf.Clamp01(t);
            return 1f - u * u * u;
        }

        public static float InOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.5f)
            {
                return 4f * t * t * t;
            }

            float u = -2f * t + 2f;
            return 1f - u * u * u * 0.5f;
        }

        /// <summary>
        /// Arrives past 1 by an amount set by <paramref name="overshoot"/> (0 = none, ~1.7 = classic) and settles back:
        /// the soft overshoot every settle in the game uses.
        /// </summary>
        public static float OutBack(float t, float overshoot)
        {
            float u = Mathf.Clamp01(t) - 1f;
            return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
        }

        /// <summary>0 at both ends, 1 in the middle, with zero slope at the ends (sin squared).</summary>
        public static float Hump(float t)
        {
            float s = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
            return s * s;
        }

        /// <summary>Hermite step from 0 at <paramref name="edge0"/> to 1 at <paramref name="edge1"/>.</summary>
        public static float Step(float edge0, float edge1, float x)
        {
            if (Mathf.Approximately(edge0, edge1))
            {
                return x < edge0 ? 0f : 1f;
            }

            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
