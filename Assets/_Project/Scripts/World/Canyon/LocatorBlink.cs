using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The fallen supply pod's locator beacon (0.5.5): a slow, warm pulse that never goes fully dark, so the ledge
    /// still reads from home between flashes. Plain logic.
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
