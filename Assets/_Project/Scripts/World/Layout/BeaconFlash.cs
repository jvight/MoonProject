using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// One sharp flash on a cue (the lander's beacon answering a spark of 07's cutter): lit for a moment with soft
    /// edges, then nothing. Plain logic.
    /// </summary>
    public static class BeaconFlash
    {
        /// <summary>
        /// The flash's level <paramref name="elapsed"/> seconds after its cue (0..1): lit for <paramref name="length"/>
        /// seconds, lighting and fading over <paramref name="edge"/> seconds at each end; 0 before the cue and after.
        /// </summary>
        public static float Level(float elapsed, float length, float edge)
        {
            if (elapsed < 0f || elapsed >= length)
            {
                return 0f;
            }

            float soft = Mathf.Max(edge, 1e-4f);
            return Mathf.Clamp01(elapsed / soft) * Mathf.Clamp01((length - elapsed) / soft);
        }
    }
}
