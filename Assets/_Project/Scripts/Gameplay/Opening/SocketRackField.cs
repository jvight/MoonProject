using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The socket rack at the pad in the running game (docs/design/opening-v3-plan.md 4.3): it feeds
    /// <see cref="SocketRack"/> from the game's events and announces every bay that changes what it shows with
    /// <see cref="RackChanged"/>. The west signal releases it, the next dock brings it out, a scan of a part's blueprint
    /// shows that part's outline and a fitted part fills its bay. Saved; <see cref="Announce"/> replays a loaded rack
    /// with Restored = true.
    /// </summary>
    public sealed class SocketRackField : IRackStatus, IDisposable
    {
        private readonly EventBus _events;
        private readonly SocketRack _rack;
        private readonly RackBayState[] _announced;
        private readonly IDisposable[] _tokens;

        public SocketRackField(EventBus events, IReadOnlyList<RackBaySettings> bays)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _rack = new SocketRack(bays);
            _announced = new RackBayState[_rack.Count];
            _tokens = new[]
            {
                events.Subscribe<OpeningMilestoneReached>(OnMilestone),
                events.Subscribe<RoverDockChanged>(OnDockChanged),
                events.Subscribe<FragmentScanned>(OnFragmentScanned),
                events.Subscribe<PartFitted>(OnPartFitted),
            };
        }

        public bool Shown => _rack.Shown;

        public int Count => _rack.Count;

        public string IdOf(int bay)
        {
            return _rack.IdOf(bay);
        }

        public RackBayState StateOf(int bay)
        {
            return _rack.StateOf(bay);
        }

        public RackSaveData Capture()
        {
            return _rack.Capture();
        }

        public void Restore(RackSaveData data)
        {
            _rack.Restore(data);
        }

        /// <summary>The session starts (after the save is applied): every bay that shows something, replayed.</summary>
        public void Announce()
        {
            for (int i = 0; i < _rack.Count; i++)
            {
                _announced[i] = _rack.StateOf(i);
                if (_announced[i] != RackBayState.Hidden)
                {
                    _events.Publish(new RackChanged(i, _announced[i], true));
                }
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _tokens.Length; i++)
            {
                _tokens[i].Dispose();
            }
        }

        private void OnMilestone(OpeningMilestoneReached reached)
        {
            if (reached.Milestone == OpeningMilestone.SignalWest)
            {
                _rack.Release();
            }
        }

        private void OnDockChanged(RoverDockChanged dock)
        {
            if (dock.Docked && _rack.Docked())
            {
                PublishChanges();
            }
        }

        private void OnFragmentScanned(FragmentScanned scanned)
        {
            int version = _rack.Version;
            _rack.BlueprintScanned(scanned.BlueprintId);
            if (_rack.Version != version)
            {
                PublishChanges();
            }
        }

        private void OnPartFitted(PartFitted fitted)
        {
            int version = _rack.Version;
            _rack.Fitted(fitted.Part);
            if (_rack.Version != version)
            {
                PublishChanges();
            }
        }

        private void PublishChanges()
        {
            for (int i = 0; i < _rack.Count; i++)
            {
                RackBayState state = _rack.StateOf(i);
                if (state != _announced[i])
                {
                    _announced[i] = state;
                    _events.Publish(new RackChanged(i, state, false));
                }
            }
        }
    }
}
