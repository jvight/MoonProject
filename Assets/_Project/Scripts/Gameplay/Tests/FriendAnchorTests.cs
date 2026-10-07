using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>A friend placed at the World's anchors (Bell): her site, her parts and her way home.</summary>
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
        public void WayHome_GoesBackOutThroughTheAlcoves_ThenTheExitAndHome()
        {
            TestWorld world = TestWorld.Flat();
            Vector3[] route = FriendAnchorPlanner.WayHome(Canyon(3), world, Placement(), Corner, 5f,
                out string problem);
            Assert.IsNull(problem);
            CollectionAssert.AreEqual(new[]
            {
                new Vector3(-262f, 0f, 78f), new Vector3(-245f, 0f, 45f), new Vector3(-225f, 0f, 75f),
                new Vector3(-205f, 0f, 60f), new Vector3(-200f, 0f, 30f), new Vector3(-195f, 0f, 30f), Corner,
            }, route);
            var noExit = new FakeAnchors(new WorldAnchor(WorldAnchorIds.CanyonLanding, Vector3.zero, Vector3.left, 5f));
            Assert.IsNull(FriendAnchorPlanner.WayHome(noExit, world, Placement(), Corner, 5f, out problem));
            StringAssert.Contains("canyon.exit", problem);
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
