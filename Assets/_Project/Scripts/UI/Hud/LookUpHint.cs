using System;

namespace MoonProject.UI
{
    /// <summary>
    /// The first time 07 rests under the open sky (the camera's wide shot opens), a gentle hint asks the player to look
    /// up with the camera. Once per save (the prompts ledger remembers): it leaves when they do look up (stargazing
    /// begins), when the wide shot ends, or after a while. It waits while a card or panel holds the screen: one message
    /// at a time, and a hint nobody saw is not spent.
    /// </summary>
    internal sealed class LookUpHint
    {
        private readonly StargazeSettings _settings;
        private readonly PromptLedger _ledger;
        private bool _wide;
        private bool _stargazing;
        private float _timer;

        public LookUpHint(StargazeSettings settings, PromptLedger ledger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        }

        public bool WantsShown { get; private set; }

        public void SetWide(bool wide)
        {
            _wide = wide;
            _timer = 0f;
        }

        public void SetStargazing(bool stargazing)
        {
            _stargazing = stargazing;
        }

        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused.</param>
        /// <param name="open">False while a place card, a story card or a panel holds the screen.</param>
        public void Tick(float deltaTime, bool open)
        {
            if (WantsShown)
            {
                _timer += deltaTime;
                if (!open || !_wide || _stargazing || _timer >= _settings.HintHoldSeconds)
                {
                    WantsShown = false;
                }

                return;
            }

            if (!_wide || _stargazing || _ledger.LookUpHinted)
            {
                return;
            }

            _timer += deltaTime;
            if (open && _timer >= _settings.HintDelay)
            {
                WantsShown = true;
                _timer = 0f;
                _ledger.RecordLookUpHinted();
            }
        }
    }
}
