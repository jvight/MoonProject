using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The rare big boulders (Art's R1 Rock_10..13): a handful on the basin floor, deterministic, never near the pad,
    /// inside an anchored place's reach, in a route's corridor or on steep ground.
    /// </summary>
    public sealed class BigBoulderTests
    {
        private const int MaxBoulders = 12;

        [Test]
        public void Plan_IsRareAndDeterministic_AndKeepsClearOfThePadPlacesAndRoutes()
        {
            var surfaceSettings = new SurfaceSettings();
            var surface = new MoonSurface(surfaceSettings, WorldSettings.DefaultSeed);
            var anchors = new WorldAnchors(surface, surfaceSettings.Canyon, new RelaySettings());
            var tuning = new BigBoulderSettings();
            Assert.IsTrue(anchors.TryGet(WorldAnchorIds.PlaceWake, out WorldAnchor wake));
            Assert.IsTrue(anchors.TryGet(WorldAnchorIds.PlaceLander, out WorldAnchor lander));
            var routes = new List<Vector2>
            {
                new Vector2(wake.Position.x, wake.Position.z), new Vector2(lander.Position.x, lander.Position.z),
            };

            List<BigBoulderPlanner.Site> plan = BigBoulderPlanner.Plan(surface, anchors, tuning, routes);
            List<BigBoulderPlanner.Site> again = BigBoulderPlanner.Plan(surface, anchors, tuning, routes);
            Assert.That(plan.Count, Is.InRange(1, MaxBoulders), "a rare tier, but present");
            Assert.AreEqual(plan.Count, again.Count);
            float maxSlopeCos = Mathf.Cos(tuning.MaxSlope * Mathf.Deg2Rad);
            for (int i = 0; i < plan.Count; i++)
            {
                Vector3 at = plan[i].Position;
                Assert.AreEqual(at, again[i].Position, "deterministic");
                var flat = new Vector2(at.x, at.z);
                Assert.GreaterOrEqual(flat.magnitude, tuning.PadClearance, "off the pad");
                Assert.AreEqual(surface.SampleHeight(at.x, at.z), at.y, 1e-3f, "on the ground");
                Assert.GreaterOrEqual(surface.SampleNormal(at.x, at.z).y, maxSlopeCos, "on gentle ground");
                for (int a = 0; a < anchors.Count; a++)
                {
                    WorldAnchor anchor = anchors.Get(a);
                    Assert.GreaterOrEqual(Vector2.Distance(flat, new Vector2(anchor.Position.x, anchor.Position.z)),
                        anchor.Radius + tuning.AnchorClearance, $"clear of {anchor.Id}");
                }

                Vector2 a0 = routes[0];
                Vector2 span = routes[1] - a0;
                float t = Mathf.Clamp01(Vector2.Dot(flat - a0, span) / span.sqrMagnitude);
                Assert.GreaterOrEqual(Vector2.Distance(flat, a0 + span * t), tuning.RouteClearance, "off the route");
                Assert.That(plan[i].Rock, Is.InRange(0, tuning.Rocks.Length - 1));
            }
        }
    }
}
