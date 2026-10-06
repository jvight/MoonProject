using System;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// No permanent HUD: when the scrap balance changes, a small chip (glowing nut + number) eases in, counts to the
    /// new total, lingers a few seconds and eases out. It stays while pinned (at the tower pad).
    /// </summary>
    internal sealed class ScrapChip
    {
        private readonly ScrapChipSettings _settings;
        private readonly Reveal _reveal;
        private readonly Label _count;
        private readonly IntText _numbers;
        private readonly CountUp _counter;
        private float _linger;
        private bool _pinned;

        public ScrapChip(VisualElement chip, VisualElement shadow, VisualElement icon, Label count,
            ScrapChipSettings settings, IntText numbers)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _count = count ?? throw new ArgumentNullException(nameof(count));
            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
            _reveal = new Reveal(chip, settings.Reveal);
            _reveal.Snap(false);
            _counter = new CountUp(settings);
            new ShadowPainter(shadow);
            new ScrapIconPainter(icon);
            _count.text = _numbers.Get(0);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The number on the chip right now.</summary>
        public int Shown => _counter.Shown;

        /// <summary>Sets the balance without showing the chip (a loaded save, not a change the player made).</summary>
        public void Snap(int total)
        {
            _counter.Snap(total);
            _count.text = _numbers.Get(total);
        }

        /// <summary>The balance changed: drift in and count to <paramref name="total"/>.</summary>
        public void Change(int total)
        {
            _counter.SetTarget(total);
            _linger = 0f;
            _reveal.Show();
        }

        public void SetPinned(bool pinned)
        {
            if (pinned && !_pinned)
            {
                _reveal.Show();
            }

            _pinned = pinned;
            if (pinned)
            {
                _linger = 0f;
            }
        }

        public void Tick(float deltaTime)
        {
            if (_counter.Step(deltaTime))
            {
                _count.text = _numbers.Get(_counter.Shown);
            }

            if (!_pinned && _reveal.Target && _counter.IsSettled)
            {
                _linger += deltaTime;
                if (_linger >= _settings.LingerSeconds)
                {
                    _reveal.Hide();
                }
            }

            _reveal.Tick(deltaTime);
        }
    }
}
