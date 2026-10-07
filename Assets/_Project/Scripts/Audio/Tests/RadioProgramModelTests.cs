using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.Audio.Tests
{
    public sealed class RadioProgramModelTests
    {
        private FakeRadioProgram _program;
        private RadioProgramModel _model;

        [SetUp]
        public void SetUp()
        {
            _program = new FakeRadioProgram(3);
            _model = new RadioProgramModel(_program.TotalTapeCount);
        }

        [Test]
        public void FirstRead_SetsTheStateSilently()
        {
            _program.DialUnlocked = true;
            _program.Channel = RadioChannel.TapeDeck;
            _program.SelectedTape = "slow_orbit";
            _program.Own("after_dark_1");
            _program.Own("slow_orbit");

            Assert.AreEqual(RadioProgramUpdate.None, _model.Apply(_program, true, out bool announce));
            Assert.IsFalse(announce, "a loaded save is never heard as a dial turn");
            Assert.AreEqual(RadioChannel.TapeDeck, _model.Station);
            Assert.AreEqual("slow_orbit", _model.SelectedTape);
            Assert.AreEqual(2, _model.OwnedCount);
            Assert.AreEqual("after_dark_1", _model.GetOwned(0));
            Assert.AreEqual("slow_orbit", _model.GetOwned(1));
        }

        [Test]
        public void BeforeBellIsRepaired_ItIsAlwaysLumenAfterDark_ButTapesStillJoinTheShow()
        {
            _program.Channel = RadioChannel.QuietHours;
            _model.Apply(_program, true, out _);
            Assert.AreEqual(RadioChannel.LumenAfterDark, _model.Station);

            _program.Own("after_dark_1");
            Assert.AreEqual(RadioProgramUpdate.Pool, _model.Apply(_program, true, out bool announce));
            Assert.IsFalse(announce);
            Assert.AreEqual(RadioChannel.LumenAfterDark, _model.Station);
            Assert.AreEqual(1, _model.OwnedCount);
        }

        [Test]
        public void UnlockingTheDial_OnLumenAfterDark_ChangesNothingHeard()
        {
            _model.Apply(_program, true, out _);
            _program.DialUnlocked = true;
            Assert.AreEqual(RadioProgramUpdate.None, _model.Apply(_program, true, out bool announce));
            Assert.IsFalse(announce);
            Assert.IsTrue(_model.DialUnlocked);
        }

        [Test]
        public void TurningTheDial_IsHeardOnlyWhileTheRadioIsOn()
        {
            _program.DialUnlocked = true;
            _model.Apply(_program, false, out _);

            _program.Channel = RadioChannel.QuietHours;
            Assert.AreEqual(RadioProgramUpdate.Station, _model.Apply(_program, false, out bool announce));
            Assert.IsFalse(announce, "before the radio comes on the station is just set");
            Assert.AreEqual(RadioChannel.QuietHours, _model.Station);

            _program.Channel = RadioChannel.LumenAfterDark;
            Assert.AreEqual(RadioProgramUpdate.Station, _model.Apply(_program, true, out announce));
            Assert.IsTrue(announce, "out of Quiet Hours: click and swish");

            Assert.AreEqual(RadioProgramUpdate.None, _model.Apply(_program, true, out announce));
            Assert.IsFalse(announce, "re-reading the same program is not a turn");
        }

        [Test]
        public void TapeDeck_ChangingTheTapeIsAStationChange_ElsewhereItIsNot()
        {
            _program.DialUnlocked = true;
            _model.Apply(_program, true, out _);

            _program.SelectedTape = "dust_and_honey";
            Assert.AreEqual(RadioProgramUpdate.None, _model.Apply(_program, true, out _),
                "choosing a tape while on the show changes nothing heard");

            _program.Channel = RadioChannel.TapeDeck;
            Assert.AreEqual(RadioProgramUpdate.Station, _model.Apply(_program, true, out bool announce));
            Assert.IsTrue(announce);

            _program.SelectedTape = "slow_orbit";
            Assert.AreEqual(RadioProgramUpdate.Station, _model.Apply(_program, true, out announce));
            Assert.IsTrue(announce);
            Assert.AreEqual("slow_orbit", _model.SelectedTape);
        }

        [Test]
        public void ANewTapeAndADialTurn_TogetherReportBoth()
        {
            _program.DialUnlocked = true;
            _model.Apply(_program, true, out _);
            _program.Own("slow_orbit");
            _program.Channel = RadioChannel.TapeDeck;
            _program.SelectedTape = "slow_orbit";

            RadioProgramUpdate update = _model.Apply(_program, true, out bool announce);
            Assert.AreEqual(RadioProgramUpdate.Pool | RadioProgramUpdate.Station, update);
            Assert.IsTrue(announce);
        }

        [Test]
        public void RelockingTheDial_ReturnsToTheShow_Unannounced()
        {
            _program.DialUnlocked = true;
            _program.Channel = RadioChannel.QuietHours;
            _model.Apply(_program, true, out _);

            _program.DialUnlocked = false;
            Assert.AreEqual(RadioProgramUpdate.Station, _model.Apply(_program, true, out bool announce));
            Assert.IsFalse(announce);
            Assert.AreEqual(RadioChannel.LumenAfterDark, _model.Station);
        }

        [Test]
        public void OwnedTapes_AreCappedAtTheProgramsTotal()
        {
            var model = new RadioProgramModel(1);
            _program.Own("a");
            _program.Own("b");
            model.Apply(_program, true, out _);
            Assert.AreEqual(1, model.OwnedCount);
            Assert.AreEqual("a", model.GetOwned(0));
        }
    }
}
