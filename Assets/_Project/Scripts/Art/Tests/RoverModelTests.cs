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
            "Body", "Body/Decal07Fresh", "Body/Weather_Paint", "Body/Weather_Rust", "Body/Weather_Dust",
            "Bogie_L", "Bogie_L/Weather_Rust", "Bogie_L/Weather_Dust", "Bogie_R", "Bogie_R/Weather_Rust",
            "Bogie_R/Weather_Dust",
            "Wheel_FL", "Wheel_FL/Weather_Dust", "Wheel_FR", "Wheel_FR/Weather_Dust", "Wheel_ML",
            "Wheel_ML/Weather_Dust", "Wheel_MR", "Wheel_MR/Weather_Dust", "Wheel_RL", "Wheel_RL/Weather_Dust",
            "Wheel_RR", "Wheel_RR/Weather_Dust",
            "Neck", "Neck/Head", "Neck/Head/Eye", "Neck/Head/Eye/TetherOrigin", "Neck/Head/RingSocket", "Neck/Head/Eyelid",
            "Neck/Head/Weather_Paint", "Neck/Head/Weather_Dust", "Neck/Head/Eye/LensSocket",
            "Neck/Head/BrowLampSocket_0", "Neck/Head/BrowLampSocket_1",
            "SolarWing", "SolarWing/CellFilled", "Antenna", "Antenna/AntennaTip", "Antenna/Pennant",
            "HeadlampSocket", "CargoSocket", "DustSocket_L", "DustSocket_R", "CoilSocket", "DrumSocket_L",
            "DrumSocket_R", "TubeSocket", "PartSocket", "SpineSocket", "DeckSocket",
        };

        private static readonly string[] EmptyNodes =
        {
            "TetherOrigin", "HeadlampSocket", "CargoSocket", "DustSocket_L", "DustSocket_R", "CoilSocket",
            "DrumSocket_L", "DrumSocket_R", "TubeSocket", "PartSocket", "SpineSocket", "DeckSocket", "RingSocket",
            "LensSocket", "BrowLampSocket_0", "BrowLampSocket_1",
        };

        /// <summary>The battery light bar's segments (docs/features/M3-20).</summary>
        private const int BatteryLamps = 8;

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
            var contract = new List<string>(ContractPaths);
            for (int i = 0; i < BatteryLamps; i++)
            {
                contract.Add("Body/BatteryLamp_" + i);
                contract.Add("Body/BatteryLamp_" + i + "/BatteryGlass_" + i);
            }

            Assert.AreEqual(RoverModelBuilder.ModelName, _rover.Name);
            CollectionAssert.AreEquivalent(contract, paths);
        }

        [Test]
        public void TheBatteryBar_RunsAcrossTheLidsRearEdge_FacingTheChaseCamera_WithGlowingGlasses()
        {
            float previousX = float.MinValue;
            for (int i = 0; i < BatteryLamps; i++)
            {
                ModelNode segment = _rover.GetDescendant("BatteryLamp_" + i);
                ModelNode glass = _rover.GetDescendant("BatteryGlass_" + i);
                Vector3 at = segment.LocalPosition;
                Vector3 facing = segment.LocalRotation * Vector3.up;
                Assert.Greater(at.x, previousX, $"{segment.Name} runs left to right");
                Assert.Less(Mathf.Abs(at.x), 0.3f, $"{segment.Name} stays on the straight of the rear edge");
                Assert.That(at.y, Is.InRange(0.9f, 1f), $"{segment.Name} sits on the lid");
                Assert.Less(at.z, -0.56f, $"{segment.Name} sits behind the folded wing's end, on the rear edge");
                Assert.Greater(facing.y, 0.5f, $"{segment.Name} faces up, where the chase camera looks down");
                Assert.Less(facing.z, -0.7f, $"{segment.Name} tips well back toward the chase camera");
                Assert.AreEqual(ModelMaterial.Palette, segment.Material);
                Assert.AreEqual(ModelMaterial.PaletteGlowOff, glass.Material, glass.Name);
                CollectionAssert.AreEqual(new[] { PaletteSwatch.LampGlass }, Swatches(glass.Mesh.Geometry));
                previousX = at.x;
            }
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
        public void ChargeRun_TheBrowPairStandsOnTheBrowOverTheEye_AndTheLensSitsOnTheTether()
        {
            ModelNode eye = _rover.GetDescendant("Eye");
            Assert.AreEqual(_rover.GetDescendant("TetherOrigin").LocalPosition,
                _rover.GetDescendant(RoverModelBuilder.LensSocketName).LocalPosition, "the lens is on the beam's mouth");
            Bounds head = _rover.GetDescendant("Head").Mesh.Geometry.Bounds;
            float last = float.MaxValue;
            for (int i = 0; i < RoverModelBuilder.BrowLampCount; i++)
            {
                ModelNode socket = _rover.GetDescendant(RoverModelBuilder.BrowLampSocketPrefix + i);
                Vector3 up = socket.LocalRotation * Vector3.up;
                Assert.AreEqual(0f, socket.LocalPosition.x, 1e-5f, "on the centreline (P3)");
                Assert.Greater(socket.LocalPosition.y, eye.LocalPosition.y + 0.15f, $"brow lamp {i} is over the eye");
                Assert.Greater(up.y, 0f, $"brow lamp {i} faces up and out");
                Assert.Less(socket.LocalPosition.z, last, "the pair runs back from the eye");
                Assert.LessOrEqual(socket.LocalPosition.y, head.max.y + 0.01f, $"brow lamp {i} sits on the head");
                last = socket.LocalPosition.z;
            }
        }

        [Test]
        public void ChargeRunParts_GlowOnlyThroughTheirGlass()
        {
            var parts = new[]
            {
                RoverModelBuilder.CreateChargeLamp(false), RoverModelBuilder.CreateChargeLamp(true),
                RoverModelBuilder.CreateFocusLens(),
            };
            foreach (ModelNode part in parts)
            {
                ModelNode glass = part.GetDescendant(RoverModelBuilder.ChargeGlassName);
                Assert.IsNotNull(glass, $"{part.Name} has its glass");
                Assert.AreEqual(ModelMaterial.PaletteGlowOff, glass.Material, $"{part.Name}'s glass starts dark");
                for (int t = 0; t < glass.Mesh.Geometry.TriangleCount; t++)
                {
                    Assert.AreEqual(PaletteSwatch.EyeGlass, MeshChecks.SwatchOf(glass.Mesh.Geometry, t),
                        $"{part.Name}'s glass glows the cutter's cyan");
                }

                MeshChecks.AssertWellFormed(part.Mesh.Geometry);
                MeshChecks.AssertWellFormed(glass.Mesh.Geometry);
            }

            Assert.Less(RoverModelBuilder.CreateFocusLens().Mesh.Geometry.Bounds.extents.x, 0.19f,
                "the lens clips inside the eye's rim");
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
            Assert.That(triangles, Is.InRange(3000, 15000), "triangle budget (the hidden gifts and weather included)");
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

        [Test]
        public void PartSocket_StandsOnTheLid_ApartFromTheTube_SoBothReadFromTheChase()
        {
            Vector3 part = _rover.GetDescendant(RoverModelBuilder.PartSocketName).LocalPosition;
            Vector3 tube = _rover.GetDescendant(RoverModelBuilder.TubeSocketName).LocalPosition;
            Assert.AreEqual(RoverModelBuilder.NeckBase.y, part.y, 0.02f, "a part stands on the lid");
            Assert.Greater(new Vector2(part.x - tube.x, part.z - tube.z).magnitude, 0.5f,
                "a part and a tube carried together never overlap");
            Assert.Greater(part.z, tube.z, "the part rides ahead of the tube, seen over the lid from the chase");
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
