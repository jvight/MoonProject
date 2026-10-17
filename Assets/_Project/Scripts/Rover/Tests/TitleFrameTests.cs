using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The title frame over a save looks from behind 07 toward the lander, or along 07's heading near it.</summary>
    public sealed class TitleFrameTests
    {
        private const float MinDistance = 12f;
        private const float RoverBearing = 30f;

        [Test]
        public void FarFromTheLander_LooksAlongTheLineToIt()
        {
            float bearing = TitleFrame.Bearing(new Vector3(10f, 3f, 0f), new Vector3(10f, 0f, 100f), RoverBearing,
                MinDistance);
            Assert.AreEqual(0f, bearing, 1e-3f);

            bearing = TitleFrame.Bearing(Vector3.zero, new Vector3(100f, 5f, 0f), RoverBearing, MinDistance);
            Assert.AreEqual(90f, bearing, 1e-3f);
        }

        [Test]
        public void NearTheLander_KeepsTheRoversHeading()
        {
            float bearing = TitleFrame.Bearing(Vector3.zero, new Vector3(4f, 0f, 3f), RoverBearing, MinDistance);
            Assert.AreEqual(RoverBearing, bearing, 1e-3f);
        }

        [Test]
        public void WithoutALander_KeepsTheRoversHeading()
        {
            Assert.AreEqual(RoverBearing, TitleFrame.Bearing(Vector3.zero, null, RoverBearing, MinDistance), 1e-3f);
        }
    }
}
