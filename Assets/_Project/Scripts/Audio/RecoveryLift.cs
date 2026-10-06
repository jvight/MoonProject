using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The recovery lift's sound over its known duration: the servo loop swells in, glides up an in-key interval
    /// as 07 rises, eases out just before touchdown, and asks for the settle sound exactly when the lift ends.
    /// </summary>
    public sealed class RecoveryLift
    {
        private const float CentsPerSemitone = 100f;
        private const float CentsPerOctave = 1200f;

        private float _elapsed;
        private float _duration;
        private float _fadeIn;
        private float _fadeOut;

        public bool Active { get; private set; }

        /// <summary>0..1 through the lift (1 when idle).</summary>
        public float Progress => Active ? Mathf.Clamp01(_elapsed / _duration) : 1f;

        /// <summary>Loop gain 0..1: smooth swell in and out, both inside the lift's duration.</summary>
        public float Gain
        {
            get
            {
                if (!Active)
                {
                    return 0f;
                }

                return Smooth(_elapsed / _fadeIn) * Smooth((_duration - _elapsed) / _fadeOut);
            }
        }

        /// <summary>Starts (or restarts) a lift of <paramref name="duration"/> seconds. Fades are capped at half of
        /// it so short lifts still swell and settle.</summary>
        public void Begin(float duration, float fadeIn, float fadeOut)
        {
            _duration = Mathf.Max(duration, Mathf.Epsilon);
            float half = 0.5f * _duration;
            _fadeIn = Mathf.Clamp(fadeIn, Mathf.Epsilon, half);
            _fadeOut = Mathf.Clamp(fadeOut, Mathf.Epsilon, half);
            _elapsed = 0f;
            Active = true;
        }

        /// <summary>Pitch multiplier gliding from 1 to <paramref name="riseSemitones"/> up over the lift.</summary>
        public float PitchFactor(float riseSemitones)
        {
            return Mathf.Pow(2f, Smooth(Progress) * riseSemitones * CentsPerSemitone / CentsPerOctave);
        }

        /// <summary>Advances time; returns true on the step the lift ends (play the settle).</summary>
        public bool Step(float deltaTime)
        {
            if (!Active)
            {
                return false;
            }

            _elapsed += Mathf.Max(0f, deltaTime);
            if (_elapsed < _duration)
            {
                return false;
            }

            Active = false;
            return true;
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
