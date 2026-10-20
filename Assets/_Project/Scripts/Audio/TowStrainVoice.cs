using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The tow cable's strain as sound (docs/design/opening-v3-purpose.md R6): how loud and how high the creak loop
    /// plays for a given <c>ITowLine.Strain01</c>. Pure and monotonic: silent at or below the slack threshold, rising
    /// to full level and its top pitch as the cable stretches. Plain logic, tested on its own.
    /// </summary>
    public static class TowStrainVoice
    {
        /// <summary>The creak's level for <paramref name="strain01"/> (0 slack .. 1 caught fast): 0 at or below
        /// <paramref name="slack"/>, easing up to 1.</summary>
        public static float Gain(float strain01, float slack)
        {
            float t = Mathf.Clamp01((strain01 - slack) / Mathf.Max(1e-4f, 1f - slack));
            return t * t * (3f - 2f * t);
        }

        /// <summary>The creak's pitch for <paramref name="strain01"/>: <paramref name="lowPitch"/> just taut up to
        /// <paramref name="highPitch"/> stretched.</summary>
        public static float Pitch(float strain01, float lowPitch, float highPitch)
        {
            return Mathf.Lerp(lowPitch, highPitch, Mathf.Clamp01(strain01));
        }
    }
}
