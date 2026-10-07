using System;

namespace MoonProject.World
{
    /// <summary>
    /// Smooth (at least C1) building blocks for the height function. Every term of the moon surface is assembled from
    /// these so that the surface gradient is continuous everywhere and central-difference normals never flicker.
    /// </summary>
    public static class SmoothMath
    {
        /// <summary>C2 smootherstep: 0 below <paramref name="edge0"/>, 1 above <paramref name="edge1"/>.</summary>
        public static float Smootherstep(float edge0, float edge1, float x)
        {
            float t = (x - edge0) / (edge1 - edge0);
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= 1f)
            {
                return 1f;
            }

            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        /// <summary>Compact C1 bump: (1 - x^2)^2 inside |x| &lt; 1, zero outside; peak 1 at x = 0.</summary>
        public static float Bump(float x)
        {
            float s = 1f - x * x;
            return s > 0f ? s * s : 0f;
        }

        /// <summary>C-infinity approximation of |x|, rounded within about <paramref name="softness"/> of 0.</summary>
        public static float SmoothAbs(float x, float softness)
        {
            return (float)Math.Sqrt(x * x + softness * softness) - softness;
        }

        /// <summary>
        /// C1 ramp from 0 to 1 over [0, <paramref name="length"/>]: quadratic ease-in and ease-out of
        /// <paramref name="ease"/> metres around a straight middle, so its steepest slope is 1 / (length - ease).
        /// </summary>
        public static float SmoothRamp(float x, float length, float ease)
        {
            if (x <= 0f)
            {
                return 0f;
            }

            if (x >= length)
            {
                return 1f;
            }

            float slope = 1f / (length - ease);
            if (x < ease)
            {
                return slope * x * x / (2f * ease);
            }

            if (x > length - ease)
            {
                float rest = length - x;
                return 1f - slope * rest * rest / (2f * ease);
            }

            return slope * (x - ease * 0.5f);
        }

        /// <summary>Polynomial smooth minimum (C1): rounds the crease where a and b cross within ~k.</summary>
        public static float SmoothMin(float a, float b, float k)
        {
            float h = Math.Max(k - Math.Abs(a - b), 0f) / k;
            return Math.Min(a, b) - h * h * k * 0.25f;
        }

        /// <summary>Polynomial smooth maximum (C1), the mirror of <see cref="SmoothMin"/>.</summary>
        public static float SmoothMax(float a, float b, float k)
        {
            return -SmoothMin(-a, -b, k);
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>Floor to int without the Math.Floor double round trip (hot path of the noise).</summary>
        public static int FastFloor(float x)
        {
            int i = (int)x;
            return x < i ? i - 1 : i;
        }
    }
}
