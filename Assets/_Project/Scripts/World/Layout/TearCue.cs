using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// What the old cable's tear does when 07's Begin wave reaches it (opening v2 §4): the last clip before the tear
    /// stutters a few times as it dims out, and one tiny cold spark ticks off the torn copper; nothing more (no smoke,
    /// no fire). Times are seconds since the Begin wave set off from 07's post. Plain logic.
    /// </summary>
    public static class TearCue
    {
        /// <summary>
        /// The last clip's stutter (0..1): from the moment the wave arrives (<paramref name="run"/> seconds in), it
        /// flickers <paramref name="flickers"/> times over <paramref name="seconds"/>, each flicker fainter, then stays
        /// out.
        /// </summary>
        public static float Stutter(float elapsed, float run, float seconds, int flickers)
        {
            float into = (elapsed - run) / Mathf.Max(seconds, 1e-4f);
            if (into < 0f || into >= 1f)
            {
                return 0f;
            }

            float flicker = 0.5f + 0.5f * Mathf.Cos(into * Mathf.Max(flickers, 1) * 2f * Mathf.PI);
            return flicker * (1f - into);
        }

        /// <summary>
        /// The spark (0..1): <paramref name="delay"/> seconds after the wave arrives it lights at once and fades over
        /// <paramref name="seconds"/>, once.
        /// </summary>
        public static float Spark(float elapsed, float run, float delay, float seconds)
        {
            float into = (elapsed - run - delay) / Mathf.Max(seconds, 1e-4f);
            return into < 0f || into >= 1f ? 0f : 1f - into;
        }
    }
}
