using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Records every gameplay event (and the rover's Hover-Jump events) with the time it was published, for order and
    /// timing assertions. The radio and story events of M3-05 (cassettes, radio program, crew logs, Bell's signals
    /// and cues, ticker lines), the relay network's of M3-06 (relay cues and restorations, the radio-hop) and the
    /// stations' of M3-14 (station cues, the bay's fitting) are recorded too but
    /// kept out of <see cref="Order"/>: the radio announces itself at every boot, and the order assertions follow the
    /// play loop.
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
            Listen(events, SonarPinged);
            Listen(events, SiteAnswered);
            Listen(events, RelicAnswered);
            Listen(events, ExcavationStarted);
            Listen(events, ExcavationStopped);
            Listen(events, RelicSurfaced);
            Listen(events, TetherAttached);
            Listen(events, TetherReleased);
            Listen(events, RelicStowed);
            Listen(events, RelicDeposited);
            Listen(events, UpgradePurchased);
            Listen(events, SignalRadiusChanged);
            Listen(events, FriendAnswered);
            Listen(events, FriendPartCollected);
            Listen(events, FriendRepairStarted);
            Listen(events, FriendRepaired);
            Listen(events, FriendGreeted);
            Listen(events, FriendSpotted);
            Listen(events, RoverJumpCharged);
            Listen(events, RoverJumped);
            Listen(events, CassetteCollected, false);
            Listen(events, RadioProgramChanged, false);
            Listen(events, CrewLogFound, false);
            Listen(events, BellSignalPicked, false);
            Listen(events, BellSignalFound, false);
            Listen(events, TickerLine, false);
            Listen(events, BellCued, false);
            Listen(events, RelayCued, false);
            Listen(events, RelayRestored, false);
            Listen(events, RadioHopListChanged, false);
            Listen(events, RadioHopStarted, false);
            Listen(events, RadioHopFinished, false);
            Listen(events, StationCued, false);
            Listen(events, RoverBayFitting, false);
        }

        /// <summary>Event type names in publish order.</summary>
        public List<string> Order { get; } = new List<string>();

        public List<Timed<SalvageCutStarted>> SalvageCutStarted { get; } = new List<Timed<SalvageCutStarted>>();

        public List<Timed<SalvageCutStopped>> SalvageCutStopped { get; } = new List<Timed<SalvageCutStopped>>();

        public List<Timed<MaterialSalvaged>> MaterialSalvaged { get; } = new List<Timed<MaterialSalvaged>>();
        public List<Timed<MaterialsChanged>> MaterialsChanged { get; } = new List<Timed<MaterialsChanged>>();
        public List<Timed<SonarPinged>> SonarPinged { get; } = new List<Timed<SonarPinged>>();
        public List<Timed<SiteAnswered>> SiteAnswered { get; } = new List<Timed<SiteAnswered>>();

        public List<Timed<RelicAnswered>> RelicAnswered { get; } = new List<Timed<RelicAnswered>>();
        public List<Timed<ExcavationStarted>> ExcavationStarted { get; } = new List<Timed<ExcavationStarted>>();
        public List<Timed<ExcavationStopped>> ExcavationStopped { get; } = new List<Timed<ExcavationStopped>>();
        public List<Timed<RelicSurfaced>> RelicSurfaced { get; } = new List<Timed<RelicSurfaced>>();
        public List<Timed<TetherAttached>> TetherAttached { get; } = new List<Timed<TetherAttached>>();
        public List<Timed<TetherReleased>> TetherReleased { get; } = new List<Timed<TetherReleased>>();
        public List<Timed<RelicStowed>> RelicStowed { get; } = new List<Timed<RelicStowed>>();

        public List<Timed<RelicDeposited>> RelicDeposited { get; } = new List<Timed<RelicDeposited>>();
        public List<Timed<UpgradePurchased>> UpgradePurchased { get; } = new List<Timed<UpgradePurchased>>();

        public List<Timed<SignalRadiusChanged>> SignalRadiusChanged { get; } =
            new List<Timed<SignalRadiusChanged>>();

        public List<Timed<FriendAnswered>> FriendAnswered { get; } = new List<Timed<FriendAnswered>>();

        public List<Timed<FriendPartCollected>> FriendPartCollected { get; } =
            new List<Timed<FriendPartCollected>>();

        public List<Timed<FriendRepairStarted>> FriendRepairStarted { get; } =
            new List<Timed<FriendRepairStarted>>();

        public List<Timed<FriendRepaired>> FriendRepaired { get; } = new List<Timed<FriendRepaired>>();
        public List<Timed<FriendGreeted>> FriendGreeted { get; } = new List<Timed<FriendGreeted>>();
        public List<Timed<FriendSpotted>> FriendSpotted { get; } = new List<Timed<FriendSpotted>>();
        public List<Timed<RoverJumpCharged>> RoverJumpCharged { get; } = new List<Timed<RoverJumpCharged>>();
        public List<Timed<RoverJumped>> RoverJumped { get; } = new List<Timed<RoverJumped>>();
        public List<Timed<CassetteCollected>> CassetteCollected { get; } = new List<Timed<CassetteCollected>>();

        public List<Timed<RadioProgramChanged>> RadioProgramChanged { get; } =
            new List<Timed<RadioProgramChanged>>();

        public List<Timed<CrewLogFound>> CrewLogFound { get; } = new List<Timed<CrewLogFound>>();
        public List<Timed<BellSignalPicked>> BellSignalPicked { get; } = new List<Timed<BellSignalPicked>>();
        public List<Timed<BellSignalFound>> BellSignalFound { get; } = new List<Timed<BellSignalFound>>();
        public List<Timed<TickerLine>> TickerLine { get; } = new List<Timed<TickerLine>>();
        public List<Timed<BellCued>> BellCued { get; } = new List<Timed<BellCued>>();

        public List<Timed<RelayCued>> RelayCued { get; } = new List<Timed<RelayCued>>();

        public List<Timed<RelayRestored>> RelayRestored { get; } = new List<Timed<RelayRestored>>();

        public List<Timed<RadioHopListChanged>> RadioHopListChanged { get; } =
            new List<Timed<RadioHopListChanged>>();

        public List<Timed<RadioHopStarted>> RadioHopStarted { get; } = new List<Timed<RadioHopStarted>>();

        public List<Timed<RadioHopFinished>> RadioHopFinished { get; } = new List<Timed<RadioHopFinished>>();

        public List<Timed<StationCued>> StationCued { get; } = new List<Timed<StationCued>>();

        public List<Timed<RoverBayFitting>> RoverBayFitting { get; } = new List<Timed<RoverBayFitting>>();

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
