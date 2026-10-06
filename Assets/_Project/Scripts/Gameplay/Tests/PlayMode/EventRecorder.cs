using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>Records every gameplay event with the time it was published, for order and timing assertions.</summary>
    public sealed class EventRecorder : IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public EventRecorder(EventBus events)
        {
            Listen(events, ScrapCollected);
            Listen(events, CurrencyChanged);
            Listen(events, SonarPinged);
            Listen(events, RelicAnswered);
            Listen(events, ExcavationStarted);
            Listen(events, ExcavationStopped);
            Listen(events, RelicSurfaced);
            Listen(events, TetherAttached);
            Listen(events, TetherReleased);
            Listen(events, RelicDeposited);
            Listen(events, UpgradePurchased);
            Listen(events, SignalRadiusChanged);
        }

        /// <summary>Event type names in publish order.</summary>
        public List<string> Order { get; } = new List<string>();

        public List<Timed<ScrapCollected>> ScrapCollected { get; } = new List<Timed<ScrapCollected>>();
        public List<Timed<CurrencyChanged>> CurrencyChanged { get; } = new List<Timed<CurrencyChanged>>();
        public List<Timed<SonarPinged>> SonarPinged { get; } = new List<Timed<SonarPinged>>();
        public List<Timed<RelicAnswered>> RelicAnswered { get; } = new List<Timed<RelicAnswered>>();
        public List<Timed<ExcavationStarted>> ExcavationStarted { get; } = new List<Timed<ExcavationStarted>>();
        public List<Timed<ExcavationStopped>> ExcavationStopped { get; } = new List<Timed<ExcavationStopped>>();
        public List<Timed<RelicSurfaced>> RelicSurfaced { get; } = new List<Timed<RelicSurfaced>>();
        public List<Timed<TetherAttached>> TetherAttached { get; } = new List<Timed<TetherAttached>>();
        public List<Timed<TetherReleased>> TetherReleased { get; } = new List<Timed<TetherReleased>>();
        public List<Timed<RelicDeposited>> RelicDeposited { get; } = new List<Timed<RelicDeposited>>();
        public List<Timed<UpgradePurchased>> UpgradePurchased { get; } = new List<Timed<UpgradePurchased>>();

        public List<Timed<SignalRadiusChanged>> SignalRadiusChanged { get; } =
            new List<Timed<SignalRadiusChanged>>();

        public void Dispose()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }

        private void Listen<T>(EventBus events, List<Timed<T>> log) where T : struct
        {
            string name = typeof(T).Name;
            _subscriptions.Add(events.Subscribe<T>(evt =>
            {
                log.Add(new Timed<T>(evt, Time.time));
                Order.Add(name);
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
