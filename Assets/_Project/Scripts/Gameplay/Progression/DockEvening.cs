using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The dock's evening (docs/features/M3-16 P6), the homecoming and the "one more trip" hook. Something new woke or
    /// was found since 07 last rested (power came back, a part, a tube or a memory came home, a friend was stitched, a
    /// mast lit, a tape or a log turned up); when 07 then comes to rest on its dock, <see cref="DockEveningOpened"/> is
    /// published once (Rover eases out to the wide shot, Audio swells). Once the shot has settled, after
    /// <c>sweepDelay</c> seconds still docked, the tower's listening sweep runs: <see cref="Tick"/> returns true once,
    /// and the caller raises one new signal pillar on the horizon. Leaving the dock first lets the evening go.
    /// </summary>
    public sealed class DockEvening : IDisposable
    {
        private readonly EventBus _events;
        private readonly float _sweepDelay;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private bool _news;
        private bool _opening;
        private Vector3 _dock;
        private float _sweepAt = float.PositiveInfinity;
        private bool _sweepPending;

        public DockEvening(EventBus events, float sweepDelay)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _sweepDelay = sweepDelay;
            _subscriptions.Add(events.Subscribe<RoverDockChanged>(OnDock));
            _subscriptions.Add(events.Subscribe<BasePowerChanged>(evt => _news |= !evt.Restored));
            _subscriptions.Add(events.Subscribe<StoryPartFound>(evt => _news |= !evt.Restored));
            _subscriptions.Add(events.Subscribe<BlueprintFound>(_ => _news = true));
            _subscriptions.Add(events.Subscribe<RelicDeposited>(_ => _news = true));
            _subscriptions.Add(events.Subscribe<FriendRepaired>(_ => _news = true));
            _subscriptions.Add(events.Subscribe<RelayRestored>(_ => _news = true));
            _subscriptions.Add(events.Subscribe<CassetteCollected>(_ => _news = true));
            _subscriptions.Add(events.Subscribe<CrewLogFound>(_ => _news = true));
        }

        /// <summary>Something new woke or was found since the last evening.</summary>
        public bool HasNews => _news;

        /// <summary>
        /// Publishes the evening when 07 has just come to rest with news; true once, when the sweep should raise a
        /// pillar.
        /// </summary>
        public bool Tick(float now)
        {
            if (_opening)
            {
                _opening = false;
                _news = false;
                _sweepPending = true;
                _sweepAt = now + _sweepDelay;
                _events.Publish(new DockEveningOpened(_dock));
            }

            if (!_sweepPending || now < _sweepAt)
            {
                return false;
            }

            _sweepPending = false;
            return true;
        }

        public void Dispose()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }

        private void OnDock(RoverDockChanged change)
        {
            if (!change.Docked)
            {
                _opening = false;
                _sweepPending = false;
                return;
            }

            if (_news)
            {
                _opening = true;
                _dock = change.Position;
            }
        }
    }
}
