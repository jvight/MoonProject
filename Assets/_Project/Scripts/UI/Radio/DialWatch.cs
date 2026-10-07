using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// Tells a turn of Bell's dial from the radio program merely being loaded or growing (pure logic, EditMode-tested).
    /// <see cref="RadioProgramChanged"/> also arrives when a save loads, when the dial unlocks and when a tape is
    /// collected. Only another station, or another tape on the Tape Deck, while the dial is unlocked and once the
    /// watch is armed (the UI's first frame, after every load) is a turn worth the readout.
    /// </summary>
    internal sealed class DialWatch
    {
        private RadioChannel _channel;
        private string _tape;

        public bool IsArmed { get; private set; }

        /// <summary>Takes the program as it is now (the loaded state) as the baseline and starts watching.</summary>
        public void Arm(IRadioProgram program)
        {
            Remember(program);
            IsArmed = true;
        }

        /// <summary>Call on every program change: true when it was a turn of the dial worth the readout.</summary>
        public bool Changed(IRadioProgram program)
        {
            RadioChannel channel = _channel;
            string tape = _tape;
            Remember(program);
            return IsArmed && program.DialUnlocked && (_channel != channel ||
                _channel == RadioChannel.TapeDeck && !string.Equals(_tape, tape, StringComparison.Ordinal));
        }

        private void Remember(IRadioProgram program)
        {
            if (program == null)
            {
                throw new ArgumentNullException(nameof(program));
            }

            _channel = program.Channel;
            _tape = program.SelectedTape;
        }
    }
}
