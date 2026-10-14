using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>Brightness curves of the sonar's answer blinks and station breathing (pure, 0..1).</summary>
    public static class MarkerEnvelope
    {
        private const float BlinkAttack = 0.15f;
        private const float BlinkEmber = 0.22f;

        /// <summary>
        /// An answering lamp <paramref name="age"/> seconds after its answer: rises smoothly, pulses slowly with period
        /// <paramref name="period"/> over <paramref name="duration"/> seconds, and fades to zero at the end.
        /// </summary>
        public static float Blink(float age, float duration, float period)
        {
            if (age <= 0f || age >= duration || duration <= 0f || period <= 0f)
            {
                return 0f;
            }

            float riseSpan = Mathf.Min(BlinkAttack, duration * 0.5f);
            float fadeSpan = Mathf.Min(period * 0.5f, duration * 0.5f);
            float rise = riseSpan <= 0f ? 1f : Ease.InOutSine(age / riseSpan);
            float fade = fadeSpan <= 0f ? 1f : Ease.InOutSine((duration - age) / fadeSpan);
            float envelope = Mathf.Min(rise, fade);
            float pulse = Mathf.Lerp(BlinkEmber, 1f, 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * age / period));
            return envelope * pulse;
        }

        /// <summary>Slow breathing 0..1 with a period of <paramref name="period"/> seconds.</summary>
        public static float Breath(float time, float period)
        {
            return period <= 0f ? 1f : 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * time / period);
        }
    }
}
