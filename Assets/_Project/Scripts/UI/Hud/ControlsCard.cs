using System;

namespace MoonProject.UI
{
    /// <summary>
    /// The first controls card (Subnautica's quiet corner card): when the full opening hands 07 over at its chase
    /// frame it shows how to drive, look and open 07's face, and goes once the player has driven a few metres and
    /// looked around, or after a while. It shows once per new game: only the full opening arms it, so a loaded save
    /// (the short wake) never shows it, and once gone it does not come back. While it wants the screen it is the only
    /// guidance (UISystem holds the prompts and the objective line). Pure logic: UISystem feeds it the beats and the
    /// input.
    /// </summary>
    internal sealed class ControlsCard
    {
        private readonly ControlsCardSettings _settings;
        private bool _armed;
        private float _driven;
        private float _looked;
        private float _shownFor;

        public ControlsCard(ControlsCardSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>True from the full opening's chase frame until the card has done its job.</summary>
        public bool WantsShown { get; private set; }

        /// <summary>The full opening began (its crackle): the card is armed for its chase frame.</summary>
        public void OpeningBegan()
        {
            _armed = true;
            WantsShown = false;
            _driven = 0f;
            _looked = 0f;
            _shownFor = 0f;
        }

        /// <summary>The opening reached its chase frame: the card shows if the full opening armed it.</summary>
        public void ChaseFrameReached()
        {
            WantsShown = _armed;
            _armed = false;
        }

        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused.</param>
        /// <param name="open">The card could be seen now (not behind 07's face or a veil).</param>
        /// <param name="metres">Metres 07 drove this step.</param>
        /// <param name="looking">The player moved the look this step.</param>
        /// <param name="guidance">The player's guidance setting: off, the card never shows.</param>
        public void Tick(float deltaTime, bool open, float metres, bool looking, bool guidance)
        {
            if (!WantsShown)
            {
                return;
            }

            _driven += metres;
            _looked += looking ? deltaTime : 0f;
            _shownFor += open ? deltaTime : 0f;
            bool learned = _driven >= _settings.DriveMetres && _looked >= _settings.LookSeconds;
            if (!guidance || learned || _shownFor >= _settings.MaxSeconds)
            {
                WantsShown = false;
            }
        }
    }
}
