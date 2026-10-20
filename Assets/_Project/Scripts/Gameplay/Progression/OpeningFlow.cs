using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The opening's objective flow (docs/design/opening-v2.md §3, opening-v3 slice 1.5): the first step not yet done,
    /// every step read from the game (<see cref="IOpeningProgress"/>) and never stored. The world leads, so the step's
    /// line is only a stall reminder: it is published through <see cref="NextStepChanged"/> after
    /// <c>stallSeconds</c> with no progress and 07 still, once per step, held for <c>reminderSeconds</c> and never
    /// shown again for that step. A step whose line would say something the world has not set up yet has no line
    /// (B7's for the spent lander reserve; B11's for nightfall), and B7's rack line waits for the cutter to sputter.
    /// It also publishes <see cref="OpeningMilestoneReached"/> as the opening reaches FirstCharge (B6), PowerOn (B8),
    /// NightReady (B9) and SignalWest (B11), the milestones it reads from the game; <see cref="Announce"/> replays the
    /// ones a loaded game has reached, with Restored = true, before the first <see cref="Tick"/>. CallAnswered (R8) is
    /// published by the answer itself, not derived here. Allocation-free per tick.
    /// </summary>
    public sealed class OpeningFlow
    {
        /// <summary>Seconds with no progress and 07 still after which a step's line is shown once.</summary>
        public const float DefaultStallSeconds = 120f;

        /// <summary>Seconds a reminder line stays up.</summary>
        public const float DefaultReminderSeconds = 20f;

        private static readonly int MilestoneCount = Enum.GetValues(typeof(OpeningMilestone)).Length;

        /// <summary>The milestones this flow reads from the game, in the order the opening reaches them.</summary>
        private static readonly OpeningMilestone[] Derived =
        {
            OpeningMilestone.FirstCharge, OpeningMilestone.PowerOn, OpeningMilestone.NightReady,
            OpeningMilestone.SignalWest,
        };

        private readonly EventBus _events;
        private readonly IOpeningProgress _game;
        private readonly StepFlow _steps;
        private readonly bool[] _reached = new bool[MilestoneCount];
        private readonly float _stallSeconds;
        private readonly float _reminderSeconds;
        private bool _announced;
        private int _watchedIndex = -1;
        private string _watchedKey = string.Empty;
        private int _remindedIndex = -1;
        private float _stalled;
        private float _shownFor;

        public OpeningFlow(EventBus events, IOpeningProgress game, float stallSeconds = DefaultStallSeconds,
            float reminderSeconds = DefaultReminderSeconds)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _stallSeconds = stallSeconds;
            _reminderSeconds = reminderSeconds;

            // The steps keep to themselves: the player is shown a line only as a stall reminder (see Tick).
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
            // R5, the first free choice, says nothing: the near sites are the lure. Night is coming once dusk begins.
            _steps.Add(() => !_game.DuskBegun ? string.Empty
                    : _game.Built(StructureIds.BatteryBank) ? OpeningKeys.MoveBatteryBank
                    : OpeningKeys.BuildBatteryBank, () => _game.NightReady);
            _steps.Add(() => _game.NightFallen ? OpeningKeys.ScanTower : string.Empty,
                () => _game.Knows(StructureIds.AntennaMast));
            _steps.Add(OpeningKeys.BuildMast, () => _game.Built(StructureIds.AntennaMast));
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

        /// <summary>The key of the line the current step would say, or empty when it has none (not shown).</summary>
        public string Current => _steps.Current;

        /// <summary>The key of the reminder line on screen now, or empty.</summary>
        public string Shown { get; private set; } = string.Empty;

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
            Tick(0f, false);
        }

        /// <param name="deltaTime">Seconds since the last tick.</param>
        /// <param name="still">07 stands still: only then does a stall build up.</param>
        public void Tick(float deltaTime, bool still)
        {
            if (!_announced)
            {
                Announce();
            }

            _steps.Tick();
            StepReminder(deltaTime, still);
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
        /// A step that holds its line for <see cref="_stallSeconds"/> of 07 standing still shows it once; moving, or
        /// any progress (a new step, a new sub-step), starts the count again, and a step that was reminded is never
        /// reminded twice.
        /// </summary>
        private void StepReminder(float deltaTime, bool still)
        {
            int index = _steps.CurrentIndex;
            string key = _steps.Current;
            if (index != _watchedIndex || key != _watchedKey)
            {
                _watchedIndex = index;
                _watchedKey = key;
                _stalled = 0f;
                Show(string.Empty);
                return;
            }

            if (Shown.Length > 0)
            {
                _shownFor += deltaTime;
                if (_shownFor >= _reminderSeconds)
                {
                    Show(string.Empty);
                }

                return;
            }

            if (!still)
            {
                _stalled = 0f;
                return;
            }

            _stalled += deltaTime;
            if (_stalled >= _stallSeconds && key.Length > 0 && index != _remindedIndex)
            {
                _remindedIndex = index;
                _shownFor = 0f;
                Show(key);
            }
        }

        private void Show(string key)
        {
            if (key == Shown)
            {
                return;
            }

            Shown = key;
            _events.Publish(new NextStepChanged(key));
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
