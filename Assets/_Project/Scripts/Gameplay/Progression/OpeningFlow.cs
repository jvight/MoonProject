using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The opening's objective flow (docs/design/opening-v2.md §3, ObjectiveFlow v2): one line at a time, the first
    /// step not yet done, every step read from the game (<see cref="IOpeningProgress"/>) and never stored, published
    /// through <see cref="NextStepChanged"/> by its <see cref="StepFlow"/>. A step whose line would say something the
    /// world has not set up yet shows no line (B7's line waits for the spent lander reserve; B11's for nightfall).
    /// It also publishes <see cref="OpeningMilestoneReached"/> as the opening reaches FirstCharge (B6), PowerOn (B8),
    /// NightReady (B9) and SignalWest (B11); <see cref="Announce"/> replays the ones a loaded game has reached, with
    /// Restored = true, before the first <see cref="Tick"/>. Allocation-free per tick.
    /// </summary>
    public sealed class OpeningFlow
    {
        private static readonly int MilestoneCount = Enum.GetValues(typeof(OpeningMilestone)).Length;

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
            _steps.Add(OpeningKeys.CutCrates,
                () => _game.Covers(StructureIds.ChargingPad) || _game.Built(StructureIds.ChargingPad));
            _steps.Add(OpeningKeys.BuildPad, () => _game.Built(StructureIds.ChargingPad));
            _steps.Add(OpeningKeys.Charge, () => _game.FirstCharge);
            _steps.Add(() => _game.ReserveSpent ? OpeningKeys.FindSolar : string.Empty,
                () => _game.Knows(StructureIds.SolarArray));
            _steps.Add(() => _game.Covers(StructureIds.SolarArray) ? OpeningKeys.BuildSolar : OpeningKeys.CutOptics,
                () => _game.PowerOn);
            _steps.Add(OpeningKeys.BuildBatteryBank, () => _game.NightReady);
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
            for (int i = 0; i < MilestoneCount; i++)
            {
                if (!_reached[i] && Holds((OpeningMilestone)i))
                {
                    _reached[i] = true;
                    _events.Publish(new OpeningMilestoneReached((OpeningMilestone)i, true));
                }
            }
        }

        public void Tick()
        {
            if (!_announced)
            {
                Announce();
            }

            _steps.Tick();
            for (int i = 0; i < MilestoneCount; i++)
            {
                if (!_reached[i] && Holds((OpeningMilestone)i))
                {
                    _reached[i] = true;
                    _events.Publish(new OpeningMilestoneReached((OpeningMilestone)i, false));
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
                    throw new ArgumentOutOfRangeException(nameof(milestone), milestone, "Not an opening milestone.");
            }
        }
    }
}
