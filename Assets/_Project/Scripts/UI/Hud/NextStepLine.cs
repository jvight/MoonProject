using System;

namespace MoonProject.UI
{
    /// <summary>
    /// The soft "next" line's rule (docs/features/M3-16 "Gentle guidance"): Gameplay names the current step's
    /// invitation; it shows only once 07 has been still a moment, never while driving, and not at all with Guidance
    /// switched off or no step to suggest.
    /// </summary>
    internal sealed class NextStepLine
    {
        private readonly GuidanceSettings _settings;
        private float _still;

        public NextStepLine(GuidanceSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The current step's localization key; empty when there is nothing to suggest.</summary>
        public string Key { get; private set; } = string.Empty;

        public bool WantsShown { get; private set; }

        public void SetKey(string key)
        {
            Key = key ?? string.Empty;
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused.</param>
        /// <param name="speed">07's speed, m/s.</param>
        /// <param name="guidance">The player's Guidance setting.</param>
        public void Tick(float deltaTime, float speed, bool guidance)
        {
            _still = speed < _settings.StillSpeed ? _still + deltaTime : 0f;
            WantsShown = guidance && Key.Length > 0 && _still >= _settings.StillSeconds;
        }
    }
}
