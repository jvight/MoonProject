using System;

namespace MoonProject.UI
{
    /// <summary>What the objective line remembers across the save: which steps' lines have been shown.</summary>
    internal interface IObjectiveMemory
    {
        bool Has(string key);

        void Add(string key);
    }

    /// <summary>
    /// The objective line's rule (docs/design/opening-v2.md §3; pure, EditMode-tested). Gameplay names the current step
    /// as a localization key (empty: no line). The line comes 1.5 s after the step began and 07 has been still for 1 s,
    /// rests bright for 8 s, then dims to a small line; it clears the moment the step changes (its step was done) and
    /// re-brightens only when 07 has been still for 4 s and 90 s passed without progress. Each step's line is bright
    /// once per save: a step already shown in this save (a loaded game) shows its line dimmed.
    /// </summary>
    internal sealed class ObjectiveLine
    {
        private readonly GuidanceSettings _settings;
        private readonly IObjectiveMemory _memory;
        private Phase _phase;
        private float _sinceKey;
        private float _sinceProgress;
        private float _still;
        private float _rested;

        public ObjectiveLine(GuidanceSettings settings, IObjectiveMemory memory)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        }

        private enum Phase
        {
            Waiting,
            Bright,
            Dim,
        }

        /// <summary>The current step's localization key; empty when there is nothing to say.</summary>
        public string Key { get; private set; } = string.Empty;

        /// <summary>The line wants the screen (before the guidance slot decides).</summary>
        public bool WantsShown { get; private set; }

        /// <summary>The line is in its bright rest rather than its small dim form.</summary>
        public bool Bright => _phase == Phase.Bright;

        /// <summary>A new step began (the last was done), or the line cleared (an empty key).</summary>
        public void SetKey(string key)
        {
            key = key ?? string.Empty;
            if (string.Equals(key, Key, StringComparison.Ordinal))
            {
                return;
            }

            Key = key;
            _phase = Phase.Waiting;
            _sinceKey = 0f;
            _sinceProgress = 0f;
            _rested = 0f;
            WantsShown = false;
        }

        /// <summary>Something moved the step forward without finishing it (a scan, a part built).</summary>
        public void NoteProgress()
        {
            _sinceProgress = 0f;
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused.</param>
        /// <param name="speed">07's speed, m/s.</param>
        /// <param name="enabled">The player's Guidance setting.</param>
        /// <param name="showing">The line is on screen now: the bright rest counts only then.</param>
        public void Tick(float deltaTime, float speed, bool enabled, bool showing)
        {
            _still = speed < _settings.StillSpeed ? _still + deltaTime : 0f;
            _sinceKey += deltaTime;
            _sinceProgress += deltaTime;
            if (!enabled || Key.Length == 0)
            {
                WantsShown = false;
                return;
            }

            switch (_phase)
            {
                case Phase.Waiting:
                    if (_sinceKey >= _settings.ObjectiveDelaySeconds && _still >= _settings.ObjectiveStillSeconds)
                    {
                        _phase = _memory.Has(Key) ? Phase.Dim : Phase.Bright;
                        _memory.Add(Key);
                        _rested = 0f;
                    }

                    break;
                case Phase.Bright:
                    if (showing)
                    {
                        _rested += deltaTime;
                    }

                    if (_rested >= _settings.ObjectiveRestSeconds)
                    {
                        _phase = Phase.Dim;
                    }

                    break;
                case Phase.Dim:
                    if (_still >= _settings.ObjectiveRebrightStillSeconds &&
                        _sinceProgress >= _settings.ObjectiveIdleSeconds)
                    {
                        _phase = Phase.Bright;
                        _rested = 0f;
                        _sinceProgress = 0f;
                    }

                    break;
            }

            WantsShown = _phase != Phase.Waiting;
        }
    }
}
