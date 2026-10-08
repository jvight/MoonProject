using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The salvage beam's sound logic (M3-13): it fades in as a cut starts; its singing edge climbs D major
    /// pentatonic one note per step of cutting (D, E, F#, A, B, D), gliding between notes, while the material's grind
    /// tightens a little; letting go fades it softly, a piece breaking loose lets it fall away quickly. Each hold
    /// climbs from the bottom again. Allocation-free.
    /// </summary>
    public sealed class CutBeamModel
    {
        private const float SemitonesPerOctave = 12f;

        // D, E, F#, A, B, and the D an octave up.
        private const int HighestStep = 5;

        private readonly SalvageAudioTuning _tuning;
        private readonly LoopFader _fader = new LoopFader();
        private readonly EasedValue _tonePitch = new EasedValue(1f);
        private float _elapsed;
        private float _fadeOut;

        public CutBeamModel(SalvageAudioTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>0 silent .. 1 cutting (eased in and out).</summary>
        public float Gain => _fader.Gain;

        public bool Audible => _fader.IsAudible;

        /// <summary>The pentatonic step the tone is on (0 = D).</summary>
        public int Step { get; private set; }

        /// <summary>Pitch of the singing tone (1 = D), gliding between pentatonic notes.</summary>
        public float TonePitch => _tonePitch.Value;

        /// <summary>Pitch of the material's texture, tightening as the climb goes on.</summary>
        public float TexturePitch => Mathf.Lerp(1f, _tuning.TextureRise, Climb);

        /// <summary>0 .. 1 how far up its climb the tone is.</summary>
        public float Climb => _tuning.TopStep > 0 ? Mathf.Clamp01((float)Step / _tuning.TopStep) : 0f;

        /// <summary>A cut started (or resumed): the climb begins again from D.</summary>
        public void Start()
        {
            _elapsed = 0f;
            Step = 0;
            _tonePitch.Snap(1f);
            _fader.FadeIn();
        }

        /// <summary>The cut stopped: a soft release, or a quick fall when the piece came loose.</summary>
        public void Stop(bool completed)
        {
            _fadeOut = completed ? _tuning.BreakFade : _tuning.ReleaseFade;
            _fader.FadeOut();
        }

        public void Update(float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            _fader.Step(dt, _tuning.CutFadeIn, _fadeOut);
            if (!_fader.IsOn)
            {
                return;
            }

            _elapsed += dt;
            int top = Mathf.Min(_tuning.TopStep, HighestStep);
            Step = Mathf.Min(Mathf.FloorToInt(_elapsed / _tuning.StepSeconds), top);
            _tonePitch.Step(Ratio(Step), dt, _tuning.Glide);
        }

        /// <summary>Pitch ratio of pentatonic <paramref name="step"/> above D.</summary>
        public static float Ratio(int step)
        {
            return Mathf.Pow(2f, Semitones(Mathf.Clamp(step, 0, HighestStep)) / SemitonesPerOctave);
        }

        /// <summary>Semitones above D of D major pentatonic's <paramref name="step"/> (0..5).</summary>
        private static int Semitones(int step)
        {
            switch (step)
            {
                case 1:
                    return 2;
                case 2:
                    return 4;
                case 3:
                    return 7;
                case 4:
                    return 9;
                case 5:
                    return 12;
                default:
                    return 0;
            }
        }
    }
}
