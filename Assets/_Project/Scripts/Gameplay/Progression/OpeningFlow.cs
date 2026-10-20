using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The opening's objective flow (docs/design/opening-v2.md §3, opening-v3 guidance audit): the first step not yet
    /// done, every step read from the game (<see cref="IOpeningProgress"/>) and never stored. The world leads; the
    /// step's line speaks through <see cref="NextStepChanged"/> only as the next line, right after progress gives the
    /// player a new line (a step done, or a step's line changing), held for
    /// <see cref="GuidanceTiming.NextLineSeconds"/> (G2), never for the line a session starts on; and as a reminder,
    /// after <see cref="GuidanceTiming.StallSeconds"/> with no progress, moving or still, and once more
    /// <see cref="GuidanceTiming.AgainSeconds"/> later, then never again for that line (G4). The line leads with its
    /// key's glyph while that key is new or has not been pressed for <see cref="GuidanceTiming.KeyFreshSeconds"/>. A
    /// step whose line would say something the world has not set up yet has no line (B7's for the spent lander
    /// reserve; B11's for nightfall), and B7's rack line waits for the cutter to sputter. It also publishes
    /// <see cref="OpeningMilestoneReached"/> as the opening reaches FirstCharge (B6), PowerOn (B8), NightReady (B9) and
    /// SignalWest (B11), the milestones it reads from the game; <see cref="Announce"/> replays the ones a loaded game
    /// has reached, with Restored = true, before the first <see cref="Tick"/>. CallAnswered (R8) is published by the
    /// answer itself, not derived here. Allocation-free per tick.
    /// </summary>
    public sealed class OpeningFlow : IDisposable
    {
        private const int MaxReminders = 2;

        private static readonly int MilestoneCount = Enum.GetValues(typeof(OpeningMilestone)).Length;

        /// <summary>The milestones this flow reads from the game, in the order the opening reaches them.</summary>
        private static readonly OpeningMilestone[] Derived =
        {
            OpeningMilestone.FirstCharge, OpeningMilestone.PowerOn, OpeningMilestone.NightReady,
            OpeningMilestone.SignalWest,
        };

        private readonly EventBus _events;
        private readonly IOpeningProgress _game;
        private readonly KeyUse _keys;
        private readonly GuidanceTiming _timing;
        private readonly StepFlow _steps;
        private readonly bool[] _reached = new bool[MilestoneCount];
        private bool _announced;
        private bool _watching;
        private int _watchedIndex = -1;
        private string _watchedKey = string.Empty;
        private int _reminders;
        private float _stalled;
        private float _shownFor;
        private float _showSeconds;
        private bool _woke;
        private readonly IDisposable _wake;

        /// <param name="keys">The action keys the player has pressed lately (the line's glyph).</param>
        /// <param name="timing">When the next line and the reminders speak.</param>
        public OpeningFlow(EventBus events, IOpeningProgress game, KeyUse keys, GuidanceTiming timing)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
            _timing = timing ?? throw new ArgumentNullException(nameof(timing));

            // The steps keep to themselves: the player is shown a line only as a next line or a reminder (see Tick).
            _steps = new StepFlow(new EventBus());
            _steps.Add(OpeningKeys.DriveToLander, () => _game.ReachedLander || _game.Knows(StructureIds.ChargingPad));
            _steps.Add(OpeningKeys.ScanLander, () => _game.Knows(StructureIds.ChargingPad));
            _steps.Add(OpeningKeys.CollectParts,
                () => _game.Covers(StructureIds.ChargingPad) || _game.Built(StructureIds.ChargingPad));
            _steps.Add(OpeningKeys.BuildPad, () => _game.Built(StructureIds.ChargingPad));
            _steps.Add(OpeningKeys.Charge, () => _game.FirstCharge);
            _steps.Add(() => _game.ReserveSpent ? OpeningKeys.FindSolar : string.Empty,
                () => _game.Knows(StructureIds.SolarArray));
            _steps.Add(IgniterLine, () => _game.IgniterFitted);
            _steps.Add(SolarLine, () => _game.PowerOn);
            _steps.Add(NightLine, () => _game.NightReady);
            _steps.Add(() => _game.NightFallen ? OpeningKeys.ScanTower : OpeningKeys.AwaitNight,
                () => _game.Knows(StructureIds.AntennaMast));
            _steps.Add(OpeningKeys.BuildMast, () => _game.Built(StructureIds.AntennaMast));
            _steps.Add(OpeningKeys.Transmit, () => _game.CallAnswered);
            _steps.Add(OpeningKeys.GoToRelay, () => _game.RelayRelit);
            _steps.Add(OpeningKeys.ScanElder06, () => _game.HeadlampUnlocked || _game.HeadlampFitted);
            _steps.Add(() => _game.RackOut ? OpeningKeys.AssembleHeadlamp : OpeningKeys.DockForRack,
                () => _game.HeadlampFitted);
            _wake = events.Subscribe<BootCheckFinished>(_ => _woke = true);
        }

        public void Dispose()
        {
            _wake.Dispose();
        }

        /// <summary>
        /// B7: the array's crates need the cutter, which has no igniter. The line asks to cut as if it worked (the rack
        /// stands in plain view, so a player may find the igniter unasked); only once the cutter has sputtered does it
        /// point at the rack. With the igniter in the hold it says what the screen will accept: a field swap fits
        /// where 07 stands, a pad part on the plate.
        /// </summary>
        private string IgniterLine()
        {
            if (_game.CarriesIgniter)
            {
                return _game.CanFitIgniter ? OpeningKeys.FitIgniter : OpeningKeys.ParkOnPad;
            }

            return _game.CutterSputtered ? OpeningKeys.TakeIgniter : SolarLine();
        }

        private string SolarLine()
        {
            return _game.Covers(StructureIds.SolarArray) ? OpeningKeys.BuildSolar : OpeningKeys.CutOptics;
        }

        /// <summary>
        /// R5 to B9: the first free choice names the near sites, then, once one is visited, the dock that ends the day;
        /// at dusk, the power module to scan, then the module to tow home, then the bank to build on it, or the bank to
        /// move into the grid's reach.
        /// </summary>
        private string NightLine()
        {
            if (!_game.DuskBegun)
            {
                return _game.DuskVisited ? OpeningKeys.DockForDusk : OpeningKeys.VisitSite;
            }

            return !_game.Knows(StructureIds.BatteryBank) ? OpeningKeys.ScanModule
                : _game.Built(StructureIds.BatteryBank) ? OpeningKeys.MoveBatteryBank
                : _game.ModuleHome ? OpeningKeys.BuildBatteryBank
                : OpeningKeys.TowModuleHome;
        }

        /// <summary>The key of the line the current step would say, or empty when it has none (not shown).</summary>
        public string Current => _steps.Current;

        /// <summary>The key of the line on screen now, or empty.</summary>
        public string Shown { get; private set; } = string.Empty;

        /// <summary>Why the line on screen shows (meaningless while <see cref="Shown"/> is empty).</summary>
        public NextLineReason ShownReason { get; private set; }

        /// <summary>The first step not yet done (the step count once the opening is over).</summary>
        public int CurrentIndex => _steps.CurrentIndex;

        /// <summary>True once <paramref name="milestone"/> has been reached (or replayed from a loaded game).</summary>
        public bool HasReached(OpeningMilestone milestone)
        {
            return _reached[(int)milestone];
        }

        /// <summary>
        /// The session starts (after the save is applied): the milestones a loaded game has reached are replayed with
        /// Restored = true, so the sky and the protocol line start from them with no transition.
        /// </summary>
        public void Announce()
        {
            _announced = true;
            Reach(true);
        }

        /// <summary>One tick with no time passing (the steps and milestones only).</summary>
        public void Tick()
        {
            Tick(0f);
        }

        /// <param name="deltaTime">Seconds since the last tick.</param>
        public void Tick(float deltaTime)
        {
            if (!_announced)
            {
                Announce();
            }

            _steps.Tick();
            Speak(deltaTime);
            Reach(false);
        }

        /// <summary>Publishes each derived milestone the game now holds and the flow has not yet reached.</summary>
        private void Reach(bool restored)
        {
            for (int i = 0; i < Derived.Length; i++)
            {
                OpeningMilestone milestone = Derived[i];
                if (!_reached[(int)milestone] && Holds(milestone))
                {
                    _reached[(int)milestone] = true;
                    _events.Publish(new OpeningMilestoneReached(milestone, restored));
                }
            }
        }

        /// <summary>
        /// Progress (a new step, or the step's line changing) shows the new line as the next line, unless it is the
        /// line a session starts on; the end of a new game's wake shows the first line the same way. With no progress
        /// the line counts up to a reminder: after the stall, and once more after the second wait; a line on screen goes
        /// when its time is up.
        /// </summary>
        private void Speak(float deltaTime)
        {
            int index = _steps.CurrentIndex;
            string key = _steps.Current;
            if (_woke && _watching && index == _watchedIndex && key == _watchedKey)
            {
                _woke = false;
                if (key.Length > 0)
                {
                    Show(key, NextLineReason.Next, _timing.NextLineSeconds);
                    return;
                }
            }

            if (index != _watchedIndex || key != _watchedKey)
            {
                bool progress = _watching;
                _watching = true;
                _watchedIndex = index;
                _watchedKey = key;
                _stalled = 0f;
                _reminders = 0;
                if (progress && key.Length > 0)
                {
                    Show(key, NextLineReason.Next, _timing.NextLineSeconds);
                }
                else
                {
                    Clear();
                }

                return;
            }

            if (Shown.Length > 0)
            {
                _shownFor += deltaTime;
                if (_shownFor >= _showSeconds)
                {
                    Clear();
                }

                return;
            }

            if (key.Length == 0 || _reminders >= MaxReminders)
            {
                return;
            }

            _stalled += deltaTime;
            if (_stalled >= (_reminders == 0 ? _timing.StallSeconds : _timing.AgainSeconds))
            {
                _stalled = 0f;
                _reminders++;
                Show(key, NextLineReason.Reminder, _timing.ReminderSeconds);
            }
        }

        private void Show(string key, NextLineReason reason, float seconds)
        {
            Shown = key;
            ShownReason = reason;
            _shownFor = 0f;
            _showSeconds = seconds;
            RoverAction action = OpeningKeys.KeyOf(key);
            bool showsKey = action != default && !_keys.UsedWithin(action, _timing.KeyFreshSeconds);
            bool hasTarget = _game.TryGetPlace(key, out string placeId, out Vector3 place);
            _events.Publish(new NextStepChanged(key, reason, action, showsKey, hasTarget, placeId, place));
        }

        private void Clear()
        {
            if (Shown.Length == 0)
            {
                return;
            }

            Shown = string.Empty;
            _events.Publish(new NextStepChanged(string.Empty));
        }

        private bool Holds(OpeningMilestone milestone)
        {
            switch (milestone)
            {
                case OpeningMilestone.FirstCharge:
                    return _game.FirstCharge;
                case OpeningMilestone.PowerOn:
                    return _game.PowerOn;
                case OpeningMilestone.NightReady:
                    return _game.NightReady;
                case OpeningMilestone.SignalWest:
                    return _game.Built(StructureIds.AntennaMast);
                default:
                    throw new ArgumentOutOfRangeException(nameof(milestone), milestone,
                        "Not a milestone the opening derives.");
            }
        }
    }
}
