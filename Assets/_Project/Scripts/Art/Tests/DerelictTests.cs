using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>The robots left behind (0.5.5 owner feedback §1.7): laid out like sites, hosting their
    /// fragments.</summary>
    public sealed class DerelictTests
    {
        private static readonly Regex PieceName = new Regex(@"^Salvage_(\d+)_(Metal|Wiring|Optics)$");

        private static IEnumerable<TestCaseData> Derelicts()
        {
            yield return new TestCaseData(DerelictBuilder.CreateElder(), "CellPack", 3, 0).SetName("06");
            yield return new TestCaseData(DerelictBuilder.CreateMule(), "WarmHeadlamp", 2, 1).SetName("Mule");
            yield return new TestCaseData(DerelictBuilder.CreateHaulBot(), "BoostCoils", 3, 2).SetName("HaulBot");
            yield return new TestCaseData(DerelictBuilder.CreateDrone(), string.Empty, 0, 1).SetName("Drone");
        }

        [TestCaseSource(nameof(Derelicts))]
        public void Derelict_IsLaidOutLikeASite_WithItsSocketsAndCuts(ModelNode derelict, string blueprint, int sockets,
            int pieces)
        {
            StringAssert.StartsWith(DerelictBuilder.Prefix, derelict.Name);
            Assert.IsNull(derelict.Mesh, "the root is an empty on the ground at its anchor");
            ModelNode skeleton = derelict.Children.Single(n => n.Name == SiteModelBuilder.SkeletonName);
            MeshChecks.AssertWellFormed(skeleton.Mesh.Geometry);
            Assert.IsFalse(MeshChecks.Swatches(skeleton.Mesh.Geometry).Any(Palette.IsEmissive),
                "eyes dark, nothing glowing: stopped, not menacing");
            Assert.Less(skeleton.Mesh.Geometry.Bounds.min.y, 0.05f, "it rests on the ground");

            for (int i = 0; i < sockets; i++)
            {
                ModelNode socket = derelict.Children.Single(
                    n => n.Name == SiteModelBuilder.FragmentSocketPrefix + blueprint + "_" + i);
                Assert.IsNull(socket.Mesh);
                Assert.Greater(socket.LocalPosition.y, 0.02f, $"{socket.Name} rests on something, not bare ground");
            }

            List<ModelNode> cuts = derelict.Children.Where(n => PieceName.IsMatch(n.Name)).ToList();
            Assert.AreEqual(pieces, cuts.Count);
            for (int i = 0; i < cuts.Count; i++)
            {
                Assert.AreEqual(i.ToString(), PieceName.Match(cuts[i].Name).Groups[1].Value, "numbered in order");
                ModelNode cut = cuts[i].Children.Single();
                Assert.AreEqual(SiteModelBuilder.CutPointName, cut.Name);
                MeshChecks.AssertWellFormed(cuts[i].Mesh.Geometry);
            }
        }

        [Test]
        public void Elder06_HoldsRosTapeCase_BesideItsBay_ClearOfTheCells()
        {
            ModelNode elder = DerelictBuilder.CreateElder();
            ModelNode socket = elder.Children.Single(n => n.Name == DerelictBuilder.TapeSocketName);
            Assert.IsNull(socket.Mesh);
            Assert.AreEqual(0f, socket.LocalPosition.y, 1e-5f, "on the ground: the case has its own dust skirt");
            Assert.Greater((socket.LocalRotation * Vector3.forward).x, 0.99f, "its seal faces out from 06's side");
            ModelNode door = elder.Children.Single(n => n.Name == SiteModelBuilder.FragmentSocketPrefix + "CellPack_1");
            Assert.Less(Vector3.Distance(socket.LocalPosition, door.LocalPosition), 0.7f, "by the bay's fallen door");

            ModelNode tapeCase = LootContainerBuilder.CreateTapeCase();
            Bounds placed = Placed(socket, tapeCase.Mesh.Geometry.Bounds);
            placed.Encapsulate(Placed(socket, tapeCase.Children.Single(n => n.Name == LootContainerBuilder.LidName),
                tapeCase.Children.Single(n => n.Name == LootContainerBuilder.LidName).Mesh.Geometry.Bounds));
            for (int i = 0; i < 3; i++)
            {
                ModelNode cell = elder.Children.Single(
                    n => n.Name == SiteModelBuilder.FragmentSocketPrefix + "CellPack_" + i);
                ModelNode fragment = FragmentBuilder.CreateFragment("CellPack", i);
                Bounds solid = Placed(cell,
                    fragment.Children.Single(n => n.Name == FragmentBuilder.SolidName).Mesh.Geometry.Bounds);
                Assert.IsFalse(placed.Intersects(solid), $"clear of {cell.Name}: {placed} vs {solid}");
            }
        }

        private static Bounds Placed(ModelNode socket, Bounds local)
        {
            return Placed(socket, null, local);
        }

        private static Bounds Placed(ModelNode socket, ModelNode child, Bounds local)
        {
            Matrix4x4 place = Matrix4x4.TRS(socket.LocalPosition, socket.LocalRotation, Vector3.one);
            if (child != null)
            {
                place *= child.LocalMatrix;
            }

            var bounds = new Bounds(place.MultiplyPoint3x4(local.min), Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                bounds.Encapsulate(place.MultiplyPoint3x4(new Vector3((i & 1) != 0 ? local.max.x : local.min.x,
                    (i & 2) != 0 ? local.max.y : local.min.y, (i & 4) != 0 ? local.max.z : local.min.z)));
            }

            return bounds;
        }

        [Test]
        public void Elder06_IsSeventhsSibling_SmallerAndDark()
        {
            Bounds elder = DerelictBuilder.CreateElder().Children[0].Mesh.Geometry.Bounds;
            Bounds rover = RoverModelBuilder.CreateModel().GetDescendant("Body").Mesh.Geometry.Bounds;
            Assert.Less(elder.size.z, rover.size.z + 0.4f, "a smaller cart than 07, even with its bay door and cells");
            Assert.Less(elder.max.y, 1.7f, "slumped, head down");
        }

        [Test]
        public void Drone_HangsFromItsSnag_AndFallsFreeWhenCut()
        {
            ModelNode drone = DerelictBuilder.CreateDrone();
            ModelNode body = drone.Children.Single(n => n.Name == DerelictBuilder.DroneNodeName);
            ModelNode boom = drone.Children.Single(n => n.Name == "Salvage_0_Optics");
            Assert.Less(body.LocalPosition.y, boom.LocalPosition.y, "it hangs below the snag");
            Assert.Greater(body.LocalPosition.y, 0.6f, "off the ground, caught in the wire");
            CollectionAssert.IsEmpty(body.Children, "nothing else holds it up");
        }
    }
}
