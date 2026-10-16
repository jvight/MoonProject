using System;
using System.Collections.Generic;
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
    /// the 12 generated library materials and textures, pure slot mapping, <see cref="R1ModelPostprocessor"/> validation
    /// of slot names and vertex colours, and <see cref="R1ModelBuilder"/> material replacement on a code-constructed
    /// mesh and renderer.
    /// </summary>
    public sealed class R1MaterialSystemTests
    {
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

        private static readonly string[] ExpectedCrtFaceShaderProperties =
        {
            "_ScreenBaseColor",
            "_ScreenEdgeColor",
            "_PhosphorColor",
            "_PhosphorGlowColor",
            "_HotCoreColor",
            "_BloomSafeMax",
            "_HotCorePeak",
            "_Smoothness",
            "_Curvature",
            "_ScanlineCount",
            "_ScanlineDepth",
            "_GlowRadius",
            "_TrailAlpha",
            "_EyeLeftRect",
            "_EyeLeftShape",
            "_EyeLeftMode",
            "_EyeRightRect",
            "_EyeRightShape",
            "_EyeRightMode",
            "_GhostLeftRect",
            "_GhostLeftShape",
            "_GhostLeftMode",
            "_GhostRightRect",
            "_GhostRightShape",
            "_GhostRightMode",
            "_FaceSymbol",
            "_FaceCrtState",
            "_FacePhases"
        };

        [Test]
        public void R1LitShader_CompilesWithoutErrors_AndExposesExpectedProperties()
        {
            Shader shader = R1MaterialLibrary.LoadShader();
            Assert.IsNotNull(shader, "MoonProject/R1/Lit shader must exist.");
            Assert.AreEqual(R1MaterialRecipes.ShaderName, shader.name);
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
                UnityEngine.Object.DestroyImmediate(mat);
            }
        }

        [Test]
        public void R1CrtFaceShader_CompilesWithoutErrors_AndExposesExpectedProperties()
        {
            Shader shader = R1MaterialLibrary.LoadCrtFaceShader();
            Assert.IsNotNull(shader, "MoonProject/R1/CrtFace shader must exist.");
            Assert.AreEqual(R1MaterialRecipes.CrtFaceShaderName, shader.name);
            Assert.IsTrue(shader.isSupported, "MoonProject/R1/CrtFace shader must be supported on the active target.");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "MoonProject/R1/CrtFace shader must compile without errors.");

            var mat = new Material(shader);
            try
            {
                foreach (string prop in ExpectedCrtFaceShaderProperties)
                {
                    Assert.IsTrue(mat.HasProperty(prop), $"R1CrtFace shader is missing expected property '{prop}'.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mat);
            }
        }

        [Test]
        public void MaterialLibrary_AllTwelveMaterialsExist_AndHaveTexturesAssigned()
        {
            IReadOnlyList<R1MaterialSpec> specs = R1MaterialRecipes.All;
            Assert.AreEqual(15, specs.Count, "R1MaterialRecipes must define all 15 fixed Blender material slots.");

            for (int i = 0; i < specs.Count; i++)
            {
                R1MaterialSpec spec = specs[i];
                Assert.IsFalse(string.IsNullOrWhiteSpace(spec.IntendedUse),
                    $"Slot '{spec.SlotName}' must document its one-line intended use.");

                string matPath = R1MaterialRecipes.GetMaterialPath(spec);
                StringAssert.StartsWith("Assets/_Project/Generated/Art/R1/Materials/", matPath);

                Material material = R1MaterialLibrary.LoadMaterial(spec.Slot);
                Assert.IsNotNull(material, $"Generated material asset missing at '{matPath}' for slot '{spec.SlotName}'.");
                Assert.IsTrue(material.enableInstancing,
                    $"Material '{spec.MaterialAssetName}' must have GPU instancing enabled.");

                if (spec.Slot == R1MaterialSlot.Screen)
                {
                    Assert.AreEqual(R1MaterialRecipes.CrtFaceMaterialName, spec.MaterialAssetName);
                    Assert.AreEqual(R1MaterialRecipes.CrtFaceShaderName, material.shader.name,
                        $"Material '{spec.MaterialAssetName}' must use '{R1MaterialRecipes.CrtFaceShaderName}'.");
                    Assert.Less(material.GetFloat("_BloomSafeMax"), Palette.BloomThreshold);
                    Assert.Greater(material.GetFloat("_HotCorePeak"), Palette.BloomThreshold);
                    continue;
                }

                Assert.AreEqual(R1MaterialRecipes.ShaderName, material.shader.name,
                    $"Material '{spec.MaterialAssetName}' must use '{R1MaterialRecipes.ShaderName}'.");

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

            R1MaterialSpec signageSpec = R1MaterialRecipes.Get(R1MaterialSlot.Signage);
            Assert.AreEqual(R1MaterialRecipes.SignageSlot, signageSpec.SlotName);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialRecipes.GetAlbedoTexturePath(signageSpec)));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialRecipes.GetNormalTexturePath(signageSpec)));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialRecipes.GetMaskTexturePath(signageSpec)));

            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialRecipes.TrimSheetAlbedoPath));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialRecipes.TrimSheetNormalPath));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialRecipes.TrimSheetMaskPath));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialRecipes.TrimSheetEmissionPath));

            var normalImporter = (TextureImporter)AssetImporter.GetAtPath(R1MaterialRecipes.TrimSheetNormalPath);
            var maskImporter = (TextureImporter)AssetImporter.GetAtPath(R1MaterialRecipes.TrimSheetMaskPath);
            var albedoImporter = (TextureImporter)AssetImporter.GetAtPath(R1MaterialRecipes.TrimSheetAlbedoPath);
            Assert.AreEqual(TextureImporterType.NormalMap, normalImporter.textureType);
            Assert.IsFalse(maskImporter.sRGBTexture, "Mask map must be imported as linear (non-sRGB).");
            Assert.IsTrue(albedoImporter.sRGBTexture, "Albedo map must be imported as sRGB.");
        }

        [Test]
        public void MaterialLibrary_ThrowsInvalidOperationException_WhenShaderOrTextureMissing()
        {
            R1MaterialSpec spec = R1MaterialRecipes.Get(R1MaterialSlot.CreamEnamel);
            Shader shader = R1MaterialLibrary.LoadShader();

            var nullShaderEx = Assert.Throws<InvalidOperationException>(() =>
                R1MaterialLibrary.CreateMaterial(null, spec));
            StringAssert.Contains("R1Lit shader", nullShaderEx.Message);

            var missingTexEx = Assert.Throws<InvalidOperationException>(() =>
                R1MaterialLibrary.CreateMaterial(shader, spec, _ => null));
            StringAssert.Contains(R1MaterialRecipes.GetAlbedoTexturePath(spec), missingTexEx.Message);
        }

        [Test]
        public void SlotMapping_MapsAllTwelveKnownSlots_AndRejectsUnknownSlots()
        {
            IReadOnlyList<R1MaterialSpec> specs = R1MaterialRecipes.All;
            for (int i = 0; i < specs.Count; i++)
            {
                R1MaterialSpec spec = specs[i];
                Assert.IsTrue(R1MaterialRecipes.TryMapSlotName(spec.SlotName, out R1MaterialSlot mapped));
                Assert.AreEqual(spec.Slot, mapped);

                Assert.IsTrue(R1MaterialRecipes.TryMapSlotName(spec.SlotName + " (Instance)", out R1MaterialSlot mappedInstance));
                Assert.AreEqual(spec.Slot, mappedInstance);
            }

            Assert.IsFalse(R1MaterialRecipes.TryMapSlotName("R1_UnknownBogusSlot", out _));
            Assert.IsFalse(R1MaterialRecipes.TryMapSlotName(string.Empty, out _));

            var ex = Assert.Throws<InvalidOperationException>(() =>
                R1ModelBuilder.MapSlotOrThrow("R1_UnknownBogusSlot", "Assets/_Project/Art/R1/Models/UnitTest.fbx"));
            StringAssert.Contains("Assets/_Project/Art/R1/Models/UnitTest.fbx", ex.Message);
            StringAssert.Contains("R1_UnknownBogusSlot", ex.Message);
        }

        [Test]
        public void ModelPostprocessor_ValidatesSlotNames_AndRequiresVertexColors()
        {
            const string ModelPath = "Assets/_Project/Art/R1/Models/UnitTest.fbx";
            Assert.IsTrue(R1ModelPostprocessor.ValidateSlotName(R1MaterialRecipes.SolarCellSlot, ModelPath));

            LogAssert.Expect(LogType.Error, new Regex(@"UnitTest\.fbx.*R1_BogusSlot"));
            Assert.IsFalse(R1ModelPostprocessor.ValidateSlotName("R1_BogusSlot", ModelPath));

            Mesh validMesh = R1MaterialLibrary.CreateBevelledCubeMesh(0.5f, 0.06f);
            var validRoot = new GameObject("ValidModelRoot");
            validRoot.AddComponent<MeshFilter>().sharedMesh = validMesh;

            var invalidMesh = new Mesh { name = "UncolouredMesh" };
            invalidMesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            invalidMesh.triangles = new[] { 0, 2, 1 };
            var invalidRoot = new GameObject("InvalidModelRoot");
            invalidRoot.AddComponent<MeshFilter>().sharedMesh = invalidMesh;

            try
            {
                Assert.IsTrue(R1ModelPostprocessor.ValidateVertexColors(validRoot, ModelPath));

                LogAssert.Expect(LogType.Error, new Regex(@"UnitTest\.fbx"));
                Assert.IsFalse(R1ModelPostprocessor.ValidateVertexColors(invalidRoot, ModelPath));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(validRoot);
                UnityEngine.Object.DestroyImmediate(validMesh);
                UnityEngine.Object.DestroyImmediate(invalidRoot);
                UnityEngine.Object.DestroyImmediate(invalidMesh);
            }
        }

        [Test]
        public void ModelBuilder_ReplacesRendererMaterialsWithLibraryMaterials_AndThrowsOnUnknownSlot()
        {
            const string ModelPath = "Assets/_Project/Art/R1/Models/Rover/TestPart.fbx";
            Assert.AreEqual(
                "Assets/_Project/Generated/Art/R1/Models/Rover/TestPart.prefab",
                R1ModelBuilder.GetGeneratedPrefabPath(ModelPath));

            Shader shader = R1MaterialLibrary.LoadShader();
            Mesh mesh = R1MaterialLibrary.CreateBevelledCubeMesh(0.5f, 0.05f);
            var root = new GameObject("CodeConstructedModel");
            var matCreamPlaceholder = new Material(shader) { name = R1MaterialRecipes.CreamEnamelSlot };
            var matSolarPlaceholder = new Material(shader) { name = R1MaterialRecipes.SolarCellSlot };
            var matScreenSlot = new Material(shader) { name = R1MaterialRecipes.ScreenSlot + " (Instance)" };
            var matBogus = new Material(shader) { name = "R1_InvalidSlotName" };

            try
            {
                var filter = root.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = new[] { matCreamPlaceholder, matSolarPlaceholder, matScreenSlot };

                R1ModelBuilder.AssignLibraryMaterials(root, ModelPath);

                Material[] assigned = renderer.sharedMaterials;
                Assert.AreEqual(3, assigned.Length);
                Assert.AreSame(R1MaterialLibrary.LoadMaterial(R1MaterialSlot.CreamEnamel), assigned[0]);
                Assert.AreSame(R1MaterialLibrary.LoadMaterial(R1MaterialSlot.SolarCell), assigned[1]);
                Assert.AreSame(R1MaterialLibrary.LoadMaterial(R1MaterialSlot.Screen), assigned[2]);
                Assert.AreEqual(R1MaterialRecipes.CrtFaceMaterialName, assigned[2].name);

                renderer.sharedMaterials = new[] { matBogus };
                var ex = Assert.Throws<InvalidOperationException>(() =>
                    R1ModelBuilder.AssignLibraryMaterials(root, ModelPath));
                StringAssert.Contains(ModelPath, ex.Message);
                StringAssert.Contains("R1_InvalidSlotName", ex.Message);

                Assert.DoesNotThrow(() => R1ModelBuilder.Build());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(matCreamPlaceholder);
                UnityEngine.Object.DestroyImmediate(matSolarPlaceholder);
                UnityEngine.Object.DestroyImmediate(matScreenSlot);
                UnityEngine.Object.DestroyImmediate(matBogus);
            }
        }

        [Test]
        public void CrtFaceState_AllFeelingsBlinksBootAndLowPower_PassEditModeContract()
        {
            var suite = new MoonProject.Rover.Face.Tests.FaceStateTests();
            suite.SetUp();
            try
            {
                suite.Easing_ConvergesTowardEachFeeling_AndMatchesPrototypeSpecs();
                suite.Blinks_NeverHappenWhileAsleep();
                suite.Blink_ClosesAndReopensWithinItsLength();
                suite.BootSequence_ShowsHorizontalLineFirst_ThenOpensEyesVertically();
                suite.CuriousFeeling_ProducesAsymmetricLeftAndRightEyes();
                suite.LowPowerAmount_ClampsToZeroOne_AndDimsBrightness();
                suite.PhosphorGhostTrail_LagsBehindEyeMovement_AndConverges();
                suite.FaceDisplay_PushesPropertyBlockVectorsToRenderer();
            }
            finally
            {
                suite.TearDown();
            }
        }
    }
}
