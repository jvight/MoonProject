using System;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The objective line on screen (docs/design/opening-v2.md §3): one plain line, high on the left, bright for its
    /// rest and a small dim line after; the low-battery hint takes the same line when the guidance slot gives it the
    /// turn. The words are looked up only when the key or the language changes.
    /// </summary>
    internal sealed class ObjectiveView
    {
        private const string DimClass = "next-line--dim";

        private readonly UiLayout _layout;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private string _key = string.Empty;
        private bool _showingBattery;

        public ObjectiveView(UiLayout layout, GuidanceSettings settings, ILocalization localization)
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

        /// <summary>True while the line on screen (or easing) is the objective.</summary>
        public bool ShowsObjective => !_reveal.IsHidden && !_showingBattery;

        /// <summary>True while the objective is shown in its small dim form (tests and captures).</summary>
        public bool IsDim => _layout.NextLine.ClassListContains(DimClass);

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="line">The objective's rule: its key, whether it wants to show and whether it is bright.</param>
        /// <param name="lineOpen">False while something else needs the screen (pause, a card, the title).</param>
        /// <param name="battery">The low-battery hint, which takes this same line when the slot gives it the turn.</param>
        /// <param name="batteryOpen">False while the hint may not show.</param>
        public void Tick(float deltaTime, ObjectiveLine line, bool lineOpen, LowBatteryHint battery, bool batteryOpen)
        {
            bool wantBattery = batteryOpen && battery.WantsShown;
            bool wantLine = !wantBattery && lineOpen && line.WantsShown;
            bool sameSource = _showingBattery == wantBattery;
            bool sameStep = sameSource && (!wantLine || _key.Length == 0 ||
                                           string.Equals(_key, line.Key, StringComparison.Ordinal));
            bool shown = (wantBattery || wantLine) && (sameStep || _reveal.IsHidden);
            if (shown && (!sameSource || (wantLine && !string.Equals(_key, line.Key, StringComparison.Ordinal))))
            {
                _showingBattery = wantBattery;
                _key = wantLine ? line.Key : string.Empty;
                Relocalize(battery);
            }
            else if (shown && wantBattery)
            {
                _layout.NextLineText.text = battery.Text;
            }

            _layout.NextLine.EnableInClassList(DimClass, shown && wantLine && !line.Bright);
            _reveal.Set(shown);
            _reveal.Tick(deltaTime);
        }

        /// <summary>A new language: the line on screen is written again.</summary>
        public void Relocalize(LowBatteryHint battery)
        {
            if (_showingBattery)
            {
                _layout.NextLineText.text = battery.Text;
            }
            else if (_key.Length > 0)
            {
                _layout.NextLineText.text = _localization.Get(_key);
            }
        }
    }
}
