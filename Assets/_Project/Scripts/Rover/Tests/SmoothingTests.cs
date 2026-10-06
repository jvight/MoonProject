using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class SmoothingTests
    {
        [Test]
        public void Damp_AfterOneHalfLife_ClosesHalfTheGap()
        {
            Assert.AreEqual(5f, Smoothing.Damp(0f, 10f, 0.2f, 0.2f), 1e-4f);
        }

        [Test]
        public void Damp_IsFrameRateIndependent()
        {
            float coarse = Smoothing.Damp(0f, 1f, 0.15f, 0.1f);
            float fine = 0f;
            for (int i = 0; i < 10; i++)
            {
                fine = Smoothing.Damp(fine, 1f, 0.15f, 0.01f);
            }

            Assert.AreEqual(coarse, fine, 1e-5f);
        }

        [Test]
        public void Damp_ZeroHalfLife_SnapsToTarget()
        {
            Assert.AreEqual(3f, Smoothing.Damp(-7f, 3f, 0f, 0.016f));
        }

        [Test]
        public void DampAngle_TakesTheShortestArc()
        {
            float result = Smoothing.DampAngle(350f, 10f, 0.1f, 0.1f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, result), 1e-3f);
        }

        [Test]
        public void SmoothStep_IsClampedAndEased()
        {
            Assert.AreEqual(0f, Smoothing.SmoothStep(1f, 3f, 0f));
            Assert.AreEqual(1f, Smoothing.SmoothStep(1f, 3f, 5f));
            Assert.AreEqual(0.5f, Smoothing.SmoothStep(1f, 3f, 2f), 1e-6f);
            Assert.Less(Smoothing.SmoothStep(1f, 3f, 1.2f), 0.1f);
        }
    }
}
