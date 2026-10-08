using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Art.Tests
{
    public sealed class LowPolyMeshBuilderTests
    {
        private static readonly Vector2[] Diamond =
        {
            new Vector2(0f, 1f), new Vector2(0.6f, 0f), new Vector2(0f, -1f), new Vector2(-0.6f, 0f),
        };

        private static readonly Vector2[] LShape =
        {
            new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(2f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 3f), new Vector2(0f, 3f),
        };

        private static readonly Vector2[] Star = BuildStar(5, 1f, 0.45f);

        private static readonly Vector2[] Pebble =
        {
            new Vector2(0f, -0.5f), new Vector2(0.4f, -0.4f), new Vector2(0.5f, 0f), new Vector2(0.35f, 0.4f),
            new Vector2(0f, 0.55f),
        };

        public static IEnumerable<TestCaseData> ClosedPrimitives()
        {
            yield return Case("box", b => b.Box(Matrix4x4.identity, new Vector3(1f, 2f, 3f), PaletteSwatch.Cream));
            yield return Case("chamfered box",
                b => b.Box(Matrix4x4.identity, new Vector3(1f, 0.5f, 2f), PaletteSwatch.Cream, 0.1f));
            yield return Case("triangular prism", b => b.Prism(Matrix4x4.identity, 0.5f, 1f, 3, PaletteSwatch.Metal));
            yield return Case("hex prism", b => b.Prism(Matrix4x4.identity, 0.5f, 0.2f, 6, PaletteSwatch.Metal));
            yield return Case("frustum", b => b.Frustum(Matrix4x4.identity, 0.6f, 0.3f, 1f, 8, PaletteSwatch.Metal));
            yield return Case("inverted frustum",
                b => b.Frustum(Matrix4x4.identity, 0f, 0.4f, 0.7f, 7, PaletteSwatch.Metal));
            yield return Case("cone", b => b.Cone(Matrix4x4.identity, 0.5f, 1.5f, 5, PaletteSwatch.WarmAccent));
            yield return Case("icosphere 0", b => b.Icosphere(Matrix4x4.identity, 1f, 0, PaletteSwatch.RockLight));
            yield return Case("icosphere 2", b => b.Icosphere(Matrix4x4.identity, 1f, 2, PaletteSwatch.RockLight));
            yield return Case("icosphere 3", b => b.Icosphere(Matrix4x4.identity, 1f, 3, PaletteSwatch.RockLight));
            yield return Case("torus", b => b.Torus(Matrix4x4.identity, 1f, 0.25f, 8, 4, PaletteSwatch.TechGlow));
            yield return Case("wedge",
                b => b.Wedge(Matrix4x4.identity, new Vector3(1f, 0.5f, 2f), PaletteSwatch.Cream));
            yield return Case("closed lathe", b => b.Lathe(Matrix4x4.identity, Pebble, 9, PaletteSwatch.Cream));
            yield return Case("convex extrusion",
                b => b.Extrude(Matrix4x4.identity, Diamond, 0.3f, PaletteSwatch.Metal));
            yield return Case("concave extrusion",
                b => b.Extrude(Matrix4x4.identity, LShape, 0.5f, PaletteSwatch.Metal));
            yield return Case("star extrusion", b => b.Extrude(Matrix4x4.identity, Star, 0.1f, PaletteSwatch.WarmLamp));
            yield return Case("displaced icosphere", b => b.Icosphere(Matrix4x4.identity, 1f, 2, PaletteSwatch.RockDark,
                new Displacement(7, 0.3f, 1.7f, 3)));
            yield return Case("displaced chamfered box", b => b.Box(Matrix4x4.identity, Vector3.one,
                PaletteSwatch.RockDark, 0.2f, new Displacement(3, 0.1f, 2f, 2)));
            yield return Case("displaced prism", b => b.Prism(Matrix4x4.identity, 0.5f, 1f, 9, PaletteSwatch.RockDark,
                true, new Displacement(11, 0.1f, 3f)));
            yield return Case("mirrored frustum", b => b.Frustum(Place.MirrorX * Matrix4x4.Translate(Vector3.right),
                0.5f, 0.2f, 1f, 6, PaletteSwatch.Metal));
            yield return Case("non-uniform scaled torus", b => b.Torus(Matrix4x4.Scale(new Vector3(2f, 0.5f, 1f)),
                1f, 0.3f, 6, 3, PaletteSwatch.Metal));
        }

        [TestCaseSource(nameof(ClosedPrimitives))]
        public void ClosedPrimitive_IsWellFormed_Closed_AndOutward(Action<LowPolyMeshBuilder> build)
        {
            var builder = new LowPolyMeshBuilder();

            build(builder);

            MeshChecks.AssertWellFormed(builder);
            MeshChecks.AssertClosedAndOutward(builder);
        }

        [Test]
        public void OpenPrism_WithoutCaps_IsWellFormed_AndHasOnlySideFaces()
        {
            var builder = new LowPolyMeshBuilder();

            builder.Prism(Matrix4x4.identity, 0.5f, 1f, 6, PaletteSwatch.Metal, false);

            MeshChecks.AssertWellFormed(builder);
            Assert.AreEqual(12, builder.TriangleCount);
        }

        [Test]
        public void Box_Volume_MatchesSize()
        {
            var builder = new LowPolyMeshBuilder();

            builder.Box(Matrix4x4.identity, new Vector3(1f, 2f, 3f), PaletteSwatch.Cream);

            Assert.AreEqual(6f, MeshChecks.SignedVolume(builder), 1e-4f);
            Assert.AreEqual(12, builder.TriangleCount);
        }

        [Test]
        public void ChamferedBox_HasBevelFaces_AndLosesOnlyEdgeVolume()
        {
            var builder = new LowPolyMeshBuilder();

            builder.Box(Matrix4x4.identity, Vector3.one, PaletteSwatch.Cream, 0.1f);

            Assert.AreEqual(44, builder.TriangleCount);
            float volume = MeshChecks.SignedVolume(builder);
            Assert.Less(volume, 1f);
            Assert.Greater(volume, 1f - 12f * 0.5f * 0.1f * 0.1f - 1e-4f);
            Assert.AreEqual(Vector3.one, builder.Bounds.size);
        }

        [Test]
        public void Prism_Volume_MatchesRegularPolygonArea()
        {
            var builder = new LowPolyMeshBuilder();
            const int sides = 7;

            builder.Prism(Matrix4x4.identity, 0.5f, 2f, sides, PaletteSwatch.Metal);

            float area = 0.5f * sides * 0.25f * Mathf.Sin(2f * Mathf.PI / sides);
            Assert.AreEqual(area * 2f, MeshChecks.SignedVolume(builder), 1e-4f);
            Assert.AreEqual(4 * sides, builder.TriangleCount);
        }

        [Test]
        public void Prism_HasAFlatSideFacingForward()
        {
            var builder = new LowPolyMeshBuilder();

            builder.Prism(Matrix4x4.identity, 0.5f, 1f, 6, PaletteSwatch.Metal);

            bool found = false;
            for (int t = 0; t < builder.TriangleCount; t++)
            {
                found |= Vector3.Dot(MeshChecks.FaceNormal(builder, t), Vector3.forward) > 0.9999f;
            }

            Assert.IsTrue(found);
        }

        [Test]
        public void Icosphere_TriangleCount_QuadruplesPerSubdivision()
        {
            for (int level = 0; level <= LowPolyMeshBuilder.MaxIcosphereSubdivisions; level++)
            {
                var builder = new LowPolyMeshBuilder();
                builder.Icosphere(Matrix4x4.identity, 1f, level, PaletteSwatch.RockLight);
                Assert.AreEqual(20 * (1 << (2 * level)), builder.TriangleCount);
            }
        }

        [Test]
        public void Extrusion_Volume_IsPolygonAreaTimesDepth_ForEitherWinding()
        {
            var forward = new LowPolyMeshBuilder();
            var reversed = new LowPolyMeshBuilder();
            var clockwise = (Vector2[])LShape.Clone();
            Array.Reverse(clockwise);

            forward.Extrude(Matrix4x4.identity, LShape, 0.5f, PaletteSwatch.Metal);
            reversed.Extrude(Matrix4x4.identity, clockwise, 0.5f, PaletteSwatch.Metal);

            Assert.AreEqual(4f * 0.5f, MeshChecks.SignedVolume(forward), 1e-4f);
            Assert.AreEqual(4f * 0.5f, MeshChecks.SignedVolume(reversed), 1e-4f);
            MeshChecks.AssertClosedAndOutward(reversed);
        }

        [Test]
        public void Extrusion_OfSelfIntersectingPolygon_Throws()
        {
            var builder = new LowPolyMeshBuilder();
            Vector2[] bowtie = { new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 1f) };

            Assert.Throws<ArgumentException>(
                () => builder.Extrude(Matrix4x4.identity, bowtie, 1f, PaletteSwatch.Metal));
        }

        [Test]
        public void Lathe_WithBands_PaintsEachBand()
        {
            var builder = new LowPolyMeshBuilder();
            PaletteSwatch[] bands =
            {
                PaletteSwatch.Charcoal, PaletteSwatch.Cream, PaletteSwatch.WarmAccent, PaletteSwatch.WarmLamp,
            };

            builder.Lathe(Matrix4x4.identity, Pebble, 6, bands);

            var seen = new HashSet<PaletteSwatch>();
            for (int t = 0; t < builder.TriangleCount; t++)
            {
                seen.Add(MeshChecks.SwatchOf(builder, t));
            }

            CollectionAssert.AreEquivalent(bands, seen);
            Assert.AreEqual(6 + 12 + 12 + 6, builder.TriangleCount);
        }

        [Test]
        public void Lathe_WithMismatchedBands_Throws()
        {
            var builder = new LowPolyMeshBuilder();
            PaletteSwatch[] bands = { PaletteSwatch.Cream };

            Assert.Throws<ArgumentException>(() => builder.Lathe(Matrix4x4.identity, Pebble, 6, bands));
        }

        [Test]
        public void Lathe_OpenProfile_FacesTheRightOfTravel()
        {
            var builder = new LowPolyMeshBuilder();
            Vector2[] upward = { new Vector2(0.5f, 0f), new Vector2(0.5f, 1f) };

            builder.Lathe(Matrix4x4.identity, upward, 8, PaletteSwatch.Cream);

            for (int t = 0; t < builder.TriangleCount; t++)
            {
                Vector3 centre = builder.Positions[3 * t] + builder.Positions[3 * t + 1] + builder.Positions[3 * t + 2];
                centre.y = 0f;
                Assert.Greater(Vector3.Dot(MeshChecks.FaceNormal(builder, t), centre), 0f);
            }
        }

        [Test]
        public void CapsPaint_ColoursOnlyTheEnds()
        {
            var builder = new LowPolyMeshBuilder();

            builder.Prism(Matrix4x4.identity, 0.5f, 1f, 8, Paint.WithCaps(PaletteSwatch.Charcoal, PaletteSwatch.Metal));

            for (int t = 0; t < builder.TriangleCount; t++)
            {
                bool isCap = Mathf.Abs(MeshChecks.FaceNormal(builder, t).y) > 0.999f;
                Assert.AreEqual(isCap ? PaletteSwatch.Metal : PaletteSwatch.Charcoal, MeshChecks.SwatchOf(builder, t));
            }
        }

        [Test]
        public void CapsPaint_OnBox_ColoursTopAndBottom()
        {
            var builder = new LowPolyMeshBuilder();

            builder.Box(Matrix4x4.identity, Vector3.one, Paint.WithCaps(PaletteSwatch.Cream, PaletteSwatch.WarmAccent));

            for (int t = 0; t < builder.TriangleCount; t++)
            {
                bool isCap = Mathf.Abs(MeshChecks.FaceNormal(builder, t).y) > 0.999f;
                PaletteSwatch expected = isCap ? PaletteSwatch.WarmAccent : PaletteSwatch.Cream;
                Assert.AreEqual(expected, MeshChecks.SwatchOf(builder, t));
            }
        }

        [Test]
        public void FacingPaint_SplitsByFinalNormal()
        {
            var builder = new LowPolyMeshBuilder();
            Paint twoTone = Paint.Facing(PaletteSwatch.RockLight, PaletteSwatch.RockDark, Vector3.up, 0.3f);

            builder.Icosphere(Matrix4x4.identity, 1f, 2, twoTone, new Displacement(5, 0.2f, 1.5f, 2));

            int light = 0;
            for (int t = 0; t < builder.TriangleCount; t++)
            {
                bool up = MeshChecks.FaceNormal(builder, t).y >= 0.3f;
                light += up ? 1 : 0;
                Assert.AreEqual(up ? PaletteSwatch.RockLight : PaletteSwatch.RockDark, MeshChecks.SwatchOf(builder, t));
            }

            Assert.Greater(light, 0);
            Assert.Less(light, builder.TriangleCount);
        }

        [Test]
        public void Repaint_ChangesOnlyTheRange()
        {
            var builder = new LowPolyMeshBuilder();
            MeshRange first = builder.Box(Matrix4x4.identity, Vector3.one, PaletteSwatch.Cream);
            MeshRange second = builder.Box(Place.At(2f, 0f, 0f), Vector3.one, PaletteSwatch.Cream);

            builder.Repaint(second, PaletteSwatch.Charcoal);
            builder.RepaintFacing(first, Vector3.up, 0.9f, PaletteSwatch.WarmAccent);

            for (int t = first.FirstTriangle; t < first.EndTriangle; t++)
            {
                bool top = MeshChecks.FaceNormal(builder, t).y > 0.9f;
                Assert.AreEqual(top ? PaletteSwatch.WarmAccent : PaletteSwatch.Cream, MeshChecks.SwatchOf(builder, t));
            }

            for (int t = second.FirstTriangle; t < second.EndTriangle; t++)
            {
                Assert.AreEqual(PaletteSwatch.Charcoal, MeshChecks.SwatchOf(builder, t));
            }

            Assert.Throws<ArgumentOutOfRangeException>(
                () => builder.Repaint(new MeshRange(20, 10), PaletteSwatch.Cream));
        }

        [Test]
        public void Uvs_AreTheSwatchCellCentre()
        {
            var builder = new LowPolyMeshBuilder();

            builder.Torus(Matrix4x4.identity, 1f, 0.2f, 6, 4, PaletteSwatch.TechGlow);

            for (int v = 0; v < builder.VertexCount; v++)
            {
                Assert.AreEqual(Palette.Uv(PaletteSwatch.TechGlow), builder.Uvs[v]);
            }
        }

        [Test]
        public void Placement_TranslatesRotatesAndScales()
        {
            var builder = new LowPolyMeshBuilder();
            var quarterTurnY = new Quaternion(0f, Mathf.Sin(Mathf.PI / 4f), 0f, Mathf.Cos(Mathf.PI / 4f));
            var position = new Vector3(3f, -1f, 2f);

            builder.Box(Place.At(position, quarterTurnY, new Vector3(2f, 1f, 1f)), new Vector3(1f, 2f, 3f),
                PaletteSwatch.Cream);

            Bounds bounds = builder.Bounds;
            AssertVector(position, bounds.center);
            AssertVector(new Vector3(3f, 2f, 2f), bounds.size);
            Assert.AreEqual(12f, MeshChecks.SignedVolume(builder), 1e-3f);
            MeshChecks.AssertWellFormed(builder);
            MeshChecks.AssertClosedAndOutward(builder);
        }

        [Test]
        public void Mirroring_KeepsFacesOutward()
        {
            var builder = new LowPolyMeshBuilder();

            builder.Wedge(Place.MirrorX * Place.At(1f, 0f, 0f), new Vector3(1f, 1f, 2f), PaletteSwatch.Cream);

            MeshChecks.AssertWellFormed(builder);
            MeshChecks.AssertClosedAndOutward(builder);
            AssertVector(new Vector3(-1f, 0f, 0f), builder.Bounds.center);
        }

        [Test]
        public void Shave_FlattensBeyondThePlane_AndKeepsTheShapeClosed()
        {
            var builder = new LowPolyMeshBuilder();
            MeshRange sphere = builder.Icosphere(Matrix4x4.identity, 1f, 2, PaletteSwatch.RockLight);
            float before = MeshChecks.SignedVolume(builder);
            Vector3 normal = new Vector3(0.3f, 1f, -0.2f).normalized;

            MeshRange shaved = builder.Shave(sphere, normal, 0.6f);

            Assert.AreEqual(0, shaved.FirstTriangle);
            for (int v = 0; v < builder.VertexCount; v++)
            {
                Assert.LessOrEqual(Vector3.Dot(builder.Positions[v], normal), 0.6f + 1e-5f);
            }

            bool flatFace = false;
            for (int t = 0; t < builder.TriangleCount; t++)
            {
                flatFace |= Vector3.Dot(MeshChecks.FaceNormal(builder, t), normal) > 0.9999f;
            }

            Assert.IsTrue(flatFace, "no face lies on the cut plane");
            Assert.Less(MeshChecks.SignedVolume(builder), before);
            MeshChecks.AssertWellFormed(builder);
            MeshChecks.AssertClosedAndOutward(builder);
        }

        [Test]
        public void Support_IsTheFurthestReachAlongADirection()
        {
            var builder = new LowPolyMeshBuilder();
            MeshRange box = builder.Box(Place.At(1f, 2f, 3f), new Vector3(2f, 4f, 6f), PaletteSwatch.Cream);

            Assert.AreEqual(4f, builder.Support(box, Vector3.up), 1e-5f);
            Assert.AreEqual(0f, builder.Support(box, Vector3.left), 1e-5f);
            Assert.AreEqual((2f + 4f + 6f) / Mathf.Sqrt(3f), builder.Support(box, Vector3.one.normalized), 1e-4f);
        }

        [Test]
        public void VolumeCentroid_IsTheCentreOfMass()
        {
            var box = new LowPolyMeshBuilder();
            box.Box(Place.At(1f, 2f, 3f), new Vector3(1f, 2f, 3f), PaletteSwatch.Cream);
            var cone = new LowPolyMeshBuilder();
            cone.Cone(Matrix4x4.identity, 1f, 2f, 24, PaletteSwatch.Cream);

            AssertVector(new Vector3(1f, 2f, 3f), box.VolumeCentroid());
            Assert.AreEqual(-0.5f, cone.VolumeCentroid().y, 0.01f, "a cone's centroid sits a quarter up its height");
            Assert.AreEqual(0f, new LowPolyMeshBuilder().VolumeCentroid().magnitude, 1e-6f);
        }

        [Test]
        public void Shave_RejectsAnEarlierRange()
        {
            var builder = new LowPolyMeshBuilder();
            MeshRange first = builder.Box(Matrix4x4.identity, Vector3.one, PaletteSwatch.Cream);
            builder.Box(Place.At(3f, 0f, 0f), Vector3.one, PaletteSwatch.Cream);

            Assert.Throws<ArgumentException>(() => builder.Shave(first, Vector3.up, 0.1f));
        }

        [Test]
        public void Bounds_EncloseEveryVertexTightly()
        {
            var builder = new LowPolyMeshBuilder();
            builder.Icosphere(Place.At(1f, 2f, 3f), 0.5f, 1, PaletteSwatch.RockLight, new Displacement(1, 0.2f, 2f));
            builder.Cone(Place.At(-2f, 0f, 0f), 0.3f, 1f, 5, PaletteSwatch.Metal);

            Vector3 min = builder.Positions[0];
            Vector3 max = min;
            for (int v = 0; v < builder.VertexCount; v++)
            {
                min = Vector3.Min(min, builder.Positions[v]);
                max = Vector3.Max(max, builder.Positions[v]);
            }

            AssertVector(min, builder.Bounds.min);
            AssertVector(max, builder.Bounds.max);
        }

        [Test]
        public void Displacement_IsDeterministicPerSeed()
        {
            LowPolyMeshBuilder a = DisplacedSphere(42);
            LowPolyMeshBuilder b = DisplacedSphere(42);
            LowPolyMeshBuilder c = DisplacedSphere(43);

            Assert.AreEqual(a.VertexCount, b.VertexCount);
            bool differs = false;
            for (int v = 0; v < a.VertexCount; v++)
            {
                Assert.AreEqual(a.Positions[v], b.Positions[v]);
                Assert.AreEqual(a.Normals[v], b.Normals[v]);
                differs |= a.Positions[v] != c.Positions[v];
            }

            Assert.IsTrue(differs, "different seeds produced identical meshes");
        }

        [Test]
        public void Displacement_StaysWithinAmplitude()
        {
            var builder = new LowPolyMeshBuilder();

            builder.Icosphere(Matrix4x4.identity, 1f, 2, PaletteSwatch.RockLight, new Displacement(9, 0.25f, 1.3f, 4));

            for (int v = 0; v < builder.VertexCount; v++)
            {
                Assert.That(builder.Positions[v].magnitude, Is.InRange(0.75f - 1e-4f, 1.25f + 1e-4f));
            }
        }

        [Test]
        public void Append_CopiesColoursAndPlacement_AndMirrorsOutward()
        {
            var part = new LowPolyMeshBuilder();
            Paint paint = Paint.WithCaps(PaletteSwatch.Cream, PaletteSwatch.Metal);
            part.Frustum(Place.At(0.5f, 0f, 0f), 0.4f, 0.2f, 0.6f, 6, paint);
            var whole = new LowPolyMeshBuilder();

            MeshRange left = whole.Append(part, Matrix4x4.identity);
            MeshRange right = whole.Append(part, Place.MirrorX);

            Assert.AreEqual(part.TriangleCount, left.TriangleCount);
            Assert.AreEqual(part.TriangleCount, right.TriangleCount);
            for (int v = 0; v < part.VertexCount; v++)
            {
                Assert.AreEqual(part.Uvs[v], whole.Uvs[v]);
            }

            MeshChecks.AssertWellFormed(whole);
            Assert.AreEqual(2f * MeshChecks.SignedVolume(part), MeshChecks.SignedVolume(whole), 1e-4f);
            AssertVector(Vector3.zero, new Vector3(whole.Bounds.center.x, 0f, 0f));
        }

        [Test]
        public void Transform_MovesRange_AndUpdatesBoundsAndNormals()
        {
            var builder = new LowPolyMeshBuilder();
            MeshRange box = builder.Box(Matrix4x4.identity, Vector3.one, PaletteSwatch.Cream);

            builder.Transform(box, Place.At(new Vector3(0f, 5f, 0f), Quaternion.identity, new Vector3(1f, 2f, 1f)));

            AssertVector(new Vector3(0f, 5f, 0f), builder.Bounds.center);
            AssertVector(new Vector3(1f, 2f, 1f), builder.Bounds.size);
            MeshChecks.AssertWellFormed(builder);
            MeshChecks.AssertClosedAndOutward(builder);
        }

        [Test]
        public void Transform_KeepsUnitNormals_OnTinyFaces()
        {
            var builder = new LowPolyMeshBuilder();
            MeshRange speck = builder.Icosphere(Matrix4x4.identity, 0.004f, 1, PaletteSwatch.Metal);

            builder.Transform(speck, Matrix4x4.Scale(Vector3.one * 0.5f));

            MeshChecks.AssertWellFormed(builder);
        }

        [Test]
        public void AppendFacing_CopiesOnlyTheFacesThatLookThatWay_LiftedAndRepainted()
        {
            var source = new LowPolyMeshBuilder();
            source.Box(Matrix4x4.identity, Vector3.one, PaletteSwatch.Cream);
            var skin = new LowPolyMeshBuilder();

            MeshRange top = skin.AppendFacing(source, Vector3.up, 0.9f, 0.01f, PaletteSwatch.CakedDust);

            Assert.AreEqual(2, top.TriangleCount, "only the top face");
            MeshChecks.AssertWellFormed(skin);
            foreach (Vector3 p in skin.Positions)
            {
                Assert.AreEqual(0.51f, p.y, 1e-5f, "lifted along the face normal");
            }

            CollectionAssert.AreEqual(new[] { PaletteSwatch.CakedDust }, MeshChecks.Swatches(skin));
        }

        [Test]
        public void AppendRepainted_CopiesOnlyThatPaint()
        {
            var source = new LowPolyMeshBuilder();
            source.Box(Matrix4x4.identity, Vector3.one, PaletteSwatch.Cream);
            source.Box(Place.At(2f, 0f, 0f), Vector3.one, PaletteSwatch.WarmAccent);
            var coat = new LowPolyMeshBuilder();

            coat.AppendRepainted(source, PaletteSwatch.WarmAccent, PaletteSwatch.FadedAccent, 0.005f);

            Assert.AreEqual(12, coat.TriangleCount);
            Assert.Greater(coat.Bounds.min.x, 1.49f, "only the orange box");
            Assert.AreEqual(1.01f, coat.Bounds.size.x, 1e-4f, "lifted out of every face");
            CollectionAssert.AreEqual(new[] { PaletteSwatch.FadedAccent }, MeshChecks.Swatches(coat));
        }

        [Test]
        public void AppendBelow_CutsTheShapeAtTheTideLine()
        {
            var source = new LowPolyMeshBuilder();
            source.Box(Matrix4x4.identity, new Vector3(1f, 2f, 1f), PaletteSwatch.Cream);
            var tide = new LowPolyMeshBuilder();

            tide.AppendBelow(source, -0.4f, 0f, PaletteSwatch.CakedDust);

            MeshChecks.AssertWellFormed(tide);
            Assert.AreEqual(-1f, tide.Bounds.min.y, 1e-5f);
            Assert.AreEqual(-0.4f, tide.Bounds.max.y, 1e-5f, "cut exactly at the line");
            float area = 0f;
            for (int v = 0; v < tide.VertexCount; v += 3)
            {
                area += Vector3.Cross(tide.Positions[v + 1] - tide.Positions[v], tide.Positions[v + 2] -
                    tide.Positions[v]).magnitude * 0.5f;
            }

            Assert.AreEqual(1f + 4f * 0.6f, area, 1e-4f, "the bottom and the lower 0.6 m of four sides");
        }

        [Test]
        public void Clear_EmptiesTheBuilder()
        {
            var builder = new LowPolyMeshBuilder();
            builder.Box(Place.At(5f, 5f, 5f), Vector3.one, PaletteSwatch.Cream);

            builder.Clear();
            builder.Box(Matrix4x4.identity, Vector3.one, PaletteSwatch.Cream);

            Assert.AreEqual(12, builder.TriangleCount);
            AssertVector(Vector3.zero, builder.Bounds.center);
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            var b = new LowPolyMeshBuilder();
            Matrix4x4 at = Matrix4x4.identity;

            Assert.Throws<ArgumentOutOfRangeException>(() => b.Box(at, new Vector3(0f, 1f, 1f), PaletteSwatch.Cream));
            Assert.Throws<ArgumentOutOfRangeException>(() => b.Box(at, Vector3.one, PaletteSwatch.Cream, 0.6f));
            Assert.Throws<ArgumentOutOfRangeException>(() => b.Prism(at, 1f, 1f, 2, PaletteSwatch.Cream));
            Assert.Throws<ArgumentOutOfRangeException>(() => b.Frustum(at, 0f, 0f, 1f, 6, PaletteSwatch.Cream));
            Assert.Throws<ArgumentOutOfRangeException>(() => b.Icosphere(at, 1f, 4, PaletteSwatch.Cream));
            Assert.Throws<ArgumentOutOfRangeException>(() => b.Torus(at, 0.2f, 0.3f, 6, 4, PaletteSwatch.Cream));
            Vector2[] onAxis = { Vector2.zero, Vector2.up };
            Assert.Throws<ArgumentException>(() => b.Lathe(at, onAxis, 6, PaletteSwatch.Cream));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Displacement(1, 0.1f, 0f));
            Assert.AreEqual(0, b.TriangleCount);
        }

        [Test]
        public void ToMesh_CopiesBuffers_Bounds_AndUses16BitIndicesWhenPossible()
        {
            var builder = new LowPolyMeshBuilder();
            builder.Box(Place.At(0f, 1f, 0f), new Vector3(2f, 1f, 1f), PaletteSwatch.Cream, 0.1f);

            Mesh mesh = builder.ToMesh("Test");
            try
            {
                Assert.AreEqual("Test", mesh.name);
                Assert.AreEqual(IndexFormat.UInt16, mesh.indexFormat);
                Assert.AreEqual(builder.VertexCount, mesh.vertexCount);
                Assert.AreEqual(builder.VertexCount, mesh.triangles.Length);
                CollectionAssert.AreEqual(builder.Normals, mesh.normals);
                CollectionAssert.AreEqual(builder.Uvs, mesh.uv);
                AssertVector(builder.Bounds.center, mesh.bounds.center);
                AssertVector(builder.Bounds.size, mesh.bounds.size);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void WriteTo_Uses32BitIndices_AboveSixtyFiveThousandVertices()
        {
            var builder = new LowPolyMeshBuilder();
            for (int i = 0; i < 18; i++)
            {
                builder.Icosphere(Place.At(i * 3f, 0f, 0f), 1f, 3, PaletteSwatch.RockLight);
            }

            var mesh = new Mesh();
            try
            {
                builder.WriteTo(mesh);

                Assert.Greater(builder.VertexCount, 65535);
                Assert.AreEqual(IndexFormat.UInt32, mesh.indexFormat);
                Assert.AreEqual(builder.VertexCount, mesh.vertexCount);
                Assert.AreEqual(builder.VertexCount - 1, mesh.triangles[mesh.triangles.Length - 1]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static LowPolyMeshBuilder DisplacedSphere(int seed)
        {
            var builder = new LowPolyMeshBuilder();
            var displacement = new Displacement(seed, 0.3f, 1.5f, 3);
            builder.Icosphere(Matrix4x4.identity, 1f, 2, PaletteSwatch.RockLight, displacement);
            return builder;
        }

        private static TestCaseData Case(string name, Action<LowPolyMeshBuilder> build)
        {
            return new TestCaseData(build).SetName($"ClosedPrimitive({name})");
        }

        private static Vector2[] BuildStar(int points, float outer, float inner)
        {
            var star = new Vector2[points * 2];
            for (int i = 0; i < star.Length; i++)
            {
                float radius = i % 2 == 0 ? outer : inner;
                float angle = i * Mathf.PI / points;
                star[i] = new Vector2(radius * Mathf.Sin(angle), radius * Mathf.Cos(angle));
            }

            return star;
        }

        private static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 1e-4f, $"x of {actual}");
            Assert.AreEqual(expected.y, actual.y, 1e-4f, $"y of {actual}");
            Assert.AreEqual(expected.z, actual.z, 1e-4f, $"z of {actual}");
        }
    }
}
