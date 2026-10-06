using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Frame-rate independent easing helpers. Every smoothed value in the rover uses half-lives (seconds to close half
    /// of the remaining gap) so behaviour is identical at 30, 60 or 144 fps and in FixedUpdate.
    /// </summary>
    public static class Smoothing
    {
        /// <summary>Fraction of the gap closed during <paramref name="deltaTime"/>; 1 when halfLife is 0.</summary>
        public static float Factor(float halfLife, float deltaTime)
        {
            if (halfLife <= 0f)
            {
                return 1f;
            }

            return 1f - Mathf.Pow(2f, -deltaTime / halfLife);
        }

        /// <summary>Exponentially approaches <paramref name="target"/>.</summary>
        public static float Damp(float current, float target, float halfLife, float deltaTime)
        {
            return current + (target - current) * Factor(halfLife, deltaTime);
        }

        /// <summary>Exponentially approaches <paramref name="target"/> along the shortest arc (degrees).</summary>
        public static float DampAngle(float current, float target, float halfLife, float deltaTime)
        {
            return current + Mathf.DeltaAngle(current, target) * Factor(halfLife, deltaTime);
        }

        /// <summary>Exponentially approaches <paramref name="target"/> per component.</summary>
        public static Vector3 Damp(Vector3 current, Vector3 target, float halfLife, float deltaTime)
        {
            return current + (target - current) * Factor(halfLife, deltaTime);
        }

        /// <summary>Hermite ease from <paramref name="edge0"/> to <paramref name="edge1"/>, clamped to 0..1.</summary>
        public static float SmoothStep(float edge0, float edge1, float x)
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
