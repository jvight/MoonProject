using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Dormant stations say "not yet" (docs/design/first-hour-v2.md §3): the first time in a session that 07 comes
    /// within a station's radius while it still sleeps, it answers with <see cref="StationNotYet"/> (Audio's soft low
    /// tone, the UI's ticker line on what wakes it). Once per station per session, so a reload reminds the player
    /// gently; a station that wakes before 07 arrives stays quiet. Nothing allocates per frame.
    /// </summary>
    public sealed class DormantAnswers
    {
        private readonly EventBus _events;
        private readonly List<Sleeper> _sleepers = new List<Sleeper>();
        private readonly List<bool> _answered = new List<bool>();

        public DormantAnswers(EventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <summary>
        /// Adds a station at <paramref name="position"/> answering with <paramref name="reasonKey"/> when 07 comes
        /// within <paramref name="radius"/> metres while <paramref name="dormant"/> says it sleeps.
        /// </summary>
        public void Add(string reasonKey, Vector3 position, float radius, Func<bool> dormant)
        {
            if (string.IsNullOrEmpty(reasonKey))
            {
                throw new ArgumentException("A dormant station needs its reason key.", nameof(reasonKey));
            }

            if (dormant == null)
            {
                throw new ArgumentNullException(nameof(dormant));
            }

            _sleepers.Add(new Sleeper(reasonKey, position, radius, dormant));
            _answered.Add(false);
        }

        /// <summary>Answers for each sleeping station 07 has just come near for the first time this session.</summary>
        public void Tick(Vector3 rover)
        {
            for (int i = 0; i < _sleepers.Count; i++)
            {
                Sleeper sleeper = _sleepers[i];
                if (_answered[i])
                {
                    continue;
                }

                float distance = SurfaceRules.HorizontalDistance(rover, sleeper.Position);
                if (distance <= sleeper.Radius && sleeper.Dormant())
                {
                    _answered[i] = true;
                    _events.Publish(new StationNotYet(sleeper.ReasonKey, sleeper.Position, distance));
                }
            }
        }

        private readonly struct Sleeper
        {
            public Sleeper(string reasonKey, Vector3 position, float radius, Func<bool> dormant)
            {
                ReasonKey = reasonKey;
                Position = position;
                Radius = radius;
                Dormant = dormant;
            }

            public string ReasonKey { get; }

            public Vector3 Position { get; }

            public float Radius { get; }

            public Func<bool> Dormant { get; }
        }
    }
}
