using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// Kenji's Rover Bay (M3-14, VISION ruling 14): the node contract the install moment drives, a turntable 07 fits on
    /// with room all round, arms folded clear above it, a clear way in, a hopper mouth at rover height facing the
    /// turntable, dark work lamps, and repeatable builds.
    /// </summary>
    public sealed class RoverBayTests
    {
        private const float TurntableRoom = 0.25f;
        private const float HeadRoom = 0.3f;
        private const float SideRoom = 0.15f;
        private const float MinHopperHeight = 0.9f;
        private const float MaxHopperHeight = 1.5f;

        private ModelNode _bay;
        private Bounds _rover;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            _bay = BaseModelBuilder.CreateRoverBay();
            var triangles = new List<Vector3>();
            SilhouetteCamera.CollectTriangles(RoverModelBuilder.CreateModel(), Matrix4x4.identity, triangles);
            _rover = new Bounds(triangles[0], Vector3.zero);
            foreach (Vector3 p in triangles)
            {
                _rover.Encapsulate(p);
            }
        }

        [Test]
        public void Bay_HasTheContractNodes()
        {
            Assert.AreEqual("RoverBay", _bay.Name);
            var expected = new List<string> { "Turntable", "HopperMouth", "Lamp_0", "Lamp_1" };
            for (int i = 0; i < 3; i++)
            {
                expected.Add($"Arm_{i}");
                expected.Add($"Arm_{i}/Upper");
                expected.Add($"Arm_{i}/Upper/Lower");
                expected.Add($"Arm_{i}/Upper/Lower/Tip");
                expected.Add($"Arm_{i}/Upper/Lower/Tip/SparkSocket");
            }

            expected.AddRange(new[] { "Weather_Paint", "Weather_Rust", "Weather_Dust" });
            var paths = new List<string>();
            Collect(_bay, string.Empty, paths);
            CollectionAssert.AreEquivalent(expected, paths);
            Assert.AreEqual(0f, _bay.Mesh.Geometry.Bounds.min.y, 0.02f, "stands on the ground at its anchor");
            Assert.IsNull(_bay.GetDescendant("HopperMouth").Mesh);
            Assert.IsNull(_bay.GetDescendant("SparkSocket").Mesh);
            foreach (ModelNode node in Nodes(_bay).Where(node => node.Mesh != null))
            {
                MeshChecks.AssertWellFormed(node.Mesh.Geometry);
                bool lamp = node.Name.StartsWith("Lamp_");
                Assert.AreEqual(lamp ? ModelMaterial.PaletteGlowOff : ModelMaterial.Palette, node.Material, node.Name);
                List<PaletteSwatch> swatches = MeshChecks.Swatches(node.Mesh.Geometry);
                Assert.IsTrue(lamp ? swatches.SequenceEqual(new[] { PaletteSwatch.LampGlass })
                    : !swatches.Any(Palette.IsEmissive), $"{node.Name}: only the lamp glasses glow, and only when lit");
            }
        }

        [Test]
        public void Turntable_Holds07_WithRoomAllRound()
        {
            ModelNode turntable = _bay.GetDescendant("Turntable");
            float radius = turntable.Mesh.Geometry.Bounds.extents.x;
            float reach = 0f;
            var triangles = new List<Vector3>();
            SilhouetteCamera.CollectTriangles(RoverModelBuilder.CreateModel(), Matrix4x4.identity, triangles);
            foreach (Vector3 p in triangles)
            {
                reach = Mathf.Max(reach, new Vector2(p.x, p.z).magnitude);
            }

            Assert.GreaterOrEqual(radius - reach, TurntableRoom, "07 (2.3 m long) fits on the turntable with room");
            Assert.Less((turntable.LocalRotation * Vector3.forward).z, -0.99f, "07 parks facing in, the way it drove");
        }

        [Test]
        public void Arms_AtRest_HangClearAbove07()
        {
            float padTop = _bay.GetDescendant("Turntable").LocalPosition.y;
            float roof = padTop + _rover.max.y + HeadRoom;
            foreach (ModelNode arm in _bay.Children.Where(child => child.Name.StartsWith("Arm_")))
            {
                foreach (Vector3 p in MeshChecks.Points(arm, Matrix4x4.identity))
                {
                    Assert.Greater(p.y, roof, $"{arm.Name} hangs into 07's way at {p}");
                }
            }
        }

        [Test]
        public void TheWayIn_IsClear_FromTheRampToTheTurntable()
        {
            ModelNode turntable = _bay.GetDescendant("Turntable");
            float padTop = turntable.LocalPosition.y;
            float front = _bay.Mesh.Geometry.Bounds.max.z;
            var corridor = new Bounds();
            corridor.SetMinMax(new Vector3(-_rover.extents.x - SideRoom, padTop + 0.05f, turntable.LocalPosition.z),
                new Vector3(_rover.extents.x + SideRoom, padTop + _rover.max.y + 0.1f, front));
            foreach (ModelNode node in Nodes(_bay).Where(node => node.Mesh != null && node.Name != "Turntable"
                && !node.Name.StartsWith("Weather_")))
            {
                Matrix4x4 world = WorldOf(node) * node.LocalMatrix;
                IReadOnlyList<Vector3> points = node.Mesh.Geometry.Positions;
                for (int v = 0; v < points.Count; v += 3)
                {
                    var face = new Bounds(world.MultiplyPoint3x4(points[v]), Vector3.zero);
                    face.Encapsulate(world.MultiplyPoint3x4(points[v + 1]));
                    face.Encapsulate(world.MultiplyPoint3x4(points[v + 2]));
                    Assert.IsFalse(corridor.Intersects(face), $"{node.Name} blocks the way in at {face.center}");
                }
            }
        }

        [Test]
        public void Hopper_IsAtRoverHeight_FacingTheTurntable()
        {
            ModelNode mouth = _bay.GetDescendant("HopperMouth");
            Assert.That(mouth.LocalPosition.y, Is.InRange(MinHopperHeight, MaxHopperHeight), "07's beam reaches it");
            Vector3 toPad = _bay.GetDescendant("Turntable").LocalPosition - mouth.LocalPosition;
            Vector3 facing = mouth.LocalRotation * Vector3.forward;
            Assert.Greater(Vector2.Dot(new Vector2(facing.x, facing.z).normalized,
                new Vector2(toPad.x, toPad.z).normalized),
                0.95f, "the mouth faces the turntable");
        }

        [Test]
        public void Bay_BuildsIdentically()
        {
            MeshChecks.AssertSameModel(_bay, BaseModelBuilder.CreateRoverBay());
        }

        /// <summary>
        /// The matrix from <paramref name="target"/>'s parent space to bay space (identity for the root).
        /// </summary>
        private Matrix4x4 WorldOf(ModelNode target)
        {
            return FindParent(_bay, target, Matrix4x4.identity) ?? Matrix4x4.identity;
        }

        private static Matrix4x4? FindParent(ModelNode node, ModelNode target, Matrix4x4 parent)
        {
            Matrix4x4 local = parent * node.LocalMatrix;
            foreach (ModelNode child in node.Children)
            {
                if (child == target)
                {
                    return local;
                }

                Matrix4x4? hit = FindParent(child, target, local);
                if (hit.HasValue)
                {
                    return hit;
                }
            }

            return null;
        }

        private static void Collect(ModelNode node, string prefix, List<string> paths)
        {
            foreach (ModelNode child in node.Children)
            {
                string path = prefix.Length == 0 ? child.Name : $"{prefix}/{child.Name}";
                paths.Add(path);
                Collect(child, path, paths);
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
