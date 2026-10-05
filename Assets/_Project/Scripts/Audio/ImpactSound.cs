using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The landing thump for an impact: silent below the threshold, then louder and lower as the impact grows
    /// (smoothstep, so the range is gentle at both ends).
    /// </summary>
    public readonly struct ImpactSound
    {
        public ImpactSound(bool audible, float volume, float pitch)
        {
            Audible = audible;
            Volume = volume;
            Pitch = pitch;
        }

        public bool Audible { get; }

        public float Volume { get; }

        public float Pitch { get; }

        public static ImpactSound ForLanding(float impactSpeed, AudioMixTuning tuning)
        {
            if (impactSpeed < tuning.ThumpMinImpact)
            {
                return new ImpactSound(false, 0f, 1f);
            }

            float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(tuning.ThumpMinImpact, tuning.ThumpFullImpact,
                impactSpeed));
            return new ImpactSound(true, Mathf.Lerp(tuning.ThumpSoftVolume, tuning.ThumpHardVolume, t),
                Mathf.Lerp(tuning.ThumpSoftPitch, tuning.ThumpHardPitch, t));
        }
    }
}
