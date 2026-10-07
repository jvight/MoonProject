using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// A friend placed at the World's anchors (Bell): her site, her parts and her way home (a walking line found over
    /// drivable ground).
    /// </summary>
    public sealed class FriendAnchorTests
    {
        private static readonly Vector3 Corner = new Vector3(-10f, 0f, 0f);

        [Test]
        public void Placement_PutsHerAtTheTerminus_FacingBack_AndHerPartsInTheAlcoves()
        {
            TestWorld world = TestWorld.Flat();
            var anchors = Canyon(3);
            FriendSite site = FriendAnchorPlanner.Plan(anchors, world, Placement(), 3, out string problem);
            Assert.IsNull(problem);
            Assert.AreEqual(new Vector3(-277f, 0f, 60f), site.Position, "2 m in, her back to the wall");
            Assert.AreEqual(Vector3.right, site.Facing, "facing back toward 07 arriving");
            Assert.AreEqual(new Vector3(-225f, 0f, 75f), site.Parts[0]);
            Assert.AreEqual(new Vector3(-245f, 0f, 45f), site.Parts[1]);
            Assert.AreEqual(new Vector3(-262f, 0f, 78f), site.Parts[2]);
        }

        [Test]
        public void Placement_LaysLeftoverPartsAlongTheDrivingLine_FarApart()
        {
            TestWorld world = TestWorld.Flat();
            FriendSite site = FriendAnchorPlanner.Plan(Canyon(1), world, Placement(), 3, out string problem);
            Assert.IsNull(problem);
            Assert.AreEqual(new Vector3(-225f, 0f, 75f), site.Parts[0], "the one alcove there is");
            const float relaxed = 40f * 0.6f;
            for (int i = 0; i < 3; i++)
            {
                Assert.GreaterOrEqual(SurfaceRules.HorizontalDistance(site.Parts[i], site.Position), relaxed - 1e-3f);
                for (int j = 0; j < i; j++)
                {
                    Assert.GreaterOrEqual(SurfaceRules.HorizontalDistance(site.Parts[i], site.Parts[j]),
                        relaxed - 1e-3f, "this short line holds them at the relaxed spacing");
                }
            }

            FriendSite roomy = FriendAnchorPlanner.Plan(Canyon(0), world, Placement(), 1, out problem);
            Assert.IsNull(problem);
            Assert.GreaterOrEqual(SurfaceRules.HorizontalDistance(roomy.Parts[0], roomy.Position), 40f,
                "with room, a leftover part keeps the full spacing from her");

            Assert.IsNull(FriendAnchorPlanner.Plan(new FakeAnchors(), world, Placement(), 3, out problem));
            StringAssert.Contains("canyon.terminus", problem, "a missing anchor is reported, never invented");
        }

        [Test]
        public void WayHome_WalksFromTheTerminusToTheWayOut_DownItsStep_AndHome()
        {
            TestWorld world = TestWorld.Flat();
            var tuning = ScriptableObject.CreateInstance<FriendTuning>();
            try
            {
                Vector3[] route = FriendAnchorPlanner.WayHome(Canyon(3), world, Placement(), Corner, 5f, tuning,
                    out string problem);
                Assert.IsNull(problem);
                CollectionAssert.AreEqual(new[]
                {
                    new Vector3(-275f, 0f, 60f), new Vector3(-200f, 0f, 30f), new Vector3(-195f, 0f, 30f), Corner,
                }, route, "open ground: straight to the way out, on past its step, then home");
                var noExit = new FakeAnchors(new WorldAnchor(WorldAnchorIds.CanyonTerminus, Vector3.zero,
                    Vector3.left, 5f));
                Assert.IsNull(FriendAnchorPlanner.WayHome(noExit, world, Placement(), Corner, 5f, tuning,
                    out problem));
                StringAssert.Contains("canyon.exit", problem);
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void WalkPath_GoesAroundAWall_ThroughItsGap_OnGentleGround()
        {
            TestWorld world = TestWorld.WithWall();
            var tuning = ScriptableObject.CreateInstance<FriendTuning>();
            try
            {
                var from = new Vector3(-50f, 0f, 0f);
                var to = new Vector3(50f, 0f, 0f);
                Vector3[] path = WalkPathPlanner.Plan(world, from, to, tuning);
                Assert.IsNotNull(path);
                Assert.AreEqual(from, path[0]);
                Assert.AreEqual(to, path[path.Length - 1]);
                bool throughGap = false;
                for (int i = 0; i < path.Length - 1; i++)
                {
                    float length = SurfaceRules.HorizontalDistance(path[i], path[i + 1]);
                    for (float along = 0f; along <= length; along += 0.5f)
                    {
                        Vector3 point = Vector3.Lerp(path[i], path[i + 1], along / length);
                        Assert.Less(world.SampleHeight(point.x, point.z), 1f, $"never over the wall ({point})");
                        throughGap |= Mathf.Abs(point.x) < 1.25f && point.z > TestWorld.WallGap;
                    }
                }

                Assert.IsTrue(throughGap, "through the gap");
                Assert.Less(path.Length, 8, "string-pulled into a few straight legs");
                Assert.IsNull(WalkPathPlanner.Plan(world, from, new Vector3(50f, 0f, 400f), tuning),
                    "off the drivable ground: no way");
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        private static FriendAnchorPlacement Placement()
        {
            return new FriendAnchorPlacement(new AnchorSpot(WorldAnchorIds.CanyonTerminus, new Vector2(0f, 2f)),
                WorldAnchorIds.CanyonAlcovePrefix, WorldAnchorIds.CanyonLanding, 40f, WorldAnchorIds.CanyonExit);
        }

        private static FakeAnchors Canyon(int alcoves)
        {
            var west = new Vector3(-1f, 0f, 0f);
            var list = new List<WorldAnchor>
            {
                new WorldAnchor(WorldAnchorIds.CanyonLanding, new Vector3(-205f, 0f, 60f), west, 8f),
                new WorldAnchor(WorldAnchorIds.CanyonTerminus, new Vector3(-275f, 0f, 60f), west, 4f),
                new WorldAnchor(WorldAnchorIds.CanyonExit, new Vector3(-200f, 0f, 30f), Vector3.right, 6f),
            };
            Vector3[] alcove =
            {
                new Vector3(-225f, 0f, 75f), new Vector3(-245f, 0f, 45f), new Vector3(-262f, 0f, 78f),
            };
            for (int i = 0; i < alcoves; i++)
            {
                list.Add(new WorldAnchor(WorldAnchorIds.CanyonAlcovePrefix + i, alcove[i], west, 5f));
            }

            return new FakeAnchors(list.ToArray());
        }

        private sealed class FakeAnchors : IWorldAnchors
        {
            private readonly WorldAnchor[] _anchors;

            public FakeAnchors(params WorldAnchor[] anchors)
            {
                _anchors = anchors;
            }

            public int Count => _anchors.Length;

            public WorldAnchor Get(int index)
            {
                return _anchors[index];
            }

            public bool TryGet(string id, out WorldAnchor anchor)
            {
                foreach (WorldAnchor candidate in _anchors)
                {
                    if (candidate.Id == id)
                    {
                        anchor = candidate;
                        return true;
                    }
                }

                anchor = default;
                return false;
            }
        }
    }
}
