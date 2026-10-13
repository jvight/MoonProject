using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// A friend's gift that is a part (0.5.5 §3.5, Tilly's solar cell): already given when an older save loads, given
    /// the next time 07 is home and settling in when the friend is repaired during play, and never twice.
    /// </summary>
    public sealed class GiftFittingTests
    {
        private readonly List<PartFitted> _fitted = new List<PartFitted>();
        private EventBus _events;
        private RoverEquipment _equipment;
        private FriendTuning _tuning;
        private Friend _tilly;
        private Rover _rover;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _fitted.Clear();
            _events.Subscribe<PartFitted>(_fitted.Add);
            _equipment = new RoverEquipment(_events, new Abilities(), EquipmentTable.Basin(), 4, station => false,
                part => 0, metal => { });
            _tuning = ScriptableObject.CreateInstance<FriendTuning>();
            _tilly = new Friend();
            _rover = new Rover();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void ARepairDuringPlay_GivesTheCellTheNextTime07IsHome_Softly_AndOnce()
        {
            GiftFitting gift = Gift();
            gift.Step();
            _tilly.Activity = FriendActivity.Home;
            _rover.Position = Vector3.forward * (_tuning.GiftHomeRadius + 5f);
            gift.Step();
            Assert.IsEmpty(_fitted, "07 is away: the gift waits");

            _rover.Position = Vector3.forward * (_tuning.GiftHomeRadius - 1f);
            _rover.Speed = _tuning.GiftMaxSpeed + 1f;
            gift.Step();
            Assert.IsEmpty(_fitted, "rushing past home: still waiting");

            _rover.Speed = 0f;
            gift.Step();
            gift.Step();
            Assert.AreEqual(1, _fitted.Count, "settling in at home: the cell, once");
            Assert.AreEqual(RoverSocket.Wing, _fitted[0].Socket);
            Assert.IsTrue(_fitted[0].Gift, "the soft moment");
        }

        [Test]
        public void AnOlderSavesRepairedFriend_GaveItLongAgo_SoItLoadsFittedSilently()
        {
            _tilly.Activity = FriendActivity.Home;
            _rover.Position = Vector3.forward * 500f;
            Gift().Step();
            Assert.IsEmpty(_fitted, "no moment on a load");
            Assert.IsTrue(_equipment.IsFitted(RoverKitPiece.SolarCell));
        }

        [Test]
        public void ASaveThatHadNotGivenItYet_GivesItAtHome()
        {
            _equipment.Restore(new EquipmentSaveData());
            _tilly.Activity = FriendActivity.Home;
            GiftFitting gift = Gift();
            gift.Step();
            Assert.AreEqual(1, _fitted.Count, "the save's own equipment says not given yet: home, it is given now");
            Assert.IsTrue(_fitted[0].Gift);
        }

        private GiftFitting Gift()
        {
            return new GiftFitting(_equipment, RoverKitPiece.SolarCell, _tilly, _rover, Vector3.zero, _tuning);
        }

        private sealed class Friend : IFriendState
        {
            public string Id => "tilly";

            public Vector3 Position => Vector3.zero;

            public FriendActivity Activity { get; set; } = FriendActivity.Dormant;

            public float RotorSpeed => 0f;

            public float RepairProgress => 0f;
        }

        private sealed class Rover : IRoverState
        {
            public Vector3 Position { get; set; }

            public Quaternion Rotation => Quaternion.identity;

            public Vector3 Velocity => Vector3.zero;

            public float Speed { get; set; }

            public float NormalizedSpeed => 0f;

            public Vector2 DriveInput => Vector2.zero;

            public bool IsGrounded => true;

            public float AirTime => 0f;

            public Vector3 GroundNormal => Vector3.up;
        }

        private sealed class Abilities : IRoverAbilities
        {
            public bool Has(RoverAbility ability)
            {
                return false;
            }

            public void Grant(RoverAbility ability)
            {
            }

            public void Revoke(RoverAbility ability)
            {
            }
        }
    }
}
