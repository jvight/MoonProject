using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Tape-style pitch wobble: a slow "wow" sine plus a faster "flutter" sine, blended into -1..1. Phases advance
    /// with time (deterministic, allocation-free); <see cref="PitchFactor"/> turns it into an AudioSource pitch.
    /// </summary>
    public sealed class WowFlutter
    {
        private const float TwoPi = 2f * Mathf.PI;
        private const float CentsPerOctave = 1200f;

        private float _wowPhase;
        private float _flutterPhase;

        /// <summary>Advances both oscillators and returns the blended wobble (-1..1).</summary>
        public float Step(float deltaTime, float wowRate, float flutterRate, float flutterShare)
        {
            _wowPhase = Mathf.Repeat(_wowPhase + wowRate * deltaTime, 1f);
            _flutterPhase = Mathf.Repeat(_flutterPhase + flutterRate * deltaTime, 1f);
            float share = Mathf.Clamp01(flutterShare);
            return (1f - share) * Mathf.Sin(TwoPi * _wowPhase) + share * Mathf.Sin(TwoPi * _flutterPhase);
        }

        /// <summary>Pitch multiplier for a wobble <paramref name="value"/> scaled to <paramref name="depthCents"/>.</summary>
        public static float PitchFactor(float value, float depthCents)
        {
            return Mathf.Pow(2f, value * depthCents / CentsPerOctave);
        }
    }
}
