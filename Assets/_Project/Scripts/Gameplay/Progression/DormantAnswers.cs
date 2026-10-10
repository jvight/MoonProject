using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Dormant stations answer the sonar (docs/features/M3-16 rule 1): when a ping's ring reaches a station that is
    /// still asleep, it answers with <see cref="StationNotYet"/> (Audio's soft low tone, UI's line on what wakes it).
    /// A station that wakes before the ring arrives stays quiet. Nothing allocates per frame.
    /// </summary>
    public sealed class DormantAnswers : IDisposable
    {
        private readonly EventBus _events;
        private readonly SonarTuning _sonar;
        private readonly List<Sleeper> _sleepers = new List<Sleeper>();
        private readonly IDisposable _pings;
        private SonarSchedule _schedule;
        private Vector3 _pingOrigin;
        private float _pingRange;
        private bool _pinged;

        public DormantAnswers(EventBus events, SonarTuning sonar)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _sonar = sonar != null ? sonar : throw new ArgumentNullException(nameof(sonar));
            _pings = events.Subscribe<SonarPinged>(OnPinged);
        }

        /// <summary>
        /// Adds a station at <paramref name="position"/> answering with <paramref name="reasonKey"/> while
        /// <paramref name="dormant"/> says it sleeps. Add every station before the first <see cref="Tick"/>.
        /// </summary>
        public void Add(string reasonKey, Vector3 position, Func<bool> dormant)
        {
            if (string.IsNullOrEmpty(reasonKey))
            {
                throw new ArgumentException("A dormant station needs its reason key.", nameof(reasonKey));
            }

            if (dormant == null)
            {
                throw new ArgumentNullException(nameof(dormant));
            }

            _sleepers.Add(new Sleeper(reasonKey, position, dormant));
            _schedule = new SonarSchedule(_sleepers.Count);
        }

        /// <summary>Schedules the answers of the latest ping and publishes those whose time has come.</summary>
        public void Tick(float now)
        {
            if (_pinged)
            {
                _pinged = false;
                for (int i = 0; i < _sleepers.Count; i++)
                {
                    if (_sleepers[i].Dormant())
                    {
                        _schedule.Add(i, Vector3.Distance(_pingOrigin, _sleepers[i].Position), now, _pingRange,
                            _sonar.RingDuration, _sonar.AnswerLag);
                    }
                }
            }

            while (_schedule != null && _schedule.TryPop(now, out int index, out float distance))
            {
                Sleeper sleeper = _sleepers[index];
                if (sleeper.Dormant())
                {
                    _events.Publish(new StationNotYet(sleeper.ReasonKey, sleeper.Position, distance));
                }
            }
        }

        public void Dispose()
        {
            _pings.Dispose();
        }

        private void OnPinged(SonarPinged ping)
        {
            _pingOrigin = ping.Origin;
            _pingRange = ping.Range;
            _pinged = true;
        }

        private readonly struct Sleeper
        {
            public Sleeper(string reasonKey, Vector3 position, Func<bool> dormant)
            {
                ReasonKey = reasonKey;
                Position = position;
                Dormant = dormant;
            }

            public string ReasonKey { get; }

            public Vector3 Position { get; }

            public Func<bool> Dormant { get; }
        }
    }
}
