using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The radio's signal strength around the base: fully clear inside the signal radius, then a smootherstep
    /// falloff to nothing over the falloff width (no audible edge anywhere).
    /// </summary>
    public static class SignalField
    {
        /// <summary>Distance on the XZ plane (height does not weaken the signal).</summary>
        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Clarity 0..1 at <paramref name="distance"/> metres from the base.</summary>
        public static float Clarity(float distance, float clearRadius, float falloffWidth)
        {
            if (falloffWidth <= 0f)
            {
                return distance <= clearRadius ? 1f : 0f;
            }

            float t = Mathf.Clamp01((distance - clearRadius) / falloffWidth);
            return 1f - t * t * t * (t * (t * 6f - 15f) + 10f);
        }
    }
}
