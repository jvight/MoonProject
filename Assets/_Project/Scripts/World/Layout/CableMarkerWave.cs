using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// A slow wave of light along a cable's marker lamps (opening v2 §4): every so often a soft brightening runs from
    /// the first marker to the last, leading the eye along the cable to what it feeds; a single wave can also be sent
    /// on a cue. Plain logic.
    /// </summary>
    public static class CableMarkerWave
    {
        /// <summary>
        /// How much the wave lifts marker <paramref name="index"/> of <paramref name="count"/> at
        /// <paramref name="time"/> (0..1): it crosses the cable over <paramref name="run"/> seconds, every
        /// <paramref name="repeat"/> seconds, each marker brightening and fading over <paramref name="width"/> of the
        /// run.
        /// </summary>
        public static float Lift(float time, int index, int count, float run, float repeat, float width)
        {
            return Shape(Mathf.Repeat(time, repeat) / run, index, count, width);
        }

        /// <summary>
        /// The lift of a single wave started <paramref name="elapsed"/> seconds ago (0..1): it crosses the cable once
        /// over <paramref name="run"/> seconds and is gone after; nothing before it starts.
        /// </summary>
        public static float Once(float elapsed, int index, int count, float run, float width)
        {
            return elapsed < 0f ? 0f : Shape(elapsed / run, index, count, width);
        }

        // How much a wave `into` its run (0 at the first marker, 1 at the last) lifts marker `index` of `count`.
        private static float Shape(float into, int index, int count, float width)
        {
            float at = count > 1 ? index / (float)(count - 1) : 0f;
            float distance = Mathf.Abs(into - at) / Mathf.Max(width, 1e-4f);
            return distance >= 1f ? 0f : 0.5f + 0.5f * Mathf.Cos(distance * Mathf.PI);
        }
    }
}
