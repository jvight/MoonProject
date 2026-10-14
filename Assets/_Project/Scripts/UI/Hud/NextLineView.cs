using System;
using MoonProject.Core;
using MoonProject.Gameplay;

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
        private bool _showingBattery;

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

        /// <summary>True while the line on screen (or easing) is the low-battery hint.</summary>
        public bool ShowsBattery => !_reveal.IsHidden && _showingBattery;

        /// <summary>True while the line on screen (or easing) is the next line.</summary>
        public bool ShowsNext => !_reveal.IsHidden && !_showingBattery;

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="line">The next line's rule: its key and whether it wants to show.</param>
        /// <param name="nextOpen">False while something else needs the screen (pause, a card, the title).</param>
        /// <param name="battery">The low-battery hint, which takes this same line when the slot gives it the turn.</param>
        /// <param name="batteryOpen">False while the hint may not show.</param>
        public void Tick(float deltaTime, NextStepLine line, bool nextOpen, LowBatteryHint battery, bool batteryOpen)
        {
            bool wantBattery = batteryOpen && battery.WantsShown;
            bool wantNext = !wantBattery && nextOpen && line.WantsShown;
            bool sameSource = _showingBattery == wantBattery;
            bool sameStep = sameSource && (!wantNext || _key.Length == 0 || SameStep(_key, line.Key));
            bool shown = (wantBattery || wantNext) && (sameStep || _reveal.IsHidden);
            if (shown && (!sameSource || (wantNext && !string.Equals(_key, line.Key, StringComparison.Ordinal))))
            {
                _showingBattery = wantBattery;
                _key = wantNext ? line.Key : string.Empty;
                Relocalize(line, battery);
            }
            else if (shown && wantBattery)
            {
                _layout.NextLineText.text = battery.Text;
            }

            _reveal.Set(shown);
            _reveal.Tick(deltaTime);
        }

        /// <summary>A new language: the line on screen is written again.</summary>
        public void Relocalize(NextStepLine line, LowBatteryHint battery)
        {
            if (_showingBattery)
            {
                _layout.NextLineText.text = battery.Text;
            }
            else if (ProgressionKeys.TryParseGarageScrap(_key, out int remaining))
            {
                _layout.NextLineText.text = string.Format(_localization.Get(ProgressionKeys.NextGarageScrap), remaining);
            }
            else if (_key.Length > 0)
            {
                _layout.NextLineText.text = _localization.Get(_key);
            }
        }

        private static bool SameStep(string a, string b)
        {
            return string.Equals(a, b, StringComparison.Ordinal) ||
                   (ProgressionKeys.TryParseGarageScrap(a, out _) && ProgressionKeys.TryParseGarageScrap(b, out _));
        }
    }
}
