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
        private const float MoveEpsilon = 0.1f;

        private readonly PromptSettings _settings;
        private readonly PromptDirector _director;
        private readonly IViewCamera _view;
        private readonly GlyphLabels _labels;
        private readonly ILocalization _localization;
        private readonly EventBus _events;
        private readonly string[] _wordKeys;
        private readonly Reveal _reveal;
        private readonly VisualElement _anchor;
        private readonly Label _word;
        private readonly GlyphView _glyph;
        private Vector2 _screen;
        private Vector2 _written = new Vector2(float.NaN, float.NaN);

        public ContextPrompt(UiLayout layout, PromptSettings settings, PromptDirector director, IViewCamera view,
            GlyphLabels labels, ILocalization localization, EventBus events)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _director = director ?? throw new ArgumentNullException(nameof(director));
            _view = view ?? throw new ArgumentNullException(nameof(view));
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

            _anchor = layout.PromptAnchor;
            _word = layout.PromptWord;
            _glyph = new GlyphView(layout.PromptGlyph, layout.PromptGlyphLabel);
            _reveal = new Reveal(layout.Prompt, settings.Reveal);
            _reveal.Snap(false);
            new ShadowPainter(layout.PromptShadow);
        }

        public bool IsVisible => !_reveal.IsHidden;

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

            bool onScreen = entry != null && Project(_director.WorldPoint, panelSize, deltaTime, _director.Started);
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

        private bool Project(Vector3 worldPoint, Vector2 panelSize, float deltaTime, bool snap)
        {
            Camera camera = _view.Camera;
            if (camera == null || panelSize.x <= 0f || panelSize.y <= 0f ||
                !ScreenAnchor.TryToPanel(camera.WorldToViewportPoint(worldPoint), panelSize, _settings.ScreenMargin,
                    out Vector2 target))
            {
                return false;
            }

            _screen = snap || _reveal.IsHidden
                ? target
                : ScreenAnchor.Follow(_screen, target, _settings.FollowHalfLife, deltaTime);
            if (float.IsNaN(_written.x) || (_screen - _written).sqrMagnitude > MoveEpsilon * MoveEpsilon)
            {
                _anchor.style.translate = new StyleTranslate(new Translate(_screen.x, _screen.y));
                _written = _screen;
            }

            return true;
        }
    }
}
