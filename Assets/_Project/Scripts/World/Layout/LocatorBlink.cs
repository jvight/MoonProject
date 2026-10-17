using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// A locator beacon's pulse (the landed lander's emergency beacon): slow and warm, never fully dark, so it still
    /// reads from far off between flashes. Plain logic.
    /// </summary>
    public static class LocatorBlink
    {
        /// <summary>The beacon's level (floor..1) at <paramref name="time"/>: a soft peak once per period.</summary>
        public static float Level(float time, float period, float sharpness, float floor)
        {
            float wave = 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * time / period);
            return Mathf.Lerp(floor, 1f, Mathf.Pow(wave, sharpness));
        }
    }
}
