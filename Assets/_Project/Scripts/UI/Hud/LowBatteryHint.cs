using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// The low-battery hint (docs/design/0.5.5-owner-feedback.md §4.1, priority 4 of the guidance slot): the one line
    /// Gameplay says when the charge first falls to 30% on a trip ("Battery low. Charge at the dock at home."), kept
    /// as guidance and not put on the ticker, so it never shows over a higher element. It waits its turn, shows for a
    /// short while, and goes at once when a charger starts filling the cells (never said at full or while charging).
    /// Gameplay's low warning fires once per discharge, so it is once per trip.
    /// </summary>
    internal sealed class LowBatteryHint
    {
        public const string LanderKey = "battery.low.lander";
        public const string PadKey = "battery.low.pad";

        private readonly GuidanceSettings _settings;
        private readonly IRoverBattery _battery;
        private readonly ILocalization _localization;
        private TickerLine _line;
        private bool _pending;
        private float _shownFor;

        public LowBatteryHint(GuidanceSettings settings, IRoverBattery battery, ILocalization localization)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _battery = battery ?? throw new ArgumentNullException(nameof(battery));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        /// <summary>True while the hint wants the screen (before the guidance slot decides).</summary>
        public bool WantsShown { get; private set; }

        /// <summary>The hint's words, built when it was offered and when the language changes.</summary>
        public string Text { get; private set; } = string.Empty;

        /// <summary>True for the ticker lines Gameplay publishes for the low warning.</summary>
        public static bool IsLowLine(TickerLine line)
        {
            return line.Key == LanderKey || line.Key == PadKey;
        }

        /// <summary>Gameplay's low warning arrived: this line is now a hint waiting to show.</summary>
        public void Offer(TickerLine line)
        {
            _line = line;
            _pending = true;
            _shownFor = 0f;
            Write();
        }

        /// <summary>A new language: the hint's words are written again.</summary>
        public void Relocalize()
        {
            if (_pending)
            {
                Write();
            }
        }

        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused.</param>
        /// <param name="showing">The hint is on screen now: the hold time counts only then.</param>
        public void Tick(float deltaTime, bool showing)
        {
            if (!_pending)
            {
                WantsShown = false;
                return;
            }

            if (_battery.IsCharging || _battery.Charge01 >= 1f)
            {
                _pending = false;
                WantsShown = false;
                return;
            }

            if (showing)
            {
                _shownFor += deltaTime;
                if (_shownFor >= _settings.LowBatteryHoldSeconds)
                {
                    _pending = false;
                    WantsShown = false;
                    return;
                }
            }

            WantsShown = true;
        }

        private void Write()
        {
            string format = _localization.Get(_line.Key);
            Text = _line.Argument.Length == 0
                ? format
                : string.Format(format, _line.ArgumentIsKey ? _localization.Get(_line.Argument) : _line.Argument);
        }
    }
}
