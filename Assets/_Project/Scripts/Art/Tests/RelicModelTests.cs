using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    public sealed class RelicModelTests
    {
        public static IEnumerable<string> Ids()
        {
            return RelicModelBuilder.Ids;
        }

        [TestCaseSource(nameof(Ids))]
        public void Relic_IsRelicSized_WithinBudget_AndPivotedOnItsCentreOfMass(string id)
        {
            ModelNode relic = RelicModelBuilder.CreateModel(id);
            LowPolyMeshBuilder geometry = relic.Mesh.Geometry;
            Vector3 size = geometry.Bounds.size;

            Assert.AreEqual("Relic_" + id, relic.Name);
            Assert.AreEqual(0, relic.Children.Count);
            MeshChecks.AssertWellFormed(geometry);
            Assert.That(Mathf.Max(size.x, Mathf.Max(size.y, size.z)), Is.InRange(0.5f, 1.2f), "largest dimension");
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
