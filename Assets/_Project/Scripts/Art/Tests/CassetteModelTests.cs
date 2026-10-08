using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>M3-05 cassettes, Bell's tape rack and Ro's log cache.</summary>
    public sealed class CassetteModelTests
    {
        // True size (VISION ruling 13): about twice a real cassette.
        private const float TapeWidth = 0.18f;
        private const float TapeHeight = 0.22f * TapeWidth / 0.35f;
        private const float TapeThickness = 0.07f * TapeWidth / 0.35f;

        [Test]
        public void Cassettes_OnePrefabPerStyle_PivotedOnTheirCentreOfMass()
        {
            CollectionAssert.AreEqual(new[] { "after_dark_1", "dust_and_honey", "slow_orbit" },
                CassetteModelBuilder.Styles.Select(style => style.Id).ToArray());
            foreach (CassetteStyle style in CassetteModelBuilder.Styles)
            {
                ModelNode tape = CassetteModelBuilder.CreateCassette(style.Id);

                Assert.AreEqual("Cassette_" + style.Id, tape.Name);
                Assert.AreEqual(tape.Name, CassetteModelBuilder.PrefabName(style.Id));
                Assert.AreEqual(0, tape.Children.Count, "a single-node pickup");
                MeshChecks.AssertWellFormed(tape.Mesh.Geometry);
                Assert.Less(tape.Mesh.Geometry.VolumeCentroid().magnitude, 1e-3f, $"{tape.Name} pivot");
            }

            Assert.Throws<System.ArgumentException>(() => CassetteModelBuilder.CreateCassette("mixtape_404"));
        }

        [Test]
        public void Cassettes_AreChunky_StandOnTheirEdge_AndGlintAmber()
        {
            foreach (CassetteStyle style in CassetteModelBuilder.Styles)
            {
                LowPolyMeshBuilder tape = CassetteModelBuilder.CreateCassette(style.Id).Mesh.Geometry;
                Bounds bounds = tape.Bounds;

                Assert.AreEqual(TapeWidth, bounds.size.x, 0.006f, "about 0.18 m wide");
                Assert.AreEqual(TapeHeight, bounds.size.y, 0.006f);
                Assert.That(bounds.size.z, Is.InRange(TapeThickness, TapeThickness + 0.015f), "chunky");
                Assert.AreEqual(-TapeHeight * 0.5f, bounds.min.y, 0.01f, "bottom edge half a tape below the pivot");
                Assert.AreEqual(0f, bounds.center.x, 0.01f);
                foreach (PaletteSwatch swatch in MeshChecks.Swatches(tape))
                {
                    Assert.IsTrue(!Palette.IsEmissive(swatch) || swatch == PaletteSwatch.LampGlass,
                        $"{style.Id}: only the tape window glows ({swatch})");
                }

                CollectionAssert.Contains(MeshChecks.Swatches(tape), PaletteSwatch.LampGlass, style.Id);
            }
        }

        [Test]
        public void Labels_TellTheTapesApart_FromBothFaces()
        {
            IReadOnlyList<CassetteStyle> styles = CassetteModelBuilder.Styles;
            CollectionAssert.AllItemsAreUnique(styles.Select(style => style.Label).ToArray(), "distinct labels");
            foreach (CassetteStyle style in styles)
            {
                Assert.AreNotEqual(style.Shell, style.Label, $"{style.Id}'s label stands out from its shell");
                LowPolyMeshBuilder tape = CassetteModelBuilder.CreateCassette(style.Id).Mesh.Geometry;
                bool front = false;
                bool back = false;
                bool top = false;
                for (int t = 0; t < tape.TriangleCount; t++)
                {
                    if (MeshChecks.SwatchOf(tape, t) != style.Label)
                    {
                        continue;
                    }

                    Vector3 normal = MeshChecks.FaceNormal(tape, t);
                    front |= normal.z > 0.99f;
                    back |= normal.z < -0.99f;
                    top |= normal.y > 0.99f;
                }

                Assert.IsTrue(front && back && top, $"{style.Id}'s label shows on both faces and along the top");
            }

            Assert.Throws<System.ArgumentException>(() =>
                new CassetteStyle("glow_tape", PaletteSwatch.Charcoal, PaletteSwatch.TechGlow, PaletteSwatch.Cream));
        }

        [Test]
        public void Shelf_HasEightUprightSlots_FilledInReadingOrder()
        {
            ModelNode shelf = BaseModelBuilder.CreateCassetteShelf();
            var slots = new Vector3[8];
            for (int i = 0; i < slots.Length; i++)
            {
                ModelNode slot = shelf.GetDescendant($"Slot_{i}");
                Assert.IsNotNull(slot, $"Slot_{i}");
                Assert.IsNull(slot.Mesh, "slots are empties");
                Assert.AreEqual(Quaternion.identity, slot.LocalRotation, "+Y up, +Z = label facing");
                slots[i] = slot.LocalPosition;
            }

            Assert.AreEqual("CassetteShelf", shelf.Name);
            CollectionAssert.AreEqual(
                Enumerable.Range(0, 8).Select(i => $"Slot_{i}").Concat(new[] { "Weather_Paint",
                "Weather_Rust", "Weather_Dust" }).ToArray(),
                shelf.Children.Select(child => child.Name).ToArray());
            for (int row = 0; row < 4; row++)
            {
                Vector3 left = slots[row * 2];
                Vector3 right = slots[row * 2 + 1];
                Assert.AreEqual(left.y, right.y, 1e-5f, "a row shares its floor");
                Assert.Greater(left.x, right.x + TapeWidth, "left cubby (seen from the front) first, side by side");
                if (row > 0)
                {
                    Assert.Less(left.y, slots[row * 2 - 2].y - TapeHeight, "top row first, one tape per cubby");
                }
            }

            LowPolyMeshBuilder rack = shelf.Mesh.Geometry;
            MeshChecks.AssertWellFormed(rack);
            Assert.AreEqual(0f, rack.Bounds.min.y, 1e-3f, "stands on the ground");
            Assert.That(rack.Bounds.max.y, Is.InRange(1f, 1.6f), "a small standing rack");
            Assert.That(rack.Bounds.size.x, Is.LessThan(1.1f));
        }

        [Test]
        public void Shelf_GivesEveryTapeRoom_ToStandOnItsSlot()
        {
            ModelNode shelf = BaseModelBuilder.CreateCassetteShelf();
            LowPolyMeshBuilder rack = shelf.Mesh.Geometry;
            foreach (ModelNode slot in shelf.Children.Where(child => child.Name.StartsWith("Slot_")))
            {
                Vector3 bottom = slot.LocalPosition;
                var tape = new Bounds(bottom + Vector3.up * (TapeHeight * 0.5f),
                    new Vector3(TapeWidth, TapeHeight, TapeThickness) - Vector3.one * 0.002f);
                for (int t = 0; t < rack.TriangleCount; t++)
                {
                    Bounds face = TriangleBounds(rack, t);
                    Assert.IsFalse(tape.Intersects(face), $"{slot.Name}: the rack cuts through a standing tape");
                }

                float floor = float.MinValue;
                for (int t = 0; t < rack.TriangleCount; t++)
                {
                    Bounds face = TriangleBounds(rack, t);
                    bool under = face.min.x <= bottom.x && face.max.x >= bottom.x && face.min.z <= bottom.z &&
                        face.max.z >= bottom.z;
                    if (under && MeshChecks.FaceNormal(rack, t).y > 0.99f && face.max.y <= bottom.y + 1e-4f)
                    {
                        floor = Mathf.Max(floor, face.max.y);
                    }
                }

                Assert.AreEqual(bottom.y, floor, 1e-3f, $"{slot.Name}: the tape stands on the cubby floor");
            }
        }

        [Test]
        public void LogCache_IsATinBoxOnTheGround_WithItsLidAjar()
        {
            ModelNode cache = PropModelBuilder.CreateLogCache();
            LowPolyMeshBuilder tin = cache.Mesh.Geometry;
            Bounds bounds = tin.Bounds;

            Assert.AreEqual("LogCache", cache.Name);
            Assert.AreEqual(0, cache.Children.Count);
            MeshChecks.AssertWellFormed(tin);
            Assert.AreEqual(0f, bounds.min.y, 1e-3f, "root on the ground");
            Assert.That(Mathf.Max(bounds.size.x, bounds.size.z), Is.InRange(0.45f, 0.56f), "about 0.5 m");
            Assert.Less(bounds.size.y, 0.4f);
            Assert.That(tin.TriangleCount, Is.InRange(300, 2000), "triangle budget");
            float front = tin.Positions.Where(p => p.z > 0.1f).Max(p => p.y);
            float back = tin.Positions.Where(p => p.z < -0.1f).Max(p => p.y);
            Assert.Greater(front, back + 0.04f, "the lid stands open at the front");
            List<PaletteSwatch> swatches = MeshChecks.Swatches(tin);
            CollectionAssert.Contains(swatches, PaletteSwatch.Cream, "Ro's pages show in the gap");
            Assert.IsFalse(swatches.Any(Palette.IsEmissive), "a tin box does not glow");
        }

        [Test]
        public void Builders_AreDeterministic()
        {
            foreach (CassetteStyle style in CassetteModelBuilder.Styles)
            {
                MeshChecks.AssertSameModel(CassetteModelBuilder.CreateCassette(style.Id),
                    CassetteModelBuilder.CreateCassette(style.Id));
            }

            MeshChecks.AssertSameModel(BaseModelBuilder.CreateCassetteShelf(), BaseModelBuilder.CreateCassetteShelf());
            MeshChecks.AssertSameModel(PropModelBuilder.CreateLogCache(), PropModelBuilder.CreateLogCache());
        }

        private static Bounds TriangleBounds(LowPolyMeshBuilder builder, int triangle)
        {
            var bounds = new Bounds(builder.Positions[triangle * 3], Vector3.zero);
            bounds.Encapsulate(builder.Positions[triangle * 3 + 1]);
            bounds.Encapsulate(builder.Positions[triangle * 3 + 2]);
            return bounds;
        }
    }
}
