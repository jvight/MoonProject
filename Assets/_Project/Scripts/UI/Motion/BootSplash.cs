using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The Outsight Worlds boot splash's timeline (pure, EditMode-tested; BOOT_SPLASH_SPEC.md): what each layer shows
    /// at the splash's time, with every move a smooth step. It plays while the boot's dark already hides the engine's
    /// warm-up; on any launch after the first, a key or click after its first second fades it out early.
    /// </summary>
    internal sealed class BootSplash
    {
        private readonly BootSplashSettings _settings;
        private float _skipFade = 1f;
        private bool _skipping;

        public BootSplash(BootSplashSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Time = settings.Duration;
        }

        /// <summary>Seconds since the splash began (its duration once it is over or was never started).</summary>
        public float Time { get; private set; }

        /// <summary>True while the splash still holds the dark.</summary>
        public bool IsPlaying => !IsDone;

        /// <summary>True once it has played out or a skip has faded it.</summary>
        public bool IsDone => Time >= _settings.Duration || (_skipping && _skipFade <= 0f);

        /// <summary>True when a key or click may cut it short (never on the very first launch).</summary>
        public bool Skippable { get; private set; }

        public float RingAlpha => Fade(Rise(_settings.RingIn0, _settings.RingIn1) * MainOut);

        public float StrokeAlpha => Fade(Rise(_settings.StrokeIn0, _settings.StrokeIn1) * MainOut);

        /// <summary>How much of the stroke has drawn out of the ring (0..1).</summary>
        public float StrokeProgress => Rise(_settings.StrokeDraw0, _settings.StrokeDraw1);

        public float GlintAlpha => Fade(Rise(_settings.GlintIn0, _settings.GlintIn1) *
                                        (1f - Rise(_settings.GlintOut0, _settings.GlintOut1)));

        public float GlintScale
        {
            get
            {
                if (Time < _settings.GlintPeak)
                {
                    return Mathf.Lerp(_settings.GlintStartScale, _settings.GlintPeakScale,
                        Rise(_settings.GlintIn0, _settings.GlintPeak));
                }

                float settled = Mathf.Lerp(_settings.GlintPeakScale, 1f,
                    Rise(_settings.GlintPeak, _settings.GlintSettled));
                return Time < _settings.GlintOut0
                    ? settled
                    : Mathf.Lerp(1f, _settings.GlintEndScale, Rise(_settings.GlintOut0, _settings.GlintOut1));
            }
        }

        public float WordAlpha => Fade(Rise(_settings.WordIn0, _settings.WordIn1) * MainOut);

        /// <summary>How far below its place the wordmark still sits, as a share of the layer's height.</summary>
        public float WordDrop => _settings.WordRise * (1f - Rise(_settings.WordIn0, _settings.WordIn1));

        private float MainOut => 1f - Rise(_settings.Dissolve0, _settings.Dissolve1);

        /// <summary>The splash begins; <paramref name="skippable"/> on every launch after the first.</summary>
        public void Start(bool skippable)
        {
            Time = 0f;
            Skippable = skippable;
            _skipping = false;
            _skipFade = 1f;
        }

        /// <summary>
        /// A key or click: a skippable splash past its first second fades out now; the first launch's plays on.
        /// </summary>
        public void Skip()
        {
            if (Skippable && IsPlaying && Time >= _settings.SkipAfterSeconds)
            {
                _skipping = true;
            }
        }

        /// <param name="unscaledDeltaTime">
        /// Real seconds since the last tick (a long hitch counts as one short step).
        /// </param>
        public void Tick(float unscaledDeltaTime)
        {
            if (IsDone)
            {
                return;
            }

            float step = Mathf.Min(unscaledDeltaTime, _settings.MaxStep);
            Time = Mathf.Min(_settings.Duration, Time + step);
            if (_skipping)
            {
                _skipFade = Mathf.Max(0f, _skipFade - step / _settings.SkipFadeSeconds);
            }
        }

        /// <summary>Sets the splash's time directly (deterministic captures).</summary>
        public void Seek(float time)
        {
            Time = Mathf.Clamp(time, 0f, _settings.Duration);
        }

        private float Fade(float alpha) => _skipping ? alpha * _skipFade : alpha;

        private float Rise(float from, float to) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, Time));
    }
}
