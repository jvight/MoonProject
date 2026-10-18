using System;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The Sun's remaining height as an arc on 07's readout (docs/design/opening-v2.md B9, the world's only clock): it
    /// comes once the base has power and goes when the Sun is down. A passive instrument, like the battery dial, so
    /// it is outside the guidance slot. Without a sky service it never shows.
    /// </summary>
    internal sealed class SunArcView
    {
        private const string LabelKey = "ui.readout.sun";
        private const string TimeKey = "ui.readout.sunset_in";
        private const string SoonKey = "ui.readout.sunset_soon";

        private readonly GuidanceSettings _settings;
        private readonly ISunlight _sun;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private readonly SunArcPainter _painter;
        private readonly UiLayout _layout;
        private int _shownMinutes = -1;

        /// <param name="sun">The sky, or null in a rig without one.</param>
        public SunArcView(UiLayout layout, GuidanceSettings settings, ILocalization localization, ISunlight sun)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _sun = sun;
            _reveal = new Reveal(layout.SunArc, settings.SunArc);
            _reveal.Snap(false);
            _painter = new SunArcPainter(layout.SunArcFace);
            Relocalize();
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>How much of the Sun is above the rim, 0..1 (tests and captures).</summary>
        public float Fraction => _painter.Fraction;

        public void Relocalize()
        {
            _layout.SunArcLabel.text = _localization.Get(LabelKey);
            _shownMinutes = -1;
        }

        /// <summary>The minutes the readout says, or -1 while it shows none (tests).</summary>
        public int ShownMinutes => _shownMinutes;

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="open">The base has power and the HUD is up.</param>
        public void Tick(float deltaTime, bool open)
        {
            bool shown = open && _sun != null && !SunArc.HasSet(_sun.SecondsToSunset) && SunArc.IsUp(_sun.SunDirection);
            if (shown)
            {
                _painter.Fraction = SunArc.Fraction(_sun.SunDirection, _settings.SunArcFullElevation);
            }

            WriteTime(shown && SunArc.CountsDown(_sun.SecondsToSunset) ? SunArc.MinutesLeft(_sun.SecondsToSunset) : -1);

            _reveal.Set(shown);
            _reveal.Tick(deltaTime);
        }

        /// <summary>Writes the time left only when its minute changes; -1 hides it (the Sun held, or set).</summary>
        private void WriteTime(int minutes)
        {
            if (minutes == _shownMinutes)
            {
                return;
            }

            _shownMinutes = minutes;
            Label time = _layout.SunArcTime;
            time.style.display = minutes < 0 ? DisplayStyle.None : DisplayStyle.Flex;
            if (minutes >= 0)
            {
                time.text = minutes <= 1 ? _localization.Get(SoonKey)
                    : string.Format(_localization.Get(TimeKey), minutes);
            }
        }
    }
}
