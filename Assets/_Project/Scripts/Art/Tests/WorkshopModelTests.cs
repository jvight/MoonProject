using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>M3-03 art: Kenji's workbench on the lander's WorkshopAnchor and the Hover-Jump coils on 07.</summary>
    public sealed class WorkshopModelTests
    {
        private static readonly Vector3 ShopPadCentre = new Vector3(12.5f, 0f, 1.2f);
        private const float ShopPadRadius = 2.4f;

        [Test]
        public void Lander_HasTheWorkshopAnchor_WhereGameplayExpectsIt()
        {
            ModelNode anchor = BaseModelBuilder.CreateLander().GetDescendant("WorkshopAnchor");

            Assert.IsNull(anchor.Mesh);
            Assert.AreEqual(new Vector3(12.5f, 0f, -2f), anchor.LocalPosition);
            Assert.AreEqual(Quaternion.identity, anchor.LocalRotation);
        }

        [Test]
        public void Workbench_HasItsGlowAndSparkNodes_AndStandsOnItsPivot()
        {
            ModelNode bench = BaseModelBuilder.CreateWorkbench();

            Assert.AreEqual("Workbench", bench.Name);
            CollectionAssert.AreEqual(new[] { "Lights", "SparkSocket" }, bench.Children.Select(c => c.Name).ToArray());
            Assert.IsNull(bench.GetDescendant("SparkSocket").Mesh);
            LowPolyMeshBuilder lights = bench.GetDescendant("Lights").Mesh.Geometry;
            for (int t = 0; t < lights.TriangleCount; t++)
            {
                Assert.IsTrue(Palette.IsEmissive(MeshChecks.SwatchOf(lights, t)), "Lights only glows");
            }

            LowPolyMeshBuilder geometry = bench.Mesh.Geometry;
            MeshChecks.AssertWellFormed(geometry);
            Assert.AreEqual(0f, geometry.Bounds.min.y, 1e-3f);
            Assert.That(geometry.Bounds.size.x, Is.GreaterThan(2f), "reads from 40 m");
            Assert.That(geometry.Bounds.max.y, Is.GreaterThan(2f), "pegboard and lamp stand tall");
            Assert.That(geometry.TriangleCount, Is.InRange(2000, 6000), "triangle budget");
        }

        [Test]
        public void Workbench_OnItsAnchor_StaysOffTheShopPad_AndClearOfTheShelf()
        {
            ModelNode bench = BaseModelBuilder.CreateWorkbench();
            Vector3 anchor = BaseModelBuilder.WorkshopAnchor;
            float shelfRight = BaseModelBuilder.ShelfAnchor.x + BaseModelBuilder.ShelfWidth * 0.5f + 0.2f;

            foreach (Vector3 local in MeshChecks.Points(bench, Matrix4x4.identity))
            {
                Vector3 p = anchor + local;
                Assert.LessOrEqual(p.z, -1.2f, "bench front edge stays behind z = -1.2");
                float fromPad = new Vector2(p.x - ShopPadCentre.x, p.z - ShopPadCentre.z).magnitude;
                Assert.GreaterOrEqual(fromPad, ShopPadRadius, "nothing stands on the shop pad");
                Assert.Greater(p.x, shelfRight, "clear of the museum shelf");
            }
        }

        [Test]
        public void HoverCoils_HaveSquashableSprings_AndGlowOffRings()
        {
            ModelNode coils = RoverModelBuilder.CreateHoverCoils();
            string[] corners = { "FL", "FR", "RL", "RR" };

            Assert.AreEqual("HoverCoils", coils.Name);
            CollectionAssert.AreEqual(corners.Select(c => "Coil_" + c).ToArray(),
                coils.Children.Select(c => c.Name).ToArray());
            foreach (string corner in corners)
            {
                ModelNode spring = coils.GetDescendant("Coil_" + corner);
                Assert.Less(spring.Mesh.Geometry.Bounds.max.y, 1e-3f, "springs hang below their pivot");
                ModelNode glow = spring.GetDescendant("Glow_" + corner);
                Assert.AreEqual(ModelMaterial.PaletteGlowOff, glow.Material);
                LowPolyMeshBuilder ring = glow.Mesh.Geometry;
                for (int t = 0; t < ring.TriangleCount; t++)
                {
                    Assert.AreEqual(PaletteSwatch.EyeGlass, MeshChecks.SwatchOf(ring, t));
                }
            }
        }

        [Test]
        public void HoverCoils_OnTheCoilSocket_FitUnderTheBelly_AboveTheGround()
        {
            ModelNode rover = RoverModelBuilder.CreateModel();
            ModelNode socket = rover.GetDescendant("CoilSocket");
            Assert.IsNull(socket.Mesh);
            Assert.AreEqual(Quaternion.identity, socket.LocalRotation);
            Assert.AreEqual("CoilSocket", rover.Children[rover.Children.Count - 1].Name, "appended last");

            foreach (Vector3 local in MeshChecks.Points(RoverModelBuilder.CreateHoverCoils(), Matrix4x4.identity))
            {
                Vector3 p = socket.LocalPosition + local;
                Assert.Less(Mathf.Abs(p.x), 0.4f, "between the bogies");
                Assert.Greater(p.y, 0.1f, "clear of the ground");
                Assert.LessOrEqual(p.y, socket.LocalPosition.y + 1e-3f, "under the belly");
            }
        }
    }
}
