using System.Collections.Generic;
using MoonProject.Core;

namespace MoonProject.Audio.Tests
{
    /// <summary>An <see cref="IRadioProgram"/> whose dial and owned tapes the test sets directly.</summary>
    internal sealed class FakeRadioProgram : IRadioProgram
    {
        private readonly List<string> _owned = new List<string>();

        public FakeRadioProgram(int totalTapes)
        {
            TotalTapeCount = totalTapes;
        }

        public bool DialUnlocked { get; set; }

        public RadioChannel Channel { get; set; }

        public string SelectedTape { get; set; } = string.Empty;

        public int OwnedTapeCount => _owned.Count;

        public int TotalTapeCount { get; }

        public string GetOwnedTape(int index)
        {
            return _owned[index];
        }

        public void Own(string cassetteId)
        {
            _owned.Add(cassetteId);
        }
    }
}
