namespace MoonProject.UI
{
    /// <summary>
    /// The opening's first prompt (docs/design/cutscenes.md O14): once the full opening reaches its chase frame and 07
    /// has rested a few seconds, "Look up" fades in if the player has not moved the look since the opening began,
    /// otherwise "Drive" if they have not driven. Each word shows at most once per save (the prompts ledger), leaves
    /// as soon as its verb is done or after a while, and waits while anything else holds the screen. A word whose verb
    /// the player found alone is never shown. Pure logic: UISystem feeds it the beats and the input.
    /// </summary>
    internal sealed class FirstPrompt
    {
        private readonly StargazeSettings _settings;
        private readonly PromptLedger _ledger;
        private bool _opening;
        private bool _chase;
        private bool _looked;
        private bool _drove;
        private float _still;
        private float _shownFor;

        public FirstPrompt(StargazeSettings settings, PromptLedger ledger)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _ledger = ledger ?? throw new System.ArgumentNullException(nameof(ledger));
        }

        /// <summary>The word on the chip (None while it shows nothing).</summary>
        public HintWord Word { get; private set; }

        /// <summary>The full opening began (O1's crackle): look and drive count from here.</summary>
        public void OpeningBegan()
        {
            _opening = true;
            _chase = false;
            _looked = false;
            _drove = false;
            _still = 0f;
        }

        /// <summary>The opening reached its chase frame (O13): the rest before the prompt starts now.</summary>
        public void ChaseFrameReached()
        {
            _chase = _opening;
            _still = 0f;
        }

        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused.</param>
        /// <param name="open">False while something else holds the screen.</param>
        /// <param name="looking">The player moved the look this step.</param>
        /// <param name="driving">The player is giving drive input this step.</param>
        public void Tick(float deltaTime, bool open, bool looking, bool driving)
        {
            if (!_opening)
            {
                return;
            }

            _looked |= looking;
            _drove |= driving;
            if (Word != HintWord.None)
            {
                _shownFor += deltaTime;
                bool done = Word == HintWord.LookUp ? looking : driving;
                if (done || !open || _shownFor >= _settings.FirstPromptHoldSeconds)
                {
                    Word = HintWord.None;
                    _still = 0f;
                }

                return;
            }

            HintWord next = Next();
            if (next == HintWord.None)
            {
                _opening = false;
                return;
            }

            if (!_chase)
            {
                return;
            }

            _still = driving || looking ? 0f : _still + deltaTime;
            if (open && _still >= _settings.FirstPromptRestSeconds)
            {
                Word = next;
                _shownFor = 0f;
                if (next == HintWord.LookUp)
                {
                    _ledger.RecordLookTaught();
                }
                else
                {
                    _ledger.RecordDriveTaught();
                }
            }
        }

        private HintWord Next()
        {
            if (!_looked && !_ledger.LookTaught)
            {
                return HintWord.LookUp;
            }

            return !_drove && !_ledger.DriveTaught ? HintWord.Drive : HintWord.None;
        }
    }
}
