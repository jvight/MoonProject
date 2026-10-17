using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// Something waiting (a cut piece held on its wreck) is taken with E, a tap or a hold (even one begun on the way),
    /// never by driving past it: the nearest within reach across the ground offers its prompt (with its name).
    /// </summary>
    public sealed class PickupReachTests
    {
        private const float Reach = 4.5f;
        private const string Metal = "material.metal";
        private const string Optics = "material.optics";

        [Test]
        public void InReach_OffersThePrompt_ButOnlyEHeldTakesIt()
        {
            var reach = new PickupReach();
            Vector3 coil = new Vector3(0f, 0f, 3f);
            Assert.IsFalse(Offer(reach, 1, false, coil), "driving up to it is not enough");
            Assert.IsFalse(Offer(reach, 2, false, coil));
            Assert.IsTrue(reach.TryGet(2, out Vector3 at, out string noun), "the prompt shows");
            Assert.AreEqual(coil, at);
            Assert.AreEqual(Metal, noun);
            Assert.IsTrue(Offer(reach, 3, true, coil), "the press takes it");
            Assert.IsFalse(reach.TryGet(6, out _, out _), "a quiet frame clears the prompt");
        }

        [Test]
        public void AHoldBegunBeforeThePartCameInReach_StillTakesIt()
        {
            var reach = new PickupReach();
            Vector3 far = new Vector3(0f, 0f, Reach + 5f);
            Vector3 near = new Vector3(0f, 0f, 2f);
            Assert.IsFalse(Offer(reach, 1, true, far), "E held on the way (a dig, a cut, a habit)");
            Assert.IsFalse(Offer(reach, 2, true, far));
            Assert.IsFalse(Offer(reach, 3, true, near), "it comes in reach...");
            Assert.IsTrue(Offer(reach, 4, true, near), "...and the hold takes it, like every other E verb");
        }

        [Test]
        public void Reach_IsAcrossTheGround_SoAPartALittleAboveOrBelowCounts()
        {
            var reach = new PickupReach();
            Vector3 onALedge = new Vector3(0f, 2.5f, 3.5f);
            Offer(reach, 1, false, onALedge);
            Assert.IsTrue(Offer(reach, 2, true, onALedge), "3.5 m away across the ground, 2.5 m up");
            Vector3 cliff = new Vector3(0f, Reach + 1f, 1f);
            Offer(reach, 3, false, cliff);
            Assert.IsFalse(Offer(reach, 4, true, cliff), "but not up a cliff");
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
            reach.Offer(1, false, PickupReach.Bundles, 0, farther, Vector3.zero, Reach, Metal);
            reach.Offer(1, false, PickupReach.Bundles, 1, near, Vector3.zero, Reach, Optics);
            Assert.IsFalse(reach.Offer(2, true, PickupReach.Bundles, 0, farther, Vector3.zero, Reach, Metal));
            Assert.IsTrue(reach.Offer(2, true, PickupReach.Bundles, 1, near, Vector3.zero, Reach, Optics));
            Assert.IsTrue(reach.TryGet(2, out _, out string noun));
            Assert.AreEqual(Optics, noun, "the prompt names the nearer one");
        }

        private static bool Offer(PickupReach reach, int frame, bool held, Vector3 at)
        {
            return reach.Offer(frame, held, PickupReach.Bundles, 0, at, Vector3.zero, Reach, Metal);
        }
    }
}
