using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// A short burst of sharp flashes on a cue (the lander's beacon answering 07's failed cutter check): a few square
    /// flashes with soft edges, then nothing. Plain logic.
    /// </summary>
    public static class BeaconFlash
    {
        /// <summary>
        /// The burst's level <paramref name="elapsed"/> seconds after its cue (0..1): <paramref name="count"/> flashes,
        /// one every <paramref name="period"/> seconds, each lit for <paramref name="onShare"/> of it with edges
        /// <paramref name="edge"/> of the period wide; 0 before the cue and after the last flash.
        /// </summary>
        public static float Level(float elapsed, int count, float period, float onShare, float edge)
        {
            if (elapsed < 0f || elapsed >= count * period)
            {
                return 0f;
            }

            float phase = Mathf.Repeat(elapsed, period) / period;
            float soft = Mathf.Max(edge, 1e-4f);
            return Mathf.Clamp01(phase / soft) * Mathf.Clamp01((onShare - phase) / soft);
        }
    }
}
