using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Routing;

namespace MoonProject.Autoplay.Tests
{
    /// <summary>The bot's coarse route round props (Plan over a grid, pulled straight where the way is clear).</summary>
    public sealed class RoutePlannerTests
    {
        private const float Cell = 2f;
        private const float Margin = 30f;
        private const float Relax = 4f;

        private static bool Wall(Vector2 point)
        {
            // a wall across x = 0 from z = -20 to z = 20
            return Mathf.Abs(point.x) < 3f && Mathf.Abs(point.y) < 20f;
        }

        [Test]
        public void AClearWay_IsOneStraightDrive()
        {
            var way = new List<Vector2>();
            bool planned = RoutePlanner.Plan(new Vector2(-30f, 40f), new Vector2(30f, 40f), Wall, Cell, Margin, Relax,
                way);

            Assert.IsTrue(planned);
            Assert.AreEqual(1, way.Count);
            Assert.AreEqual(new Vector2(30f, 40f), way[0]);
        }

        [Test]
        public void AWallInTheWay_IsDrivenRound_NeverThrough()
        {
            var way = new List<Vector2>();
            Vector2 start = new Vector2(-15f, 0f);
            Vector2 goal = new Vector2(15f, 0f);

            bool planned = RoutePlanner.Plan(start, goal, Wall, Cell, Margin, Relax, way);

            Assert.IsTrue(planned);
            Assert.Greater(way.Count, 1, "a corner to go round");
            Assert.AreEqual(goal, way[way.Count - 1]);
            Vector2 from = start;
            foreach (Vector2 point in way)
            {
                Assert.IsTrue(RoutePlanner.Clear(from, point, Wall, Cell / 2f), $"{from} to {point} is clear");
                from = point;
            }
        }

        [Test]
        public void AGoalBesideAProp_IsReached_AndAStartAgainstOneIsLeft()
        {
            var way = new List<Vector2>();
            Vector2 start = new Vector2(-4f, 0f);
            Vector2 goal = new Vector2(4f, 0f);

            Assert.IsTrue(RoutePlanner.Plan(start, goal, Wall, Cell, Margin, Relax, way));
            Assert.AreEqual(goal, way[way.Count - 1]);
        }

        [Test]
        public void AGoalWalledIn_FallsBackToTheStraightDrive()
        {
            var way = new List<Vector2>();
            Vector2 goal = new Vector2(100f, 100f);

            bool planned = RoutePlanner.Plan(Vector2.zero, goal, point => (point - goal).magnitude is > 8f and < 12f,
                Cell, Margin, Relax, way);

            Assert.IsFalse(planned);
            CollectionAssert.AreEqual(new[] { goal }, way);
        }
    }
}
