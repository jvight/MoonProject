using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// Bell's home on the radio tower (M3-05): BellCorner and CassetteShelfAnchor on every stage, clear of the tower
    /// upgrade pad (gameplay's RadioTowerTuning: 3.4 m in front of the tower, radius 2.4, a 0.35 m ring on its edge),
    /// the lander and the tower itself, with 2.5 m of open ground around Bell for her dance and for 07 parking.
    /// </summary>
    public sealed class BellCornerTests
    {
        private const float ClearRadius = 2.5f;
        private const float PadRadius = 2.4f;
        private const float PadRingOuter = PadRadius + 0.175f;
        private const float RoverHalfLength = 1.1f;
        private const float ParkingDistance = 2f;
        private static readonly Vector3 PadCentre = new Vector3(0f, 0f, 3.4f);

        [Test]
        public void EveryStage_CarriesTheSameSockets_OnTheGround()
        {
            ModelNode first = BaseModelBuilder.CreateTower(1);
            for (int level = 1; level <= 3; level++)
            {
                ModelNode tower = BaseModelBuilder.CreateTower(level);
                foreach (string name in new[] { "BellCorner", "CassetteShelfAnchor" })
                {
                    ModelNode socket = tower.Children.SingleOrDefault(child => child.Name == name);
                    Assert.IsNotNull(socket, $"L{level} needs {name} as a direct child");
                    Assert.IsNull(socket.Mesh, $"{name} is an empty");
                    Assert.AreEqual(0f, socket.LocalPosition.y, 1e-5f, $"{name} on the ground");
                    Assert.AreEqual(1f, (socket.LocalRotation * Vector3.up).y, 1e-5f, $"{name} turns about +Y only");
                    ModelNode reference = first.GetDescendant(name);
                    Assert.AreEqual(reference.LocalPosition, socket.LocalPosition, $"{name} on L{level}");
                    Assert.AreEqual(reference.LocalRotation, socket.LocalRotation, $"{name} on L{level}");
                }

                Assert.AreEqual("BeaconSocket", tower.Children[1].Name, "existing nodes keep their order");
            }
        }

        [Test]
        public void BellsCircle_IsClearOfTheTower_TheLander_HerShelfAndThePad()
        {
            Vector3 corner = BaseModelBuilder.BellCorner;
            Assert.GreaterOrEqual(Flat(corner - PadCentre), ClearRadius + PadRingOuter, "circle off the pad's ring");
            for (int level = 1; level <= 3; level++)
            {
                foreach (Vector3 p in MeshChecks.Points(BaseModelBuilder.CreateTower(level), Matrix4x4.identity))
                {
                    Assert.GreaterOrEqual(Flat(p - corner), ClearRadius, $"L{level} reaches into Bell's circle");
                }
            }

            foreach (Vector3 p in LanderInTowerSpace())
            {
                Assert.GreaterOrEqual(Flat(p - corner), ClearRadius, "the lander reaches into Bell's circle");
            }

            foreach (Vector3 p in ShelfOnItsAnchor())
            {
                Assert.GreaterOrEqual(Flat(p - corner), ClearRadius, "the tape rack stands in Bell's circle");
            }
        }

        [Test]
        public void Shelf_StaysOffThePad_TheLander_AndTheTowerFoot()
        {
            Bounds lander = Enclose(LanderInTowerSpace());
            float plinth = BaseModelBuilder.TowerPlinthSize * 0.5f;
            foreach (Vector3 p in ShelfOnItsAnchor())
            {
                Assert.GreaterOrEqual(Flat(p - PadCentre), PadRingOuter, "the rack stands on the pad");
                Assert.IsTrue(p.x < lander.min.x - 0.5f || p.z < lander.min.z - 0.5f || p.z > lander.max.z + 0.5f,
                    "the rack crowds the lander");
                Assert.IsTrue(Mathf.Abs(p.x) > plinth + 0.5f || Mathf.Abs(p.z) > plinth + 0.5f,
                    "the rack crowds the tower foot");
            }
        }

        [Test]
        public void BellAndHerShelf_FaceTheLander()
        {
            Vector3 lander = -BaseModelBuilder.TowerAnchor;
            ModelNode tower = BaseModelBuilder.CreateTower(1);
            foreach (string name in new[] { "BellCorner", "CassetteShelfAnchor" })
            {
                ModelNode socket = tower.GetDescendant(name);
                Vector3 facing = socket.LocalRotation * Vector3.forward;
                Vector3 toLander = lander - socket.LocalPosition;
                toLander.y = 0f;
                Assert.Less(Vector3.Angle(facing, toLander), 30f, $"{name} +Z faces the lander");
            }
        }

        [Test]
        public void Bell_FitsHerCircle_And07ParksAtHerDial_OffThePad()
        {
            ModelNode corner = BaseModelBuilder.CreateTower(2).GetDescendant("BellCorner");
            Matrix4x4 standing = corner.LocalMatrix;
            foreach (Vector3 p in MeshChecks.Points(BellModelBuilder.CreateBell(false), standing))
            {
                Assert.Less(Flat(p - corner.LocalPosition), 1f, "Bell stands well inside her circle");
            }

            Vector3 parked = standing.MultiplyPoint3x4(Vector3.forward * ParkingDistance);
            Assert.Greater(Flat(parked - PadCentre), PadRadius + 0.5f, "parking at her dial never triggers the pad");
            float plinth = BaseModelBuilder.TowerPlinthSize * 0.5f;
            var gap = new Vector2(Mathf.Max(0f, Mathf.Abs(parked.x) - plinth),
                Mathf.Max(0f, Mathf.Abs(parked.z) - plinth));
            Assert.Greater(gap.magnitude, RoverHalfLength + 0.5f, "07 parks clear of the tower foot");
        }

        private static Vector3[] LanderInTowerSpace()
        {
            Matrix4x4 lander = Matrix4x4.Translate(-BaseModelBuilder.TowerAnchor);
            return MeshChecks.Points(BaseModelBuilder.CreateLander(), lander).ToArray();
        }

        private static Vector3[] ShelfOnItsAnchor()
        {
            ModelNode anchor = BaseModelBuilder.CreateTower(1).GetDescendant("CassetteShelfAnchor");
            return MeshChecks.Points(BaseModelBuilder.CreateCassetteShelf(), anchor.LocalMatrix).ToArray();
        }

        private static float Flat(Vector3 offset)
        {
            return new Vector2(offset.x, offset.z).magnitude;
        }

        private static Bounds Enclose(Vector3[] points)
        {
            var bounds = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 p in points)
            {
                bounds.Encapsulate(p);
            }

            return bounds;
        }
    }
}
