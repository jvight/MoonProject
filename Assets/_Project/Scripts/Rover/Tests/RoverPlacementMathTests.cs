using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>Where IRoverPlacement sets 07 down: the terrain's height and slope, the rotation's yaw only.</summary>
    public sealed class RoverPlacementMathTests
    {
        private const float Radius = 0.45f;

        [Test]
        public void Yaw_IsTheHeadingOfTheForward_IgnoringPitchAndRoll()
        {
            Assert.AreEqual(90f, RoverPlacementMath.Yaw(Quaternion.Euler(0f, 90f, 0f)), 1e-3f);
            Assert.AreEqual(270f, RoverPlacementMath.Yaw(Quaternion.Euler(0f, -90f, 0f)), 1e-3f);
            Assert.AreEqual(135f, RoverPlacementMath.Yaw(Quaternion.Euler(25f, 135f, -10f)), 0.5f);
            Assert.AreEqual(0f, RoverPlacementMath.Yaw(Quaternion.LookRotation(Vector3.forward)), 1e-3f);
        }

        [Test]
        public void Ground_TakesTheTerrainHeight_NotTheRequestedY()
        {
            var terrain = new FuncTerrain((x, z) => 3f + 0.1f * x);
            Vector3 ground = RoverPlacementMath.Ground(terrain, new Vector3(10f, 50f, -4f), out Vector3 normal);
            Assert.AreEqual(new Vector3(10f, 4f, -4f), ground);
            Assert.AreEqual(1f, normal.magnitude, 1e-4f);
        }

        [Test]
        public void RestingCentre_OnASlope_TouchesTheSlopeAtTheGroundPoint()
        {
            float slope = Mathf.Tan(25f * Mathf.Deg2Rad);
            var terrain = new FuncTerrain((x, z) => slope * x);
            Vector3 ground = RoverPlacementMath.Ground(terrain, new Vector3(20f, 0f, 5f), out Vector3 normal);
            Vector3 centre = RoverPlacementMath.RestingCentre(ground, normal, Radius);

            Vector3 expectedNormal = new Vector3(-slope, 1f, 0f).normalized;
            Assert.Less(Vector3.Angle(expectedNormal, normal), 0.5f, "The slope's normal, not world up.");
            var plane = new Plane(normal, ground);
            Assert.AreEqual(Radius, plane.GetDistanceToPoint(centre), 1e-3f, "The sphere touches the slope.");
            Assert.Less(centre.x, ground.x, "Lifted off the slope along its normal, back over the downhill side.");
        }
    }
}
