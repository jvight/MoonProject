using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>The relay mast contract (M3-06), its broken pose, the relay part and its far silhouette.</summary>
    public sealed class RelayModelTests
    {
        private const float PadRadius = 3f;

        // The pad's front stays open (07 parks and radio-hops arrive there); only the back edge holds the mast.
        private const float PadFrontEdge = -1.2f;
        private const float ParkedHeight = 2.5f;

        // The default camera's vertical field of view and the reference screen height.
        private const float Fov = 60f;
        private const int ScreenHeight = 1080;

        [TestCase(false)]
        [TestCase(true)]
        public void Mast_HasExactlyTheContractNodes_UnderTheirParents(bool broken)
        {
            ModelNode mast = RelayModelBuilder.CreateMast(broken);
            var expected = new Dictionary<string, string>
            {
                { "Base", mast.Name },
                { "Mast", mast.Name },
                { "Dish", "Mast" },
                { "Lamp", "Mast" },
                { "PartSocket", mast.Name },
                { "BeamPoint", mast.Name },
            };
            var parents = new Dictionary<string, string>();
            Collect(mast, parents);

            Assert.AreEqual(broken ? "RelayMast_Broken" : "RelayMast", mast.Name);
            CollectionAssert.AreEquivalent(expected, parents);
            Assert.IsNull(mast.Mesh, "the root is an empty at the pad centre");
            foreach (string empty in new[] { "PartSocket", "BeamPoint" })
            {
                Assert.IsNull(mast.GetDescendant(empty).Mesh, $"{empty} is an empty");
            }

            foreach (string rendered in new[] { "Base", "Mast", "Dish", "Lamp" })
            {
                MeshChecks.AssertWellFormed(mast.GetDescendant(rendered).Mesh.Geometry);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Lamp_IsTheOnlyGlowRenderer_DarkUntilLit_AndWarm(bool broken)
        {
            ModelNode mast = RelayModelBuilder.CreateMast(broken);
            foreach (ModelNode node in Nodes(mast).Where(node => node.Mesh != null))
            {
                List<PaletteSwatch> swatches = MeshChecks.Swatches(node.Mesh.Geometry);
                if (node.Name == "Lamp")
                {
                    Assert.AreEqual(ModelMaterial.PaletteGlowOff, node.Material, "the lamp starts dark");
                    CollectionAssert.AreEqual(new[] { PaletteSwatch.SignalGlass }, swatches);
                    Assert.Greater(Palette.GetGlow(PaletteSwatch.SignalGlass).r, Palette.BloomThreshold,
                        "lit at 1 it glows HDR warm, above the bloom threshold");
                    continue;
                }

                Assert.AreEqual(ModelMaterial.Palette, node.Material, node.Name);
                Assert.IsFalse(swatches.Any(Palette.IsEmissive), $"{node.Name} must not glow");
            }
        }

        [Test]
        public void Mast_StandsAtTheBackOfThePad_AboutEightMetresToTheLamp_FacingHome()
        {
            ModelNode mast = RelayModelBuilder.CreateMast(false);
            ModelNode pole = mast.GetDescendant("Mast");
            Vector3 lamp = pole.LocalMatrix.MultiplyPoint3x4(pole.GetDescendant("Lamp").LocalPosition);
            Vector3 socket = mast.GetDescendant("PartSocket").LocalPosition;

            Assert.That(lamp.y, Is.InRange(7.5f, 9f), "about 8 m to the lamp");
            Assert.Less(pole.LocalPosition.z, PadFrontEdge, "the mast rises from the back of the pad");
            Assert.That(Flat(pole.LocalPosition), Is.LessThan(PadRadius), "on the flat pad");
            Assert.Greater(socket.z, pole.LocalPosition.z, "the junction box faces the pad and home");
            Assert.AreEqual(Quaternion.identity, mast.GetDescendant("PartSocket").LocalRotation, "+Z out of the box");
            Assert.Less(MeshChecks.Points(mast.GetDescendant("Base"), Matrix4x4.identity).Min(p => p.y), -0.3f,
                "the footing is sunk so a sloping pad edge never shows a gap");
            Vector3 dishFacing = pole.GetDescendant("Dish").LocalRotation * Vector3.forward;
            Assert.Greater(dishFacing.z, 0.99f, "the restored dish looks home");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PadFront_StaysClear_For07AndRadioHops(bool broken)
        {
            foreach (Vector3 p in MeshChecks.Points(RelayModelBuilder.CreateMast(broken), Matrix4x4.identity))
            {
                bool onPad = Flat(p) < PadRadius && p.z > PadFrontEdge && p.y < ParkedHeight;
                Assert.IsFalse(onPad, $"{p} stands on the open front of the pad");
            }
        }

        [Test]
        public void Broken_LeansTwelveToFifteenDegrees_DishHanging_SameRig()
        {
            ModelNode mast = RelayModelBuilder.CreateMast(false);
            ModelNode broken = RelayModelBuilder.CreateMast(true);
            Quaternion lean = broken.GetDescendant("Mast").LocalRotation;

            Assert.That(Vector3.Angle(lean * Vector3.up, Vector3.up), Is.InRange(12f, 15f), "leaning");
            Assert.Less((lean * Vector3.up).z, 0f, "leaning away from the pad and home");
            Quaternion dish = lean * broken.GetDescendant("Dish").LocalRotation;
            Assert.Less((dish * Vector3.forward).y, -0.5f, "the dish hangs face-down");
            Assert.AreEqual(mast.GetDescendant("Mast").LocalPosition, broken.GetDescendant("Mast").LocalPosition,
                "same pivot: the mast straightens about its foot");
            Assert.AreEqual(mast.GetDescendant("Lamp").Mesh.Name, broken.GetDescendant("Lamp").Mesh.Name);
            Assert.AreEqual(mast.GetDescendant("Dish").Mesh.Name, broken.GetDescendant("Dish").Mesh.Name);
            Assert.AreNotEqual(mast.GetDescendant("Base").Mesh.Name, broken.GetDescendant("Base").Mesh.Name,
                "slack guy wires and an open box");
        }

        [TestCase(200f, 160, 2)]
        [TestCase(280f, 80, 1)]
        public void Silhouette_ReadsFromAfar_AsASolidTaperedMast(float distance, int minPixels, int minRowWidth)
        {
            ModelNode mast = RelayModelBuilder.CreateMast(false);
            float pixelsPerMetre = ScreenHeight / (2f * distance * Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad));
            Vector3 lamp = mast.GetDescendant("Mast").LocalMatrix.MultiplyPoint3x4(
                mast.GetDescendant("Mast").GetDescendant("Lamp").LocalPosition);
            HashSet<Vector2Int> covered = FrontSilhouette(mast, pixelsPerMetre);

            Assert.GreaterOrEqual(covered.Count, minPixels, $"pixels covered at {distance} m");
            Assert.GreaterOrEqual(lamp.y * pixelsPerMetre, 30f * 200f / distance, "tall enough to see");
            int bottom = Mathf.CeilToInt(0.5f * pixelsPerMetre);
            int top = Mathf.FloorToInt(lamp.y * pixelsPerMetre);
            for (int row = bottom; row <= top; row++)
            {
                int width = covered.Count(cell => cell.y == row);
                Assert.GreaterOrEqual(width, minRowWidth, $"the silhouette thins to {width} px at row {row}");
            }
        }

        [Test]
        public void Part_IsChunky_AmberNotCyan_AndPivotedOnItsCentreOfMass()
        {
            ModelNode part = RelayModelBuilder.CreatePart();
            LowPolyMeshBuilder geometry = part.Mesh.Geometry;
            Vector3 size = geometry.Bounds.size;

            Assert.AreEqual("Part_RelayModule", part.Name);
            Assert.AreEqual(0, part.Children.Count);
            MeshChecks.AssertWellFormed(geometry);
            Assert.That(Mathf.Max(size.x, Mathf.Max(size.y, size.z)), Is.InRange(0.27f, 0.36f));
            Assert.Less(geometry.VolumeCentroid().magnitude, 1e-3f, "centre-of-mass pivot");
            List<PaletteSwatch> swatches = MeshChecks.Swatches(geometry);
            CollectionAssert.Contains(swatches, PaletteSwatch.WarmLamp, "glints amber");
            CollectionAssert.DoesNotContain(swatches, PaletteSwatch.TechGlow, "must not read as scrap");
        }

        [Test]
        public void Builders_AreDeterministic()
        {
            foreach (bool broken in new[] { false, true })
            {
                MeshChecks.AssertSameModel(RelayModelBuilder.CreateMast(broken), RelayModelBuilder.CreateMast(broken));
            }

            MeshChecks.AssertSameModel(RelayModelBuilder.CreatePart(), RelayModelBuilder.CreatePart());
        }

        /// <summary>
        /// Screen cells covered by the mast seen from home (looking along -Z, orthographic at that distance's scale):
        /// every triangle rasterised at the cell centres.
        /// </summary>
        private static HashSet<Vector2Int> FrontSilhouette(ModelNode mast, float pixelsPerMetre)
        {
            var cells = new HashSet<Vector2Int>();
            Vector3[] points = MeshChecks.Points(mast, Matrix4x4.identity).ToArray();
            for (int t = 0; t + 2 < points.Length; t += 3)
            {
                Vector2 a = Screen(points[t], pixelsPerMetre);
                Vector2 b = Screen(points[t + 1], pixelsPerMetre);
                Vector2 c = Screen(points[t + 2], pixelsPerMetre);
                int minX = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
                int maxX = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
                int minY = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
                int maxY = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (Inside(new Vector2(x + 0.5f, y + 0.5f), a, b, c))
                        {
                            cells.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }

            return cells;
        }

        private static Vector2 Screen(Vector3 p, float pixelsPerMetre)
        {
            return new Vector2(-p.x, p.y) * pixelsPerMetre;
        }

        private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float ab = Cross(b - a, p - a);
            float bc = Cross(c - b, p - b);
            float ca = Cross(a - c, p - c);
            return (ab >= 0f && bc >= 0f && ca >= 0f) || (ab <= 0f && bc <= 0f && ca <= 0f);
        }

        private static float Cross(Vector2 u, Vector2 v)
        {
            return u.x * v.y - u.y * v.x;
        }

        private static float Flat(Vector3 p)
        {
            return new Vector2(p.x, p.z).magnitude;
        }

        private static void Collect(ModelNode node, Dictionary<string, string> parents)
        {
            foreach (ModelNode child in node.Children)
            {
                Assert.IsFalse(parents.ContainsKey(child.Name), $"{child.Name} appears twice");
                parents.Add(child.Name, node.Name);
                Collect(child, parents);
            }
        }

        private static IEnumerable<ModelNode> Nodes(ModelNode node)
        {
            yield return node;
            foreach (ModelNode child in node.Children)
            {
                foreach (ModelNode descendant in Nodes(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
