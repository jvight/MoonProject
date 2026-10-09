using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The camera under 07 stops above the dust and turns the rest of the way into looking up.</summary>
    public sealed class LowOrbitTests
    {
        private const float Radius = 7.5f;
        private const float TargetHeight = 1.1f;
        private const float Lowest = 0.6f;

        [Test]
        public void Floor_KeepsTheCameraAtTheLowestHeight()
        {
            float floor = LowOrbit.Floor(Radius, TargetHeight, Lowest);
            Assert.Less(floor, 0f, "The camera may sink below the follow point.");
            Assert.AreEqual(Lowest, TargetHeight + Radius * Mathf.Sin(floor * Mathf.Deg2Rad), 1e-4f);
        }

        [TestCase(0f)]
        [TestCase(10f)]
        [TestCase(19f)]
        public void AimLift_TiltsTheViewUpByTheRestOfTheElevation(float tilt)
        {
            float floor = LowOrbit.Floor(Radius, TargetHeight, Lowest);
            float lift = LowOrbit.AimLift(floor, tilt, Radius);
            float cameraY = Radius * Mathf.Sin(floor * Mathf.Deg2Rad);
            float across = Radius * Mathf.Cos(floor * Mathf.Deg2Rad);
            float aimPitch = Mathf.Atan2(lift - cameraY, across) * Mathf.Rad2Deg;
            Assert.AreEqual(tilt - floor, aimPitch, 1e-3f, "Looking at the follow point, plus the tilt.");
        }
    }
}
