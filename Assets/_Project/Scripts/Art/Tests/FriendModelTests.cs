using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>The friend Tilly rig contract, her broken variant, her part pickups and the lander perch.</summary>
    public sealed class FriendModelTests
    {
        private static readonly string[] TillyNodes =
        {
            "Body", "Eye", "Rotor_FL", "Rotor_FR", "Rotor_RL", "Rotor_RR", "Antenna",
            "PartLamp_0", "PartLamp_1", "PartLamp_2", "TetherPoint",
        };

        [TestCase(false)]
        [TestCase(true)]
        public void Tilly_HasExactlyTheContractNodes(bool broken)
        {
            ModelNode tilly = FriendModelBuilder.CreateTilly(broken);

            Assert.AreEqual(broken ? "Tilly_Broken" : "Tilly", tilly.Name);
            CollectionAssert.AreEqual(TillyNodes, tilly.Children.Select(child => child.Name).ToArray());
            Assert.IsNull(tilly.Mesh);
            Assert.IsNull(tilly.GetDescendant("TetherPoint").Mesh, "TetherPoint is an empty");
            foreach (ModelNode node in tilly.Children.Where(child => child.Mesh != null))
            {
                MeshChecks.AssertWellFormed(node.Mesh.Geometry);
            }
        }

        [Test]
        public void Tilly_StandsOnHerFeet_IsSmall_AndWithinBudget()
        {
            ModelNode tilly = FriendModelBuilder.CreateTilly(false);
            Bounds bounds = Measure(tilly, out int triangles);

            Assert.AreEqual(0f, bounds.min.y, 1e-3f, "feet on the ground at the pivot");
            Assert.That(Mathf.Max(bounds.size.x, bounds.size.z), Is.InRange(0.5f, 0.75f), "about 0.6 m wide");
            Assert.That(triangles, Is.InRange(1500, 3000), "triangle budget");
            Assert.Less(tilly.GetDescendant("TetherPoint").LocalPosition.y, 0.1f, "tether hook under the body");
            Assert.Greater(tilly.GetDescendant("Eye").LocalPosition.z, 0.15f, "the eye looks out along +Z");
        }

        [Test]
        public void Tilly_RotorsSpinAboutY_InTheirDucts()
        {
            ModelNode tilly = FriendModelBuilder.CreateTilly(false);
            foreach (string name in new[] { "Rotor_FL", "Rotor_FR", "Rotor_RL", "Rotor_RR" })
            {
                ModelNode rotor = tilly.GetDescendant(name);
                Assert.AreEqual(Quaternion.identity, rotor.LocalRotation, name);
                Bounds blades = rotor.Mesh.Geometry.Bounds;
                Assert.Less(Mathf.Max(blades.extents.x, blades.extents.z), 0.078f, $"{name} fits inside its duct");
            }

            Assert.Less(tilly.GetDescendant("Rotor_FL").LocalPosition.x, 0f);
            Assert.Greater(tilly.GetDescendant("Rotor_FL").LocalPosition.z, 0f);
            Assert.Greater(tilly.GetDescendant("Rotor_RR").LocalPosition.x, 0f);
            Assert.Less(tilly.GetDescendant("Rotor_RR").LocalPosition.z, 0f);
        }

        [Test]
        public void GlowRenderers_StartDarkWhereGameplayLightsThem()
        {
            ModelNode tilly = FriendModelBuilder.CreateTilly(false);
            ModelNode broken = FriendModelBuilder.CreateTilly(true);

            Assert.AreEqual(ModelMaterial.Palette, tilly.GetDescendant("Eye").Material, "repaired eye glows");
            Assert.AreEqual(ModelMaterial.PaletteGlowOff, broken.GetDescendant("Eye").Material, "broken eye is dark");
            CollectionAssert.Contains(Swatches(tilly.GetDescendant("Eye")), PaletteSwatch.EyeGlass);
            for (int i = 0; i < 3; i++)
            {
                foreach (ModelNode model in new[] { tilly, broken })
                {
                    ModelNode lamp = model.GetDescendant($"PartLamp_{i}");
                    Assert.AreEqual(ModelMaterial.PaletteGlowOff, lamp.Material, "part lamps start at 0/3");
                    CollectionAssert.AreEqual(new[] { PaletteSwatch.LampGlass }, Swatches(lamp));
                }
            }
        }

        [Test]
        public void Broken_LiesOnHerSide_InTheDust_WithABentRotor()
        {
            ModelNode tilly = FriendModelBuilder.CreateTilly(false);
            ModelNode broken = FriendModelBuilder.CreateTilly(true);
            Bounds bounds = Measure(broken, out _);

            Assert.AreEqual(-FriendModelBuilder.BrokenSink, bounds.min.y, 1e-3f, "rests sunk slightly in the dust");
            Vector3 up = broken.GetDescendant("Body").LocalRotation * Vector3.up;
            Assert.Less(Mathf.Abs(up.y), 0.5f, "lying on her side");
            Assert.AreNotEqual(tilly.GetDescendant("Rotor_FR").Mesh.Name, broken.GetDescendant("Rotor_FR").Mesh.Name);
            Assert.AreEqual(tilly.GetDescendant("Rotor_FL").Mesh.Name, broken.GetDescendant("Rotor_FL").Mesh.Name);
            CollectionAssert.Contains(Swatches(broken.GetDescendant("Body")), PaletteSwatch.DustLight);
        }

        [Test]
        public void Parts_AreSmall_AmberNotCyan_AndPivotedOnTheirCentreOfMass()
        {
            foreach (string name in FriendModelBuilder.PartNames)
            {
                ModelNode part = FriendModelBuilder.CreatePart(name);
                LowPolyMeshBuilder geometry = part.Mesh.Geometry;
                Vector3 size = geometry.Bounds.size;

                Assert.AreEqual(name, part.Name);
                MeshChecks.AssertWellFormed(geometry);
                Assert.That(Mathf.Max(size.x, Mathf.Max(size.y, size.z)), Is.InRange(0.25f, 0.4f), name);
                Assert.Less(geometry.VolumeCentroid().magnitude, 1e-3f, $"{name} pivot");
                List<PaletteSwatch> swatches = Swatches(part);
                CollectionAssert.Contains(swatches, PaletteSwatch.WarmLamp, $"{name} glints amber");
                CollectionAssert.DoesNotContain(swatches, PaletteSwatch.TechGlow, $"{name} must not read as scrap");
            }

            Assert.Throws<System.ArgumentException>(() => FriendModelBuilder.CreatePart("Part_TillySpoon"));
        }

        [Test]
        public void Lander_GainsTillysPerch_AndKeepsEveryExistingNode()
        {
            ModelNode lander = BaseModelBuilder.CreateLander();
            string[] expected =
            {
                "Windows", "ShelfAnchor", "TowerAnchor", "LampSocket_0", "LampSocket_1", "LampSocket_2",
                "LampSocket_3", "FriendSocket_tilly",
            };

            CollectionAssert.AreEqual(expected, lander.Children.Select(child => child.Name).ToArray());
            ModelNode perch = lander.GetDescendant("FriendSocket_tilly");
            Assert.IsNull(perch.Mesh);
            Assert.AreEqual(Quaternion.identity, perch.LocalRotation, "+Y up, +Z = the hatch side");
            Assert.Greater(perch.LocalPosition.y, 3f, "up on the porch");
            Assert.AreEqual(perch.LocalPosition.y, Support(lander.Mesh.Geometry, perch.LocalPosition), 0.01f,
                "the socket sits on the perch cushion");
        }

        private static Bounds Measure(ModelNode model, out int triangles)
        {
            var bounds = new Bounds();
            bool any = false;
            triangles = 0;
            foreach (ModelNode node in model.Children.Where(child => child.Mesh != null))
            {
                LowPolyMeshBuilder geometry = node.Mesh.Geometry;
                triangles += geometry.TriangleCount;
                Matrix4x4 local = node.LocalMatrix;
                for (int v = 0; v < geometry.VertexCount; v++)
                {
                    Vector3 p = local.MultiplyPoint3x4(geometry.Positions[v]);
                    if (any)
                    {
                        bounds.Encapsulate(p);
                    }
                    else
                    {
                        bounds = new Bounds(p, Vector3.zero);
                        any = true;
                    }
                }
            }

            return bounds;
        }

        /// <summary>Highest mesh point within 5 cm (horizontally) of <paramref name="point"/>.</summary>
        private static float Support(LowPolyMeshBuilder geometry, Vector3 point)
        {
            float top = float.MinValue;
            for (int v = 0; v < geometry.VertexCount; v++)
            {
                Vector3 p = geometry.Positions[v];
                if (new Vector2(p.x - point.x, p.z - point.z).magnitude < 0.05f)
                {
                    top = Mathf.Max(top, p.y);
                }
            }

            return top;
        }

        private static List<PaletteSwatch> Swatches(ModelNode node)
        {
            LowPolyMeshBuilder geometry = node.Mesh.Geometry;
            var swatches = new List<PaletteSwatch>();
            for (int t = 0; t < geometry.TriangleCount; t++)
            {
                PaletteSwatch swatch = MeshChecks.SwatchOf(geometry, t);
                if (!swatches.Contains(swatch))
                {
                    swatches.Add(swatch);
                }
            }

            return swatches;
        }
    }
}
