using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How a relic's answer sounds at a given distance from the rover: near relics answer fuller and brighter, far
    /// ones quieter and darker (a low-pass sweeping down logarithmically). The bright end is the clip itself, which
    /// is already gentle, so nothing ever gets harsh.
    /// </summary>
    public readonly struct RelicAnswerTone
    {
        public RelicAnswerTone(float volume, float cutoffHz)
        {
            Volume = volume;
            CutoffHz = cutoffHz;
        }

        public float Volume { get; }

        public float CutoffHz { get; }

        public static RelicAnswerTone ForDistance(float distance, GameplayAudioTuning tuning)
        {
            float t = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(tuning.AnswerNearDistance, tuning.AnswerFarDistance, distance));
            float cutoff = tuning.AnswerNearCutoff * Mathf.Pow(tuning.AnswerFarCutoff / tuning.AnswerNearCutoff, t);
            return new RelicAnswerTone(Mathf.Lerp(tuning.AnswerNearVolume, tuning.AnswerFarVolume, t), cutoff);
        }
    }
}
