using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>Easing curves on 0..1 (inputs are clamped). Every UI motion goes through these: nothing pops.</summary>
    internal static class UiEase
    {
        /// <summary>Symmetric S-curve: slow start, slow settle. Reversing midway retraces the same curve.</summary>
        public static float InOutSine(float t)
        {
            return 0.5f - 0.5f * Mathf.Cos(Mathf.PI * Mathf.Clamp01(t));
        }

        /// <summary>Quick start, long gentle settle (counting numbers).</summary>
        public static float OutCubic(float t)
        {
            float u = 1f - Mathf.Clamp01(t);
            return 1f - u * u * u;
        }
    }
}
