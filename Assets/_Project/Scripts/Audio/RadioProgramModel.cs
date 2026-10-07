using System;
using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>What re-reading the radio program changed.</summary>
    [Flags]
    public enum RadioProgramUpdate
    {
        None = 0,

        /// <summary>The set of owned tapes changed: rebuild the shuffle pool.</summary>
        Pool = 1,

        /// <summary>The station the player hears changed (channel, or the Tape Deck's tape).</summary>
        Station = 2,
    }

    /// <summary>
    /// The radio's view of <see cref="IRadioProgram"/>: the station actually heard (always Lumen After Dark until
    /// the dial is unlocked), the chosen tape and the owned tapes in collection order. <see cref="Apply"/> re-reads
    /// the program, reports what changed and whether a station change should be heard as a dial turn (click and
    /// swish): only after the first read, while the radio is on and the dial is unlocked. Boot, a save load and
    /// changes before the radio comes on just set the state. Allocation-free after construction.
    /// </summary>
    public sealed class RadioProgramModel
    {
        private readonly string[] _owned;
        private bool _applied;

        /// <param name="tapeCapacity">Most tapes that can ever be owned (the program's TotalTapeCount).</param>
        public RadioProgramModel(int tapeCapacity)
        {
            if (tapeCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tapeCapacity), tapeCapacity, "Cannot be negative.");
            }

            _owned = new string[tapeCapacity];
        }

        public bool DialUnlocked { get; private set; }

        /// <summary>The station heard: <see cref="RadioChannel.LumenAfterDark"/> whenever the dial is locked.</summary>
        public RadioChannel Station { get; private set; } = RadioChannel.LumenAfterDark;

        /// <summary>The Tape Deck's cassette id (empty when none is chosen).</summary>
        public string SelectedTape { get; private set; } = string.Empty;

        public int OwnedCount { get; private set; }

        public string GetOwned(int index)
        {
            return _owned[index];
        }

        /// <summary>Reads <paramref name="program"/>; returns what changed since the last apply (None the first time).
        /// <paramref name="announce"/> is true when a station change should be heard as a dial turn.</summary>
        public RadioProgramUpdate Apply(IRadioProgram program, bool radioOn, out bool announce)
        {
            announce = false;
            if (program == null)
            {
                throw new ArgumentNullException(nameof(program));
            }

            RadioProgramUpdate update = RadioProgramUpdate.None;
            int owned = Math.Min(program.OwnedTapeCount, _owned.Length);
            bool poolChanged = owned != OwnedCount;
            for (int i = 0; i < owned; i++)
            {
                string id = program.GetOwnedTape(i) ?? string.Empty;
                if (!string.Equals(_owned[i], id, StringComparison.Ordinal))
                {
                    _owned[i] = id;
                    poolChanged = true;
                }
            }

            OwnedCount = owned;
            DialUnlocked = program.DialUnlocked;
            RadioChannel station = DialUnlocked ? program.Channel : RadioChannel.LumenAfterDark;
            string tape = program.SelectedTape ?? string.Empty;
            bool stationChanged = station != Station ||
                                  (station == RadioChannel.TapeDeck &&
                                   !string.Equals(tape, SelectedTape, StringComparison.Ordinal));
            Station = station;
            SelectedTape = tape;
            if (poolChanged)
            {
                update |= RadioProgramUpdate.Pool;
            }

            if (stationChanged)
            {
                update |= RadioProgramUpdate.Station;
            }

            if (!_applied)
            {
                _applied = true;
                return RadioProgramUpdate.None;
            }

            announce = stationChanged && radioOn && DialUnlocked;
            return update;
        }
    }
}
