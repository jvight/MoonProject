using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The Hover-Jump charge hum: each reported charge step moves the hum up D major pentatonic from D (D E F# A B for
    /// strengths 0, ¼, ½, ¾, 1) with a short glide, swelling as it grows; a leap stops it quickly (the boing takes
    /// over) and a cancel lets it fade softly.
    /// </summary>
    public sealed class JumpChargeModel
    {
        private const float CentsPerSemitone = 100f;
        private const float CentsPerOctave = 1200f;

        /// <summary>Semitones above D of the pentatonic steps D E F# A B.</summary>
        private static readonly int[] StepSemitones = { 0, 2, 4, 7, 9 };

        private readonly LoopFader _fader = new LoopFader();
        private readonly EasedValue _semitones = new EasedValue(0f);
        private float _strength;
        private float _stopFade;

        public bool IsCharging { get; private set; }

        public bool IsAudible => _fader.IsAudible;

        public float Pitch => Mathf.Pow(2f, _semitones.Value * CentsPerSemitone / CentsPerOctave);

        /// <summary>Semitones above D the hum is heading to (the current step).</summary>
        public int TargetSemitones { get; private set; }

        /// <summary>Gain 0..1: the fade times the swell from <paramref name="startVolume"/> at strength 0.</summary>
        public float Gain(float startVolume)
        {
            return _fader.Gain * Mathf.Lerp(startVolume, 1f, _strength);
        }

        /// <summary>A charge step arrived (strength 0 starts a fresh charge).</summary>
        public void Charge(float strength)
        {
            _strength = Mathf.Clamp01(strength);
            int index = Mathf.Clamp(Mathf.RoundToInt(_strength * (StepSemitones.Length - 1)), 0,
                StepSemitones.Length - 1);
            TargetSemitones = StepSemitones[index];
            if (!IsCharging)
            {
                IsCharging = true;
                _semitones.Snap(TargetSemitones);
            }

            _fader.FadeIn();
        }

        /// <summary>The jump was released into a leap: hand over quickly.</summary>
        public void Leap(float fadeTime)
        {
            Stop(fadeTime);
        }

        /// <summary>The charge ended without a leap: let the hum go softly.</summary>
        public void Cancel(float fadeTime)
        {
            Stop(fadeTime);
        }

        public void Step(float deltaTime, float fadeInTime, float glideTime)
        {
            _semitones.Step(TargetSemitones, deltaTime, glideTime);
            _fader.Step(deltaTime, fadeInTime, _stopFade);
        }

        private void Stop(float fadeTime)
        {
            if (!IsCharging)
            {
                return;
            }

            IsCharging = false;
            _stopFade = fadeTime;
            _fader.FadeOut();
        }
    }
}
