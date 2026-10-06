using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class GazeTests
    {
        [Test]
        public void Angles_StraightAhead_IsZero()
        {
            Assert.AreEqual(Vector2.zero, HeadAim.Angles(Vector3.forward, 110f, 55f, 25f));
        }

        [Test]
        public void Angles_RightAndUp_ArePositive()
        {
            Vector2 aim = HeadAim.Angles(new Vector3(1f, 1f, 1f), 110f, 55f, 25f);
            Assert.AreEqual(45f, aim.x, 1e-3f);
            Assert.AreEqual(35.264f, aim.y, 1e-2f);
        }

        [Test]
        public void Angles_AreClampedToTheNecksReach()
        {
            Vector2 behind = HeadAim.Angles(new Vector3(0.1f, 0f, -1f), 110f, 55f, 25f);
            Assert.AreEqual(110f, behind.x, 1e-3f);
            Vector2 zenith = HeadAim.Angles(new Vector3(0f, 1f, 0.01f), 110f, 55f, 25f);
            Assert.AreEqual(55f, zenith.y, 1e-3f);
            Vector2 ground = HeadAim.Angles(new Vector3(0f, -1f, 0.2f), 110f, 55f, 25f);
            Assert.AreEqual(-25f, ground.y, 1e-3f);
        }

        [Test]
        public void Angles_StraightUp_KeepsYawCentred()
        {
            Assert.AreEqual(0f, HeadAim.Angles(Vector3.up, 110f, 55f, 25f).x);
        }

        [Test]
        public void Requests_HighestPriorityWins_AndNullClears()
        {
            var requests = new GazeRequests();
            Assert.IsFalse(requests.TryGetTop(out _, out _));

            requests.Set(GazePriority.Glance, new Vector3(1f, 0f, 0f));
            requests.Set(GazePriority.Focus, new Vector3(3f, 0f, 0f));
            requests.Set(GazePriority.Interest, new Vector3(2f, 0f, 0f));
            Assert.IsTrue(requests.TryGetTop(out Vector3 point, out GazePriority priority));
            Assert.AreEqual(GazePriority.Focus, priority);
            Assert.AreEqual(3f, point.x);

            requests.Set(GazePriority.Focus, null);
            requests.TryGetTop(out point, out priority);
            Assert.AreEqual(GazePriority.Interest, priority);

            requests.Clear();
            Assert.IsFalse(requests.TryGetTop(out _, out _));
        }
    }
}
