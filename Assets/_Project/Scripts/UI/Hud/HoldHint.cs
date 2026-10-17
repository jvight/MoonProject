using System;

namespace MoonProject.UI
{
    /// <summary>
    /// How the opening teaches Tab (docs/design/0.5.5-owner-feedback.md §3): the first time 07 carries something and
    /// its hold has never been opened, one "Tab · Hold" chip shows, once per save, until the hold is opened or a while
    /// passes. It is a prompt, so it holds the one guidance slot.
    /// </summary>
    internal sealed class HoldHint
    {
        private readonly StargazeSettings _settings;
        private readonly PromptLedger _ledger;
        private float _timer;

        public HoldHint(StargazeSettings settings, PromptLedger ledger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        }

        public bool WantsShown { get; private set; }

        /// <summary>True when the chip would start now: 07 carries something, never shown, hold never opened.</summary>
        public bool Wants(bool carrying)
        {
            return WantsShown || (carrying && !_ledger.HoldChipShown && !_ledger.HoldBooted);
        }

        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused.</param>
        /// <param name="open">False while another guidance element or a panel holds the screen.</param>
        /// <param name="carrying">07 carries something in its hold.</param>
        public void Tick(float deltaTime, bool open, bool carrying)
        {
            if (WantsShown)
            {
                _timer += deltaTime;
                if (!open || _ledger.HoldBooted || _timer >= _settings.HintHoldSeconds)
                {
                    WantsShown = false;
                }

                return;
            }

            if (open && Wants(carrying))
            {
                WantsShown = true;
                _timer = 0f;
                _ledger.RecordHoldChipShown();
            }
        }
    }
}
