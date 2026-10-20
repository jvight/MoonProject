using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The socket rack in the running game (<see cref="SocketRackField"/>): the release's event reveals it (the one
    /// source of truth; a dock alone does nothing), each bay that changes is announced once, and a loaded rack whose
    /// release is done is replayed as restored.
    /// </summary>
    public sealed class SocketRackFieldTests
    {
        private OpeningTuning _tuning;
        private EventBus _events;
        private SocketRackField _field;
        private readonly List<RackChanged> _changes = new List<RackChanged>();

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<OpeningTuning>();
            _events = new EventBus();
            _field = new SocketRackField(_events, _tuning.RackBays);
            _changes.Clear();
            _events.Subscribe<RackChanged>(_changes.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _field.Dispose();
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void TheReleasesReveal_BringsOutTheTwoSeenOutlines_AndNothingElseDoes()
        {
            _events.Publish(new OpeningMilestoneReached(OpeningMilestone.SignalWest, false));
            _events.Publish(new RoverDockChanged(true, Vector3.zero, Quaternion.identity));
            Assert.AreEqual(0, _changes.Count, "the west signal and a dock are the release's to read, not the rack's");

            Reveal();
            Assert.IsTrue(_field.Shown);
            Assert.AreEqual(2, _changes.Count, "the headlamp's and the cell pack's outlines");
            Assert.AreEqual(RackBayState.Outline, _changes[0].State);
            Assert.IsFalse(_changes[0].Restored);

            Reveal();
            Assert.AreEqual(2, _changes.Count, "a second reveal changes nothing");
        }

        [Test]
        public void AScanOfTheAdapter_AndAFittedPart_AreEachAnnouncedOnce()
        {
            Reveal();
            _changes.Clear();

            _events.Publish(new FragmentScanned("frag", "rover.hex_adapter", 0, 3, false, 0));
            _events.Publish(new FragmentScanned("frag", "rover.hex_adapter", 1, 3, false, 0));
            Assert.AreEqual(1, _changes.Count);
            Assert.AreEqual(2, _changes[0].Bay);
            Assert.AreEqual(RackBayState.Outline, _changes[0].State);

            _events.Publish(new PartFitted(RoverSocket.Lamp, RoverKitPiece.LampBar, false, false,
                RoverKitPiece.LampBar, 0));
            Assert.AreEqual(2, _changes.Count);
            Assert.AreEqual(0, _changes[1].Bay);
            Assert.AreEqual(RackBayState.Filled, _changes[1].State);
            Assert.AreEqual(RackBayState.Filled, _field.StateOf(0));
        }

        [Test]
        public void ALoadedGameWhoseReleaseIsDone_ReplaysTheRackAsRestored()
        {
            _field.Restore(new RackSaveData { seen = new string[0], filled = new[] { "rack.cell_pack" } });
            _events.Publish(new ReleaseStageChanged(ReleaseStage.Done, true));
            _field.Announce();
            Assert.AreEqual(2, _changes.Count);
            Assert.IsTrue(_changes[0].Restored && _changes[1].Restored);
            Assert.AreEqual(RackBayState.Filled, _changes[1].State);
        }

        [Test]
        public void ALoadedGameWhoseReleaseIsNotDone_KeepsTheRackHidden()
        {
            _field.Restore(new RackSaveData { seen = new string[0], filled = new[] { "rack.cell_pack" } });
            _events.Publish(new ReleaseStageChanged(ReleaseStage.RackPending, true));
            _field.Announce();
            Assert.IsFalse(_field.Shown);
            Assert.AreEqual(0, _changes.Count);
        }

        [Test]
        public void TheDefaultTuning_HasAValidRack()
        {
            Assert.IsNull(_tuning.ValidateRack());
        }

        private void Reveal()
        {
            _events.Publish(new SocketRackRevealed(Vector3.zero));
        }
    }
}
