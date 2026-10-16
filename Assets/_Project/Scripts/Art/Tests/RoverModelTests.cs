using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>The R1 07 v3 Amber CRT rover rig contract (docs/ARCHITECTURE.md, "Contract: rover model rig").</summary>
    public sealed class RoverModelTests
    {
        private const string FbxPath = "Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx";
        private const string PrefabPath = "Assets/_Project/Generated/Art/R1/Models/Rover/RoverModel.prefab";

        private static readonly string[] RequiredMeshPaths =
        {
            "Body", "Body/Decal07Fresh",
            "Body/ChargeConduits/ConduitGlow",
            "Body/Stage_1_L/StageGlow_1_L",
            "Body/Stage_1_R/StageGlow_1_R",
            "Body/Stage_2_L/StageGlow_2_L",
            "Body/Stage_2_R/StageGlow_2_R",
            "Body/Stage_3_L/StageGlow_3_L",
            "Body/Stage_3_R/StageGlow_3_R",
            "Body/CargoHatch/UpperCanopy", "Body/CargoHatch/LowerChute",
            "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR",
            "Arm_FL", "Arm_FR", "Arm_RL", "Arm_RR",
            "Shock_FL", "Shock_FR", "Shock_RL", "Shock_RR",
            "Neck", "Neck/NeckLift/Head", "Neck/NeckLift/Head/Screen",
            "Neck/NeckLift/Head/Eye", "Neck/NeckLift/Head/Eye/LockRing/LockGlow",
            "SolarWing", "SolarWing/CellFilled",
            "Antenna", "Antenna/AntennaTip", "Antenna/Pennant",
            "CutterTurret", "CutterTurret/CutterHead",
            "DigScoop", "DigScoop/ScoopArms", "DigScoop/ScoopArms/ScoopBucket",
            "DigScoop/ScoopCyl_L", "DigScoop/ScoopCyl_R",
            "WinchSocket", "WinchSocket/WinchSpool",
        };

        private static readonly string[] RequiredEmptyPaths =
        {
            "Body/ChargeConduits",
            "Body/Stage_1_L", "Body/Stage_1_R", "Body/Stage_2_L", "Body/Stage_2_R", "Body/Stage_3_L", "Body/Stage_3_R",
            "Body/CargoHold", "Body/CargoHold/HatchWindow",
            "Body/CargoHold/HatchOrigin", "Body/CargoHold/StoryRackSocket",
            "Body/CargoHatch", "Body/CargoHatch/IntakeRoller",
            "Wheel_RR/WheelsSocket",
            "Neck/NeckLift",
            "Neck/NeckLift/Head/Visor", "Neck/NeckLift/Head/RearVentStrip", "Neck/NeckLift/Head/DeckCover",
            "Neck/NeckLift/Head/DeckSocket", "Neck/NeckLift/Head/RingSocket",
            "Neck/NeckLift/Head/Eye/LockRing",
            "Neck/NeckLift/Head/Eye/EyeOrigin", "Neck/NeckLift/Head/Eye/LensSocket", "Neck/NeckLift/Head/Eye/ScanFanSocket",
            "SolarWing/WingSocket",
            "Antenna/AntennaSocket", "Antenna/SonarLoop", "Antenna/SonarLoop/AntennaOrigin",
            "CutterTurret/CutterHead/CutterOrigin", "CutterTurret/CutterHead/CutterSocket",
            "CutterTurret/CutterHead/StitchOrigin", "CutterTurret/CutterHead/CoilSocket_T1",
            "DigScoop/ScoopArms/ScoopBucket/ScoopOrigin",
            "WinchSocket/TowHook", "WinchSocket/TowHook/WinchOrigin", "WinchSocket/TetherSocket",
            "CargoSocket", "HoldSocket", "CradleSocket", "HeadlampSocket", "LampSocket",
            "CoilSocket", "BellySocket", "DrumSocket_L", "DrumSocket_R", "FlanksSocket",
            "BinSocket_L", "BinSocket_R", "PanniersSocket", "SpineSocket", "BackSocket",
            "DustSocket_L", "DustSocket_R",
        };

        private GameObject _prefab;

        [OneTimeSetUp]
        public void LoadPrefab()
        {
            R1ModelBuilder.Build();
            _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(_prefab, $"Missing generated R1 rover prefab at '{PrefabPath}'.");
        }

        [Test]
        public void Hierarchy_ContainsAllContractMeshAndEmptyPaths()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath), $"Missing source FBX at '{FbxPath}'.");
            Transform root = _prefab.transform;

            foreach (string path in RequiredMeshPaths)
            {
                Transform node = root.Find(path);
                Assert.IsNotNull(node, $"Missing required mesh node '{path}'.");
                Assert.IsNotNull(node.GetComponent<MeshRenderer>(), $"Node '{path}' must have its own MeshRenderer.");
                Assert.IsNotNull(node.GetComponent<MeshFilter>()?.sharedMesh, $"Node '{path}' must have a MeshFilter mesh.");
            }

            for (int i = 0; i < 8; i++)
            {
                string lampPath = $"Body/BatteryLamp_{i}";
                string glassPath = $"{lampPath}/BatteryGlass_{i}";
                Transform lamp = root.Find(lampPath);
                Transform glass = root.Find(glassPath);
                Assert.IsNotNull(lamp, $"Missing '{lampPath}'.");
                Assert.IsNotNull(glass, $"Missing '{glassPath}'.");
                Assert.IsNotNull(glass.GetComponent<MeshRenderer>(), $"'{glassPath}' must have its own MeshRenderer.");

                string cratePath = $"Body/CargoHold/HatchWindow/HatchCrate_{i}";
                Transform crate = root.Find(cratePath);
                Assert.IsNotNull(crate, $"Missing '{cratePath}'.");
                Assert.IsNotNull(crate.GetComponent<MeshRenderer>(), $"'{cratePath}' must have its own MeshRenderer.");
            }

            foreach (string path in RequiredEmptyPaths)
            {
                Transform node = root.Find(path);
                Assert.IsNotNull(node, $"Missing required socket/origin node '{path}'.");
                Assert.IsNull(node.GetComponent<Renderer>(), $"Socket/origin node '{path}' must be an empty transform.");
            }

            Assert.IsNull(root.Find("Wheel_ML"), "Legacy middle wheel Wheel_ML must not exist on 4-wheel v3 rig.");
            Assert.IsNull(root.Find("Wheel_MR"), "Legacy middle wheel Wheel_MR must not exist on 4-wheel v3 rig.");
            Assert.IsNull(root.Find("Bogie_L"), "Legacy Bogie_L must not exist on 4-wheel v3 rig.");
            Assert.IsNull(root.Find("Bogie_R"), "Legacy Bogie_R must not exist on 4-wheel v3 rig.");
        }

        [Test]
        public void Wheels_SitOnTheGround_WithFourWheelIndependentSuspension()
        {
            Transform root = _prefab.transform;
            foreach (string name in new[] { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" })
            {
                Transform wheel = root.Find(name);
                Assert.IsNotNull(wheel, name);
                Assert.AreEqual(0.35f, wheel.localPosition.y, 0.02f, $"{name} hub height");
                Assert.AreEqual(0.61f, Mathf.Abs(wheel.localPosition.x), 0.02f, $"{name} half-track");
                Assert.AreEqual(0.53f, Mathf.Abs(wheel.localPosition.z), 0.02f, $"{name} half-wheelbase");
                Assert.AreEqual(name.EndsWith("L"), wheel.localPosition.x < 0f, $"{name} side");
                Assert.AreEqual(name.Contains("F"), wheel.localPosition.z > 0f, $"{name} row");
            }
        }

        [Test]
        public void Screen_IsDedicatedSingleSlotMeshRenderer_UsingR1ScreenPlaceholder()
        {
            Transform screen = _prefab.transform.Find("Neck/NeckLift/Head/Screen");
            Assert.IsNotNull(screen);
            var renderer = screen.GetComponent<MeshRenderer>();
            Assert.IsNotNull(renderer);
            Assert.AreEqual(1, renderer.sharedMaterials.Length, "Screen must have exactly one material slot.");
            Assert.AreEqual(R1MaterialRecipes.Get(R1MaterialSlot.Screen).MaterialAssetName,
                renderer.sharedMaterials[0].name);
        }

        [Test]
        public void Model_FitsTheContractSize_AndTriangleBudget()
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = -min;
            int triangles = 0;

            foreach (MeshFilter filter in _prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                Assert.IsNotNull(mesh, filter.name);
                triangles += mesh.triangles.Length / 3;
                Matrix4x4 localToRoot = _prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (Vector3 v in mesh.vertices)
                {
                    Vector3 p = localToRoot.MultiplyPoint3x4(v);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
            }

            Vector3 size = max - min;
            Assert.AreEqual(0f, min.y, 0.02f, "wheels touch the ground at y = 0");
            Assert.That(size.x, Is.InRange(1.2f, 1.68f), "width");
            Assert.That(size.z, Is.InRange(1.4f, 2.15f), "length");
            Assert.That(max.y, Is.InRange(1.3f, 1.78f), "height");
            Assert.That(triangles, Is.InRange(10000, 80000), "R1 hero triangle budget (<= 80,000)");
        }

        [Test]
        public void SubPrefabs_HoverCoilsScanFanAndFocusLens_AreWellFormed()
        {
            string lampMatName = R1MaterialRecipes.Get(R1MaterialSlot.LampAmber).MaterialAssetName;

            GameObject lens = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Generated/Art/R1/Models/Rover/FocusLens.prefab");
            Assert.IsNotNull(lens, "FocusLens.prefab");
            Assert.IsNotNull(lens.GetComponent<MeshFilter>()?.sharedMesh, "FocusLens root mesh");
            Transform glass = lens.transform.Find(RoverModelBuilder.FocusLensGlassName);
            Assert.IsNotNull(glass, "FocusLens/Glass");
            Assert.IsNotNull(glass.GetComponent<MeshFilter>()?.sharedMesh, "FocusLens/Glass mesh");
            Assert.AreEqual(lampMatName, glass.GetComponent<MeshRenderer>()?.sharedMaterial?.name);

            GameObject fan = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Generated/Art/R1/Models/Rover/ScanFan.prefab");
            Assert.IsNotNull(fan, "ScanFan.prefab");
            Assert.IsNotNull(fan.GetComponent<MeshFilter>()?.sharedMesh, "ScanFan mesh");
            CollectionAssert.Contains(
                fan.GetComponent<MeshRenderer>()?.sharedMaterials.Select(m => m != null ? m.name : string.Empty),
                lampMatName,
                "ScanFan includes R1_LampAmber");

            GameObject coils = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Generated/Art/R1/Models/Rover/HoverCoils.prefab");
            Assert.IsNotNull(coils, "HoverCoils.prefab");
            Assert.IsNotNull(coils.GetComponent<MeshFilter>()?.sharedMesh, "HoverCoils root mesh");
        }
    }
}
