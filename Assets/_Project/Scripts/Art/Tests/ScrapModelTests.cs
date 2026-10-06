using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    public sealed class ScrapModelTests
    {
        public static IEnumerable<string> Names()
        {
            return ScrapModelBuilder.Names;
        }

        [TestCaseSource(nameof(Names))]
        public void Scrap_IsSmall_Glinting_AndPivotedOnItsCentreOfMass(string name)
        {
            ModelNode scrap = ScrapModelBuilder.CreateModel(name);
            LowPolyMeshBuilder geometry = scrap.Mesh.Geometry;
            Vector3 size = geometry.Bounds.size;

            Assert.AreEqual(name, scrap.Name);
            MeshChecks.AssertWellFormed(geometry);
            Assert.That(Mathf.Max(size.x, Mathf.Max(size.y, size.z)), Is.InRange(0.3f, 0.5f), "longest side");
            Assert.Less(geometry.VolumeCentroid().magnitude, 1e-3f, "pivot at the centre of mass");

            bool glows = false;
            for (int t = 0; t < geometry.TriangleCount; t++)
            {
                glows |= MeshChecks.SwatchOf(geometry, t) == PaletteSwatch.TechGlow;
            }

            Assert.IsTrue(glows, $"{name} needs a TechGlow accent");
        }

        [Test]
        public void UnknownScrap_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => ScrapModelBuilder.CreateModel("Scrap_Spoon"));
        }
    }
}
