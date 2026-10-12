using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// When a kit is fitted that lets 07 do something new, a glass card stays up for a few seconds with its key, its
    /// word and one line of what it does (docs/design/first-hour-qa.md R1: the card teaches the key, the world gives
    /// the first use). Cards wait their turn behind one another and behind whatever else holds the upper third (the
    /// kit's title first).
    /// </summary>
    internal sealed class UnlockCard
    {
        private readonly UnlockCardSettings _settings;
        private readonly ILocalization _localization;
        private readonly GlyphLabels _glyphs;
        private readonly Reveal _reveal;
        private readonly GlyphView _glyph;
        private readonly VisualElement _glyphElement;
        private readonly Label _verb;
        private readonly Label _line;
        private readonly Queue<AbilityRow> _waiting = new Queue<AbilityRow>();
        private AbilityRow _shown;
        private bool _showing;
        private bool _leaving;
        private float _timer;

        public UnlockCard(UiLayout layout, UnlockCardSettings settings, ILocalization localization, GlyphLabels glyphs)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _glyphs = glyphs ?? throw new ArgumentNullException(nameof(glyphs));
            VisualElement root = layout.UnlockCard;
            _reveal = new Reveal(root, settings.Reveal);
            _reveal.Snap(false);
            _glyphElement = Require<VisualElement>(root, "unlock-glyph");
            _glyph = new GlyphView(_glyphElement, Require<Label>(root, "unlock-glyph-label"));
            _verb = Require<Label>(root, "unlock-card-verb");
            _line = Require<Label>(root, "unlock-card-line");
        }

        /// <summary>True while a card is on screen (or easing).</summary>
        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True while a card waits its turn.</summary>
        public bool IsWaiting => _waiting.Count > 0;

        /// <summary>The ability of the card on screen (tests and captures).</summary>
        public RoverAbility Shown => _shown.Ability;

        /// <summary>Queues the card of <paramref name="ability"/>; false when the "07 can" page does not list it.</summary>
        public bool Gained(RoverAbility ability)
        {
            if (!AbilityTable.TryFind(ability, out AbilityRow row))
            {
                return false;
            }

            _waiting.Enqueue(row);
            return true;
        }

        /// <summary>Re-reads the words in the new language.</summary>
        public void Relocalize()
        {
            if (_showing || _leaving)
            {
                Write(InputDeviceKind.KeyboardMouse);
            }
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused.</param>
        /// <param name="open">False while another block holds the upper third.</param>
        /// <param name="device">The active device, for the key.</param>
        public void Tick(float deltaTime, bool open, InputDeviceKind device)
        {
            if (!_showing && !_leaving && open && _waiting.Count > 0)
            {
                _shown = _waiting.Dequeue();
                Write(device);
                _showing = true;
                _timer = 0f;
                _reveal.Show();
            }

            if (_showing)
            {
                if (_shown.HasAction)
                {
                    _glyph.Set(_glyphs.For(_shown.Action, device), device);
                }

                _timer += deltaTime;
                if (_timer >= _settings.HoldSeconds)
                {
                    _showing = false;
                    _leaving = true;
                    _reveal.Hide();
                }
            }

            _reveal.Tick(deltaTime);
            if (_leaving && _reveal.IsHidden)
            {
                _leaving = false;
            }
        }

        private void Write(InputDeviceKind device)
        {
            _verb.text = _localization.Get(_shown.VerbKey);
            _line.text = _localization.Get(_shown.LineKey);
            _glyphElement.style.display = _shown.HasAction
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            if (_shown.HasAction)
            {
                _glyph.Set(_glyphs.For(_shown.Action, device), device);
            }
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"GameUI.uxml has no {typeof(T).Name} named '{name}' under '{root.name}'.");
        }
    }
}
