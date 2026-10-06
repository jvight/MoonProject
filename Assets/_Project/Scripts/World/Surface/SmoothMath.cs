using System;

namespace MoonProject.World
{
    /// <summary>
    /// Smooth (at least C1) building blocks for the height function. Every term of the moon surface is assembled from
    /// these so that the surface gradient is continuous everywhere and central-difference normals never flicker.
    /// </summary>
    public static class SmoothMath
    {
        /// <summary>Ken Perlin's C2 smootherstep: 0 below <paramref name="edge0"/>, 1 above <paramref name="edge1"/>.</summary>
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

        /// <summary>C-infinity approximation of |x| that is rounded within roughly <paramref name="softness"/> of 0.</summary>
        public static float SmoothAbs(float x, float softness)
        {
            return (float)Math.Sqrt(x * x + softness * softness) - softness;
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>Floor to int without the Math.Floor double round trip (the noise hot path calls this a lot).</summary>
        public static int FastFloor(float x)
        {
            int i = (int)x;
            return x < i ? i - 1 : i;
        }
    }
}
