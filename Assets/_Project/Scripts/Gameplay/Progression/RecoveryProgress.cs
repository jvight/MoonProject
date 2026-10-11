using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// <see cref="IRecoveryProgress"/> over what the game already holds (docs/features/M3-21 §1): Power lights a lamp
    /// per tower level, Link one per lit chain mast (the Peak's foot lamp waits for a later version), the Dish and
    /// Answer stay dark in this version. <see cref="Refresh"/> publishes <see cref="RecoveryProgressChanged"/> for each
    /// row whose lit lamps changed; <see cref="Announce"/> (after a save is applied) takes the state silently and
    /// publishes every row once with Restored = true. Nothing allocates per frame.
    /// </summary>
    public sealed class RecoveryProgress : IRecoveryProgress
    {
        private readonly EventBus _events;
        private readonly Func<int> _towerLevel;
        private readonly Func<int> _litChainMasts;
        private readonly int[] _lamps;
        private readonly int[] _lit;

        /// <param name="towerLevel">The tower level on show.</param>
        /// <param name="towerLevels">The tower's levels: Power's lamps.</param>
        /// <param name="litChainMasts">Lit masts of the Link's chain.</param>
        /// <param name="lamps">Each row's lamps beyond what the station itself counts.</param>
        public RecoveryProgress(EventBus events, Func<int> towerLevel, int towerLevels, Func<int> litChainMasts,
            int chainMasts, RecoveryLamps lamps)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _towerLevel = towerLevel ?? throw new ArgumentNullException(nameof(towerLevel));
            _litChainMasts = litChainMasts ?? throw new ArgumentNullException(nameof(litChainMasts));
            if (towerLevels < 1 || chainMasts < 0)
            {
                throw new ArgumentException($"Power needs at least one tower level (got {towerLevels}) and Link a " +
                                            $"chain of zero or more masts (got {chainMasts}).");
            }

            int count = Enum.GetValues(typeof(RecoveryTarget)).Length;
            _lamps = new int[count];
            _lit = new int[count];
            _lamps[(int)RecoveryTarget.Power] = towerLevels;
            _lamps[(int)RecoveryTarget.Link] = chainMasts + lamps.PeakFoot;
            _lamps[(int)RecoveryTarget.Dish] = lamps.Dish;
            _lamps[(int)RecoveryTarget.Answer] = lamps.Answer;
        }

        public int LitLamps(RecoveryTarget target)
        {
            return _lit[(int)target];
        }

        public int Lamps(RecoveryTarget target)
        {
            return _lamps[(int)target];
        }

        public bool IsComplete(RecoveryTarget target)
        {
            int index = (int)target;
            return _lamps[index] > 0 && _lit[index] >= _lamps[index];
        }

        /// <summary>Publishes each row whose lit lamps changed since the last call.</summary>
        public void Refresh()
        {
            for (int i = 0; i < _lit.Length; i++)
            {
                int lit = Derive(i);
                if (lit == _lit[i])
                {
                    continue;
                }

                _lit[i] = lit;
                _events.Publish(new RecoveryProgressChanged((RecoveryTarget)i, lit, _lamps[i], false));
            }
        }

        /// <summary>Takes a loaded game's rows silently and tells everyone each row once (no moments).</summary>
        public void Announce()
        {
            for (int i = 0; i < _lit.Length; i++)
            {
                _lit[i] = Derive(i);
            }

            for (int i = 0; i < _lit.Length; i++)
            {
                _events.Publish(new RecoveryProgressChanged((RecoveryTarget)i, _lit[i], _lamps[i], true));
            }
        }

        private int Derive(int index)
        {
            int lit;
            switch ((RecoveryTarget)index)
            {
                case RecoveryTarget.Power:
                    lit = _towerLevel();
                    break;
                case RecoveryTarget.Link:
                    lit = _litChainMasts();
                    break;
                default:
                    lit = 0;
                    break;
            }

            return Math.Max(0, Math.Min(lit, _lamps[index]));
        }
    }
}
