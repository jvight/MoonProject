using System;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class GazeTests
    {
        private readonly object _sonar = new object();
        private readonly object _tether = new object();
        private readonly object _scrap = new object();

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
        public void Requests_Empty_HasNoTarget()
        {
            Assert.IsFalse(new GazeRequests().TryGetTop(out _));
        }

        [Test]
        public void Requests_HighestPriorityWins()
        {
            var requests = new GazeRequests();
            requests.Set(_scrap, new Vector3(1f, 0f, 0f), 0);
            requests.Set(_tether, new Vector3(3f, 0f, 0f), 2);
            requests.Set(_sonar, new Vector3(2f, 0f, 0f), 1);
            Assert.IsTrue(requests.TryGetTop(out Vector3 point));
            Assert.AreEqual(3f, point.x);
        }

        [Test]
        public void Requests_TiesGoToTheMostRecent_AndUpdatesKeepTheirPlace()
        {
            var requests = new GazeRequests();
            requests.Set(_sonar, new Vector3(1f, 0f, 0f), 1);
            requests.Set(_scrap, new Vector3(2f, 0f, 0f), 1);
            requests.TryGetTop(out Vector3 point);
            Assert.AreEqual(2f, point.x);

            requests.Set(_sonar, new Vector3(5f, 0f, 0f), 1);
            requests.TryGetTop(out point);
            Assert.AreEqual(2f, point.x, "Refreshing an older request does not steal the tie.");
        }

        [Test]
        public void Requests_SameOwnerUpdatesTargetAndPriority()
        {
            var requests = new GazeRequests();
            requests.Set(_tether, new Vector3(1f, 0f, 0f), 0);
            requests.Set(_sonar, new Vector3(2f, 0f, 0f), 1);
            requests.Set(_tether, new Vector3(4f, 0f, 0f), 5);
            Assert.AreEqual(2, requests.Count);
            requests.TryGetTop(out Vector3 point);
            Assert.AreEqual(4f, point.x);
        }

        [Test]
        public void Requests_ClearWithdrawsOnlyThatOwner()
        {
            var requests = new GazeRequests();
            requests.Set(_tether, new Vector3(1f, 0f, 0f), 5);
            requests.Set(_sonar, new Vector3(2f, 0f, 0f), 1);
            requests.Clear(_tether);
            requests.Clear(_scrap);
            requests.TryGetTop(out Vector3 point);
            Assert.AreEqual(2f, point.x);
            requests.Clear(_sonar);
            Assert.IsFalse(requests.TryGetTop(out _));
        }

        [Test]
        public void Requests_NullOwner_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GazeRequests().Set(null, Vector3.zero, 0));
        }

        [Test]
        public void Requests_UpdatingEveryFrame_KeepsOneEntryPerOwner()
        {
            var requests = new GazeRequests();
            requests.Set(_tether, Vector3.one, 2);
            requests.Set(_sonar, Vector3.one, 1);
            for (int i = 0; i < 10; i++)
            {
                requests.Set(_tether, new Vector3(i, 0f, 0f), 2);
                requests.TryGetTop(out _);
            }

            Assert.AreEqual(2, requests.Count);
        }
    }
}
