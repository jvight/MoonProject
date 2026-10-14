using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The dock's evening (docs/features/M3-16 P6), the homecoming and the "one more trip" hook. Something new woke or
    /// was found since 07 last rested (power came back, a part or a memory came home, a friend was stitched, a
    /// mast lit, a tape or a log turned up); when 07 then comes to rest on its dock, <see cref="DockEveningOpened"/> is
    /// published once (Rover eases out to the wide shot, Audio swells). Once the shot has settled, after
    /// <c>sweepDelay</c> seconds still docked, the tower's listening sweep runs: <see cref="Tick"/> returns true once,
    /// and the caller raises one new signal pillar on the horizon. The first evening after a recovery row completes
    /// holds <c>completionHold</c> seconds longer on the tower (docs/features/M3-21 §2), and says so in the event.
    /// Leaving the dock first lets the evening go.
    /// </summary>
    public sealed class DockEvening : IDisposable
    {
        private readonly EventBus _events;
        private readonly float _sweepDelay;
        private readonly float _completionHold;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private bool _news;
        private bool _rowCompleted;
        private bool _opening;
        private Vector3 _dock;
        private float _sweepAt = float.PositiveInfinity;
        private bool _sweepPending;

        public DockEvening(EventBus events, float sweepDelay, float completionHold)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _sweepDelay = sweepDelay;
            _completionHold = completionHold;
            _subscriptions.Add(events.Subscribe<RecoveryProgressChanged>(OnRecovery));
            _subscriptions.Add(events.Subscribe<RoverDockChanged>(OnDock));
            _subscriptions.Add(events.Subscribe<BasePowerChanged>(evt => _news |= !evt.Restored));
            _subscriptions.Add(events.Subscribe<ComponentFound>(evt => _news |= !evt.Restored));
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
                float hold = _rowCompleted ? _completionHold : 0f;
                _opening = false;
                _news = false;
                _rowCompleted = false;
                _sweepPending = true;
                _sweepAt = now + _sweepDelay + hold;
                _events.Publish(new DockEveningOpened(_dock, hold));
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

        private void OnRecovery(RecoveryProgressChanged change)
        {
            if (!change.Restored && change.Lamps > 0 && change.LitLamps >= change.Lamps)
            {
                _rowCompleted = true;
                _news = true;
            }
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
