using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// A waiting part is picked up by a press, never by driving past it: the nearest part within reach offers its
    /// prompt (with its name), only a fresh press takes it, and the lift and the landing are Collect beats.
    /// </summary>
    public sealed class PickupReachTests
    {
        private const float Reach = 4.5f;
        private const string Coil = "pickup.part_towercoil";
        private const string Rotor = "pickup.tilly.rotor";

        [Test]
        public void InReach_OffersThePrompt_ButOnlyAPressTakesIt()
        {
            var reach = new PickupReach();
            Vector3 coil = new Vector3(0f, 0f, 3f);
            Assert.IsFalse(Offer(reach, 1, false, coil), "driving up to it is not enough");
            Assert.IsFalse(Offer(reach, 2, false, coil));
            Assert.IsTrue(reach.TryGet(2, out Vector3 at, out string noun), "the prompt shows");
            Assert.AreEqual(coil, at);
            Assert.AreEqual(Coil, noun);
            Assert.IsTrue(Offer(reach, 3, true, coil), "the press takes it");
            Assert.IsFalse(Offer(reach, 4, true, coil), "holding does not take another");
            Assert.IsFalse(reach.TryGet(6, out _, out _), "a quiet frame clears the prompt");
        }

        [Test]
        public void OutOfReach_NoPrompt_AndAPressTakesNothing()
        {
            var reach = new PickupReach();
            Vector3 far = new Vector3(0f, 0f, Reach + 1f);
            Assert.IsFalse(Offer(reach, 1, false, far));
            Assert.IsFalse(Offer(reach, 2, true, far));
            Assert.IsFalse(reach.TryGet(2, out _, out _));
        }

        [Test]
        public void TwoInReach_TheNearerIsTheOneTaken()
        {
            var reach = new PickupReach();
            Vector3 near = new Vector3(0f, 0f, 1f);
            Vector3 farther = new Vector3(0f, 0f, 3f);
            reach.Offer(1, false, PickupReach.TowerParts, 0, farther, Vector3.zero, Reach, Coil);
            reach.Offer(1, false, PickupReach.Friends, 0, near, Vector3.zero, Reach, Rotor);
            Assert.IsFalse(reach.Offer(2, true, PickupReach.TowerParts, 0, farther, Vector3.zero, Reach, Coil));
            Assert.IsTrue(reach.Offer(2, true, PickupReach.Friends, 0, near, Vector3.zero, Reach, Rotor));
            Assert.IsTrue(reach.TryGet(2, out _, out string noun));
            Assert.AreEqual(Rotor, noun, "the prompt names the nearer part");
        }

        [Test]
        public void TheLiftAndTheLanding_AreCollectBeats()
        {
            var events = new EventBus();
            var beats = new List<FeelBeat>();
            events.Subscribe<FeelBeat>(beats.Add);
            Vector3 part = new Vector3(2f, 0f, 3f);
            PickupReach.Lifted(events, part, 0.8f);
            PickupReach.Landed(events, Vector3.up);
            Assert.AreEqual(2, beats.Count);
            Assert.AreEqual(FeelKind.Collect, beats[0].Kind);
            Assert.AreEqual(FeelPhase.Act, beats[0].Phase);
            Assert.AreEqual(part, beats[0].Position, "the lift starts at the part");
            Assert.AreEqual(0.8f, beats[0].Seconds, 1e-5f);
            Assert.AreEqual(FeelPhase.Impact, beats[1].Phase, "the clunk on 07's back");
            Assert.AreEqual(Coil, PickupReach.NounKey("Part_TowerCoil"));
        }

        private static bool Offer(PickupReach reach, int frame, bool held, Vector3 at)
        {
            return reach.Offer(frame, held, PickupReach.TowerParts, 0, at, Vector3.zero, Reach, Coil);
        }
    }
}
