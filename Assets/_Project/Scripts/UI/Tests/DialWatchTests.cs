using System;
using System.Collections.Generic;
using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// When the dial readout answers: a turn of Bell's dial made while the game runs, never the program being loaded,
    /// the dial unlocking or a tape being collected; and every station has a name to show.
    /// </summary>
    public sealed class DialWatchTests
    {
        private Program _program;
        private DialWatch _watch;

        [SetUp]
        public void SetUp()
        {
            _program = new Program();
            _watch = new DialWatch();
        }

        [Test]
        public void TheProgramLoadedWithASave_IsNotATurn()
        {
            _program.DialUnlocked = true;
            _program.Channel = RadioChannel.TapeDeck;
            _program.SelectedTape = "slow_orbit";
            Assert.IsFalse(_watch.Changed(_program), "the save loaded before the UI's first frame");

            _watch.Arm(_program);
            Assert.IsFalse(_watch.Changed(_program), "the loaded state is the baseline, not news");
        }

        [Test]
        public void AnotherStation_IsATurn()
        {
            _program.DialUnlocked = true;
            _watch.Arm(_program);
            _program.Channel = RadioChannel.QuietHours;
            Assert.IsTrue(_watch.Changed(_program));
            Assert.IsFalse(_watch.Changed(_program), "the same station twice is one turn");
            _program.Channel = RadioChannel.LumenAfterDark;
            Assert.IsTrue(_watch.Changed(_program));
        }

        [Test]
        public void AnotherTape_IsATurnOnlyOnTheTapeDeck()
        {
            _program.DialUnlocked = true;
            _program.Channel = RadioChannel.TapeDeck;
            _program.SelectedTape = "after_dark_1";
            _watch.Arm(_program);

            _program.SelectedTape = "dust_and_honey";
            Assert.IsTrue(_watch.Changed(_program), "a new tape in the deck");

            _program.Channel = RadioChannel.LumenAfterDark;
            Assert.IsTrue(_watch.Changed(_program));
            _program.SelectedTape = "slow_orbit";
            Assert.IsFalse(_watch.Changed(_program), "the deck's choice changing behind another station is quiet");
        }

        [Test]
        public void ACollectedTape_IsNotATurn()
        {
            _program.DialUnlocked = true;
            _program.Channel = RadioChannel.TapeDeck;
            _program.SelectedTape = "after_dark_1";
            _watch.Arm(_program);
            _program.Tapes.Add("dust_and_honey");
            Assert.IsFalse(_watch.Changed(_program));
        }

        [Test]
        public void ALockedDial_ShowsNothing_AndUnlockingItIsNotATurn()
        {
            _watch.Arm(_program);
            _program.Channel = RadioChannel.QuietHours;
            Assert.IsFalse(_watch.Changed(_program), "before Bell is repaired the dial does not exist");

            _program.Channel = RadioChannel.LumenAfterDark;
            _watch.Changed(_program);
            _program.DialUnlocked = true;
            Assert.IsFalse(_watch.Changed(_program), "Bell waking unlocks the dial on the default station");
        }

        [Test]
        public void EveryStation_HasItsOwnNameKey()
        {
            var keys = new HashSet<string>();
            foreach (RadioChannel channel in Enum.GetValues(typeof(RadioChannel)))
            {
                string key = UiKeys.RadioChannelName(channel);
                StringAssert.StartsWith("radio.channel.", key);
                Assert.IsTrue(keys.Add(key), $"{channel} shares its key");
            }
        }

        private sealed class Program : IRadioProgram
        {
            public List<string> Tapes { get; } = new List<string>();

            public bool DialUnlocked { get; set; }

            public RadioChannel Channel { get; set; }

            public string SelectedTape { get; set; } = string.Empty;

            public int OwnedTapeCount => Tapes.Count;

            public int TotalTapeCount => 3;

            public string GetOwnedTape(int index)
            {
                return Tapes[index];
            }
        }
    }
}
