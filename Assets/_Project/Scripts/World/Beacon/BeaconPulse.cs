using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The beacon's breathing curve: a raised cosine (eases in and out, no hard edges), shaped by an exponent so the
    /// light rests dim a little longer and swells softly. Pure, so it is tested and shared by lamp and halo.
    /// </summary>
    public static class BeaconPulse
    {
        /// <summary>Pulse in [0, 1] at <paramref name="time"/> seconds; peaks once per period at half period.</summary>
        public static float Evaluate(float time, float period, float sharpness)
        {
            float phase = Mathf.Repeat(time / period, 1f);
            float wave = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
            return Mathf.Pow(wave, sharpness);
        }
    }
}
