using System;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// How the opening teaches "fit at the bay" within the one-guidance rule (docs/design/0.5.5-owner-feedback.md
    /// §3.5): right after the first craft, while 07 rests where the waiting part can be fitted, one "Tab · Fit" chip
    /// shows, until the 07 tab is opened or a while passes. It retires for good after the first fit (the prompts ledger
    /// remembers). It is a prompt, so it holds the one guidance slot.
    /// </summary>
    internal sealed class FitHint
    {
        private readonly StargazeSettings _settings;
        private readonly PromptLedger _ledger;
        private readonly IRoverEquipment _equipment;
        private float _timer;

        public FitHint(StargazeSettings settings, PromptLedger ledger, IRoverEquipment equipment)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        public bool WantsShown { get; private set; }

        /// <summary>True when the chip would start now: a part waits, 07 rests where it fits, none was fitted yet.</summary>
        public bool Wants()
        {
            return WantsShown || (!_ledger.FitTaught && _equipment.RowCount > 0 &&
                                  _equipment.CanFit(_equipment.RowPart(0)));
        }

        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused.</param>
        /// <param name="open">False while another guidance element or a panel holds the screen.</param>
        /// <param name="cargoOpen">The Tab screen is open: the chip has done its job.</param>
        public void Tick(float deltaTime, bool open, bool cargoOpen)
        {
            if (WantsShown)
            {
                _timer += deltaTime;
                if (!open || cargoOpen || _ledger.FitTaught || !Wants() || _timer >= _settings.HintHoldSeconds)
                {
                    WantsShown = false;
                }

                return;
            }

            if (open && !cargoOpen && Wants())
            {
                WantsShown = true;
                _timer = 0f;
            }
        }
    }
}
