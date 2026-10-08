using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A radio-hop's timing in seconds from its start (docs/features/M3-06): the view eases to a soft dark as the
    /// static rises, rests dark while 07 is placed on the target pad, then eases back in as the static resolves. About
    /// 2 s, never instant. Pure.
    /// </summary>
    public sealed class HopSequence
    {
        private readonly float _fadeOut;
        private readonly float _dark;
        private readonly float _fadeIn;

        public HopSequence(float fadeOut, float dark, float fadeIn)
        {
            if (fadeOut <= 0f || dark < 0f || fadeIn <= 0f)
            {
                throw new ArgumentException("A hop eases out and back in; it is never instant.");
            }

            _fadeOut = fadeOut;
            _dark = dark;
            _fadeIn = fadeIn;
        }

        public static HopSequence For(RelayTuning tuning)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            return new HopSequence(tuning.HopFadeOut, tuning.HopDark, tuning.HopFadeIn);
        }

        /// <summary>The view is fully dark: 07 is placed on the target pad (s).</summary>
        public float PlaceAt => _fadeOut;

        /// <summary>The view starts to ease back in (s): the hop is finished.</summary>
        public float FinishAt => _fadeOut + _dark;

        public float Duration => FinishAt + _fadeIn;

        /// <summary>0 clear .. 1 dark.</summary>
        public float Fade(float t)
        {
            if (t < _fadeOut)
            {
                return Ease.InOutSine(t / _fadeOut);
            }

            return t < FinishAt ? 1f : 1f - Ease.InOutSine((t - FinishAt) / _fadeIn);
        }

        /// <summary>0..1 through the whole hop.</summary>
        public float Progress(float t)
        {
            return Mathf.Clamp01(t / Duration);
        }

        public bool Done(float t)
        {
            return t >= Duration;
        }
    }
}
