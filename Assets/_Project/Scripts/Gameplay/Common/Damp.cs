using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Frame-rate independent exponential approach: after one time constant 63 % of the gap is closed, whatever the
    /// frame rate. A non-positive time constant jumps straight to the target.
    /// </summary>
    public static class Damp
    {
        public static float Toward(float current, float target, float timeConstant, float deltaTime)
        {
            return current + (target - current) * Factor(timeConstant, deltaTime);
        }

        public static Vector3 Toward(Vector3 current, Vector3 target, float timeConstant, float deltaTime)
        {
            return current + (target - current) * Factor(timeConstant, deltaTime);
        }

        /// <summary>Fraction of the remaining gap closed during <paramref name="deltaTime"/>.</summary>
        public static float Factor(float timeConstant, float deltaTime)
        {
            if (timeConstant <= 0f)
            {
                return 1f;
            }

            return deltaTime <= 0f ? 0f : 1f - Mathf.Exp(-deltaTime / timeConstant);
        }
    }
}
