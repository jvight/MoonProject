using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>The rover rig contract (docs/ARCHITECTURE.md, "Contract: rover model rig") and the 07 budget.</summary>
    public sealed class RoverModelTests
    {
        private static readonly string[] ContractPaths =
        {
            "Body", "Body/Decal07Fresh", "Bogie_L", "Bogie_R",
            "Wheel_FL", "Wheel_FR", "Wheel_ML", "Wheel_MR", "Wheel_RL", "Wheel_RR",
            "Neck", "Neck/Head", "Neck/Head/Eye", "Neck/Head/Eye/TetherOrigin", "Neck/Head/Eyelid",
            "SolarWing", "SolarWing/CellFilled", "Antenna", "Antenna/AntennaTip", "Antenna/Pennant",
            "HeadlampSocket", "CargoSocket", "DustSocket_L", "DustSocket_R", "CoilSocket", "DrumSocket_L",
            "DrumSocket_R",
        };

        private static readonly string[] EmptyNodes =
        {
            "TetherOrigin", "HeadlampSocket", "CargoSocket", "DustSocket_L", "DustSocket_R", "CoilSocket",
            "DrumSocket_L", "DrumSocket_R",
        };

        private ModelNode _rover;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            _rover = RoverModelBuilder.CreateModel();
        }

        [Test]
        public void Hierarchy_IsExactlyTheContract()
        {
            var paths = new List<string>();
            CollectPaths(_rover, string.Empty, paths);

            Assert.AreEqual(RoverModelBuilder.ModelName, _rover.Name);
            CollectionAssert.AreEquivalent(ContractPaths, paths);
        }

        [Test]
        public void Wheels_SitOnTheGround_WithRadiusAndRollAxisPerContract()
        {
            foreach (string name in new[] { "Wheel_FL", "Wheel_FR", "Wheel_ML", "Wheel_MR", "Wheel_RL", "Wheel_RR" })
            {
                ModelNode wheel = _rover.GetDescendant(name);
                Assert.AreEqual(RoverModelBuilder.WheelRadius, wheel.LocalPosition.y, 1e-5f, name);
                Assert.AreEqual(RoverModelBuilder.WheelTrack, Mathf.Abs(wheel.LocalPosition.x), 1e-5f, name);
                Assert.AreEqual(name[name.Length - 1] == 'L', wheel.LocalPosition.x < 0f, $"{name} side");
                Assert.AreEqual(Quaternion.identity, wheel.LocalRotation, name);

                float reach = 0f;
                IReadOnlyList<Vector3> positions = wheel.Mesh.Geometry.Positions;
                for (int v = 0; v < positions.Count; v++)
                {
                    reach = Mathf.Max(reach, new Vector2(positions[v].y, positions[v].z).magnitude);
                }

                Assert.AreEqual(RoverModelBuilder.WheelRadius, reach, 0.012f, $"{name} rolls on a 0.35 m radius");
            }
        }

        [Test]
        public void Sockets_AreEmpty_AndPointWherePromised()
        {
            foreach (string name in EmptyNodes)
            {
                Assert.IsNull(_rover.GetDescendant(name).Mesh, $"{name} must be an empty");
            }

            ModelNode headlamp = _rover.GetDescendant("HeadlampSocket");
            Assert.Greater((headlamp.LocalRotation * Vector3.forward).z, 0.99f, "headlamp shines forward");
            Assert.Less(headlamp.LocalPosition.y, 0.7f, "headlamp sits low on the body front");
            Assert.AreEqual(Quaternion.identity, _rover.GetDescendant("TetherOrigin").LocalRotation);
            Assert.AreEqual(0f, _rover.GetDescendant("DustSocket_L").LocalPosition.y, 1e-5f);
            Assert.AreEqual(-RoverModelBuilder.WheelBase, _rover.GetDescendant("DustSocket_R").LocalPosition.z, 1e-5f);
        }

        [Test]
        public void GlowingParts_AreTheirOwnRenderers_WithEmissiveSwatches()
        {
            AssertOnlyEmissiveIn(_rover.GetDescendant("AntennaTip").Mesh.Geometry);
            CollectionAssert.Contains(Swatches(_rover.GetDescendant("Eye").Mesh.Geometry), PaletteSwatch.WarmLamp);
            CollectionAssert.DoesNotContain(Swatches(_rover.GetDescendant("Head").Mesh.Geometry),
                PaletteSwatch.WarmLamp);
        }

        [Test]
        public void EveryMesh_IsWellFormed()
        {
            foreach (ModelNode node in AllNodes(_rover))
            {
                if (node.Mesh != null)
                {
                    MeshChecks.AssertWellFormed(node.Mesh.Geometry);
                }
            }
        }

        [Test]
        public void Model_FitsTheContractSize_AndTriangleBudget()
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = -min;
            int triangles = 0;
            Accumulate(_rover, Matrix4x4.identity, ref min, ref max, ref triangles);

            Vector3 size = max - min;
            Assert.AreEqual(0f, min.y, 0.01f, "wheels touch the ground at y = 0");
            Assert.That(size.z, Is.InRange(2.1f, 2.4f), "length");
            Assert.That(size.x, Is.InRange(1.4f, 1.7f), "width");
            Assert.That(max.y, Is.InRange(1.35f, 1.8f), "height (the hooded head and whip antenna top it)");
            Assert.That(triangles, Is.InRange(3000, 7000), "triangle budget (the hidden gifts included)");
        }

        [Test]
        public void CreateModel_IsDeterministic()
        {
            ModelNode again = RoverModelBuilder.CreateModel();

            CollectionAssert.AreEqual(_rover.GetDescendant("Body").Mesh.Geometry.Positions,
                again.GetDescendant("Body").Mesh.Geometry.Positions);
            CollectionAssert.AreEqual(_rover.GetDescendant("Head").Mesh.Geometry.Uvs,
                again.GetDescendant("Head").Mesh.Geometry.Uvs);
        }

        private static void CollectPaths(ModelNode node, string prefix, List<string> paths)
        {
            foreach (ModelNode child in node.Children)
            {
                string path = prefix.Length == 0 ? child.Name : $"{prefix}/{child.Name}";
                paths.Add(path);
                CollectPaths(child, path, paths);
            }
        }

        private static IEnumerable<ModelNode> AllNodes(ModelNode node)
        {
            yield return node;
            foreach (ModelNode child in node.Children)
            {
                foreach (ModelNode descendant in AllNodes(child))
                {
                    yield return descendant;
                }
            }
        }

        private static void Accumulate(ModelNode node, Matrix4x4 parent, ref Vector3 min, ref Vector3 max,
            ref int triangles)
        {
            Matrix4x4 world = parent * node.LocalMatrix;
            if (node.Mesh != null)
            {
                LowPolyMeshBuilder geometry = node.Mesh.Geometry;
                triangles += geometry.TriangleCount;
                for (int v = 0; v < geometry.VertexCount; v++)
                {
                    Vector3 p = world.MultiplyPoint3x4(geometry.Positions[v]);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
            }

            foreach (ModelNode child in node.Children)
            {
                Accumulate(child, world, ref min, ref max, ref triangles);
            }
        }

        private static HashSet<PaletteSwatch> Swatches(LowPolyMeshBuilder geometry)
        {
            var swatches = new HashSet<PaletteSwatch>();
            for (int t = 0; t < geometry.TriangleCount; t++)
            {
                swatches.Add(MeshChecks.SwatchOf(geometry, t));
            }

            return swatches;
        }

        private static void AssertOnlyEmissiveIn(LowPolyMeshBuilder geometry)
        {
            foreach (PaletteSwatch swatch in Swatches(geometry))
            {
                Assert.IsTrue(Palette.IsEmissive(swatch), $"{swatch} does not glow");
            }
        }
    }
}
