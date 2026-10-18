using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>The scannable fragments and their places (0.5.5 owner feedback §1).</summary>
    public sealed class FragmentTests
    {
        private static IEnumerable<TestCaseData> Fragments()
        {
            foreach ((string blueprint, int count) in FragmentBuilder.Sets)
            {
                for (int i = 0; i < count; i++)
                {
                    yield return new TestCaseData(blueprint, i).SetName($"{blueprint}_{i}");
                }
            }
        }

        [Test]
        public void TheFifteenFragments_ComeInTheSpecsSets()
        {
            Assert.AreEqual(15, FragmentBuilder.Sets.Sum(set => set.Count));
            CollectionAssert.AreEquivalent(
                new[] { "CargoCradle", "WarmHeadlamp", "BoostCoils", "CellPack", "FocusLens", "SideBins" },
                FragmentBuilder.Sets.Select(set => set.Blueprint));
        }

        [TestCaseSource(nameof(Fragments))]
        public void Fragment_HasTheScanContract_RestingOnItsSurface(string blueprint, int index)
        {
            ModelNode fragment = FragmentBuilder.CreateFragment(blueprint, index);
            Assert.AreEqual(FragmentBuilder.PrefabName(blueprint, index), fragment.Name);
            Assert.IsNull(fragment.Mesh, "the root is an empty on its surface");
            CollectionAssert.AreEqual(new[]
            {
                FragmentBuilder.SolidName, FragmentBuilder.LinesName, FragmentBuilder.ScanTargetName,
                FragmentBuilder.BracketsName, FragmentBuilder.DustOutlineName,
            }, fragment.Children.Select(n => n.Name));

            ModelNode solid = fragment.Children[0];
            Assert.AreEqual(ModelMaterial.ScanSolid, solid.Material);
            Bounds bounds = solid.Mesh.Geometry.Bounds;
            Assert.AreEqual(0f, bounds.min.y, 1e-3f, "it rests on its surface, not sunk or floating");
            Assert.AreEqual(0f, bounds.center.x, 1e-3f, "its footprint is centred on the root");
            Assert.AreEqual(0f, bounds.center.z, 1e-3f, "its footprint is centred on the root");
            Assert.That(bounds.size.magnitude, Is.InRange(0.25f, 2f), "a scannable size, small to large");
            MeshChecks.AssertWellFormed(solid.Mesh.Geometry);

            ModelNode lines = fragment.Children[1];
            Assert.AreEqual(ModelMaterial.ScanLines, lines.Material);
            Assert.Greater(lines.Mesh.Geometry.TriangleCount, 24, "the drawing has lines");
            Bounds drawn = lines.Mesh.Geometry.Bounds;
            Assert.Less(Vector3.Distance(drawn.center, bounds.center), 0.1f, "the drawing traces the object");

            Assert.IsTrue(bounds.Contains(fragment.Children[2].LocalPosition), "ScanTarget is on the object");
            ModelNode brackets = fragment.Children[3];
            Assert.IsFalse(brackets.Active, "the brackets wait for gameplay");
            Assert.AreEqual(ModelMaterial.PaletteGlowOff, brackets.Material);
            Assert.IsTrue(MeshChecks.Swatches(brackets.Mesh.Geometry).All(Palette.IsEmissive), "cyan glass brackets");
            ModelNode dust = fragment.Children[4];
            Assert.IsFalse(dust.Active, "the dust outline waits for the dissolve");
            Assert.AreEqual(ModelMaterial.ScanSolid, dust.Material);
            Assert.Less(dust.Mesh.Geometry.Bounds.max.y, 0.02f, "the outline lies on the ground");
        }

        [Test]
        public void RelayHut_IsAnOpenFrontedShell_ForNothingButShelter()
        {
            ModelNode hut = RelayModelBuilder.CreateHut();
            MeshChecks.AssertWellFormed(hut.Mesh.Geometry);
            CollectionAssert.IsEmpty(hut.Children, "06 hosts the cells now (§1.7)");
            Assert.IsFalse(MeshChecks.Swatches(hut.Mesh.Geometry).Any(Palette.IsEmissive));
        }

    }
}
