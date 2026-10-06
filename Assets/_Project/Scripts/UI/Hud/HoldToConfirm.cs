using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// A ring that fills while a button is held and confirms once full (pure logic, EditMode-tested). No accidental
    /// purchases:
    /// <list type="bullet">
    /// <item>A hold only counts once the button has been seen released while the offer was available, so a button
    /// already held when the panel appeared (driving onto the pad with it down) never buys.</item>
    /// <item>Letting go early drains the ring smoothly instead of snapping it to empty; nothing is lost.</item>
    /// <item>It confirms exactly once per hold; the next purchase needs a fresh press.</item>
    /// </list>
    /// </summary>
    internal sealed class HoldToConfirm
    {
        private readonly TowerPanelSettings _settings;
        private bool _armed;
        private bool _confirmed;

        public HoldToConfirm(TowerPanelSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>0..1 fill of the ring.</summary>
        public float Progress { get; private set; }

        /// <summary>True when a press now would start filling the ring.</summary>
        public bool Armed => _armed;

        /// <param name="available">The offer can be confirmed right now (visible and affordable).</param>
        /// <param name="held">The confirm button is down.</param>
        /// <param name="deltaTime">Unscaled seconds since the last step.</param>
        /// <returns>True exactly once, on the step the ring fills.</returns>
        public bool Step(bool available, bool held, float deltaTime)
        {
            if (!held)
            {
                _confirmed = false;
                _armed = available;
            }
            else if (!available)
            {
                _armed = false;
            }

            if (held && _armed && !_confirmed)
            {
                Progress = _settings.HoldSeconds <= 0f
                    ? 1f
                    : Mathf.Min(1f, Progress + deltaTime / _settings.HoldSeconds);
                if (Progress >= 1f)
                {
                    _confirmed = true;
                    _armed = false;
                    return true;
                }

                return false;
            }

            if (!(held && _confirmed))
            {
                Progress = _settings.DrainSeconds <= 0f
                    ? 0f
                    : Mathf.Max(0f, Progress - deltaTime / _settings.DrainSeconds);
            }

            return false;
        }

        /// <summary>Empties the ring and disarms it (the offer went away).</summary>
        public void Reset()
        {
            Progress = 0f;
            _armed = false;
            _confirmed = false;
        }
    }
}
