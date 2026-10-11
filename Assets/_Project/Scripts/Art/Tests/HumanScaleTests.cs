using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// The crew's world is human scale (VISION ruling 13: a person is 1.75 m): the habitat lander's cabin stands
    /// them upright with room over their head, its portholes sit at their eye height, and the lab's keepsake cubbies
    /// are within their reach with room for the true-size relics.
    /// </summary>
    public sealed class HumanScaleTests
    {
        private const float Person = 1.75f;
        private const float LargestRelic = 0.5f;

        // The lab's keepsake cubbies keep everything between a person's waist and shoulder (and 07's lens).
        private const float CubbyLow = 0.85f;
        private const float CubbyHigh = 1.65f;

        [Test]
        public void Cabin_StandsAPersonUpright_WithPortholesAtTheirEyes()
        {
            ModelNode lander = BaseModelBuilder.CreateLander();
            ModelNode lab = lander.GetDescendant(BaseModelBuilder.LabName);
            float floor = lab.LocalPosition.y;
            Bounds room = lab.Mesh.Geometry.Bounds;
            Assert.Greater(room.max.y, Person + 0.5f, "head room in the cabin");
            ModelNode windows = lander.GetDescendant("Windows");
            foreach (ModelNode pane in new[] { windows }.Concat(windows.Children))
            {
                float eye = pane.Mesh.Geometry.Bounds.center.y - floor;
                Assert.That(eye, Is.InRange(1.4f, 1.65f), $"{pane.Name} at a person's eye height");
            }
        }

        [Test]
        public void KeepsakeShelf_IsWithinReach_WithRoomForEveryRelic()
        {
            ModelNode shelf = BaseModelBuilder.CreateLander()
                .GetDescendant(BaseModelBuilder.StationPrefix + "Keepsakes");
            float floor = -shelf.LocalPosition.y;
            List<Vector3> slots = Enumerable.Range(0, 6)
                .Select(i => shelf.GetDescendant(BaseModelBuilder.SlotPrefix + i).LocalPosition).ToList();
            Assert.IsNull(shelf.GetDescendant(BaseModelBuilder.SlotPrefix + "6"));
            foreach (Vector3 slot in slots)
            {
                Assert.That(slot.y - floor, Is.InRange(CubbyLow, CubbyHigh), "one shelf at lens and waist height");
            }

            List<float> row = slots.Select(slot => slot.x).OrderBy(x => x).ToList();
            for (int i = 1; i < row.Count; i++)
            {
                Assert.Greater(row[i] - row[i - 1], 0.3f, "neighbouring keepsakes never crowd");
            }

            Bounds shelfBounds = shelf.Mesh.Geometry.Bounds;
            Assert.Greater(shelfBounds.max.y - slots[0].y, LargestRelic + 0.1f, "a relic fits under the shelf's top");
            foreach (string id in RelicModelBuilder.Ids)
            {
                Assert.LessOrEqual(RelicModelBuilder.TrueSize(id), LargestRelic, id);
            }
        }
    }
}
