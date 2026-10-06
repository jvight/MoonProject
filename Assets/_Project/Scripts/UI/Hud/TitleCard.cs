using System;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The opening title: while 07 wakes up, a soft "Lofi Lunar" wordmark drifts in low on screen, rests, and fades
    /// away. No menus before play; it plays once per session.
    /// </summary>
    internal sealed class TitleCard
    {
        private readonly TitleSettings _settings;
        private readonly Reveal _reveal;
        private Phase _phase;
        private float _timer;

        public TitleCard(VisualElement title, TitleSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _reveal = new Reveal(title, settings.Reveal);
            _reveal.Snap(false);
        }

        private enum Phase
        {
            Idle,
            Waiting,
            Rising,
            Resting,
            Leaving,
            Done,
        }

        /// <summary>True from the wake-up until the wordmark has fully faded away.</summary>
        public bool IsPlaying => _phase != Phase.Idle && _phase != Phase.Done;

        /// <summary>Starts the title (ignored if it already played this session).</summary>
        public void Play()
        {
            if (_phase != Phase.Idle)
            {
                return;
            }

            _phase = Phase.Waiting;
            _timer = 0f;
        }

        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused so the title waits too.</param>
        public void Tick(float deltaTime)
        {
            switch (_phase)
            {
                case Phase.Waiting:
                    _timer += deltaTime;
                    if (_timer >= _settings.Delay)
                    {
                        _reveal.Show();
                        _phase = Phase.Rising;
                    }

                    break;
                case Phase.Rising:
                    if (_reveal.IsShown)
                    {
                        _timer = 0f;
                        _phase = Phase.Resting;
                    }

                    break;
                case Phase.Resting:
                    _timer += deltaTime;
                    if (_timer >= _settings.Hold)
                    {
                        _reveal.Hide();
                        _phase = Phase.Leaving;
                    }

                    break;
                case Phase.Leaving:
                    if (_reveal.IsHidden)
                    {
                        _phase = Phase.Done;
                    }

                    break;
            }

            _reveal.Tick(deltaTime);
        }
    }
}
