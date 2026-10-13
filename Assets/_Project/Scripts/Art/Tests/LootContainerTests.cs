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
            yield return new TestCaseData(LootContainerBuilder.CreateCabinet()).SetName("Cabinet");
            yield return new TestCaseData(LootContainerBuilder.CreateSpareCrate()).SetName("SpareCrate");
        }

        [TestCaseSource(nameof(Containers))]
        public void Container_HasASealedLid_OneCutAway_AndItsFindInside(ModelNode container)
        {
            Assert.IsNotNull(container.Mesh, "the body");
            MeshChecks.AssertWellFormed(container.Mesh.Geometry);
            ModelNode lid = container.Children.Single(n => n.Name == LootContainerBuilder.LidName);
            ModelNode item = container.Children.Single(n => n.Name == LootContainerBuilder.ItemSocketName);
            AssertSealedLid(lid, item.LocalPosition);
            Assert.IsNull(item.Mesh);
            Assert.IsTrue(container.Mesh.Geometry.Bounds.Contains(item.LocalPosition), "the find sits inside");
            Assert.IsFalse(MeshChecks.Swatches(container.Mesh.Geometry).Any(Palette.IsEmissive),
                "only the seal (and the pod's beacon) glow, and only when lit");
        }

        [Test]
        public void SpareCrate_HoldsTheTapeDeck_StencilledAsOursOnKenjisBench()
        {
            ModelNode crate = LootContainerBuilder.CreateSpareCrate();
            Bounds body = crate.Mesh.Geometry.Bounds;
            Vector3 deck = RoverKitBuilder.CreateTapeDeck().Mesh.Geometry.Bounds.size;
            Assert.Greater(body.size.x, deck.z, "the deck lies along the crate");
            ModelNode item = crate.Children.Single(n => n.Name == LootContainerBuilder.ItemSocketName);
            Assert.Greater(Mathf.Abs((item.LocalRotation * Vector3.forward).x), 0.99f,
                "the socket turns the deck's length along the crate");
            Assert.Greater(body.size.y, deck.y, "standing upright inside");
            Assert.Greater(body.size.z, deck.x, "and fits front to back");
            Assert.IsTrue(MeshChecks.Swatches(crate.Mesh.Geometry).Contains(PaletteSwatch.Charcoal), "the stencil");

            ModelNode site = SiteModelBuilder.CreateSite("garage");
            ModelNode seat = site.Children.Single(n => n.Name == SiteModelBuilder.SpareCrateSocketName);
            Matrix4x4 place = Matrix4x4.TRS(seat.LocalPosition, seat.LocalRotation, Vector3.one);
            Bounds placed = Transformed(place, crate.Mesh.Geometry.Bounds);
            Assert.Greater(placed.min.y, 0.9f, "on the bench, not the ground");
            foreach (ModelNode socket in site.Children.Where(
                n => n.Name.StartsWith(SiteModelBuilder.FragmentSocketPrefix, System.StringComparison.Ordinal)))
            {
                string[] parts = socket.Name.Split('_');
                ModelNode fragment = FragmentBuilder.CreateFragment(parts[1], int.Parse(parts[2]));
                Bounds solid = fragment.Children.Single(n => n.Name == FragmentBuilder.SolidName).Mesh.Geometry.Bounds;
                Bounds other = Transformed(Matrix4x4.TRS(socket.LocalPosition, socket.LocalRotation, Vector3.one),
                    solid);
                Assert.IsFalse(placed.Intersects(other), $"clear of {socket.Name}: {placed} vs {other}");
            }
        }

        private static Bounds Transformed(Matrix4x4 place, Bounds local)
        {
            var bounds = new Bounds(place.MultiplyPoint3x4(local.min), Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                bounds.Encapsulate(place.MultiplyPoint3x4(new Vector3((i & 1) != 0 ? local.max.x : local.min.x,
                    (i & 2) != 0 ? local.max.y : local.min.y, (i & 4) != 0 ? local.max.z : local.min.z)));
            }

            return bounds;
        }

        [Test]
        public void SupplyPod_CarriesItsCrackedBeacon_ForWorldsCanyonGlow()
        {
            ModelNode glow = LootContainerBuilder.CreateSupplyPod().Children
                .Single(n => n.Name == LootContainerBuilder.GlowName);
            Assert.AreEqual(ModelMaterial.PaletteGlowOff, glow.Material, "dark until World lights it");
            Assert.IsTrue(MeshChecks.Swatches(glow.Mesh.Geometry).All(Palette.IsEmissive));
            Assert.AreEqual(3.5f, glow.LocalPosition.y, 0.25f, "atop its locator mast, about 3.5 m up, seen from home");
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
            ModelNode inside = pod.Children.Single(n => n.Name == LootContainerBuilder.PartSocketPrefix + "2");
            AssertSealedLid(pod.Children.Single(n => n.Name == LootContainerBuilder.LidName), inside.LocalPosition);
        }

        private static void AssertSealedLid(ModelNode lid, Vector3 find)
        {
            Assert.IsNotNull(lid.Mesh);
            Assert.AreEqual(Quaternion.identity, lid.LocalRotation, "shut at rest");
            Assert.Greater(lid.LocalPosition.y, find.y, "the lid closes over the find");
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
