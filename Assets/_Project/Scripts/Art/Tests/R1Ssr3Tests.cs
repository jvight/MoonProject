using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// Verifies the R1 SSR-3 elder rover family models and prefabs:
    /// <list type="bullet">
    ///   <item><c>SSR3_Chassis</c> (<c>tools/art/qa/contracts/ssr3_chassis.json</c>): ~1.40 m L × 0.95 m W × 0.75 m H to deck,
    ///   4 wheels (<c>Wheel_FL</c>, <c>Wheel_FR</c>, <c>Wheel_RL</c>, <c>Wheel_RR</c>), single off-center <c>Headlamp</c> with
    ///   dark <c>R1_DarkGlass</c> lens, <c>Topper_Socket</c>, <c>Unit_Number</c>, no <c>R1_LampAmber</c>, and baked vertex colours.</item>
    /// </list>
    /// </summary>
    public sealed class R1Ssr3Tests
    {
        private const string ContractsDir = "tools/art/qa/contracts";
        public const string Ssr3ChassisFbxPath = "Assets/_Project/Art/R1/Models/Rover/SSR3_Chassis.fbx";
        public const string Ssr3ChassisPrefabPath = "Assets/_Project/Generated/Art/R1/Models/Rover/SSR3_Chassis.prefab";

        private static readonly (string ContractFile, string FbxRelativePath)[] Ssr3Contracts =
        {
            ("ssr3_chassis.json", "Rover/SSR3_Chassis.fbx"),
        };

        [Serializable]
        private struct ContractSpec
        {
            public string model;
            public BoundsSpec bounds;
        }

        [Serializable]
        private struct BoundsSpec
        {
            public float[] size;
            public float tolerance;
            public float[] min_size;
            public float[] max_size;
        }

        [OneTimeSetUp]
        public void EnsureBuilt()
        {
            R1MaterialLibrary.BuildMaterials();
            R1ModelBuilder.Build();
        }

        private static IEnumerable<TestCaseData> ContractCases()
        {
            for (int i = 0; i < Ssr3Contracts.Length; i++)
            {
                (string contractFile, string fbxRelativePath) = Ssr3Contracts[i];
                yield return new TestCaseData(contractFile, fbxRelativePath)
                    .SetName($"Ssr3_{Path.GetFileNameWithoutExtension(contractFile)}_MatchesContractBounds");
            }
        }

        [TestCaseSource(nameof(ContractCases))]
        public void Ssr3Prefab_RendererAndMeshBounds_MatchContract(string contractFile, string fbxRelativePath)
        {
            string contractPath = Path.Combine(ContractsDir, contractFile);
            Assert.IsTrue(File.Exists(contractPath), $"Missing contract JSON at '{contractPath}'.");
            ContractSpec contract = JsonUtility.FromJson<ContractSpec>(File.ReadAllText(contractPath));
            Assert.IsNotNull(contract.bounds.size, $"Contract '{contractFile}' is missing bounds.size.");
            Assert.AreEqual(3, contract.bounds.size.Length, $"Contract '{contractFile}' bounds.size must have 3 axes.");

            string fbxPath = R1MaterialRecipes.ModelsFolder + "/" + fbxRelativePath;
            string prefabPath = R1ModelBuilder.GetGeneratedPrefabPath(fbxPath);
            GameObject prefab = MeshChecks.LoadGeneratedPrefab(prefabPath);

            Bounds meshBounds = MeshChecks.PrefabBounds(prefab, prefab.transform.localToWorldMatrix);
            Vector3 actualMeshSize = new Vector3(meshBounds.size.x, meshBounds.size.z, meshBounds.size.y);
            AssertAxisBounds(contractFile, "mesh", actualMeshSize, contract.bounds);

            Bounds rendererBounds = MeasureInstantiatedRendererBounds(prefab);
            Vector3 actualRendererSize = new Vector3(rendererBounds.size.x, rendererBounds.size.z, rendererBounds.size.y);
            AssertAxisBounds(contractFile, "renderer", actualRendererSize, contract.bounds);
        }

        [Test]
        public void Ssr3Chassis_ExposesRequiredRigSockets_UsesDarkHeadlamp_AndHasBakedWeathering()
        {
            GameObject chassisPrefab = MeshChecks.LoadGeneratedPrefab(Ssr3ChassisPrefabPath);
            Assert.IsNotNull(chassisPrefab.transform.Find("Body"), "SSR3_Chassis missing Body.");
            Assert.IsNotNull(chassisPrefab.transform.Find("Headlamp"), "SSR3_Chassis missing Headlamp.");
            Transform headlampGlass = chassisPrefab.transform.Find("Headlamp/HeadlampGlass");
            Assert.IsNotNull(headlampGlass, "SSR3_Chassis missing Headlamp/HeadlampGlass.");
            Assert.IsNotNull(chassisPrefab.transform.Find("Wheel_FL"), "SSR3_Chassis missing Wheel_FL.");
            Assert.IsNotNull(chassisPrefab.transform.Find("Wheel_FR"), "SSR3_Chassis missing Wheel_FR.");
            Assert.IsNotNull(chassisPrefab.transform.Find("Wheel_RL"), "SSR3_Chassis missing Wheel_RL.");
            Assert.IsNotNull(chassisPrefab.transform.Find("Wheel_RR"), "SSR3_Chassis missing Wheel_RR.");
            Transform topperSocket = chassisPrefab.transform.Find("Topper_Socket");
            Assert.IsNotNull(topperSocket, "SSR3_Chassis missing Topper_Socket.");
            Vector3 topperWorldPos = chassisPrefab.transform.TransformPoint(topperSocket.localPosition);
            Assert.AreEqual(0.69f, topperWorldPos.y, 0.02f, "Topper_Socket should sit on the dorsal deck (~0.69 m Y).");
            Assert.IsNotNull(chassisPrefab.transform.Find("Unit_Number"), "SSR3_Chassis missing Unit_Number.");

            Renderer glassRenderer = headlampGlass.GetComponent<Renderer>();
            Assert.IsNotNull(glassRenderer, "HeadlampGlass must have a Renderer.");
            Assert.AreEqual(R1MaterialRecipes.DarkGlassSlot, glassRenderer.sharedMaterial.name,
                "SSR3_Chassis HeadlampGlass must use dark R1_DarkGlass.");

            Renderer[] renderers = chassisPrefab.GetComponentsInChildren<Renderer>(true);
            Assert.GreaterOrEqual(renderers.Length, 7, "SSR3_Chassis must have at least 7 mesh renderers.");
            foreach (Renderer r in renderers)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    Assert.IsNotNull(m, $"Null material on '{r.name}'.");
                    Assert.AreNotEqual(R1MaterialRecipes.LampAmberSlot, m.name,
                        $"SSR3_Chassis renderer '{r.name}' must not use active R1_LampAmber.");
                }
            }

            MeshFilter[] filters = chassisPrefab.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter mf in filters)
            {
                Mesh mesh = mf.sharedMesh;
                Assert.IsNotNull(mesh, $"Missing sharedMesh on '{mf.name}'.");
                Color[] colors = mesh.colors;
                Assert.IsNotNull(colors, $"Mesh '{mesh.name}' is missing vertex colors.");
                Assert.AreEqual(mesh.vertexCount, colors.Length, $"Mesh '{mesh.name}' vertex color count mismatch.");
            }
        }

        private static Bounds MeasureInstantiatedRendererBounds(GameObject prefab)
        {
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                Assert.IsNotEmpty(renderers, $"Prefab '{prefab.name}' has no Renderer components.");
                Bounds combined = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combined.Encapsulate(renderers[i].bounds);
                }
                return combined;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void AssertAxisBounds(string contractFile, string kind, Vector3 actual, BoundsSpec expected)
        {
            string[] axisNames = { "width (X)", "length (Z)", "height (Y)" };
            for (int axis = 0; axis < 3; axis++)
            {
                float actualVal = actual[axis];
                float targetVal = expected.size[axis];
                float minVal = expected.min_size != null && expected.min_size.Length == 3
                    ? expected.min_size[axis]
                    : targetVal - expected.tolerance;
                float maxVal = expected.max_size != null && expected.max_size.Length == 3
                    ? expected.max_size[axis]
                    : targetVal + expected.tolerance;

                Assert.GreaterOrEqual(actualVal, minVal,
                    $"[{contractFile}] {kind} {axisNames[axis]} = {actualVal:F3} m is below contract min {minVal:F3} m (target {targetVal:F3} m).");
                Assert.LessOrEqual(actualVal, maxVal,
                    $"[{contractFile}] {kind} {axisNames[axis]} = {actualVal:F3} m exceeds contract max {maxVal:F3} m (target {targetVal:F3} m).");
            }
        }
    }
}
