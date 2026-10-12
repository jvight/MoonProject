using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The soft "next" line in two tiers (docs/features/M3-16 "Gentle guidance", M3-21 §3). First the teaching steps in
    /// fixed order, each with its line key and what completes it (<see cref="Add"/>); then only the recovery targets'
    /// needs (<see cref="AddNeed"/>), each naming something the player has already seen: an unseen need is passed over
    /// for the next one. The line is the first teaching step not yet done, else the first seen need not yet met, else
    /// empty. It is read from the game's state, never stored, so a loaded game shows the right line.
    /// <see cref="NextStepChanged"/> is published on the first <see cref="Tick"/> and whenever the line changes.
    /// Nothing allocates per frame.
    /// </summary>
    public sealed class StepFlow
    {
        private readonly EventBus _events;
        private readonly List<string> _keys = new List<string>();
        private readonly List<Func<bool>> _done = new List<Func<bool>>();
        private readonly List<Func<bool>> _seen = new List<Func<bool>>();
        private readonly List<Func<string>> _lines = new List<Func<string>>();
        private bool _announced;

        public StepFlow(EventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <summary>
        /// The index of the first step not yet done, teaching step or need (the step count once all are done).
        /// </summary>
        public int CurrentIndex { get; private set; }

        /// <summary>The current line key, or empty when nothing is left to say.</summary>
        public string Current { get; private set; } = string.Empty;

        /// <summary>Teaching steps added so far.</summary>
        public int TeachingCount { get; private set; }

        /// <summary>True once every teaching step is done: from now on the line speaks only of the targets.</summary>
        public bool PastTeaching => CurrentIndex >= TeachingCount;

        /// <summary>Appends a teaching step: <paramref name="lineKey"/> shows until <paramref name="done"/>.</summary>
        /// <exception cref="InvalidOperationException">A need was added already: teaching comes first.</exception>
        public void Add(string lineKey, Func<bool> done)
        {
            if (TeachingCount != _keys.Count)
            {
                throw new InvalidOperationException("Teaching steps come before the targets' needs.");
            }

            Append(lineKey, done, null);
            TeachingCount++;
        }

        /// <summary>
        /// Appends a teaching step whose line names its exact sub-step now (<paramref name="line"/>, e.g. bring the
        /// part, park on the pad, hold E) until <paramref name="done"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">A need was added already: teaching comes first.</exception>
        public void Add(Func<string> line, Func<bool> done)
        {
            if (TeachingCount != _keys.Count)
            {
                throw new InvalidOperationException("Teaching steps come before the targets' needs.");
            }

            _keys.Add(string.Empty);
            _done.Add(done ?? throw new ArgumentNullException(nameof(done)));
            _seen.Add(null);
            _lines.Add(line ?? throw new ArgumentNullException(nameof(line)));
            TeachingCount++;
        }

        /// <summary>
        /// Appends a recovery target's need: <paramref name="lineKey"/> shows until <paramref name="done"/>, and only
        /// while <paramref name="seen"/> (what it names has been seen).
        /// </summary>
        public void AddNeed(string lineKey, Func<bool> done, Func<bool> seen)
        {
            Append(lineKey, done, seen ?? throw new ArgumentNullException(nameof(seen)));
        }

        public void Tick()
        {
            string key = string.Empty;
            int index = _keys.Count;
            for (int i = 0; i < _keys.Count; i++)
            {
                if (_done[i]())
                {
                    continue;
                }

                index = Math.Min(index, i);
                if (_seen[i] == null || _seen[i]())
                {
                    key = _lines[i] != null ? _lines[i]() : _keys[i];
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

        private void Append(string lineKey, Func<bool> done, Func<bool> seen)
        {
            if (string.IsNullOrEmpty(lineKey))
            {
                throw new ArgumentException("A step needs its line key.", nameof(lineKey));
            }

            _keys.Add(lineKey);
            _done.Add(done ?? throw new ArgumentNullException(nameof(done)));
            _seen.Add(seen);
            _lines.Add(null);
        }
    }
}
