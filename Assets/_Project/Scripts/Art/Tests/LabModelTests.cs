using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// The crew's lab inside the habitat lander (docs/ARCHITECTURE.md, "Contract: the lab (M3-19)"): the stations'
    /// frames round the turntable in LabStation order, the camera's ring rail that never meets anything, the big
    /// porthole's lamp ring, the tech board with its recovery panel, and the keepsake shelf.
    /// </summary>
    public sealed class LabModelTests
    {
        // The lab's stations, warm lamps and room cost at most this many triangles all the way down.
        private const int LabBudget = 16000;

        // Nothing of the lab comes this near the ring-rail camera's path (it rides just under the rail).
        private const float CameraClearance = 0.12f;
        private const float CameraDrop = 0.08f;

        private static readonly string[] Stations = { "Chart", "Board", "Keepsakes" };

        private ModelNode _lander;
        private ModelNode _lab;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            _lander = BaseModelBuilder.CreateLander();
            _lab = _lander.GetDescendant(BaseModelBuilder.LabName);
        }

        [Test]
        public void Lab_StandsOnTheCabinFloor_WithItsStationsInOrder_RoundTheTurntable()
        {
            Assert.IsTrue(_lander.Children.Contains(_lab), "Lab sits right under the lander");
            Assert.AreEqual(Quaternion.identity, _lab.LocalRotation, "the lander's axes");
            Assert.AreEqual(_lander.GetDescendant(BaseModelBuilder.PlatformUpName).LocalPosition, _lab.LocalPosition,
                "its origin is the platform's top when it is up");
            float previousYaw = float.NegativeInfinity;
            foreach (string name in Stations)
            {
                ModelNode station = _lab.GetDescendant(BaseModelBuilder.StationPrefix + name);
                Assert.IsNotNull(station, name);
                Assert.IsTrue(_lab.Children.Contains(station), $"{name} sits right under the lab");
                Vector3 inward = station.LocalRotation * Vector3.forward;
                var flat = new Vector3(station.LocalPosition.x, 0f, station.LocalPosition.z);
                Assert.Less(Vector3.Dot(inward, flat.normalized), -0.99f, $"{name} faces the turntable's axis");
                Assert.That(station.LocalPosition.y, Is.InRange(1.1f, 1.4f), $"{name} at 07's lens height");
                Assert.That(flat.magnitude, Is.InRange(1.9f, 2.6f), $"{name} in the ring of stations");
                float yaw = Mathf.Repeat(Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg - 180f, 360f);
                Assert.Greater(yaw, previousYaw, $"{name} comes clockwise after the station before");
                previousYaw = yaw;
            }
        }

        [Test]
        public void Chart_SitsUnderTheBigPorthole_AndItsLampRingCountsUpTo32()
        {
            ModelNode chart = _lab.GetDescendant(BaseModelBuilder.StationPrefix + "Chart");
            Vector3 porthole = _lander.GetDescendant(BaseModelBuilder.BigPortholeName).Mesh.Geometry.Bounds.center;
            var toChart = new Vector3(chart.LocalPosition.x, 0f, chart.LocalPosition.z);
            var toPorthole = new Vector3(porthole.x, 0f, porthole.z);
            Assert.Less(Vector3.Angle(toChart, toPorthole), 5f, "the chart is under the big porthole");
            Assert.Greater(porthole.y - _lab.LocalPosition.y, chart.LocalPosition.y, "the porthole is over it");

            ModelNode ring = _lander.GetDescendant(BaseModelBuilder.PortholeLampsName);
            Assert.IsTrue(_lander.Children.Contains(ring), "the lamp ring shows from outside, not under the lab");
            CollectionAssert.AreEqual(Enumerable.Range(0, BaseModelBuilder.PortholeLampCount)
                .Select(i => BaseModelBuilder.LampPrefix + i), ring.Children.Select(c => c.Name));
            Assert.AreEqual(32, BaseModelBuilder.PortholeLampCount, "room for 0.8's nodes");
            float radius = 0f;
            foreach (ModelNode lamp in ring.Children)
            {
                Assert.AreEqual(ModelMaterial.PaletteGlowOff, lamp.Material, $"{lamp.Name} starts dark");
                float distance = Vector3.Distance(lamp.LocalPosition, porthole);
                radius = radius == 0f ? distance : radius;
                Assert.AreEqual(radius, distance, 0.03f, $"{lamp.Name} on the ring round the porthole");
            }

            Vector3 first = ring.Children[0].LocalPosition;
            Vector3 second = ring.Children[1].LocalPosition;
            Assert.Greater(first.y, porthole.y + radius * 0.95f, "the ring starts at the top");
            Assert.Less(second.y, first.y, "and runs round from there");
        }

        [Test]
        public void RingRail_RidesUnderTheCeiling_AndTheCameraNeverMeetsAnything()
        {
            ModelNode rail = _lab.GetDescendant(BaseModelBuilder.RingRailName);
            var axis = new Vector2(rail.LocalPosition.x, rail.LocalPosition.z);
            Assert.AreEqual(0f, axis.magnitude, 1e-4f, "on the axis");
            Bounds room = _lab.Mesh.Geometry.Bounds;
            Assert.Less(rail.LocalPosition.y, room.max.y, "under the ceiling");
            Assert.Greater(rail.LocalPosition.y, 2f, "over 07's head");
            float radius = BaseModelBuilder.LabRingRadius;
            float camera = rail.LocalPosition.y - CameraDrop;
            var things = new List<Vector3>();
            foreach (ModelNode child in _lab.Children.Where(c => c.Name != BaseModelBuilder.RingRailName))
            {
                SilhouetteCamera.CollectTriangles(child, Matrix4x4.identity, things);
            }

            things.AddRange(_lab.Mesh.Geometry.Positions);
            foreach (Vector3 p in things)
            {
                float across = new Vector2(p.x, p.z).magnitude - radius;
                float up = p.y - camera;
                Assert.Greater(new Vector2(across, up).magnitude, CameraClearance,
                    $"something at {p} comes within reach of the ring-rail camera");
            }
        }

        [Test]
        public void Board_PinsEveryNode_UnderTheRecoveryPanelsFourRows()
        {
            ModelNode board = _lab.GetDescendant(BaseModelBuilder.StationPrefix + "Board");
            for (int n = 0; n < 9; n++)
            {
                Assert.IsNotNull(board.GetDescendant(BaseModelBuilder.PinPrefix + n), $"pin {n}");
                Assert.IsFalse(board.GetDescendant(BaseModelBuilder.SheetPrefix + n).Active, $"sheet {n} waits");
                Assert.AreEqual(ModelMaterial.PaletteGlowOff, board.GetDescendant(BaseModelBuilder.LampPrefix + n)
                    .Material, $"lamp {n} starts dark");
            }

            Assert.IsNull(board.GetDescendant(BaseModelBuilder.PinPrefix + 9), "only 0.5's nine nodes");
            ModelNode panel = board.GetDescendant(BaseModelBuilder.RecoveryPanelName);
            CollectionAssert.AreEqual(new[] { "Row_Power", "Row_Link", "Row_Dish", "Row_Answer" },
                panel.Children.Select(c => c.Name));
            CollectionAssert.AreEqual(new[] { 3, 8, 4, 1 }, panel.Children.Select(c => c.Children.Count));
            Assert.Greater(panel.LocalPosition.y, board.GetDescendant(BaseModelBuilder.PinPrefix + 0).LocalPosition.y,
                "the panel heads the board");
            foreach (ModelNode lamp in panel.Children.SelectMany(row => row.Children))
            {
                Assert.AreEqual(ModelMaterial.PaletteGlowOff, lamp.Material, "dark glass until lit");
            }

            ModelNode link = panel.GetDescendant("Row_Link");
            Assert.Greater(link.Children.Last().Mesh.Geometry.Bounds.size.x,
                link.Children.First().Mesh.Geometry.Bounds.size.x, "the Peak's foot is the larger last lamp");
        }

        [Test]
        public void Keepsakes_HaveSixCubbies_AndAReadingLampPerCrewSection()
        {
            ModelNode shelf = _lab.GetDescendant(BaseModelBuilder.StationPrefix + "Keepsakes");
            for (int i = 0; i < 6; i++)
            {
                ModelNode slot = shelf.GetDescendant(BaseModelBuilder.SlotPrefix + i);
                Assert.IsNull(slot.Mesh, $"slot {i} is an empty");
                Assert.AreEqual(Quaternion.identity, slot.LocalRotation, "+Y up, +Z out of the cubby");
            }

            for (int i = 0; i < 4; i++)
            {
                ModelNode lamp = shelf.GetDescendant(BaseModelBuilder.ReadingLampPrefix + i);
                Assert.AreEqual(ModelMaterial.PaletteGlowOff, lamp.Material, $"reading lamp {i} starts dark");
            }

            Assert.IsNull(shelf.GetDescendant(BaseModelBuilder.ReadingLampPrefix + 4), "four crew, four lamps");
        }

        [Test]
        public void Lab_IsWellFormed_AndWithinBudget()
        {
            int triangles = 0;
            foreach (LowPolyMeshBuilder mesh in Meshes(_lab))
            {
                MeshChecks.AssertWellFormed(mesh);
                triangles += mesh.TriangleCount;
            }

            TestContext.WriteLine($"Lab: {triangles} triangles");
            Assert.LessOrEqual(triangles, LabBudget, "the lab's triangle budget");
        }

        private static IEnumerable<LowPolyMeshBuilder> Meshes(ModelNode node)
        {
            if (node.Mesh != null)
            {
                yield return node.Mesh.Geometry;
            }

            foreach (LowPolyMeshBuilder mesh in node.Children.SelectMany(Meshes))
            {
                yield return mesh;
            }
        }
    }
}
