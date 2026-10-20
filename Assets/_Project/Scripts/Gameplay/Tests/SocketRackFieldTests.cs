using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The socket rack in the running game (<see cref="SocketRackField"/>): the game's events drive it, each bay that
    /// changes is announced once, and a loaded rack is replayed as restored.
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
        public void TheWestSignal_ThenADock_BringOutTheTwoSeenOutlines()
        {
            Dock();
            Assert.AreEqual(0, _changes.Count, "held before the west signal");

            _events.Publish(new OpeningMilestoneReached(OpeningMilestone.SignalWest, false));
            Assert.AreEqual(0, _changes.Count, "the signal alone shows nothing yet");

            Dock();
            Assert.IsTrue(_field.Shown);
            Assert.AreEqual(2, _changes.Count, "the headlamp's and the cell pack's outlines");
            Assert.AreEqual(RackBayState.Outline, _changes[0].State);
            Assert.IsFalse(_changes[0].Restored);

            Dock();
            Assert.AreEqual(2, _changes.Count, "later docks change nothing");
        }

        [Test]
        public void AScanOfTheAdapter_AndAFittedPart_AreEachAnnouncedOnce()
        {
            _events.Publish(new OpeningMilestoneReached(OpeningMilestone.SignalWest, false));
            Dock();
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
        public void ALoadedRack_IsReplayedAsRestored_AndStaysOut()
        {
            _field.Restore(new RackSaveData { shown = true, seen = new string[0], filled = new[] { "rack.cell_pack" } });
            _field.Announce();
            Assert.AreEqual(2, _changes.Count);
            Assert.IsTrue(_changes[0].Restored && _changes[1].Restored);
            Assert.AreEqual(RackBayState.Filled, _changes[1].State);

            _changes.Clear();
            _events.Publish(new OpeningMilestoneReached(OpeningMilestone.SignalWest, true));
            Dock();
            Assert.AreEqual(0, _changes.Count, "no second reveal");
        }

        [Test]
        public void TheDefaultTuning_HasAValidRack()
        {
            Assert.IsNull(_tuning.ValidateRack());
        }

        private void Dock()
        {
            _events.Publish(new RoverDockChanged(true, Vector3.zero, Quaternion.identity));
            _events.Publish(new RoverDockChanged(false, Vector3.zero, Quaternion.identity));
        }
    }
}
