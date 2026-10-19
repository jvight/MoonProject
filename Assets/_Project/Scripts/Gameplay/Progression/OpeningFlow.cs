using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The opening's objective flow (docs/design/opening-v2.md §3, ObjectiveFlow v2): one line at a time, the first
    /// step not yet done, every step read from the game (<see cref="IOpeningProgress"/>) and never stored, published
    /// through <see cref="NextStepChanged"/> by its <see cref="StepFlow"/>. A step whose line would say something the
    /// world has not set up yet shows no line (B6's igniter line waits for the igniter in the hold; B7's for the spent
    /// lander reserve; B11's for nightfall).
    /// It also publishes <see cref="OpeningMilestoneReached"/> as the opening reaches FirstCharge (B6), PowerOn (B8),
    /// NightReady (B9) and SignalWest (B11), the milestones it reads from the game; <see cref="Announce"/> replays the
    /// ones a loaded game has reached, with Restored = true, before the first <see cref="Tick"/>. CallAnswered (R8) is
    /// published by the answer itself, not derived here. Allocation-free per tick.
    /// </summary>
    public sealed class OpeningFlow
    {
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
        private bool _announced;

        public OpeningFlow(EventBus events, IOpeningProgress game)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _steps = new StepFlow(events);
            _steps.Add(OpeningKeys.DriveToLander, () => _game.ReachedLander || _game.Knows(StructureIds.ChargingPad));
            _steps.Add(OpeningKeys.ScanLander, () => _game.Knows(StructureIds.ChargingPad));
            _steps.Add(OpeningKeys.CollectParts,
                () => (_game.Covers(StructureIds.ChargingPad) || _game.Built(StructureIds.ChargingPad)) &&
                      (_game.CarriesIgniter || _game.IgniterFitted));
            _steps.Add(OpeningKeys.BuildPad, () => _game.Built(StructureIds.ChargingPad));
            _steps.Add(OpeningKeys.Charge, () => _game.FirstCharge);
            // The line says what the screen will accept: a field swap fits where 07 stands, a pad part on the plate.
            _steps.Add(() => !_game.CarriesIgniter ? string.Empty
                    : _game.CanFitIgniter ? OpeningKeys.FitIgniter : OpeningKeys.ParkOnPad,
                () => _game.IgniterFitted);
            _steps.Add(() => _game.ReserveSpent ? OpeningKeys.FindSolar : string.Empty,
                () => _game.Knows(StructureIds.SolarArray));
            _steps.Add(() => _game.Covers(StructureIds.SolarArray) ? OpeningKeys.BuildSolar : OpeningKeys.CutOptics,
                () => _game.PowerOn);
            _steps.Add(() => _game.Built(StructureIds.BatteryBank) ? OpeningKeys.MoveBatteryBank
                    : OpeningKeys.BuildBatteryBank, () => _game.NightReady);
            _steps.Add(() => _game.NightFallen ? OpeningKeys.ScanTower : string.Empty,
                () => _game.Knows(StructureIds.AntennaMast));
            _steps.Add(OpeningKeys.BuildMast, () => _game.Built(StructureIds.AntennaMast));
        }

        /// <summary>The objective line's key, or empty when nothing is to be said.</summary>
        public string Current => _steps.Current;

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

        public void Tick()
        {
            if (!_announced)
            {
                Announce();
            }

            _steps.Tick();
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
