using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// What a given radio clarity sounds like: low-pass cutoff (swept logarithmically, so the haze opens evenly to
    /// the ear), music and static volume scales and the depth of the tape wobble.
    /// </summary>
    public readonly struct RadioMix
    {
        public RadioMix(float cutoffHz, float musicVolume, float staticVolume, float wobbleCents)
        {
            CutoffHz = cutoffHz;
            MusicVolume = musicVolume;
            StaticVolume = staticVolume;
            WobbleCents = wobbleCents;
        }

        public float CutoffHz { get; }

        public float MusicVolume { get; }

        public float StaticVolume { get; }

        public float WobbleCents { get; }

        /// <summary>The mix for <paramref name="clarity"/> (0 = no signal, 1 = perfect).</summary>
        public static RadioMix Evaluate(float clarity, RadioTuning tuning)
        {
            float c = Mathf.Clamp01(clarity);
            float sweep = Mathf.Pow(c, tuning.CutoffCurve);
            float cutoff = tuning.MinCutoff * Mathf.Pow(tuning.MaxCutoff / tuning.MinCutoff, sweep);
            float staticVolume = Mathf.Lerp(tuning.StaticFloorVolume, tuning.StaticMaxVolume,
                Mathf.Pow(1f - c, tuning.StaticCurve));
            float musicVolume = Mathf.Lerp(tuning.MusicVolumeAtNoSignal, 1f, c);
            float wobble = Mathf.Lerp(tuning.MaxWobbleCents, tuning.BaseWobbleCents, c);
            return new RadioMix(cutoff, musicVolume, staticVolume, wobble);
        }
    }
}
