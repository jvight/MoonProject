using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// As a piece of kit settles onto 07, its name (<see cref="KitNames"/>) drifts in high on screen, clear of the
    /// install moment's view of 07, on a soft dusk of shadow that keeps it legible over the bright Earth, rests a
    /// moment and fades away (docs/features/M3-11), like a friend's name the first time it wakes. Names that arrive
    /// while one is up wait their turn.
    /// </summary>
    internal sealed class KitTitle
    {
        private readonly KitTitleSettings _settings;
        private readonly ILocalization _localization;
        private readonly Label _label;
        private readonly Reveal _reveal;
        private readonly Queue<string> _waiting = new Queue<string>();
        private Phase _phase;
        private float _timer;

        public KitTitle(UiLayout layout, KitTitleSettings settings, ILocalization localization)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _label = layout.KitTitleName;
            _reveal = new Reveal(layout.KitTitle, settings.Reveal);
            _reveal.Snap(false);
            new ShadowPainter(layout.KitTitleShadow);
        }

        private enum Phase
        {
            Idle,
            Waiting,
            Showing,
            Leaving,
        }

        /// <summary>True while a name is on screen (or easing).</summary>
        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The key of the name being shown, or null.</summary>
        public string Current { get; private set; }

        /// <summary>Shows the name under <paramref name="key"/> once the names before it have gone.</summary>
        public void Enqueue(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("A kit title needs a key.", nameof(key));
            }

            _waiting.Enqueue(key);
        }

        /// <summary>Re-reads the name in the new language (a name on screen changes in place).</summary>
        public void Relocalize()
        {
            if (Current != null)
            {
                _label.text = _localization.Get(Current);
            }
        }

        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused so the name waits too.</param>
        public void Tick(float deltaTime)
        {
            switch (_phase)
            {
                case Phase.Idle:
                    if (_waiting.Count > 0)
                    {
                        Current = _waiting.Dequeue();
                        _label.text = _localization.Get(Current);
                        _timer = 0f;
                        _phase = Phase.Waiting;
                    }

                    break;
                case Phase.Waiting:
                    _timer += deltaTime;
                    if (_timer >= _settings.Delay)
                    {
                        _reveal.Show();
                        _timer = 0f;
                        _phase = Phase.Showing;
                    }

                    break;
                case Phase.Showing:
                    if (_reveal.IsShown)
                    {
                        _timer += deltaTime;
                        if (_timer >= _settings.HoldSeconds)
                        {
                            _reveal.Hide();
                            _phase = Phase.Leaving;
                        }
                    }

                    break;
                case Phase.Leaving:
                    if (_reveal.IsHidden)
                    {
                        Current = null;
                        _phase = Phase.Idle;
                    }

                    break;
            }

            _reveal.Tick(deltaTime);
        }
    }
}
