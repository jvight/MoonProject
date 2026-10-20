using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// relay.1's repair (opening v3 R10): nothing is offered until relay.1 has answered the call; then a held E takes
    /// the spare from the open cabinet into the hold; let go and held again at the feed socket it fits it, and only
    /// then: the relay is lit (published once), its contact pad charges a 07 standing on it, and it counts as a charge
    /// point for the range ring; a save keeps all of it, lays a carried spare back into the hold, and replays a lit
    /// relay as Restored.
    /// </summary>
    public sealed class RelayRepairTests
    {
        private static readonly Vector3 Spare = new Vector3(2.5f, 0.5f, 0f);
        private static readonly Vector3 Socket = new Vector3(0f, 1f, 1.5f);
        private static readonly Vector3 Pad = new Vector3(0f, 0f, -3f);
        private static readonly Vector3 Far = new Vector3(40f, 0f, 0f);

        private readonly List<RelayRelit> _relit = new List<RelayRelit>();
        private readonly List<StoryPartCollected> _parts = new List<StoryPartCollected>();
        private readonly List<GameObject> _made = new List<GameObject>();
        private EventBus _events;
        private MaterialCatalog _catalog;
        private CargoHold _hold;
        private PickupReach _pickups;
        private RelayRepairSettings _settings;
        private MovableRover _rover;
        private RelayRepair _relay;
        private GameObject _spareProp;
        private GameObject _fittedProp;
        private int _frame;

        [SetUp]
        public void SetUp()
        {
            _relit.Clear();
            _parts.Clear();
            _events = new EventBus();
            _events.Subscribe<RelayRelit>(_relit.Add);
            _events.Subscribe<StoryPartCollected>(_parts.Add);
            _catalog = CargoFixtures.Catalog();
            _hold = CargoFixtures.Stock(_events, _catalog).Hold;
            _pickups = new PickupReach();
            _settings = new RelayRepairSettings();
            _rover = new MovableRover { Position = Far };
            _spareProp = Make("FeedUnitSpare");
            _fittedProp = Make("FeedUnitFitted");
            _fittedProp.SetActive(false);
            _relay = NewRelay();
            Answer();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void BeforeTheAnswer_TheCabinetAndTheSocketOfferNothing()
        {
            RelayRepair silent = NewRelay();
            _rover.Position = Spare;
            for (int i = 0; i < 3; i++)
            {
                silent.Step(++_frame, true);
            }

            Assert.IsFalse(silent.Open);
            Assert.IsFalse(silent.SpareTaken, "relay.1 has not answered yet: its cabinet is not 07's to open");
            Assert.IsTrue(_relay.Open, "answered (a loaded game's replayed milestone too)");
            silent.Dispose();
        }

        [Test]
        public void AHeldE_TakesTheSpare_OnlyWithinReach()
        {
            Hold(true, 3);
            Assert.IsFalse(_relay.SpareTaken, "out of reach");

            _rover.Position = Spare;
            Hold(true, 3);
            Assert.IsTrue(_relay.SpareTaken, "in reach, a held E takes it");
            Assert.IsTrue(_hold.HasPart(_settings.FeedPart), "into the hold");
            Assert.IsFalse(_spareProp.activeSelf, "the cabinet stands empty");
            Assert.AreEqual(1, _parts.Count, "with its pickup note");
            Assert.AreEqual(_settings.FeedPart, _parts[0].PartId);
            Assert.IsEmpty(_relit, "taken, not fitted");
        }

        [Test]
        public void TheSpare_FitsAtTheSocket_OnlyOnASecondPress_ThenTheRelayIsLit()
        {
            _rover.Position = Socket;
            Assert.IsFalse(_relay.TryGetInstall(out _), "nothing in the hold to fit");
            Hold(true, 3);
            Assert.IsTrue(_relay.SpareTaken, "the cabinet stands within reach of the socket too");
            Assert.IsTrue(_relay.TryGetInstall(out Vector3 at), "the socket offers the spare");
            Assert.AreEqual(Socket, at);
            Hold(true, 5);
            Assert.IsFalse(_relay.Fitted, "the press that took it does not fit it");

            Hold(false, 1);
            Hold(true, 1);
            Assert.IsTrue(_relay.Fitted, "let go and held again: fitted");
            Assert.IsFalse(_hold.HasPart(_settings.FeedPart), "it left the hold");
            Assert.IsTrue(_fittedProp.activeSelf, "the unit shows in the socket");
            Assert.AreEqual(1, _relit.Count, "the relay is lit, once");
            Assert.AreEqual("relay.1", _relit[0].RelayId);
            Assert.IsFalse(_relit[0].Restored);
            Assert.IsFalse(_relay.TryGetInstall(out _), "nothing more to fit");

            Hold(true, 3);
            Assert.AreEqual(1, _relit.Count, "only once");
        }

        [Test]
        public void TheLitRelay_ChargesOnItsPad_AndCountsAsAChargePoint()
        {
            var points = new List<Vector3>();
            _relay.CopyChargePoints(points);
            Assert.IsEmpty(points, "a dead relay is no charge point");
            Assert.IsFalse(_relay.TryFindUnder(Pad, out _), "nor does its pad charge");

            Repair();
            _relay.CopyChargePoints(points);
            Assert.AreEqual(new[] { Pad }, points.ToArray(), "lit: the ring counts its pad");
            Assert.IsTrue(_relay.TryFindUnder(Pad + new Vector3(_settings.PadRadius - 0.2f, 0f, 0f), out Vector3 pad));
            Assert.AreEqual(Pad, pad);
            Assert.IsFalse(_relay.TryFindUnder(Pad + new Vector3(_settings.PadRadius + 0.5f, 0f, 0f), out _));
        }

        [Test]
        public void ASave_LaysACarriedSpareBack_AndALitRelayIsReplayedAsRestored()
        {
            _rover.Position = Spare;
            Hold(true, 3);
            RelaySaveData carried = _relay.Capture();
            Assert.IsTrue(carried.spareCarried);

            _hold.RemovePart(_settings.FeedPart);
            RelayRepair loaded = NewRelay();
            Answer();
            loaded.Restore(carried);
            Assert.IsFalse(_spareProp.activeSelf, "the cabinet stays empty");
            loaded.Announce();
            Assert.IsTrue(_hold.HasPart(_settings.FeedPart), "the carried spare is back in the hold");
            Assert.IsEmpty(_relit);

            _rover.Position = Socket;
            Hold(false, 1);
            loaded.Step(++_frame, true);
            Assert.IsTrue(loaded.Fitted, "and it fits as before");
            RelaySaveData lit = loaded.Capture();
            _relit.Clear();
            _fittedProp.SetActive(false);

            RelayRepair reloaded = NewRelay();
            Answer();
            reloaded.Restore(lit);
            Assert.IsTrue(_fittedProp.activeSelf, "a lit relay shows its unit at once");
            reloaded.Announce();
            Assert.AreEqual(1, _relit.Count, "stated at the session start");
            Assert.IsTrue(_relit[0].Restored, "as Restored, so World's lamp burns steady at once");
            Assert.IsFalse(_hold.HasPart(_settings.FeedPart), "a fitted spare is not laid back");
        }

        /// <summary>relay.1 answers the call (R8), or a loaded game replays that it did.</summary>
        private void Answer()
        {
            _events.Publish(new OpeningMilestoneReached(OpeningMilestone.CallAnswered, true));
        }

        private void Repair()
        {
            _rover.Position = Socket;
            Hold(true, 3);
            Hold(false, 1);
            Hold(true, 1);
            Assert.IsTrue(_relay.Fitted);
        }

        private void Hold(bool held, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                _relay.Step(++_frame, held);
            }
        }

        private RelayRepair NewRelay()
        {
            return new RelayRepair(_events, _hold, _rover, _pickups, _settings, Spare, Socket, Pad, _spareProp,
                _fittedProp);
        }

        private GameObject Make(string name)
        {
            var made = new GameObject(name);
            _made.Add(made);
            return made;
        }

        private sealed class MovableRover : IRoverState
        {
            public Vector3 Position { get; set; }
            public Quaternion Rotation => Quaternion.identity;
            public Vector3 Velocity => Vector3.zero;
            public float Speed => 0f;
            public float NormalizedSpeed => 0f;
            public Vector2 DriveInput => Vector2.zero;
            public bool IsGrounded => true;
            public float AirTime => 0f;
            public Vector3 GroundNormal => Vector3.up;
        }
    }
}
