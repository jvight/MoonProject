using System;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The "next" line on screen: one soft line high and centred, on the floating names' soft shadow, eased in and out
    /// as <see cref="NextStepLine"/> wants. The words are looked up only when the key or the language changes.
    /// </summary>
    internal sealed class NextLineView
    {
        private readonly UiLayout _layout;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private string _key = string.Empty;

        public NextLineView(UiLayout layout, GuidanceSettings settings, ILocalization localization)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _reveal = new Reveal(layout.NextLine, settings.Line);
            _reveal.Snap(false);
            new ShadowPainter(layout.NextLineShadow);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="line">The line's rule: its key and whether it wants to show.</param>
        /// <param name="open">False while something else needs the screen (pause, a card, the title).</param>
        public void Tick(float deltaTime, NextStepLine line, bool open)
        {
            bool shown = open && line.WantsShown;
            if (shown && !ReferenceEquals(_key, line.Key))
            {
                _key = line.Key;
                Relocalize();
            }

            _reveal.Set(shown);
            _reveal.Tick(deltaTime);
        }

        public void Relocalize()
        {
            if (_key.Length > 0)
            {
                _layout.NextLineText.text = _localization.Get(_key);
            }
        }
    }
}
