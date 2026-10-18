using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// Shows the <see cref="PromptDirector"/>'s prompt: a glyph and one word (the localized "hint.&lt;kind&gt;" string)
    /// floating above the relevant world point, projected through the player's camera and softly following it on
    /// screen. It hides while its point is behind the camera. Each appearance is published as a
    /// <see cref="UiCue"/>.
    /// </summary>
    internal sealed class ContextPrompt
    {
        private readonly PromptDirector _director;
        private readonly GlyphLabels _labels;
        private readonly ILocalization _localization;
        private readonly EventBus _events;
        private readonly string[] _wordKeys;
        private readonly Reveal _reveal;
        private readonly WorldAnchor _anchor;
        private readonly Label _word;
        private readonly VisualElement _chip;
        private readonly GlyphView _glyph;
        private readonly VisualElement[] _cards;
        private readonly float _cardGap;
        private readonly float _screenMargin;
        private readonly float _followHalfLife;
        private float _lift;

        /// <param name="cards">The cards the prompt keeps clear of when its point sits under one.</param>
        public ContextPrompt(UiLayout layout, PromptSettings settings, PromptDirector director, IViewCamera view,
            GlyphLabels labels, ILocalization localization, EventBus events, VisualElement[] cards)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _director = director ?? throw new ArgumentNullException(nameof(director));
            _labels = labels ?? throw new ArgumentNullException(nameof(labels));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _cards = cards ?? throw new ArgumentNullException(nameof(cards));
            _cardGap = settings.CardGap;
            _screenMargin = settings.ScreenMargin;
            _followHalfLife = settings.FollowHalfLife;
            int kinds = 0;
            foreach (InteractionKind kind in Enum.GetValues(typeof(InteractionKind)))
            {
                kinds = Math.Max(kinds, (int)kind + 1);
            }

            _wordKeys = new string[kinds];
            foreach (PromptEntry entry in settings.Entries)
            {
                _wordKeys[(int)entry.Kind] = UiKeys.Hint(entry.Kind);
            }

            _anchor = new WorldAnchor(layout.PromptAnchor, view, settings.ScreenMargin, settings.FollowHalfLife,
                true);
            _word = layout.PromptWord;
            _chip = layout.Prompt;
            _glyph = new GlyphView(layout.PromptGlyph, layout.PromptGlyphLabel);
            _reveal = new Reveal(layout.Prompt, settings.Reveal);
            _reveal.Snap(false);
            new ShadowPainter(layout.PromptShadow);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>
        /// Pixels something anchored at the same point must rise to rest on top of the prompt for
        /// <paramref name="kind"/>, <paramref name="gap"/> included, eased with the prompt's fade (0 when another
        /// prompt or none is shown).
        /// </summary>
        public float StackHeight(InteractionKind kind, float gap)
        {
            if (_director.Displayed != kind || _reveal.IsHidden)
            {
                return 0f;
            }

            float height = _chip.resolvedStyle.height;
            return float.IsNaN(height) ? 0f : (height + gap) * _reveal.Visibility;
        }

        /// <param name="deltaTime">Unscaled seconds since the last tick.</param>
        /// <param name="gateOpen">False while something else has the player's attention.</param>
        /// <param name="slotGranted">The guidance slot lets the chip show (the dwell runs without it).</param>
        /// <param name="primary">The gameplay's most relevant action right now.</param>
        /// <param name="device">The active device, for the glyph.</param>
        /// <param name="panelSize">Size of the UI panel (the screen in panel units).</param>
        public void Tick(float deltaTime, bool gateOpen, bool slotGranted, InteractionHint primary,
            InputDeviceKind device, Vector2 panelSize)
        {
            _director.Step(deltaTime, gateOpen, primary, _reveal.IsHidden, slotGranted);
            PromptEntry entry = _director.Entry;
            if (entry != null)
            {
                if (_director.Started)
                {
                    WriteWord(entry);
                }

                _glyph.Set(_labels.For(entry.Action, device), device);
            }

            bool snap = _director.Started || _reveal.IsHidden;
            bool onScreen = entry != null && _anchor.Track(_director.WorldPoint, panelSize, deltaTime, snap);
            if (onScreen)
            {
                ClearCards(panelSize, deltaTime, snap);
            }

            bool wasHidden = _reveal.IsHidden;
            _reveal.Set(_director.WantsShown && onScreen);
            if (wasHidden && _reveal.Target)
            {
                _events.Publish(new UiCue(UiCueKind.PromptShown));
            }

            _reveal.Tick(deltaTime);
        }

        /// <summary>Eases the prompt up (or down) off any card it would sit over, and back when the card goes.</summary>
        private void ClearCards(Vector2 panelSize, float deltaTime, bool snap)
        {
            float width = _chip.resolvedStyle.width;
            float height = _chip.resolvedStyle.height;
            if (float.IsNaN(width) || width <= 0f || float.IsNaN(height))
            {
                return;
            }

            // Placed from the anchor and the chip's size, not its world bound: the chip hangs from the anchor by a
            // percentage translate, which the world bound only knows a layout later.
            Vector2 foot = _anchor.Screen;
            var chip = new Rect(foot.x - width * 0.5f, foot.y - height, width, height);
            var screen = new Rect(Vector2.zero, panelSize);
            float target = 0f;
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i].resolvedStyle.display == DisplayStyle.None)
                {
                    continue;
                }

                float lift = PromptClearance.Lift(chip, _cards[i].worldBound, _cardGap, screen, _screenMargin);
                chip.y -= lift;
                target += lift;
            }

            _lift = snap ? target
                : ScreenAnchor.Follow(new Vector2(0f, _lift), new Vector2(0f, target), _followHalfLife, deltaTime).y;
            _anchor.Lift = _lift;
        }

        /// <summary>Re-reads the word in the new language (a prompt on screen changes in place).</summary>
        public void Relocalize()
        {
            PromptEntry entry = _director.Entry;
            if (entry != null)
            {
                WriteWord(entry);
            }
        }

        /// <summary>The verb's word, then what it acts on when the hint names one ("Pick up Tower Coil").</summary>
        private void WriteWord(PromptEntry entry)
        {
            string verb = _localization.Get(_wordKeys[(int)entry.Kind]);
            string noun = _director.NounKey;
            _word.text = string.IsNullOrEmpty(noun)
                ? verb
                : string.Concat(verb, " ", _localization.Get(noun));
        }
    }
}
