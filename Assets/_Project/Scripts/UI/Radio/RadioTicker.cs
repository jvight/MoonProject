using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The radio ticker (docs/features/M3-05 "The ticker"): Bell's radio as one soft line along the bottom of the HUD,
    /// with a small amber on-air lamp breathing beside it. <see cref="TickerQueue"/> decides which line shows and when;
    /// this eases it in and out. The bottom band it lives in sits below every panel and below the margin that keeps
    /// world-anchored prompts off the screen edge, and the UI closes its gate while a card, a dig, a prompt, the tether
    /// reticle or the dial readout has the player's attention, so it never covers any of them.
    /// </summary>
    internal sealed class RadioTicker
    {
        private const float WriteEpsilon = 1e-3f;

        private readonly TickerSettings _settings;
        private readonly TickerQueue _queue;
        private readonly Reveal _reveal;
        private readonly Label _text;
        private readonly VisualElement _lamp;
        private int _writtenRevision = -1;
        private float _breath;
        private float _writtenLamp = -1f;

        public RadioTicker(UiLayout layout, TickerSettings settings, TickerQueue queue)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _reveal = new Reveal(layout.Ticker, settings.Reveal);
            _reveal.Snap(false);
            _text = layout.TickerText;
            _lamp = layout.TickerLamp;
        }

        /// <summary>True while a line is on screen (or easing).</summary>
        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True once the line on screen has fully eased in.</summary>
        public bool IsShown => _reveal.IsShown;

        /// <param name="deltaTime">Unscaled seconds; 0 while paused, so the line and the queue wait.</param>
        /// <param name="gateOpen">False while something else has the player's attention.</param>
        public void Tick(float deltaTime, bool gateOpen)
        {
            _queue.Step(deltaTime, gateOpen, _reveal.IsShown, _reveal.IsHidden);
            Write();
            _reveal.Set(_queue.WantsShown);
            _reveal.Tick(deltaTime);
            if (!_reveal.IsHidden)
            {
                Breathe(deltaTime);
            }
        }

        /// <summary>Re-reads the line on screen in the new language.</summary>
        public void Relocalize()
        {
            _queue.Relocalize();
            Write();
        }

        private void Write()
        {
            if (_queue.Revision != _writtenRevision)
            {
                _text.text = _queue.Text;
                _writtenRevision = _queue.Revision;
            }
        }

        private void Breathe(float deltaTime)
        {
            float period = _settings.LampBreathSeconds;
            _breath = (_breath + deltaTime) % period;
            float wave = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * _breath / period);
            float opacity = Mathf.Lerp(_settings.LampMinOpacity, 1f, wave);
            if (Mathf.Abs(opacity - _writtenLamp) > WriteEpsilon)
            {
                _lamp.style.opacity = opacity;
                _writtenLamp = opacity;
            }
        }
    }
}
