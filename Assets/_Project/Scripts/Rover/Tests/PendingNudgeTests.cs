using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.Tests
{
    public sealed class PendingNudgeTests
    {
        private const float MaxSeconds = 15f;

        private static NextStepChanged Step(string target)
        {
            return new NextStepChanged("next.line", NextLineReason.Next, default, false, true, target, Vector3.forward);
        }

        [Test]
        public void ARequestAskedWhileBusy_IsGivenBackOnceTheCameraIsFree_AndOnlyOnce()
        {
            var pending = new PendingNudge();
            pending.Hold(Step("array"), 10f);

            Assert.IsFalse(pending.TryRelease(false, 11f, MaxSeconds, out _), "still busy: it waits");
            Assert.IsTrue(pending.IsHeld);
            Assert.IsTrue(pending.TryRelease(true, 12f, MaxSeconds, out NextStepChanged given));
            Assert.AreEqual("array", given.TargetId);
            Assert.IsFalse(pending.TryRelease(true, 13f, MaxSeconds, out _), "once only");
        }

        [Test]
        public void AStaleRequest_IsDroppedWithoutFiring()
        {
            var pending = new PendingNudge();
            pending.Hold(Step("array"), 10f);

            Assert.IsFalse(pending.TryRelease(true, 10f + MaxSeconds + 0.1f, MaxSeconds, out _));
            Assert.IsFalse(pending.IsHeld);
        }

        [Test]
        public void ALaterRequest_ReplacesTheHeldOne_AndClearDropsIt()
        {
            var pending = new PendingNudge();
            pending.Hold(Step("array"), 10f);
            pending.Hold(Step("relay"), 11f);
            Assert.IsTrue(pending.TryRelease(true, 12f, MaxSeconds, out NextStepChanged given));
            Assert.AreEqual("relay", given.TargetId);

            pending.Hold(Step("array"), 20f);
            pending.Clear();
            Assert.IsFalse(pending.TryRelease(true, 21f, MaxSeconds, out _));
        }
    }
}
