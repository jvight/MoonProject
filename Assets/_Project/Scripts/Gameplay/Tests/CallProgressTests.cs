using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The call from the sky as a state machine: Broken on reserve, Locked at the first charge, Clear when the base is
    /// night-ready, Answered at the answer, never back; its identity decoding one at the first charge, two more at the
    /// answer, then one per dock until it reads in full, nothing twice; saved and replayed on a loaded game.
    /// </summary>
    public sealed class CallProgressTests
    {
        private EventBus _events;
        private CallSettings _settings;
        private CallProgress _call;
        private readonly List<CallStateChanged> _states = new List<CallStateChanged>();
        private readonly List<CallDecoded> _decodes = new List<CallDecoded>();

        private sealed class FixedLayout : IWorldLayout
        {
            public Vector3 BasePosition => Vector3.zero;

            public Vector3 PeakPosition => Vector3.zero;

            public Vector3 EarthDirection => new Vector3(0f, 0.5f, 1f).normalized;
        }

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _settings = new CallSettings();
            _states.Clear();
            _decodes.Clear();
            _events.Subscribe<CallStateChanged>(_states.Add);
            _events.Subscribe<CallDecoded>(_decodes.Add);
            _call = New();
        }

        [TearDown]
        public void TearDown()
        {
            _call.Dispose();
        }

        private CallProgress New()
        {
            return new CallProgress(_events, _settings, new FixedLayout());
        }

        private void Reach(OpeningMilestone milestone, bool restored = false)
        {
            _events.Publish(new OpeningMilestoneReached(milestone, restored));
        }

        private void Dock()
        {
            _events.Publish(new RoverDockChanged(true, Vector3.zero, Quaternion.identity));
            _events.Publish(new RoverDockChanged(false, Vector3.zero, Quaternion.identity));
        }

        [Test]
        public void ANewGame_StartsBroken_WithNothingDecoded_OfTheFiveCharactersOfArkIv()
        {
            Assert.AreEqual(CallState.Broken, _call.State, "07 runs on reserve");
            Assert.AreEqual(0, _call.Decoded);
            Assert.AreEqual(5, _call.Total, "ARK IV without its space");
        }

        [Test]
        public void EachMilestone_MovesTheCall_Forward_AndAnnouncesIt()
        {
            Reach(OpeningMilestone.FirstCharge);
            Assert.AreEqual(CallState.Locked, _call.State);
            Reach(OpeningMilestone.PowerOn);
            Assert.AreEqual(CallState.Locked, _call.State, "power alone does not clear the carrier");
            Reach(OpeningMilestone.NightReady);
            Assert.AreEqual(CallState.Clear, _call.State);
            Reach(OpeningMilestone.SignalWest);
            Assert.AreEqual(CallState.Clear, _call.State);
            Reach(OpeningMilestone.CallAnswered);
            Assert.AreEqual(CallState.Answered, _call.State);

            Assert.AreEqual(3, _states.Count, "one announcement per change");
            Assert.AreEqual(CallState.Locked, _states[0].State);
            Assert.AreEqual(CallState.Clear, _states[1].State);
            Assert.AreEqual(CallState.Answered, _states[2].State);
            Assert.IsFalse(_states[2].Restored);
        }

        [Test]
        public void TheCall_NeverStepsBack()
        {
            Reach(OpeningMilestone.NightReady);
            Reach(OpeningMilestone.FirstCharge);
            Assert.AreEqual(CallState.Clear, _call.State, "a later state holds when an earlier milestone comes after");
        }

        [Test]
        public void TheIdentity_DecodesOneAtTheFirstCharge_TwoMoreAtTheAnswer_ThenOnePerDock_CappedAtItsLength()
        {
            Dock();
            Assert.AreEqual(0, _call.Decoded, "a dock before the answer decodes nothing");
            Reach(OpeningMilestone.FirstCharge);
            Assert.AreEqual(1, _call.Decoded, "A?? ??");
            Reach(OpeningMilestone.NightReady);
            Dock();
            Assert.AreEqual(1, _call.Decoded);
            Reach(OpeningMilestone.CallAnswered);
            Assert.AreEqual(3, _call.Decoded, "two more at the answer");
            Dock();
            Assert.AreEqual(4, _call.Decoded, "one per dock after it");
            Dock();
            Assert.AreEqual(5, _call.Decoded, "ARK IV in full");
            Dock();
            Dock();
            Assert.AreEqual(5, _call.Decoded, "capped at its length");

            var counts = new List<int>();
            foreach (CallDecoded decoded in _decodes)
            {
                Assert.AreEqual(5, decoded.CharactersTotal);
                Assert.IsFalse(decoded.Restored);
                counts.Add(decoded.CharactersDecoded);
            }

            CollectionAssert.AreEqual(new[] { 1, 3, 4, 5 }, counts, "each step announced once, no repeats");
        }

        [Test]
        public void ARepeatedMilestone_DecodesNothingTwice()
        {
            Reach(OpeningMilestone.FirstCharge);
            Reach(OpeningMilestone.FirstCharge, true);
            Reach(OpeningMilestone.CallAnswered);
            Reach(OpeningMilestone.CallAnswered, true);
            Assert.AreEqual(3, _call.Decoded);
            Assert.AreEqual(2, _decodes.Count);
        }

        [Test]
        public void ALoadedGame_RestoresTheCall_AndReplaysItAsRestored_WithoutDecodingAgain()
        {
            Reach(OpeningMilestone.FirstCharge);
            Reach(OpeningMilestone.NightReady);
            Reach(OpeningMilestone.CallAnswered);
            Dock();
            CallSaveData saved = _call.Capture();
            _call.Dispose();
            _states.Clear();
            _decodes.Clear();

            _call = New();
            _call.Restore(saved);
            _call.Announce();
            Reach(OpeningMilestone.FirstCharge, true);
            Reach(OpeningMilestone.NightReady, true);
            Reach(OpeningMilestone.CallAnswered, true);

            Assert.AreEqual(CallState.Answered, _call.State);
            Assert.AreEqual(4, _call.Decoded, "the save keeps the dock's decode");
            Assert.AreEqual(1, _states.Count, "announced once, from the save");
            Assert.IsTrue(_states[0].Restored);
            Assert.AreEqual(1, _decodes.Count, "and the decode once");
            Assert.IsTrue(_decodes[0].Restored);
            Assert.AreEqual(4, _decodes[0].CharactersDecoded);
        }

        [Test]
        public void ASaveFromBeforeTheCall_TakesItsStateAndDecodeFromTheReplayedMilestones()
        {
            _call.Restore(null);
            _call.Announce();
            _states.Clear();
            _decodes.Clear();
            Reach(OpeningMilestone.FirstCharge, true);
            Reach(OpeningMilestone.NightReady, true);
            Assert.AreEqual(CallState.Clear, _call.State);
            Assert.AreEqual(1, _call.Decoded, "the first charge's character, from the replay");
            Assert.IsTrue(_states.TrueForAll(changed => changed.Restored));
            Assert.IsTrue(_decodes.TrueForAll(decoded => decoded.Restored));
        }

        [Test]
        public void ThePointOfLight_StandsBesideEarth_InTheSky()
        {
            Vector3 earth = new FixedLayout().EarthDirection;
            Vector3 call = _call.Direction;
            Assert.AreEqual(1f, call.magnitude, 1e-4f);
            float apart = Vector3.Angle(earth, call);
            Assert.Greater(apart, 1f, "not on Earth itself");
            Assert.Less(apart, 15f, "beside it");
            Assert.Greater(call.y, earth.y, "a little above it");
        }
    }
}
