using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Records the salvage and scan events with the time each was published, for order and timing assertions. A
    /// fragment scan is recorded but kept out of <see cref="Order"/>: the order assertions follow the salvage loop.
    /// </summary>
    public sealed class EventRecorder : IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public EventRecorder(EventBus events)
        {
            Listen(events, SalvageCutStarted);
            Listen(events, SalvageCutStopped);
            Listen(events, MaterialSalvaged);
            Listen(events, MaterialsChanged);
            Listen(events, FragmentScanned, false);
        }

        /// <summary>Event type names in publish order.</summary>
        public List<string> Order { get; } = new List<string>();

        public List<Timed<SalvageCutStarted>> SalvageCutStarted { get; } = new List<Timed<SalvageCutStarted>>();

        public List<Timed<SalvageCutStopped>> SalvageCutStopped { get; } = new List<Timed<SalvageCutStopped>>();

        public List<Timed<MaterialSalvaged>> MaterialSalvaged { get; } = new List<Timed<MaterialSalvaged>>();

        public List<Timed<MaterialsChanged>> MaterialsChanged { get; } = new List<Timed<MaterialsChanged>>();

        public List<Timed<FragmentScanned>> FragmentScanned { get; } = new List<Timed<FragmentScanned>>();

        public void Dispose()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }

        private void Listen<T>(EventBus events, List<Timed<T>> log, bool ordered = true) where T : struct
        {
            string name = typeof(T).Name;
            _subscriptions.Add(events.Subscribe<T>(evt =>
            {
                log.Add(new Timed<T>(evt, Time.time));
                if (ordered)
                {
                    Order.Add(name);
                }
            }));
        }

        /// <summary>An event and the game time it was published at.</summary>
        public readonly struct Timed<T> where T : struct
        {
            public Timed(T value, float time)
            {
                Value = value;
                Time = time;
            }

            public T Value { get; }

            public float Time { get; }
        }
    }
}
