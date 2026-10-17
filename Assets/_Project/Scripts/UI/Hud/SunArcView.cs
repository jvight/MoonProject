using System;
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

        private readonly GuidanceSettings _settings;
        private readonly ISunlight _sun;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private readonly SunArcPainter _painter;
        private readonly UiLayout _layout;

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
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="open">The base has power and the HUD is up.</param>
        public void Tick(float deltaTime, bool open)
        {
            bool shown = open && _sun != null && SunArc.IsUp(_sun.SunDirection);
            if (shown)
            {
                _painter.Fraction = SunArc.Fraction(_sun.SunDirection, _settings.SunArcFullElevation);
            }

            _reveal.Set(shown);
            _reveal.Tick(deltaTime);
        }
    }
}
