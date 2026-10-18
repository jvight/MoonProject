using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>Directions in 07's heading frame read as yaw (right of ahead) and pitch (above level).</summary>
    public sealed class LensGazeTests
    {
        [Test]
        public void Angles_ReadYawToTheRightAndPitchUpward()
        {
            Assert.AreEqual(Vector2.zero, LensGaze.Angles(Vector3.forward));
            Vector2 right = LensGaze.Angles(new Vector3(1f, 0f, 1f));
            Assert.AreEqual(45f, right.x, 1e-3f);
            Assert.AreEqual(0f, right.y, 1e-3f);
            Vector2 up = LensGaze.Angles(new Vector3(0f, 1f, 1f));
            Assert.AreEqual(0f, up.x, 1e-3f);
            Assert.AreEqual(45f, up.y, 1e-3f);
            Assert.AreEqual(-90f, LensGaze.Angles(Vector3.left).x, 1e-3f);
        }
    }
}
