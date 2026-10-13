using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>Brightness curves of the sonar's site markers (pure, 0..1).</summary>
    public static class MarkerEnvelope
    {
        /// <summary>
        /// A light pillar <paramref name="age"/> seconds after its answer: fades in softly over
        /// <paramref name="rise"/>, holds, then fades out over the last <paramref name="fade"/> seconds of
        /// <paramref name="lifetime"/>.
        /// </summary>
        public static float Pillar(float age, float lifetime, float rise, float fade)
        {
            if (age < 0f || age >= lifetime)
            {
                return 0f;
            }

            float up = rise <= 0f ? 1f : Ease.InOutSine(age / rise);
            float remaining = lifetime - age;
            float down = fade <= 0f ? 1f : Ease.InOutSine(remaining / Mathf.Min(fade, lifetime));
            return Mathf.Min(up, down);
        }

        /// <summary>Slow breathing 0..1 with a period of <paramref name="period"/> seconds.</summary>
        public static float Breath(float time, float period)
        {
            return period <= 0f ? 1f : 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * time / period);
        }
    }
}
