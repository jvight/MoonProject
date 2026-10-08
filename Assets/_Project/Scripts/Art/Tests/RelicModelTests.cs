using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>The six relics at true size (VISION ruling 13), pivoted on their centre of mass.</summary>
    public sealed class RelicModelTests
    {
        // Ruling 13: small personal things stay under about half a metre.
        private const float MaxRelicSize = 0.5f + 0.002f;

        public static IEnumerable<string> Ids()
        {
            return RelicModelBuilder.Ids;
        }

        [TestCaseSource(nameof(Ids))]
        public void Relic_IsTrueSized_WithinBudget_AndPivotedOnItsCentreOfMass(string id)
        {
            ModelNode relic = RelicModelBuilder.CreateModel(id);
            LowPolyMeshBuilder geometry = relic.Mesh.Geometry;
            Vector3 size = geometry.Bounds.size;

            Assert.AreEqual("Relic_" + id, relic.Name);
            Assert.AreEqual(0, relic.Children.Count);
            MeshChecks.AssertWellFormed(geometry);
            float largest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            Assert.AreEqual(RelicModelBuilder.TrueSize(id), largest, 0.002f, "largest dimension at its true size");
            Assert.LessOrEqual(largest, MaxRelicSize, "never a giant prop");
            Assert.That(geometry.TriangleCount, Is.InRange(300, 1200), "triangle budget");
            Assert.Less(geometry.VolumeCentroid().magnitude, 1e-3f, "pivot at the centre of mass");
        }

        [Test]
        public void Relics_AreDistinct_AndUnknownIdsThrow()
        {
            var sizes = new HashSet<Vector3>();
            foreach (string id in RelicModelBuilder.Ids)
            {
                Assert.IsTrue(sizes.Add(RelicModelBuilder.CreateModel(id).Mesh.Geometry.Bounds.size), id);
            }

            Assert.AreEqual(6, sizes.Count);
            Assert.Throws<System.ArgumentException>(() => RelicModelBuilder.CreateModel("spoon"));
        }
    }
}
