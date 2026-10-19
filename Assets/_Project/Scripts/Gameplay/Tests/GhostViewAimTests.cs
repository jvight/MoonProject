using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The placing ghost follows the camera's aim (owner build 95b1dd3a: "the preview doesn't follow the mouse"): the
    /// ground under the view's centre, held between the placement distances of 07, straight ahead at the far one when
    /// the view never meets the ground, and settling on the apron's bay.
    /// </summary>
    public sealed class GhostViewAimTests
    {
        private static readonly Vector3 Rover = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 Eye = new Vector3(0f, 3f, -7f);

        private BuildingTuning _tuning;
        private FlatGround _ground;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<BuildingTuning>();
            _ground = new FlatGround();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void TheGhostStandsWhereTheViewsCentreMeetsTheGround()
        {
            Vector3 aim = new Vector3(0f, 0f, 6f);
            Vector3 point = GhostAim.PointFromView(_ground, Eye, aim - Eye, Rover, null, _tuning);
            Assert.AreEqual(aim.x, point.x, 0.05f);
            Assert.AreEqual(aim.z, point.z, 0.05f);
        }

        [Test]
        public void TurningTheViewMovesTheGhostWithIt_NotAlwaysAheadOf07()
        {
            Vector3 left = new Vector3(-5f, 0f, 3f);
            Vector3 right = new Vector3(5f, 0f, 3f);
            Vector3 toLeft = GhostAim.PointFromView(_ground, Eye, left - Eye, Rover, null, _tuning);
            Vector3 toRight = GhostAim.PointFromView(_ground, Eye, right - Eye, Rover, null, _tuning);
            Assert.Less(toLeft.x, -2f, "a view turned left puts the ghost to the left");
            Assert.Greater(toRight.x, 2f, "and one turned right to the right");
        }

        [Test]
        public void TheGhostIsHeldBetweenTheNearestAndFarthestPlacementDistances()
        {
            Vector3 underFoot = GhostAim.PointFromView(_ground, Eye, new Vector3(0f, -3f, 7f), Rover, null, _tuning);
            Assert.GreaterOrEqual(Flat(underFoot - Rover).magnitude, _tuning.MinPlacementDistance - 1e-3f);

            Vector3 far = GhostAim.PointFromView(_ground, Eye, new Vector3(0f, -0.04f, 1f), Rover, null, _tuning);
            Assert.LessOrEqual(Flat(far - Rover).magnitude, _tuning.MaxPlacementDistance + 1e-3f);
            Assert.AreEqual(_tuning.MaxPlacementDistance, Flat(far - Rover).magnitude, 1e-3f,
                "a view that meets the ground beyond reach puts the ghost at the farthest spot on that line");
        }

        [Test]
        public void AViewThatNeverMeetsTheGround_PutsTheGhostStraightAheadAtTheFarthestDistance()
        {
            Vector3 point = GhostAim.PointFromView(_ground, Eye, new Vector3(0f, 0.3f, 1f), Rover, null, _tuning);
            Assert.AreEqual(0f, point.x, 1e-3f);
            Assert.AreEqual(_tuning.MaxPlacementDistance, point.z, 1e-3f);
        }

        [Test]
        public void TheGhostSettlesOnTheApronWhenTheViewRestsNearIt()
        {
            Vector3 apron = new Vector3(0.5f, 0f, 5f);
            Vector3 aim = new Vector3(0f, 0f, 4f);
            Vector3 point = GhostAim.PointFromView(_ground, Eye, aim - Eye, Rover, apron, _tuning);
            Assert.AreEqual(apron, point, "the pad's ghost settles on the marked bay");

            Vector3 away = GhostAim.PointFromView(_ground, Eye, new Vector3(-9f, 0f, 3f) - Eye, Rover, apron, _tuning);
            Assert.AreNotEqual(apron, away, "and does not when the view is elsewhere");
        }

        [Test]
        public void LookingAlongTheApron_AtAnyPitch_SettlesTheGhostOnIt()
        {
            Vector3 apron = new Vector3(0f, 0f, 5f);
            Vector3 level = GhostAim.PointFromView(_ground, Eye, new Vector3(0f, -0.05f, 1f), Rover, apron, _tuning);
            Vector3 steep = GhostAim.PointFromView(_ground, Eye, new Vector3(0f, -1.5f, 1f), Rover, apron, _tuning);
            Assert.AreEqual(apron, level, "a near-level view along the bay still settles on it");
            Assert.AreEqual(apron, steep, "and so does a steep one");

            Vector3 distant = new Vector3(0f, 0f, _tuning.WeldRange + 20f);
            Assert.AreNotEqual(distant, GhostAim.PointFromView(_ground, Eye, new Vector3(0f, -0.05f, 1f), Rover,
                distant, _tuning), "a bay beyond 07's reach is not settled on");
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        private sealed class FlatGround : ITerrainQuery
        {
            public Rect PlayableArea => new Rect(-500f, -500f, 1000f, 1000f);

            public bool IsDrivable(float x, float z)
            {
                return true;
            }

            public float SampleHeight(float x, float z)
            {
                return 0f;
            }

            public Vector3 SampleNormal(float x, float z)
            {
                return Vector3.up;
            }
        }
    }
}
