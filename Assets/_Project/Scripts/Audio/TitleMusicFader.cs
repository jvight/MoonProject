using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Envelope for the title screen theme: silent until <c>TitleShown</c>, fades in to full over
    /// <c>fadeInSeconds</c> (~2 s), stays playing through Settings and Credits on the title screen, and fades out to
    /// silence over <c>fadeOutSeconds</c> (~2.5 s) when Continue (<c>SessionStarted</c>) or New Game
    /// (<c>NewGameRequested</c>) begins play.
    /// </summary>
    public sealed class TitleMusicFader
    {
        private const float MinDuration = 0.01f;

        private float _level;
        private bool _active;
        private bool _fadingOut;

        /// <summary>Current 0..1 envelope gain for the title theme source.</summary>
        public float Level => _level;

        /// <summary>True while the title theme should be audible or is still fading in/out.</summary>
        public bool IsPlaying => _active;

        /// <summary>True once Continue or New Game has started fading the title theme out.</summary>
        public bool IsFadingOut => _fadingOut;

        /// <summary>Called on <c>TitleShown</c>: begins fading the title theme in toward 1.</summary>
        public void ShowTitle()
        {
            _active = true;
            _fadingOut = false;
        }

        /// <summary>
        /// Called on <c>SessionStarted</c> or <c>NewGameRequested</c>: fades the title theme out to 0 and stops it.
        /// Idempotent when called more than once (e.g. <c>NewGameRequested</c> followed by <c>SessionStarted</c>).
        /// </summary>
        public void FadeOut()
        {
            if (!_active)
            {
                return;
            }

            if (_level <= 0f)
            {
                _active = false;
                _fadingOut = false;
                return;
            }

            _fadingOut = true;
        }

        /// <summary>Advances the fade by <paramref name="dt"/> unscaled seconds and returns <see cref="Level"/>.</summary>
        public float Step(float dt, float fadeInSeconds, float fadeOutSeconds)
        {
            if (!_active || dt <= 0f)
            {
                return _level;
            }

            if (_fadingOut)
            {
                float fallRate = 1f / Mathf.Max(MinDuration, fadeOutSeconds);
                _level = Mathf.MoveTowards(_level, 0f, dt * fallRate);
                if (_level <= 0f)
                {
                    _level = 0f;
                    _active = false;
                    _fadingOut = false;
                }
            }
            else
            {
                float riseRate = 1f / Mathf.Max(MinDuration, fadeInSeconds);
                _level = Mathf.MoveTowards(_level, 1f, dt * riseRate);
            }

            return _level;
        }
    }
}
