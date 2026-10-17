using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// <see cref="IRecoveryProgress"/> over what the game already holds (docs/features/M3-21 §1, opening-v2 §5): Power
    /// lights a lamp per built step of the base's power (the pad, solar power on, a night-ready base); Link, Dish and
    /// Answer stay dark through the opening. <see cref="Refresh"/> publishes <see cref="RecoveryProgressChanged"/> for
    /// a row whose lit lamps changed; <see cref="Announce"/> (after a save is applied) takes the state silently and
    /// publishes every row once with Restored = true. Nothing allocates per frame.
    /// </summary>
    public sealed class RecoveryProgress : IRecoveryProgress
    {
        private readonly EventBus _events;
        private readonly Func<int> _powerLit;
        private readonly int[] _lamps;
        private readonly int[] _lit;

        /// <param name="powerLit">The base's power steps built now.</param>
        /// <param name="powerLamps">Power's lamps: every step of the base's power.</param>
        /// <param name="lamps">The other rows' lamps.</param>
        public RecoveryProgress(EventBus events, Func<int> powerLit, int powerLamps, RecoveryLamps lamps)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _powerLit = powerLit ?? throw new ArgumentNullException(nameof(powerLit));
            if (powerLamps < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(powerLamps), powerLamps, "Power needs a lamp.");
            }

            int count = Enum.GetValues(typeof(RecoveryTarget)).Length;
            _lamps = new int[count];
            _lit = new int[count];
            _lamps[(int)RecoveryTarget.Power] = powerLamps;
            _lamps[(int)RecoveryTarget.Link] = lamps.Link;
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

        /// <summary>Publishes Power's row when its lit lamps changed since the last call.</summary>
        public void Refresh()
        {
            int power = (int)RecoveryTarget.Power;
            int lit = PowerLit();
            if (lit == _lit[power])
            {
                return;
            }

            _lit[power] = lit;
            _events.Publish(new RecoveryProgressChanged(RecoveryTarget.Power, lit, _lamps[power], false));
        }

        /// <summary>Takes a loaded game's rows silently and tells everyone each row once (no moments).</summary>
        public void Announce()
        {
            _lit[(int)RecoveryTarget.Power] = PowerLit();
            for (int i = 0; i < _lit.Length; i++)
            {
                _events.Publish(new RecoveryProgressChanged((RecoveryTarget)i, _lit[i], _lamps[i], true));
            }
        }

        private int PowerLit()
        {
            return Math.Max(0, Math.Min(_powerLit(), _lamps[(int)RecoveryTarget.Power]));
        }
    }
}
