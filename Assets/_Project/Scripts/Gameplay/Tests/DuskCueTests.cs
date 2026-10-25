using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The first dusk follows progress (docs/design/opening-v3-payoff-audit.md §4, R5 merged into B8): after power-on it
    /// begins at the first of 07 docking, wherever it has been, or 07 learning the battery bank from the power module;
    /// never before power-on, only once; a loaded game replays a begun dusk as Restored.
    /// </summary>
    public sealed class DuskCueTests
    {
        private static readonly Vector3 Pad = Vector3.zero;

        private readonly List<OpeningDuskBegan> _began = new List<OpeningDuskBegan>();
        private EventBus _events;
        private DuskCue _cue;

        [SetUp]
        public void SetUp()
        {
            _began.Clear();
            _events = new EventBus();
            _events.Subscribe<OpeningDuskBegan>(_began.Add);
            _cue = new DuskCue(_events);
        }

        [TearDown]
        public void TearDown()
        {
            _cue.Dispose();
        }

        [Test]
        public void TheFirstDockAfterPowerOn_BeginsTheDusk_Once()
        {
            _cue.Step(true, false);
            Assert.IsFalse(_cue.Begun, "power on, 07 out roaming: the afternoon holds");

            Dock();
            _cue.Step(true, false);
            Assert.IsTrue(_cue.Begun, "the next dock ends the day");
            Assert.AreEqual(1, _began.Count);
            Assert.IsFalse(_began[0].Restored, "begun now, not replayed");

            Dock();
            _cue.Step(true, true);
            Assert.AreEqual(1, _began.Count, "only once");
        }

        [Test]
        public void ScanningThePowerModule_BeginsTheDuskAtOnce_WithNoDock()
        {
            _cue.Step(true, true);
            Assert.IsTrue(_cue.Begun, "the battery bank learned: night is the next goal");
            Assert.AreEqual(1, _began.Count);
        }

        [Test]
        public void ADockOrTheBankLearned_BeforePowerOn_BeginsNothing_TheBankBeginsItTheMomentThePowerComesOn()
        {
            Dock();
            _cue.Step(false, true);
            Assert.IsFalse(_cue.Begun, "before power-on the afternoon comes first");
            _cue.Step(true, false);
            Assert.IsFalse(_cue.Begun, "a dock before power-on does not carry over");
            _cue.Step(true, true);
            Assert.IsTrue(_cue.Begun, "the bank already known begins the dusk the moment the base is powered");
        }

        [Test]
        public void ALoadedGame_ReplaysABegunDuskAsRestored_AndNeverBeginsItAgain()
        {
            Dock();
            _cue.Step(true, false);
            var begun = new OpeningSaveData();
            _cue.Capture(begun);

            _began.Clear();
            var reloaded = new DuskCue(_events);
            reloaded.Restore(begun);
            reloaded.Announce();
            Assert.AreEqual(1, _began.Count, "a begun dusk is stated at the session start");
            Assert.IsTrue(_began[0].Restored, "replayed, so the sky snaps to it");
            Dock();
            reloaded.Step(true, true);
            Assert.AreEqual(1, _began.Count, "and never begins again");
            reloaded.Dispose();

            var fresh = new DuskCue(_events);
            fresh.Restore(new OpeningSaveData());
            fresh.Announce();
            Assert.AreEqual(1, _began.Count, "a game not yet at dusk states nothing");
            fresh.Dispose();
        }

        private void Dock()
        {
            _events.Publish(new RoverDockChanged(true, Pad, Quaternion.identity));
        }
    }
}
