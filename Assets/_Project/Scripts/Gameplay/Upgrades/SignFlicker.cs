using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A lit sign's first power-on, over t (seconds since the power came): it rises from dark with a few soft dimming
    /// flickers that grow shallower until it holds steady at full. Pure curve: 0 before, 1 once it has settled.
    /// </summary>
    public static class SignFlicker
    {
        /// <param name="duration">Seconds until it holds steady.</param>
        /// <param name="flickers">How many times it dims on the way.</param>
        /// <param name="depth">How deep the first dims are (0 just fades it on).</param>
        public static float Level(float t, float duration, int flickers, float depth)
        {
            if (t <= 0f)
            {
                return 0f;
            }

            if (t >= duration)
            {
                return 1f;
            }

            float u = t / duration;
            float dim = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * flickers * u);
            return Ease.OutCubic(u) * (1f - Mathf.Clamp01(depth) * (1f - u) * dim);
        }
    }
}
