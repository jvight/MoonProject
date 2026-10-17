using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// A slow wave of light along a cable's marker lamps (opening v2 §4): every so often a soft brightening runs from
    /// the first marker to the last, leading the eye along the cable to what it feeds. Plain logic.
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
            float into = Mathf.Repeat(time, repeat) / run;
            float at = count > 1 ? index / (float)(count - 1) : 0f;
            float distance = Mathf.Abs(into - at) / Mathf.Max(width, 1e-4f);
            return distance >= 1f ? 0f : 0.5f + 0.5f * Mathf.Cos(distance * Mathf.PI);
        }
    }
}
