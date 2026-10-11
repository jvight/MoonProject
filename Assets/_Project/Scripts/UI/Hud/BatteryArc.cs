using System;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The thin segmented arc high in the middle of the HUD, one segment per cell (docs/features/M3-20 "How 07 shows
    /// it"). Once it has revealed itself (the charge first dropped below the reveal share,
    /// <see cref="BatteryArcRevealed"/>) it shows while the cells are not full; it hides while 07 works the beam (the
    /// flank lamps carry it then) and whenever something else needs the screen. Allocation-free per frame.
    /// </summary>
    internal sealed class BatteryArc
    {
        private readonly IRoverBattery _battery;
        private readonly IBeamStatus _beam;
        private readonly Reveal _reveal;
        private readonly BatteryArcPainter _painter;

        public BatteryArc(UiLayout layout, BatteryArcSettings settings, IRoverBattery battery, IBeamStatus beam)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _battery = battery ?? throw new ArgumentNullException(nameof(battery));
            _beam = beam ?? throw new ArgumentNullException(nameof(beam));
            _reveal = new Reveal(layout.BatteryArc, settings.Reveal);
            _reveal.Snap(false);
            _painter = new BatteryArcPainter(layout.BatteryArc);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The cells and charge the arc shows.</summary>
        public BatteryArcPainter Painter => _painter;

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="revealed">The arc has revealed itself in this game.</param>
        /// <param name="open">False while something else needs the screen (pause, a card, the title,
        /// stargazing).</param>
        public void Tick(float deltaTime, bool revealed, bool open)
        {
            int cells = _battery.Cells;
            float charge = _battery.Charge;
            _painter.Show(cells, charge);
            _reveal.Set(open && revealed && charge < cells && !_beam.Working);
            _reveal.Tick(deltaTime);
        }
    }
}
