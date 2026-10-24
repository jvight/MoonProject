using System;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The Sun's remaining height as a small arc glyph at the top right (docs/design/opening-v3-feel-audit.md row 3):
    /// it comes once the base has power and goes when the Sun is down. No words and no minutes: dusk comes with
    /// progress, not with time, so a countdown would be false and would hurry a calm screen. A passive instrument,
    /// like the battery dial, so it is outside the guidance slot. Without a sky service it never shows.
    /// </summary>
    internal sealed class SunArcView
    {
        private readonly GuidanceSettings _settings;
        private readonly ISunlight _sun;
        private readonly Reveal _reveal;
        private readonly SunArcPainter _painter;

        /// <param name="sun">The sky, or null in a rig without one.</param>
        public SunArcView(UiLayout layout, GuidanceSettings settings, ISunlight sun)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _sun = sun;
            _reveal = new Reveal(layout.SunArc, settings.SunArc);
            _reveal.Snap(false);
            _painter = new SunArcPainter(layout.SunArcFace);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>How much of the Sun is above the rim, 0..1 (tests and captures).</summary>
        public float Fraction => _painter.Fraction;

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="open">The base has power and the HUD is up.</param>
        public void Tick(float deltaTime, bool open)
        {
            bool shown = open && _sun != null && !SunArc.HasSet(_sun.SecondsToSunset) && SunArc.IsUp(_sun.SunDirection);
            if (shown)
            {
                _painter.Fraction = SunArc.Fraction(_sun.SunDirection, _settings.SunArcFullElevation);
            }

            _reveal.Set(shown);
            _reveal.Tick(deltaTime);
        }
    }
}
