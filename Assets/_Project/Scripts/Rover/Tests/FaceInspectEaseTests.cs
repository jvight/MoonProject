using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    public sealed class FaceInspectEaseTests
    {
        private const float Ramp = 0.3f;

        [Test]
        public void ItStartsAtNothing_AndEndsAtWhole()
        {
            Assert.AreEqual(0f, FaceInspectEase.Weight(0f, Ramp), 1e-5f);
            Assert.AreEqual(1f, FaceInspectEase.Weight(1f, Ramp), 1e-5f);
            Assert.AreEqual(0.5f, FaceInspectEase.Weight(0.5f, Ramp), 1e-5f);
        }

        [Test]
        public void ItNeverGoesBack_AndNeverJumps()
        {
            float last = 0f;
            for (int i = 1; i <= 200; i++)
            {
                float t = i / 200f;
                float w = FaceInspectEase.Weight(t, Ramp);
                Assert.GreaterOrEqual(w, last, "monotonic");
                Assert.LessOrEqual(w - last, FaceInspectEase.PeakSpeed(Ramp) / 200f + 1e-4f, "never faster than its peak");
                last = w;
            }
        }

        [Test]
        public void ItsPeakSpeed_IsWellUnderASmoothsteps()
        {
            const float smoothstepPeak = 1.5f;
            Assert.Less(FaceInspectEase.PeakSpeed(Ramp), smoothstepPeak);
        }

        [Test]
        public void ItIsSymmetric()
        {
            for (int i = 0; i <= 20; i++)
            {
                float t = i / 20f;
                Assert.AreEqual(1f - FaceInspectEase.Weight(1f - t, Ramp), FaceInspectEase.Weight(t, Ramp), 1e-5f);
            }
        }
    }
}
