using System;

namespace MoonProject.UI
{
    /// <summary>
    /// The save tick (docs/design/polish-pass.md §1.2): when the game really saves, a tiny cream "07" with a check mark
    /// eases in at the corner, rests a second and eases away. Never a spinner, never a message; a second save while it
    /// is up simply keeps it up a little longer.
    /// </summary>
    internal sealed class SaveTick
    {
        private readonly SaveTickSettings _settings;
        private readonly Reveal _reveal;
        private float _rest;

        public SaveTick(UiLayout layout, SaveTickSettings settings)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _reveal = new Reveal(layout.SaveTick, settings.Reveal);
            _reveal.Snap(false);
            new CheckPainter(layout.SaveTickCheck);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The game saved: the sign comes up (or stays up) for its hold.</summary>
        public void Saved()
        {
            _rest = _settings.HoldSeconds;
            _reveal.Show();
        }

        /// <param name="deltaTime">Unscaled seconds since the last tick.</param>
        public void Tick(float deltaTime)
        {
            if (_reveal.IsShown)
            {
                _rest -= deltaTime;
                if (_rest <= 0f)
                {
                    _reveal.Hide();
                }
            }

            _reveal.Tick(deltaTime);
        }
    }
}
