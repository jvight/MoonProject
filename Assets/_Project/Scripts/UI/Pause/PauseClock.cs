using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// How fast game time runs around the pause menu (pure, EditMode-tested): it eases to a full stop when the menu
    /// opens and eases back to full speed after resuming, on real (unscaled) time, so the world settles rather than
    /// freezing on a frame.
    /// </summary>
    internal sealed class PauseClock
    {
        private readonly PauseSettings _settings;
        private float _running = 1f;

        public PauseClock(PauseSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool Paused { get; private set; }

        /// <summary>Multiplier on normal game speed: 1 = running, 0 = stopped.</summary>
        public float Scale => UiEase.InOutSine(_running);

        /// <summary>True once time has fully stopped (paused) or fully recovered (running).</summary>
        public bool IsSettled => Paused ? _running <= 0f : _running >= 1f;

        public void Pause()
        {
            Paused = true;
        }

        public void Resume()
        {
            Paused = false;
        }

        /// <summary>Advances by real seconds; returns true when <see cref="Scale"/> changed.</summary>
        public bool Step(float unscaledDeltaTime)
        {
            float before = _running;
            if (Paused)
            {
                _running = _settings.FreezeSeconds <= 0f
                    ? 0f
                    : Mathf.Max(0f, _running - unscaledDeltaTime / _settings.FreezeSeconds);
            }
            else
            {
                _running = _settings.ResumeSeconds <= 0f
                    ? 1f
                    : Mathf.Min(1f, _running + unscaledDeltaTime / _settings.ResumeSeconds);
            }

            return before != _running;
        }
    }
}
