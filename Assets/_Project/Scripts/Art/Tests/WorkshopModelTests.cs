using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// M3-03 art: the lander's WorkshopAnchor (Kenji's Rover Bay stands on it, clear of the museum shelf) and the
    /// Hover-Jump coils on 07.
    /// </summary>
    public sealed class WorkshopModelTests
    {
        private const float ShelfClearance = 0.2f;

        [Test]
        public void Lander_HasTheWorkshopAnchor_WhereGameplayExpectsIt()
        {
            ModelNode anchor = BaseModelBuilder.CreateLander().GetDescendant("WorkshopAnchor");

            Assert.IsNull(anchor.Mesh);
            Assert.AreEqual(new Vector3(12.5f, 0f, -2f), anchor.LocalPosition);
            Assert.AreEqual(Quaternion.identity, anchor.LocalRotation);
        }

        [Test]
        public void RoverBay_OnItsAnchor_StandsClearOfTheShelf()
        {
            var shelf = new Bounds(BaseModelBuilder.ShelfAnchor, Vector3.zero);
            foreach (Vector3 p in MeshChecks.Points(BaseModelBuilder.CreateShelf(),
                Matrix4x4.Translate(BaseModelBuilder.ShelfAnchor), false))
            {
                shelf.Encapsulate(p);
            }

            shelf.Expand(new Vector3(ShelfClearance, 0f, ShelfClearance) * 2f);
            foreach (Vector3 p in MeshChecks.Points(BaseModelBuilder.CreateRoverBay(),
                Matrix4x4.Translate(BaseModelBuilder.WorkshopAnchor), false))
            {
                bool onShelf = p.x > shelf.min.x && p.x < shelf.max.x && p.z > shelf.min.z && p.z < shelf.max.z;
                Assert.IsFalse(onShelf, $"the bay reaches the museum shelf at {p}");
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
            var names = new List<string>();
            foreach (ModelNode child in rover.Children)
            {
                names.Add(child.Name);
            }

            Assert.Greater(names.IndexOf("CoilSocket"), names.IndexOf("DustSocket_R"), "appended after the M1 rig");

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
