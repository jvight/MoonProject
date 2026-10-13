using System;

namespace MoonProject.UI
{
    /// <summary>
    /// How the opening teaches Tab, at the moment of need (docs/design/0.5.5-owner-feedback.md §3): the first time 07
    /// is at the tower and its hold has never been opened, one "Tab · Hold" chip shows, once per save, until the hold
    /// is opened or a while passes. It is a prompt, so it holds the one guidance slot and the tower's card waits for it.
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

        /// <summary>True when the chip would start now: at the tower, never shown, hold never opened.</summary>
        public bool Wants(bool atTower)
        {
            return WantsShown || (atTower && !_ledger.HoldChipShown && !_ledger.HoldBooted);
        }

        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused.</param>
        /// <param name="open">False while another guidance element or a panel holds the screen.</param>
        /// <param name="atTower">07 is at the tower, offered its build.</param>
        public void Tick(float deltaTime, bool open, bool atTower)
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

            if (open && Wants(atTower))
            {
                WantsShown = true;
                _timer = 0f;
                _ledger.RecordHoldChipShown();
            }
        }
    }
}
