using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// Decides which radio ticker line shows and when (pure logic, EditMode-tested).
    /// <list type="bullet">
    /// <item>One line at a time, in the order the lines arrived, after a breath of quiet (the gap).</item>
    /// <item>A line rests fully visible for a time that grows with its length, then eases out.</item>
    /// <item>Lines wait while the gate is closed (a card, a dig, a prompt, ...). A line already up when the gate
    /// closes eases out and comes back, for its whole rest, once the gate has stayed open for the gap; but only
    /// <see cref="TickerSettings.MaxYields"/> times: interrupted once more, it eases away for good.</item>
    /// <item>A waiting line, or a line that yielded and waits hidden to come back, takes the argument of a newer line
    /// with the same key ("Now playing" names the latest track, a signal the latest bearing); the line on screen is
    /// not queued again; past the capacity the oldest waiting line is dropped.</item>
    /// </list>
    /// Text is formatted when a line starts or the language changes, never per step. The view reports whether its line
    /// is fully shown or fully hidden, and shows <see cref="Text"/> while <see cref="WantsShown"/>.
    /// </summary>
    internal sealed class TickerQueue
    {
        private readonly TickerSettings _settings;
        private readonly TickerText _format;
        private readonly List<TickerLine> _waiting;
        private Phase _phase;
        private TickerLine _line;
        private float _timer;
        private float _hold;
        private int _yields;

        public TickerQueue(TickerSettings settings, TickerText format)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _format = format ?? throw new ArgumentNullException(nameof(format));
            _waiting = new List<TickerLine>(settings.Capacity + 1);
        }

        private enum Phase
        {
            /// <summary>No line: counting the gap before the next waiting line may start.</summary>
            Idle,

            /// <summary>A line is up (easing in or resting).</summary>
            Showing,

            /// <summary>The line has rested long enough and eases out for good.</summary>
            Leaving,

            /// <summary>The gate closed under the line: it eases out, to come back later.</summary>
            Yielding,

            /// <summary>The line yielded and waits, hidden, for the gate and the gap.</summary>
            Returning,
        }

        /// <summary>True while the line should be visible; false while it eases out or waits.</summary>
        public bool WantsShown => _phase == Phase.Showing;

        /// <summary>True from the moment a line starts until it has eased out for good.</summary>
        public bool HasLine => _phase != Phase.Idle;

        /// <summary>The line started last (shown, yielding or just gone).</summary>
        public TickerLine Line => _line;

        /// <summary>The current line's words in the current language.</summary>
        public string Text { get; private set; } = string.Empty;

        /// <summary>Grows each time <see cref="Text"/> changes, so the view writes the label only then.</summary>
        public int Revision { get; private set; }

        /// <summary>Lines waiting their turn (the current line not included).</summary>
        public int Waiting => _waiting.Count;

        /// <summary>Seconds the current line rests fully visible.</summary>
        public float HoldSeconds => _hold;

        /// <summary>Queues <paramref name="line"/>; a line without a key is a wiring bug and is logged.</summary>
        public void Enqueue(TickerLine line)
        {
            if (string.IsNullOrEmpty(line.Key))
            {
                Debug.LogError($"{nameof(TickerQueue)}: a ticker line arrived without a key; nothing queued.");
                return;
            }

            if (HasLine && Same(line, _line))
            {
                return;
            }

            if (_phase == Phase.Returning && string.Equals(_line.Key, line.Key, StringComparison.Ordinal))
            {
                Replace(line);
                return;
            }

            for (int i = 0; i < _waiting.Count; i++)
            {
                if (string.Equals(_waiting[i].Key, line.Key, StringComparison.Ordinal))
                {
                    _waiting[i] = line;
                    return;
                }
            }

            _waiting.Add(line);
            if (_waiting.Count > _settings.Capacity)
            {
                _waiting.RemoveAt(0);
            }
        }

        /// <param name="deltaTime">Seconds since the last step; 0 while paused, so everything waits.</param>
        /// <param name="gateOpen">False while something else has the player's attention.</param>
        /// <param name="shown">True when the view's line is fully visible.</param>
        /// <param name="hidden">True when the view's line is fully hidden.</param>
        public void Step(float deltaTime, bool gateOpen, bool shown, bool hidden)
        {
            switch (_phase)
            {
                case Phase.Idle:
                    CountGap(deltaTime, gateOpen);
                    if (_waiting.Count > 0 && gateOpen && _timer >= _settings.GapSeconds)
                    {
                        TickerLine next = _waiting[0];
                        _waiting.RemoveAt(0);
                        Start(next);
                    }

                    break;
                case Phase.Showing:
                    if (!gateOpen)
                    {
                        _phase = _yields < _settings.MaxYields ? Phase.Yielding : Phase.Leaving;
                        _yields++;
                    }
                    else if (shown)
                    {
                        _timer += deltaTime;
                        if (_timer >= _hold)
                        {
                            _phase = Phase.Leaving;
                        }
                    }

                    break;
                case Phase.Leaving:
                    if (hidden)
                    {
                        _phase = Phase.Idle;
                        _timer = 0f;
                    }

                    break;
                case Phase.Yielding:
                    if (gateOpen && !hidden)
                    {
                        _phase = Phase.Showing;
                    }
                    else if (hidden)
                    {
                        _phase = Phase.Returning;
                        _timer = 0f;
                    }

                    break;
                case Phase.Returning:
                    CountGap(deltaTime, gateOpen);
                    if (gateOpen && _timer >= _settings.GapSeconds)
                    {
                        _phase = Phase.Showing;
                        _timer = 0f;
                    }

                    break;
            }
        }

        /// <summary>Re-reads the current line in the new language (waiting lines are read when they start).</summary>
        public void Relocalize()
        {
            if (HasLine)
            {
                Text = _format.Format(_line);
                Revision++;
            }
        }

        private void Start(TickerLine line)
        {
            Replace(line);
            _timer = 0f;
            _phase = Phase.Showing;
        }

        /// <summary>Makes <paramref name="line"/> current: news, so it may make way again.</summary>
        private void Replace(TickerLine line)
        {
            _yields = 0;
            _line = line;
            Text = _format.Format(line);
            Revision++;
            _hold = _settings.HoldSeconds(Text.Length);
        }

        private void CountGap(float deltaTime, bool gateOpen)
        {
            _timer = gateOpen ? Mathf.Min(_timer + deltaTime, _settings.GapSeconds) : 0f;
        }

        private static bool Same(TickerLine a, TickerLine b)
        {
            return string.Equals(a.Key, b.Key, StringComparison.Ordinal) &&
                   string.Equals(a.Argument, b.Argument, StringComparison.Ordinal) &&
                   a.ArgumentIsKey == b.ArgumentIsKey;
        }
    }
}
