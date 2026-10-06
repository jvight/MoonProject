using System;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class GroundPlaneFitTests
    {
        private static Vector3[] WheelGrid(Func<float, float, float> height)
        {
            var points = new Vector3[6];
            int i = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int side = 0; side < 2; side++)
                {
                    float x = side == 0 ? -0.7f : 0.7f;
                    float z = 0.8f - row * 0.8f;
                    points[i++] = new Vector3(x, height(x, z), z);
                }
            }

            return points;
        }

        [Test]
        public void FlatGround_GivesUp()
        {
            Assert.IsTrue(GroundPlaneFit.TryFit(WheelGrid((x, z) => 0.3f), 6, out Vector3 normal));
            Assert.Less(Vector3.Angle(normal, Vector3.up), 1e-3f);
        }

        [Test]
        public void Uphill_GivesNoseUpPitch()
        {
            float slope = Mathf.Tan(15f * Mathf.Deg2Rad);
            Assert.IsTrue(GroundPlaneFit.TryFit(WheelGrid((x, z) => z * slope), 6, out Vector3 normal));
            Assert.AreEqual(15f, GroundPlaneFit.PitchOf(normal), 1e-3f);
            Assert.AreEqual(0f, GroundPlaneFit.RollOf(normal), 1e-3f);
        }

        [Test]
        public void RightSideHigh_GivesPositiveRoll()
        {
            float slope = Mathf.Tan(10f * Mathf.Deg2Rad);
            Assert.IsTrue(GroundPlaneFit.TryFit(WheelGrid((x, z) => x * slope), 6, out Vector3 normal));
            Assert.AreEqual(10f, GroundPlaneFit.RollOf(normal), 1e-3f);
        }

        [Test]
        public void SingleRockUnderOneWheel_IsAveragedOut()
        {
            Vector3[] points = WheelGrid((x, z) => 0f);
            points[0].y = 0.3f;
            Assert.IsTrue(GroundPlaneFit.TryFit(points, 6, out Vector3 normal));
            Assert.Less(Vector3.Angle(normal, Vector3.up), 12f);
        }

        [Test]
        public void TooFewOrCollinearPoints_Fail()
        {
            Assert.IsFalse(GroundPlaneFit.TryFit(WheelGrid((x, z) => 0f), 2, out _));
            var line = new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, 0.1f, 1f), new Vector3(0f, 0.2f, 2f) };
            Assert.IsFalse(GroundPlaneFit.TryFit(line, 3, out Vector3 normal));
            Assert.AreEqual(Vector3.up, normal);
        }

        [TestCase(12f, 0f)]
        [TestCase(-20f, 0f)]
        [TestCase(0f, 8f)]
        [TestCase(10f, -6f)]
        public void Tilt_IsConsistentWithPitchAndRoll(float pitch, float roll)
        {
            Vector3 up = GroundPlaneFit.Tilt(pitch, roll) * Vector3.up;
            Assert.AreEqual(pitch, GroundPlaneFit.PitchOf(up), 0.01f);
            Assert.AreEqual(roll, GroundPlaneFit.RollOf(up), 0.5f);
            if (pitch != 0f)
            {
                Vector3 forward = GroundPlaneFit.Tilt(pitch, 0f) * Vector3.forward;
                Assert.AreEqual(Mathf.Sign(pitch), Mathf.Sign(forward.y), "Positive pitch raises the nose.");
            }
        }
    }
}
