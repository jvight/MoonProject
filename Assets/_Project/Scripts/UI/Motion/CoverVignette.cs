using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The cover over a New Game that reloads (pure, EditMode-tested): the dark of 07's own tube closes in from the
    /// screen's edges like a camera settling onto the screen, App reloads under it, and on the new game it opens again
    /// on 07's dark face. <see cref="Cover01"/> runs 0 (open) to 1 (closed), eased.
    /// </summary>
    internal sealed class CoverVignette
    {
        private readonly BootFaceSettings _settings;
        private float _amount;
        private bool _closing;

        public CoverVignette(BootFaceSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>How closed the cover is, eased: 0 open, 1 the whole screen the tube's dark.</summary>
        public float Cover01 => Mathf.SmoothStep(0f, 1f, _amount);

        public bool IsVisible => _amount > 0f;

        /// <summary>Closes in from the edges over the tuned seconds.</summary>
        public void Close()
        {
            _closing = true;
        }

        /// <summary>Closed at once: the new game after the reload begins under the cover.</summary>
        public void SnapClosed()
        {
            _amount = 1f;
            _closing = true;
        }

        /// <summary>Opens again from the centre over the tuned seconds.</summary>
        public void Open()
        {
            _closing = false;
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        public void Tick(float deltaTime)
        {
            float seconds = _closing ? _settings.CoverCloseSeconds : _settings.CoverOpenSeconds;
            float step = Math.Max(deltaTime, 0f) / seconds;
            _amount = Mathf.Clamp01(_amount + (_closing ? step : -step));
        }
    }
}
