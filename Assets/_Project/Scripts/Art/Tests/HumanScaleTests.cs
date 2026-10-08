using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// The crew's world is human scale (VISION ruling 13: a person is 1.75 m): the lander's door lets one through
    /// upright, its porthole sits at their eye height, the porch rail at their waist, and the museum shelf has tiers
    /// they reach with room for the true-size relics.
    /// </summary>
    public sealed class HumanScaleTests
    {
        private const float Person = 1.75f;
        private const float LargestRelic = 0.5f;

        // The door panel stands proud of the cabin's front face in this band of depth, its porthole glass just
        // ahead of it.
        private const float DoorNear = 1.37f;
        private const float DoorFar = 1.45f;
        private const float PortholeFar = 1.5f;

        [Test]
        public void LanderDoor_LetsAPersonThroughUpright()
        {
            Bounds door = Faces(BaseModelBuilder.CreateLander().Mesh.Geometry, PaletteSwatch.Metal,
                p => Mathf.Abs(p.x) < 0.6f && p.z > DoorNear && p.z < DoorFar && p.y > BaseModelBuilder.LanderDeckTop);
            Assert.GreaterOrEqual(door.size.y, Person + 0.05f, "head room through the door");
            Assert.That(door.size.x, Is.InRange(0.7f, 1f), "a person's width with room");
            Assert.AreEqual(BaseModelBuilder.LanderDeckTop, door.min.y, 0.06f, "the door opens onto the deck");
        }

        [Test]
        public void LanderPorthole_AndRail_SitAtAPersonsEyeAndWaist()
        {
            ModelNode lander = BaseModelBuilder.CreateLander();
            float deck = BaseModelBuilder.LanderDeckTop;
            Bounds porthole = Faces(lander.GetDescendant("Windows").Mesh.Geometry, PaletteSwatch.WarmLamp,
                p => Mathf.Abs(p.x) < 0.3f && p.z > DoorNear && p.z < PortholeFar);
            Assert.That(porthole.center.y - deck, Is.InRange(1.4f, 1.65f), "the door's porthole at eye height");

            Bounds rail = Faces(lander.Mesh.Geometry, PaletteSwatch.WarmAccent,
                p => p.z > DoorNear && p.y > deck && p.y < deck + 1.5f);
            Assert.That(rail.min.y - deck, Is.GreaterThan(0.9f), "the porch rail at waist height");
            Assert.That(rail.max.y - deck, Is.LessThan(1.1f), "the porch rail at waist height");
        }

        [Test]
        public void MuseumShelf_IsWithinReach_WithRoomForEveryRelic()
        {
            ModelNode shelf = BaseModelBuilder.CreateShelf();
            List<Vector3> slots = Enumerable.Range(0, 6)
                .Select(i => shelf.GetDescendant($"Slot_{i}").LocalPosition).ToList();
            float lower = slots.Min(slot => slot.y);
            float upper = slots.Max(slot => slot.y);
            Bounds cabinet = shelf.Mesh.Geometry.Bounds;

            Assert.Less(lower, 0.5f, "the lower tier at knee height");
            Assert.That(upper, Is.InRange(0.8f, 1.3f), "the upper tier at a person's waist");
            Assert.That(cabinet.max.y, Is.InRange(Person, Person + 0.4f), "the awning just over a person's head");
            Assert.Less(BaseModelBuilder.ShelfWidth, 3.2f, "a shelf, not a building");
            Assert.Greater(upper - lower, LargestRelic + 0.1f, "a relic fits under the upper tier");
            Assert.Greater(cabinet.max.y - upper, LargestRelic + 0.1f, "a relic fits under the awning");
            List<float> row = slots.Where(slot => slot.y == lower).Select(slot => slot.x).OrderBy(x => x).ToList();
            for (int i = 1; i < row.Count; i++)
            {
                Assert.Greater(row[i] - row[i - 1], LargestRelic + 0.1f, "neighbouring relics never touch");
            }

            foreach (string id in RelicModelBuilder.Ids)
            {
                Assert.LessOrEqual(RelicModelBuilder.TrueSize(id), LargestRelic, id);
            }
        }

        /// <summary>
        /// The bounds of the <paramref name="swatch"/> triangles whose every corner passes <paramref name="keep"/>.
        /// </summary>
        private static Bounds Faces(LowPolyMeshBuilder geometry, PaletteSwatch swatch, System.Func<Vector3, bool> keep)
        {
            var points = new List<Vector3>();
            for (int t = 0; t < geometry.TriangleCount; t++)
            {
                if (MeshChecks.SwatchOf(geometry, t) != swatch)
                {
                    continue;
                }

                Vector3 a = geometry.Positions[t * 3];
                Vector3 b = geometry.Positions[t * 3 + 1];
                Vector3 c = geometry.Positions[t * 3 + 2];
                if (keep(a) && keep(b) && keep(c))
                {
                    points.Add(a);
                    points.Add(b);
                    points.Add(c);
                }
            }

            Assert.IsNotEmpty(points, $"no {swatch} faces where expected");
            var bounds = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 p in points)
            {
                bounds.Encapsulate(p);
            }

            return bounds;
        }
    }
}
