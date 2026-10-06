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

        public ContextPrompt(UiLayout layout, PromptSettings settings, PromptDirector director, IViewCamera view,
            GlyphLabels labels, ILocalization localization, EventBus events)
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
        /// <param name="primary">The gameplay's most relevant action right now.</param>
        /// <param name="device">The active device, for the glyph.</param>
        /// <param name="panelSize">Size of the UI panel (the screen in panel units).</param>
        public void Tick(float deltaTime, bool gateOpen, InteractionHint primary, InputDeviceKind device,
            Vector2 panelSize)
        {
            _director.Step(deltaTime, gateOpen, primary, _reveal.IsHidden);
            PromptEntry entry = _director.Entry;
            if (entry != null)
            {
                if (_director.Started)
                {
                    _word.text = _localization.Get(_wordKeys[(int)entry.Kind]);
                }

                _glyph.Set(_labels.For(entry.Action, device), device);
            }

            bool onScreen = entry != null && _anchor.Track(_director.WorldPoint, panelSize, deltaTime,
                _director.Started || _reveal.IsHidden);
            bool wasHidden = _reveal.IsHidden;
            _reveal.Set(_director.WantsShown && onScreen);
            if (wasHidden && _reveal.Target)
            {
                _events.Publish(new UiCue(UiCueKind.PromptShown));
            }

            _reveal.Tick(deltaTime);
        }

        /// <summary>Re-reads the word in the new language (a prompt on screen changes in place).</summary>
        public void Relocalize()
        {
            PromptEntry entry = _director.Entry;
            if (entry != null)
            {
                _word.text = _localization.Get(_wordKeys[(int)entry.Kind]);
            }
        }
    }
}
