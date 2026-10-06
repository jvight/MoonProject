using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Art.Tests
{
    public sealed class RockGeneratorTests
    {
        public static IEnumerable<RockStyle> Styles()
        {
            foreach (RockStyle style in Enum.GetValues(typeof(RockStyle)))
            {
                yield return style;
            }
        }

        [TestCaseSource(nameof(Styles))]
        public void Rock_IsClosed_TwoTone_SizedAndGrounded(RockStyle style)
        {
            var builder = new LowPolyMeshBuilder();
            const float size = 1.7f;

            MeshRange range = RockGenerator.Build(builder, 12345, size, style, Matrix4x4.identity);

            MeshChecks.AssertWellFormed(builder);
            MeshChecks.AssertClosedAndOutward(builder);
            Bounds bounds = builder.GetBounds(range);
            Assert.AreEqual(size, Mathf.Max(bounds.size.x, bounds.size.z), 1e-3f);
            Assert.AreEqual(-RockGenerator.BuriedFraction(style) * bounds.size.y, bounds.min.y, 1e-3f);

            for (int t = range.FirstTriangle; t < range.EndTriangle; t++)
            {
                bool up = MeshChecks.FaceNormal(builder, t).y >= RockGenerator.LightFacingMinDot;
                PaletteSwatch expected = up ? PaletteSwatch.RockLight : PaletteSwatch.RockDark;
                Assert.AreEqual(expected, MeshChecks.SwatchOf(builder, t));
            }
        }

        [Test]
        public void Rock_IsDeterministicPerSeed_AndVariesAcrossSeeds()
        {
            var first = new LowPolyMeshBuilder();
            var again = new LowPolyMeshBuilder();
            var other = new LowPolyMeshBuilder();

            RockGenerator.Build(first, 7, 1f, RockStyle.Rounded, Matrix4x4.identity);
            RockGenerator.Build(again, 7, 1f, RockStyle.Rounded, Matrix4x4.identity);
            RockGenerator.Build(other, 8, 1f, RockStyle.Rounded, Matrix4x4.identity);

            CollectionAssert.AreEqual(first.Positions, again.Positions);
            CollectionAssert.AreEqual(first.Uvs, again.Uvs);
            CollectionAssert.AreNotEqual(first.Positions, other.Positions);
        }

        [Test]
        public void Rocks_BatchIntoOneBuilder_AtTheirPlacements()
        {
            var builder = new LowPolyMeshBuilder();

            MeshRange a = RockGenerator.Build(builder, 1, 0.5f, RockStyle.Pebble, Place.At(10f, 2f, 0f));
            MeshRange b = RockGenerator.Build(builder, 2, 3f, RockStyle.Boulder, Place.At(-10f, 0f, 5f));

            Assert.AreEqual(a.EndTriangle, b.FirstTriangle);
            Assert.AreEqual(80, a.TriangleCount);
            Assert.AreEqual(320, b.TriangleCount);
            Assert.AreEqual(10f, builder.GetBounds(a).center.x, 1e-3f);
            Assert.AreEqual(-10f, builder.GetBounds(b).center.x, 1e-3f);
            MeshChecks.AssertClosedAndOutward(builder);
        }

        [Test]
        public void Grit_StaysAtMostTwentyTriangles_ForEverySeed()
        {
            var builder = new LowPolyMeshBuilder();
            for (int seed = 0; seed < 50; seed++)
            {
                builder.Clear();
                MeshRange grit = RockGenerator.Build(builder, seed, 0.2f, RockStyle.Grit, Matrix4x4.identity);
                Assert.That(grit.TriangleCount, Is.InRange(16, 20), $"seed {seed}");
                MeshChecks.AssertClosedAndOutward(builder);
            }
        }

        [Test]
        public void InvalidSize_Throws()
        {
            var builder = new LowPolyMeshBuilder();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => RockGenerator.Build(builder, 1, 0f, RockStyle.Pebble, Matrix4x4.identity));
        }
    }
}
