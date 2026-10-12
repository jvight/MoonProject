using System;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The soft "next" line's rule (docs/features/M3-16 "Gentle guidance", M3-24 B): Gameplay names the current step's
    /// invitation. A teaching step's line shows as soon as 07 has been still a moment, since a new player needs it
    /// at once; a recovery target's need waits a long while on the same step without progress first (P8). Never
    /// while driving, and not at all with Guidance switched off or no step to suggest.
    /// </summary>
    internal sealed class NextStepLine
    {
        private readonly GuidanceSettings _settings;
        private float _still;
        private float _onStep;

        public NextStepLine(GuidanceSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The current step's localization key; empty when there is nothing to suggest.</summary>
        public string Key { get; private set; } = string.Empty;

        public bool WantsShown { get; private set; }

        public void SetKey(string key)
        {
            key = key ?? string.Empty;
            if (!string.Equals(key, Key, StringComparison.Ordinal))
            {
                _onStep = 0f;
            }

            Key = key;
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused.</param>
        /// <param name="speed">07's speed, m/s.</param>
        /// <param name="guidance">The player's Guidance setting.</param>
        public void Tick(float deltaTime, float speed, bool guidance)
        {
            _onStep += deltaTime;
            _still = speed < _settings.StillSpeed ? _still + deltaTime : 0f;
            float patience = ProgressionKeys.SpeaksOfTargets(Key) ? _settings.StepPatienceSeconds : 0f;
            WantsShown = guidance && Key.Length > 0 && _onStep >= patience && _still >= _settings.StillSeconds;
        }
    }
}
