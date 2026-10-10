using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The soft "next" line (docs/features/M3-16 "Gentle guidance"): the steps in order, each with its line key and
    /// what completes it; the line is the first step not yet done, or empty once all are. It is read from the game's
    /// state, never stored, so a loaded game shows the right line. <see cref="NextStepChanged"/> is published on the
    /// first <see cref="Tick"/> and whenever the line changes. Nothing allocates per frame.
    /// </summary>
    public sealed class StepFlow
    {
        private readonly EventBus _events;
        private readonly List<string> _keys = new List<string>();
        private readonly List<Func<bool>> _done = new List<Func<bool>>();
        private bool _announced;

        public StepFlow(EventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <summary>The index of the current step (the step count once every step is done).</summary>
        public int CurrentIndex { get; private set; }

        /// <summary>The current line key, or empty when every step is done.</summary>
        public string Current { get; private set; } = string.Empty;

        /// <summary>Appends a step: <paramref name="lineKey"/> shows until <paramref name="done"/>.</summary>
        public void Add(string lineKey, Func<bool> done)
        {
            if (string.IsNullOrEmpty(lineKey))
            {
                throw new ArgumentException("A step needs its line key.", nameof(lineKey));
            }

            _keys.Add(lineKey);
            _done.Add(done ?? throw new ArgumentNullException(nameof(done)));
        }

        public void Tick()
        {
            string key = string.Empty;
            int index = _keys.Count;
            for (int i = 0; i < _keys.Count; i++)
            {
                if (!_done[i]())
                {
                    key = _keys[i];
                    index = i;
                    break;
                }
            }

            CurrentIndex = index;

            if (_announced && key == Current)
            {
                return;
            }

            _announced = true;
            Current = key;
            _events.Publish(new NextStepChanged(key));
        }
    }
}
