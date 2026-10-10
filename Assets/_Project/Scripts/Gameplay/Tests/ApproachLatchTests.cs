using NUnit.Framework;
using MoonProject.Gameplay.Lift;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>The jammed winch's creak: once per approach, re-armed only well outside the radius.</summary>
    public sealed class ApproachLatchTests
    {
        private const float Radius = 5f;
        private const float Rearm = 3f;

        [Test]
        public void ComingNear_FiresOnce()
        {
            var latch = new ApproachLatch();
            Assert.IsFalse(latch.Step(9f, Radius, Rearm));
            Assert.IsTrue(latch.Step(4.9f, Radius, Rearm));
            Assert.IsFalse(latch.Step(2f, Radius, Rearm), "Still near: no repeat.");
        }

        [Test]
        public void IdlingAtTheEdge_NeverRepeats_LeavingWellOutRearms()
        {
            var latch = new ApproachLatch();
            latch.Step(4f, Radius, Rearm);
            Assert.IsFalse(latch.Step(Radius + Rearm - 0.1f, Radius, Rearm));
            Assert.IsFalse(latch.Step(4f, Radius, Rearm), "Back from the edge: not a new approach.");
            Assert.IsFalse(latch.Step(Radius + Rearm + 0.1f, Radius, Rearm));
            Assert.IsTrue(latch.Step(4f, Radius, Rearm), "A new approach.");
        }
    }
}
