using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// Verifies the R1 stylized-realism foundation: <c>MoonProject/R1/Lit</c> shader compilation and property contract,
    /// the 11 library materials and trim-sheet/per-slot textures, and <see cref="R1ModelPostprocessor"/> FBX import
    /// settings and Blender material slot mapping.
    /// </summary>
    public sealed class R1MaterialSystemTests
    {
        private const string TempFolderName = "__R1ModelPostprocessorTest__";
        private readonly List<string> _createdFolders = new List<string>();

        private static readonly string[] ExpectedShaderProperties =
        {
            "_BaseMap",
            "_BaseColor",
            "_BumpMap",
            "_BumpScale",
            "_MaskMap",
            "_MetallicScale",
            "_SmoothnessScale",
            "_SmoothnessMin",
            "_SmoothnessMax",
            "_OcclusionStrength",
            "_EdgeWearColor",
            "_EdgeWearAmount",
            "_EdgeWearThreshold",
            "_EdgeWearSharpness",
            "_EdgeWearMetallic",
            "_EdgeWearSmoothness",
            "_DustColor",
            "_DustAmount",
            "_DustSharpness",
            "_DustUpThreshold",
            "_DustOcclusionWeight",
            "_DustSmoothness",
            "_ColorVarScale",
            "_ColorVarAmount",
            "_EmissionMap",
            "_EmissionColor",
            "_EmissionStrength",
            "_BloomSafeMax",
            "_HotCoreThreshold",
            "_HotCorePeak"
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            R1MaterialLibrary.EnsureInMemoryBindings();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdFolders.Count - 1; i >= 0; i--)
            {
                AssetDatabase.DeleteAsset(_createdFolders[i]);
            }

            foreach (string folder in _createdFolders)
            {
                Assert.IsFalse(Directory.Exists(folder), $"{folder} was not cleaned up.");
                Assert.IsFalse(File.Exists(folder + ".meta"), $"{folder}.meta was not cleaned up.");
            }

            _createdFolders.Clear();
        }

        [Test]
        public void R1LitShader_CompilesWithoutErrors_AndExposesExpectedProperties()
        {
            Shader shader = R1MaterialLibrary.LoadShader();
            Assert.IsNotNull(shader, "MoonProject/R1/Lit shader must exist.");
            Assert.AreEqual(R1MaterialCatalog.ShaderName, shader.name);
            Assert.IsTrue(shader.isSupported, "MoonProject/R1/Lit shader must be supported on the active target.");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "MoonProject/R1/Lit shader must compile without errors.");

            var mat = new Material(shader);
            try
            {
                foreach (string prop in ExpectedShaderProperties)
                {
                    Assert.IsTrue(mat.HasProperty(prop), $"R1Lit shader is missing expected property '{prop}'.");
                }
            }
            finally
            {
                Object.DestroyImmediate(mat);
            }
        }

        [Test]
        public void MaterialLibrary_AllElevenMaterialsExist_AndHaveTexturesAssigned()
        {
            IReadOnlyList<R1MaterialSpec> specs = R1MaterialCatalog.All;
            Assert.AreEqual(11, specs.Count, "R1MaterialCatalog must define all 11 fixed Blender material slots.");

            for (int i = 0; i < specs.Count; i++)
            {
                R1MaterialSpec spec = specs[i];
                Assert.IsFalse(string.IsNullOrWhiteSpace(spec.IntendedUse),
                    $"Slot '{spec.SlotName}' must document its one-line intended use.");

                string matPath = R1MaterialCatalog.GetMaterialPath(spec);
                var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                Assert.IsNotNull(material, $"Material asset missing at '{matPath}' for slot '{spec.SlotName}'.");
                Assert.AreEqual(R1MaterialCatalog.ShaderName, material.shader.name,
                    $"Material '{spec.MaterialAssetName}' must use '{R1MaterialCatalog.ShaderName}'.");
                Assert.IsTrue(material.enableInstancing,
                    $"Material '{spec.MaterialAssetName}' must have GPU instancing enabled.");

                Assert.IsNotNull(material.GetTexture("_BaseMap"),
                    $"Material '{spec.MaterialAssetName}' is missing _BaseMap.");
                Assert.IsNotNull(material.GetTexture("_BumpMap"),
                    $"Material '{spec.MaterialAssetName}' is missing _BumpMap.");
                Assert.IsNotNull(material.GetTexture("_MaskMap"),
                    $"Material '{spec.MaterialAssetName}' is missing _MaskMap.");

                if (spec.HasEmission)
                {
                    Assert.IsNotNull(material.GetTexture("_EmissionMap"),
                        $"Emissive material '{spec.MaterialAssetName}' is missing _EmissionMap.");
                    Assert.Less(material.GetFloat("_BloomSafeMax"), Palette.BloomThreshold,
                        "Bloom-safe body ceiling must stay below Palette.BloomThreshold.");
                    Assert.Greater(material.GetFloat("_HotCorePeak"), Palette.BloomThreshold,
                        "Hot core HDR peak must exceed Palette.BloomThreshold.");
                }
            }

            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialCatalog.TrimSheetAlbedoPath));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialCatalog.TrimSheetNormalPath));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialCatalog.TrimSheetMaskPath));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialCatalog.TrimSheetEmissionPath));

            var normalImporter = (TextureImporter)AssetImporter.GetAtPath(R1MaterialCatalog.TrimSheetNormalPath);
            var maskImporter = (TextureImporter)AssetImporter.GetAtPath(R1MaterialCatalog.TrimSheetMaskPath);
            var albedoImporter = (TextureImporter)AssetImporter.GetAtPath(R1MaterialCatalog.TrimSheetAlbedoPath);
            Assert.AreEqual(TextureImporterType.NormalMap, normalImporter.textureType);
            Assert.IsFalse(maskImporter.sRGBTexture, "Mask map must be imported as linear (non-sRGB).");
            Assert.IsTrue(albedoImporter.sRGBTexture, "Albedo map must be imported as sRGB.");
        }

        [Test]
        public void ModelPostprocessor_MapsAllKnownSlots_AndEnforcesFbxImportSettings()
        {
            IReadOnlyList<R1MaterialSpec> specs = R1MaterialCatalog.All;
            for (int i = 0; i < specs.Count; i++)
            {
                R1MaterialSpec spec = specs[i];
                bool ok = R1ModelPostprocessor.TryResolveSlotMaterial(
                    spec.SlotName,
                    "Assets/_Project/Art/R1/Models/UnitTest.fbx",
                    out Material resolved);
                Assert.IsTrue(ok, $"Expected known slot '{spec.SlotName}' to resolve.");
                Assert.IsNotNull(resolved);
                Assert.AreEqual(spec.MaterialAssetName, resolved.name);
            }

            string fbxPath = R1MaterialCatalog.ModelsFolder + "/" + TempFolderName + "/test_valid.fbx";
            byte[] fbxBytes = R1ModelPostprocessor.CreateMinimalAsciiFbxBytes(
                R1MaterialCatalog.CreamEnamelSlot,
                R1MaterialCatalog.ScreenSlot);
            ImportTempFile(fbxPath, fbxBytes);

            var importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            Assert.IsNotNull(importer);
            Assert.AreEqual(1f, importer.globalScale, 1e-5f);
            Assert.IsTrue(importer.useFileUnits);
            Assert.IsTrue(importer.bakeAxisConversion);
            Assert.IsFalse(importer.isReadable);
            Assert.AreEqual(ModelImporterNormals.Import, importer.importNormals);
            Assert.AreEqual(ModelImporterTangents.CalculateMikk, importer.importTangents);
            Assert.AreEqual(ModelImporterMaterialImportMode.None, importer.materialImportMode);

            var importedModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            Assert.IsNotNull(importedModel);
            var renderer = importedModel.GetComponentInChildren<Renderer>();
            Assert.IsNotNull(renderer);
            Assert.GreaterOrEqual(renderer.sharedMaterials.Length, 1);
            Assert.AreEqual(R1MaterialCatalog.CreamEnamelSlot, renderer.sharedMaterials[0].name);
            if (renderer.sharedMaterials.Length > 1)
            {
                Assert.AreEqual(R1MaterialCatalog.ScreenPlaceholderMaterialName, renderer.sharedMaterials[1].name);
            }
        }

        [Test]
        public void ModelPostprocessor_RejectsUnknownSlot_WithLogErrorContainingModelPath()
        {
            string fbxPath = R1MaterialCatalog.ModelsFolder + "/" + TempFolderName + "/test_invalid.fbx";
            LogAssert.Expect(LogType.Error, new Regex(@"test_invalid\.fbx.*R1_UnknownBogusSlot"));

            byte[] fbxBytes = R1ModelPostprocessor.CreateMinimalAsciiFbxBytes("R1_UnknownBogusSlot");
            ImportTempFile(fbxPath, fbxBytes);
        }

        private void ImportTempFile(string assetPath, byte[] bytes)
        {
            string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            EnsureFolderTracked(folder);
            File.WriteAllBytes(assetPath, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private void EnsureFolderTracked(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolderTracked(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            _createdFolders.Add(folder);
        }
    }
}
