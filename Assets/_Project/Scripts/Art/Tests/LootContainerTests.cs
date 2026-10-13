using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>The containers finds live in (0.5.5 owner feedback §2): one sealed, cut-once contract for all.</summary>
    public sealed class LootContainerTests
    {
        private static IEnumerable<TestCaseData> Containers()
        {
            yield return new TestCaseData(LootContainerBuilder.CreateCoilCrate()).SetName("CoilCrate");
            yield return new TestCaseData(LootContainerBuilder.CreateTapeCase()).SetName("TapeCase");
            yield return new TestCaseData(LootContainerBuilder.CreateJunctionBox()).SetName("JunctionBox");
            yield return new TestCaseData(LootContainerBuilder.CreateSupplyPod()).SetName("SupplyPod");
        }

        [TestCaseSource(nameof(Containers))]
        public void Container_HasASealedLid_OneCutAway_AndItsFindInside(ModelNode container)
        {
            Assert.IsNotNull(container.Mesh, "the body");
            MeshChecks.AssertWellFormed(container.Mesh.Geometry);
            ModelNode lid = container.Children.Single(n => n.Name == LootContainerBuilder.LidName);
            AssertSealedLid(lid, container.Mesh.Geometry.Bounds);
            ModelNode item = container.Children.Single(n => n.Name == LootContainerBuilder.ItemSocketName);
            Assert.IsNull(item.Mesh);
            Assert.IsTrue(container.Mesh.Geometry.Bounds.Contains(item.LocalPosition), "the find sits inside");
            Assert.IsFalse(MeshChecks.Swatches(container.Mesh.Geometry).Any(Palette.IsEmissive),
                "only the seal (and the pod's beacon) glow, and only when lit");
        }

        [Test]
        public void SupplyPod_CarriesItsCrackedBeacon_ForWorldsCanyonGlow()
        {
            ModelNode glow = LootContainerBuilder.CreateSupplyPod().Children
                .Single(n => n.Name == LootContainerBuilder.GlowName);
            Assert.AreEqual(ModelMaterial.PaletteGlowOff, glow.Material, "dark until World lights it");
            Assert.IsTrue(MeshChecks.Swatches(glow.Mesh.Geometry).All(Palette.IsEmissive));
            Assert.Greater(glow.LocalPosition.y, 0.2f, "on the pod, not a halo on the ground");
        }

        [Test]
        public void TillySled_HoldsHerThreeParts_OneCut_OneDug_OneInItsPod()
        {
            ModelNode sled = LootContainerBuilder.CreateTillySled();
            MeshChecks.AssertWellFormed(sled.Mesh.Geometry);
            ModelNode clamp = sled.Children.Single(n => n.Name == LootContainerBuilder.ClampName);
            Assert.IsNotNull(clamp.Children.SingleOrDefault(n => n.Name == LootContainerBuilder.CutPointName));
            Assert.AreEqual(ModelMaterial.PaletteGlowOff,
                clamp.Children.Single(n => n.Name == LootContainerBuilder.SealName).Material);
            Assert.IsNotNull(sled.Children.SingleOrDefault(n => n.Name == LootContainerBuilder.PartSocketPrefix + "0"));
            ModelNode dug = sled.Children.Single(n => n.Name == LootContainerBuilder.PartSocketPrefix + "1");
            Assert.Less(dug.LocalPosition.y, 0.1f, "dug from the gap under the hull");
            ModelNode pod = sled.Children.Single(n => n.Name == LootContainerBuilder.SensorPodName);
            AssertSealedLid(pod.Children.Single(n => n.Name == LootContainerBuilder.LidName), pod.Mesh.Geometry.Bounds);
            Assert.IsNotNull(pod.Children.SingleOrDefault(n => n.Name == LootContainerBuilder.PartSocketPrefix + "2"));
        }

        private static void AssertSealedLid(ModelNode lid, Bounds body)
        {
            Assert.IsNotNull(lid.Mesh);
            Assert.AreEqual(Quaternion.identity, lid.LocalRotation, "shut at rest");
            Assert.Greater(lid.LocalPosition.y, body.center.y, "hinged high on the body");
            ModelNode cut = lid.Children.Single(n => n.Name == LootContainerBuilder.CutPointName);
            Assert.IsNull(cut.Mesh);
            Assert.Greater(cut.LocalPosition.z, lid.Mesh.Geometry.Bounds.max.z - 0.03f, "on the front seam");
            ModelNode seal = lid.Children.Single(n => n.Name == LootContainerBuilder.SealName);
            Assert.AreEqual(ModelMaterial.PaletteGlowOff, seal.Material, "the seal glows only when lit");
            Assert.IsTrue(MeshChecks.Swatches(seal.Mesh.Geometry).All(Palette.IsEmissive), "the seal is all glass");
            MeshChecks.AssertWellFormed(lid.Mesh.Geometry);
        }
    }
}
