using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How far power has come back through the base (docs/features/M3-16): each radio tower level wakes one more
    /// stage, L1 home, L2 the bay, L3 the lift; a tower with no level leaves the base asleep. A level landing in play
    /// announces its stage once (<see cref="BasePowerChanged"/>); a restored game is announced once at start with
    /// Restored = true, so listeners apply it at once with no wake moment.
    /// </summary>
    public sealed class BasePower : IBasePower
    {
        private readonly EventBus _events;

        public BasePower(EventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public BasePowerStage Stage { get; private set; }

        public bool HasReached(BasePowerStage stage)
        {
            return Stage >= stage;
        }

        /// <summary>The stage tower level <paramref name="towerLevel"/> powers (levels count like stages).</summary>
        public static BasePowerStage StageFor(int towerLevel)
        {
            return towerLevel <= 0 ? BasePowerStage.Asleep
                : towerLevel >= (int)BasePowerStage.Chart ? BasePowerStage.Chart
                : (BasePowerStage)towerLevel;
        }

        /// <summary>The tower shows <paramref name="towerLevel"/> in play: a new stage wakes, once.</summary>
        public void Show(int towerLevel)
        {
            BasePowerStage stage = StageFor(towerLevel);
            if (stage == Stage)
            {
                return;
            }

            Stage = stage;
            _events.Publish(new BasePowerChanged(stage, false));
        }

        /// <summary>Takes the loaded tower level's stage silently; <see cref="Announce"/> tells the others.</summary>
        public void Restore(int towerLevel)
        {
            Stage = StageFor(towerLevel);
        }

        /// <summary>Tells everyone the stage the game starts in (Restored = true: no wake moment).</summary>
        public void Announce()
        {
            _events.Publish(new BasePowerChanged(Stage, true));
        }
    }
}
