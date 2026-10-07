using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// A friend making its own way home (Bell): the player's view as maths, and the walk that ends with her set down
    /// at home only when nobody could see it.
    /// </summary>
    public sealed class WalkHomeTests
    {
        private const float Frame = 0.02f;

        private readonly List<Object> _created = new List<Object>();
        private FriendTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = Track(ScriptableObject.CreateInstance<FriendTuning>());
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void View_SeesWhatIsOnScreen_AndNothingBehind()
        {
            var view = new ViewFrustum(Vector3.zero, Quaternion.identity, 60f, 1f);
            Assert.IsTrue(view.Sees(new Vector3(0f, 0f, 10f), 0.1f));
            Assert.IsTrue(view.Sees(new Vector3(5f, 0f, 10f), 0.1f), "27 degrees off: inside the 30 degree half-width");
            Assert.IsFalse(view.Sees(new Vector3(10f, 0f, 10f), 0.1f), "45 degrees off");
            Assert.IsFalse(view.Sees(new Vector3(0f, 7f, 10f), 0.1f), "above the top edge");
            Assert.IsFalse(view.Sees(new Vector3(0f, 0f, -10f), 1f), "behind");
            Assert.IsTrue(view.Sees(new Vector3(0f, 0f, -0.5f), 1f), "a body around the eye is seen");
            Assert.IsTrue(view.Sees(new Vector3(6.5f, 0f, 10f), 1.5f), "partly on screen counts");

            var wide = new ViewFrustum(Vector3.zero, Quaternion.identity, 60f, 2f);
            Assert.IsTrue(wide.Sees(new Vector3(10f, 0f, 10f), 0.1f), "a wide screen sees farther to the side");
            var turned = new ViewFrustum(Vector3.zero, Quaternion.Euler(0f, 180f, 0f), 60f, 1f);
            Assert.IsTrue(turned.Sees(new Vector3(0f, 0f, -10f), 0.1f));
        }

        [Test]
        public void WalkHome_WalksItsRouteOnTheSurface_WhileWatched()
        {
            TestWorld world = TestWorld.Basin();
            var route = new[]
            {
                SurfaceRules.OnSurface(world, 0f, 40f), SurfaceRules.OnSurface(world, 20f, 40f),
                SurfaceRules.OnSurface(world, 20f, 10f),
            };
            var walk = new WalkHome(SurfaceRules.OnSurface(world, 0f, 20f), route);
            var rover = new Vector3(0f, 0f, 15f);
            ViewFrustum view = Watching(rover);
            Vector3 last = walk.Position;
            float longest = 0f;
            int frames = 0;
            while (!walk.IsHome && frames < 10000)
            {
                walk.Step(Frame, world, view, rover, _tuning);
                longest = Mathf.Max(longest, SurfaceRules.HorizontalDistance(last, walk.Position));
                Assert.AreEqual(world.SampleHeight(walk.Position.x, walk.Position.z), walk.Position.y, 1e-3f);
                last = walk.Position;
                frames++;
            }

            Assert.IsTrue(walk.IsHome, "watched all the way, it walks all the way");
            Assert.IsFalse(walk.PlacedUnseen);
            Assert.AreEqual(20f, walk.Position.x, 1e-3f);
            Assert.AreEqual(10f, walk.Position.z, 1e-3f);
            Assert.LessOrEqual(longest, _tuning.WalkSpeed * Frame + 1e-3f, "never a jump");
            float shortest = 20f + 20f + 30f - 4f * _tuning.WalkWaypointReach;
            Assert.That(frames * Frame, Is.InRange(shortest / _tuning.WalkSpeed, 70f / _tuning.WalkSpeed + 0.1f),
                "at walking pace, rounding the corners");
        }

        [Test]
        public void WalkHome_IsSetDownAtHome_OnlyWhenNobodyCouldSeeIt()
        {
            TestWorld world = TestWorld.Flat();
            var rover = new Vector3(0f, 0f, 0f);
            ViewFrustum view = Watching(rover);
            var behindHome = new[] { new Vector3(0f, 0f, -130f) };

            var unseen = new WalkHome(new Vector3(0f, 0f, -100f), behindHome);
            unseen.Step(Frame, world, view, rover, _tuning);
            Assert.IsTrue(unseen.IsHome, "out of view, home out of view, far from 07");
            Assert.IsTrue(unseen.PlacedUnseen);
            Assert.AreEqual(behindHome[0], unseen.Position);

            var near = new WalkHome(new Vector3(0f, 0f, -30f), behindHome);
            near.Step(Frame, world, view, rover, _tuning);
            Assert.IsFalse(near.IsHome, "too close to 07: it keeps walking");

            var homeInView = new WalkHome(new Vector3(0f, 0f, -100f), new[] { new Vector3(0f, 0f, 50f) });
            homeInView.Step(Frame, world, view, rover, _tuning);
            Assert.IsFalse(homeInView.IsHome, "its home is on screen: no popping in there");

            var seen = new WalkHome(new Vector3(0f, 0f, 100f), behindHome);
            seen.Step(Frame, world, view, rover, _tuning);
            Assert.IsFalse(seen.IsHome, "on screen: no vanishing");
            Assert.Less(seen.Position.z, 100f, "it walks on");
        }

        private static ViewFrustum Watching(Vector3 rover)
        {
            Vector3 camera = rover + new Vector3(0f, 3f, -8f);
            Quaternion look = Quaternion.LookRotation(rover + Vector3.up - camera);
            return new ViewFrustum(camera, look, 60f, 16f / 9f);
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
