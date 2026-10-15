using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using MoonProject.Art;
using MoonProject.Art.Editor;
using MoonProject.World;
using MoonProject.World.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MoonProject.R1Lab.Editor
{
    /// <summary>
    /// Builds the R1 stylized-realism look-lab materials, terrain mesh, post-processing volume, and scene
    /// (<c>Assets/_Project/Experiments/R1Lab/R1Lab.unity</c>), captures the four 1920x1080 comparison shots
    /// in both <c>R1Lab.unity</c> and <c>Main.unity</c>, and records 1080p frame-time, triangle, and memory metrics.
    /// </summary>
    public static class R1LabBuilder
    {
        public const string LabRoot = "Assets/_Project/Experiments/R1Lab";
        public const string ShadersDir = LabRoot + "/Shaders";
        public const string TexturesDir = LabRoot + "/Textures";
        public const string ModelsDir = LabRoot + "/Models";
        public const string MaterialsDir = LabRoot + "/Materials";
        public const string ScenePath = LabRoot + "/R1Lab.unity";
        public const string VolumeProfilePath = LabRoot + "/R1VolumeProfile.asset";
        public const string EnvCubemapPath = TexturesDir + "/T_R1_EnvCubemap.asset";
        public const string TerrainMeshPath = ModelsDir + "/R1_TerrainPatchMesh.asset";

        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        private struct PoseSpec
        {
            public string Id;
            public Vector3 CameraOffset;
            public Vector3 LookAtOffset;
            public float FieldOfView;
        }

        private static readonly PoseSpec[] Poses =
        {
            new PoseSpec
            {
                Id = "a_crate_close",
                CameraOffset = new Vector3(0.05f, 0.64f, -2.30f),
                LookAtOffset = new Vector3(0.68f, 0.25f, -1.12f),
                FieldOfView = 40f,
            },
            new PoseSpec
            {
                Id = "b_rocks_close",
                CameraOffset = new Vector3(-3.55f, 1.08f, 0.15f),
                LookAtOffset = new Vector3(-2.02f, 0.56f, 2.25f),
                FieldOfView = 42f,
            },
            new PoseSpec
            {
                Id = "c_wide_shot",
                CameraOffset = new Vector3(-0.35f, 2.25f, -8.60f),
                LookAtOffset = new Vector3(0.55f, 1.25f, 4.20f),
                FieldOfView = 50f,
            },
            new PoseSpec
            {
                Id = "d_07_among_assets",
                CameraOffset = new Vector3(-0.12f, 1.32f, -4.45f),
                LookAtOffset = new Vector3(0.20f, 0.56f, 0.65f),
                FieldOfView = 44f,
            },
        };

        [MenuItem("MoonProject/R1 Lab/Build and Capture All")]
        public static void BuildAndCaptureAll()
        {
            EnsureDirectories();
            ConfigureImporters();

            Cubemap envCubemap = BuildEnvCubemap();
            BuildMaterials();
            Mesh terrainMesh = BuildTerrainMesh();
            VolumeProfile volumeProfile = BuildR1VolumeProfile();

            BuildLabScene(terrainMesh, volumeProfile, envCubemap);

            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "r1");
            Directory.CreateDirectory(outDir);

            var reportBuilder = new StringBuilder();
            reportBuilder.AppendLine("{");

            CaptureAndMeasureLabScene(outDir, reportBuilder);
            CaptureAndMeasureMainScene(outDir, reportBuilder);
            AppendWorldAudit(reportBuilder);

            reportBuilder.AppendLine("}");
            string metricsPath = Path.Combine(outDir, "metrics.json");
            File.WriteAllText(metricsPath, reportBuilder.ToString());
            Debug.Log($"[R1Lab] Wrote metrics to {metricsPath}");

            // Leave R1Lab.unity open and clean
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(MaterialsDir);
            Directory.CreateDirectory(ModelsDir);
            Directory.CreateDirectory(TexturesDir);
            Directory.CreateDirectory(ShadersDir);
            AssetDatabase.Refresh();
        }

        private static void ConfigureImporters()
        {
            string[] texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesDir });
            foreach (string guid in texGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                bool isNormal = path.EndsWith("_Normal.png", StringComparison.OrdinalIgnoreCase);
                bool isMask = path.EndsWith("_Mask.png", StringComparison.OrdinalIgnoreCase);
                bool isBake = path.EndsWith("_Bake.png", StringComparison.OrdinalIgnoreCase);

                bool changed = false;
                TextureImporterType targetType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (importer.textureType != targetType)
                {
                    importer.textureType = targetType;
                    changed = true;
                }

                bool targetSRGB = !(isNormal || isMask || isBake);
                if (importer.sRGBTexture != targetSRGB)
                {
                    importer.sRGBTexture = targetSRGB;
                    changed = true;
                }

                TextureWrapMode targetWrap = isBake ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                if (importer.wrapMode != targetWrap)
                {
                    importer.wrapMode = targetWrap;
                    changed = true;
                }

                if (importer.anisoLevel != 8)
                {
                    importer.anisoLevel = 8;
                    changed = true;
                }

                if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
                {
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }

            string[] modelFiles =
            {
                ModelsDir + "/R1_SupplyCrate.fbx",
                ModelsDir + "/R1_RockCluster.fbx",
                ModelsDir + "/R1_WreckPanel.fbx",
            };

            foreach (string modelPath in modelFiles)
            {
                var modelImporter = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (modelImporter == null)
                {
                    continue;
                }

                bool changed = false;
                if (!modelImporter.isReadable)
                {
                    modelImporter.isReadable = true;
                    changed = true;
                }

                if (modelImporter.importNormals != ModelImporterNormals.Import)
                {
                    modelImporter.importNormals = ModelImporterNormals.Import;
                    changed = true;
                }

                if (modelImporter.importTangents != ModelImporterTangents.CalculateMikk)
                {
                    modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
                    changed = true;
                }

                if (modelImporter.materialImportMode != ModelImporterMaterialImportMode.None)
                {
                    modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
                    changed = true;
                }

                if (!modelImporter.bakeAxisConversion)
                {
                    modelImporter.bakeAxisConversion = true;
                    changed = true;
                }

                if (changed)
                {
                    modelImporter.SaveAndReimport();
                }
            }
        }

        private static Cubemap BuildEnvCubemap()
        {
            const int size = 64;
            var cubemap = new Cubemap(size, TextureFormat.RGBAHalf, true)
            {
                name = "T_R1_EnvCubemap",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
            };

            Vector3 earthDir = new Vector3(-0.48f, 0.36f, 0.80f).normalized;
            var pixels = new Color[size * size];

            CubemapFace[] faces =
            {
                CubemapFace.PositiveX, CubemapFace.NegativeX,
                CubemapFace.PositiveY, CubemapFace.NegativeY,
                CubemapFace.PositiveZ, CubemapFace.NegativeZ,
            };

            foreach (CubemapFace face in faces)
            {
                for (int y = 0; y < size; y++)
                {
                    float v = ((y + 0.5f) / size) * 2f - 1f;
                    for (int x = 0; x < size; x++)
                    {
                        float u = ((x + 0.5f) / size) * 2f - 1f;
                        Vector3 dir = FaceUVToDirection(face, u, v).normalized;

                        // Lunar night environment: cool Earthlight directional sheen + soft horizon bounce + regolith ground
                        Color skyCol = new Color(0.10f, 0.12f, 0.16f, 1f);
                        Color horizonCol = new Color(0.24f, 0.27f, 0.33f, 1f);
                        Color groundCol = new Color(0.12f, 0.13f, 0.15f, 1f);

                        float horizonT = Mathf.Exp(-Mathf.Abs(dir.y) * 4.5f);
                        Color baseEnv = dir.y >= 0f
                            ? Color.Lerp(skyCol, horizonCol, horizonT)
                            : Color.Lerp(groundCol, horizonCol, horizonT * 0.65f);

                        float earthDot = Mathf.Max(0f, Vector3.Dot(dir, earthDir));
                        float earthGlow = Mathf.Pow(earthDot, 6f) * 0.85f + Mathf.Pow(earthDot, 32f) * 1.35f;
                        Color col = baseEnv + new Color(0.52f, 0.58f, 0.72f, 0f) * earthGlow;
                        pixels[y * size + x] = col;
                    }
                }

                cubemap.SetPixels(pixels, face);
            }

            cubemap.Apply(true, false);
            SaveOrReplaceAsset(cubemap, EnvCubemapPath);
            return AssetDatabase.LoadAssetAtPath<Cubemap>(EnvCubemapPath);
        }

        private static Vector3 FaceUVToDirection(CubemapFace face, float u, float v)
        {
            switch (face)
            {
                case CubemapFace.PositiveX: return new Vector3(1f, -v, -u);
                case CubemapFace.NegativeX: return new Vector3(-1f, -v, u);
                case CubemapFace.PositiveY: return new Vector3(u, 1f, v);
                case CubemapFace.NegativeY: return new Vector3(u, -1f, -v);
                case CubemapFace.PositiveZ: return new Vector3(u, -v, 1f);
                default: return new Vector3(-u, -v, -1f);
            }
        }

        private static void BuildMaterials()
        {
            Shader litShader = Shader.Find("MoonProject/R1/Lit");
            Shader terrainShader = Shader.Find("MoonProject/R1/Terrain");
            if (litShader == null || terrainShader == null)
            {
                throw new InvalidOperationException("R1 shaders not found. Check MoonProject/R1/Lit and MoonProject/R1/Terrain.");
            }

            Texture2D trimAlbedo = LoadTex("T_R1_TrimSheet_Albedo.png");
            Texture2D trimNormal = LoadTex("T_R1_TrimSheet_Normal.png");
            Texture2D trimMask = LoadTex("T_R1_TrimSheet_Mask.png");
            Texture2D trimEmissive = LoadTex("T_R1_TrimSheet_Emission.png");

            Texture2D crateBake = LoadTex("T_R1_SupplyCrate_Bake.png");
            Texture2D rockBake = LoadTex("T_R1_RockCluster_Bake.png");
            Texture2D wreckBake = LoadTex("T_R1_WreckPanel_Bake.png");

            Texture2D rubberAlbedo = LoadTex("T_R1_Rubber_Albedo.png");
            Texture2D rubberNormal = LoadTex("T_R1_Rubber_Normal.png");
            Texture2D rubberMask = LoadTex("T_R1_Rubber_Mask.png");

            Texture2D foilAlbedo = LoadTex("T_R1_GoldFoil_Albedo.png");
            Texture2D foilNormal = LoadTex("T_R1_GoldFoil_Normal.png");
            Texture2D foilMask = LoadTex("T_R1_GoldFoil_Mask.png");

            Texture2D rockAlbedo = LoadTex("T_R1_Rock_Albedo.png");
            Texture2D rockNormal = LoadTex("T_R1_Rock_Normal.png");
            Texture2D rockMask = LoadTex("T_R1_Rock_Mask.png");

            Texture2D regAlbedo = LoadTex("T_R1_Regolith_Albedo.png");
            Texture2D regNormal = LoadTex("T_R1_Regolith_Normal.png");
            Texture2D regMask = LoadTex("T_R1_Regolith_Mask.png");

            // 1. M_R1_SupplyCrate
            var matCrate = new Material(litShader) { name = "M_R1_SupplyCrate", enableInstancing = true };
            matCrate.SetTexture("_BaseMap", trimAlbedo);
            matCrate.SetColor("_BaseColor", Color.white);
            matCrate.SetTexture("_BumpMap", trimNormal);
            matCrate.SetFloat("_BumpScale", 1.15f);
            matCrate.SetTexture("_MaskMap", trimMask);
            matCrate.SetFloat("_Metallic", 1.0f);
            matCrate.SetFloat("_Smoothness", 1.0f);
            matCrate.SetFloat("_OcclusionStrength", 0.90f);
            matCrate.SetTexture("_BakeMap", crateBake);
            matCrate.SetFloat("_UseUV1ForBake", 1.0f);
            matCrate.SetColor("_EdgeWearColor", new Color(0.68f, 0.70f, 0.74f, 1f));
            matCrate.SetFloat("_EdgeWearStrength", 0.90f);
            matCrate.SetFloat("_EdgeWearThreshold", 0.54f);
            matCrate.SetFloat("_EdgeWearMetallic", 0.78f);
            matCrate.SetFloat("_EdgeWearSmoothness", 0.64f);
            matCrate.SetColor("_DustColor", new Color(0.58f, 0.59f, 0.62f, 1f));
            matCrate.SetFloat("_DustStrength", 0.56f);
            matCrate.SetFloat("_DustUpThreshold", 0.30f);
            matCrate.SetFloat("_DustCreaseWeight", 0.95f);
            matCrate.SetFloat("_DustGroundHeight", 0.14f);
            matCrate.SetFloat("_DustGroundFade", 0.18f);
            matCrate.SetFloat("_DustGroundStrength", 0.50f);
            matCrate.SetTexture("_EmissionMap", trimEmissive);
            matCrate.SetColor("_EmissionColor", Color.white);
            matCrate.SetFloat("_EmissionStrength", 1.15f);
            SaveOrReplaceAsset(matCrate, MaterialsDir + "/M_R1_SupplyCrate.mat");

            // 2. M_R1_Rubber
            var matRubber = new Material(litShader) { name = "M_R1_Rubber", enableInstancing = true };
            matRubber.SetTexture("_BaseMap", rubberAlbedo);
            matRubber.SetColor("_BaseColor", Color.white);
            matRubber.SetTexture("_BumpMap", rubberNormal);
            matRubber.SetFloat("_BumpScale", 1.25f);
            matRubber.SetTexture("_MaskMap", rubberMask);
            matRubber.SetFloat("_Metallic", 1.0f);
            matRubber.SetFloat("_Smoothness", 1.0f);
            matRubber.SetFloat("_OcclusionStrength", 0.90f);
            matRubber.SetTexture("_BakeMap", crateBake);
            matRubber.SetFloat("_UseUV1ForBake", 1.0f);
            matRubber.SetColor("_EdgeWearColor", new Color(0.36f, 0.37f, 0.41f, 1f));
            matRubber.SetFloat("_EdgeWearStrength", 0.45f);
            matRubber.SetFloat("_EdgeWearThreshold", 0.56f);
            matRubber.SetFloat("_EdgeWearMetallic", 0.05f);
            matRubber.SetFloat("_EdgeWearSmoothness", 0.28f);
            matRubber.SetColor("_DustColor", new Color(0.56f, 0.57f, 0.60f, 1f));
            matRubber.SetFloat("_DustStrength", 0.65f);
            matRubber.SetFloat("_DustGroundHeight", 0.12f);
            matRubber.SetFloat("_DustGroundFade", 0.16f);
            matRubber.SetFloat("_DustGroundStrength", 0.60f);
            SaveOrReplaceAsset(matRubber, MaterialsDir + "/M_R1_Rubber.mat");

            // 3. M_R1_Rock
            var matRock = new Material(litShader) { name = "M_R1_Rock", enableInstancing = true };
            matRock.SetTexture("_BaseMap", rockAlbedo);
            matRock.SetTextureScale("_BaseMap", new Vector2(1.4f, 1.4f));
            matRock.SetColor("_BaseColor", new Color(0.68f, 0.70f, 0.75f, 1f));
            matRock.SetTexture("_BumpMap", rockNormal);
            matRock.SetFloat("_BumpScale", 0.85f);
            matRock.SetTexture("_MaskMap", rockMask);
            matRock.SetFloat("_Metallic", 1.0f);
            matRock.SetFloat("_Smoothness", 1.0f);
            matRock.SetFloat("_OcclusionStrength", 0.92f);
            matRock.SetTexture("_BakeMap", rockBake);
            matRock.SetFloat("_UseUV1ForBake", 1.0f);
            matRock.SetColor("_EdgeWearColor", new Color(0.62f, 0.64f, 0.69f, 1f));
            matRock.SetFloat("_EdgeWearStrength", 0.85f);
            matRock.SetFloat("_EdgeWearThreshold", 0.53f);
            matRock.SetFloat("_EdgeWearMetallic", 0.04f);
            matRock.SetFloat("_EdgeWearSmoothness", 0.34f);
            matRock.SetColor("_DustColor", new Color(0.58f, 0.59f, 0.63f, 1f));
            matRock.SetFloat("_DustStrength", 0.38f);
            matRock.SetFloat("_DustUpThreshold", 0.52f);
            matRock.SetFloat("_DustCreaseWeight", 0.90f);
            matRock.SetFloat("_DustSmoothness", 0.13f);
            matRock.SetFloat("_DustGroundHeight", 0.22f);
            matRock.SetFloat("_DustGroundFade", 0.26f);
            matRock.SetFloat("_DustGroundStrength", 0.65f);
            matRock.SetFloat("_ColorVarScale", 0.55f);
            matRock.SetFloat("_ColorVarStrength", 0.08f);
            SaveOrReplaceAsset(matRock, MaterialsDir + "/M_R1_Rock.mat");

            // 4. M_R1_WreckTrim
            var matWreck = new Material(litShader) { name = "M_R1_WreckTrim", enableInstancing = true };
            matWreck.SetTexture("_BaseMap", trimAlbedo);
            matWreck.SetColor("_BaseColor", Color.white);
            matWreck.SetTexture("_BumpMap", trimNormal);
            matWreck.SetFloat("_BumpScale", 1.25f);
            matWreck.SetTexture("_MaskMap", trimMask);
            matWreck.SetFloat("_Metallic", 1.0f);
            matWreck.SetFloat("_Smoothness", 1.0f);
            matWreck.SetFloat("_OcclusionStrength", 0.92f);
            matWreck.SetTexture("_BakeMap", wreckBake);
            matWreck.SetFloat("_UseUV1ForBake", 1.0f);
            matWreck.SetColor("_EdgeWearColor", new Color(0.70f, 0.72f, 0.76f, 1f));
            matWreck.SetFloat("_EdgeWearStrength", 0.95f);
            matWreck.SetFloat("_EdgeWearThreshold", 0.53f);
            matWreck.SetFloat("_EdgeWearMetallic", 0.82f);
            matWreck.SetFloat("_EdgeWearSmoothness", 0.65f);
            matWreck.SetColor("_DustColor", new Color(0.58f, 0.59f, 0.62f, 1f));
            matWreck.SetFloat("_DustStrength", 0.68f);
            matWreck.SetFloat("_DustUpThreshold", 0.24f);
            matWreck.SetFloat("_DustCreaseWeight", 1.0f);
            matWreck.SetFloat("_DustGroundHeight", 0.30f);
            matWreck.SetFloat("_DustGroundFade", 0.35f);
            matWreck.SetFloat("_DustGroundStrength", 0.78f);
            matWreck.SetTexture("_EmissionMap", trimEmissive);
            matWreck.SetColor("_EmissionColor", Color.white);
            matWreck.SetFloat("_EmissionStrength", 1.32f);
            matWreck.SetFloat("_BloomSafeMax", 1.06f);
            matWreck.SetFloat("_HotCoreThreshold", 0.85f);
            matWreck.SetFloat("_HotCorePeak", 1.44f);
            SaveOrReplaceAsset(matWreck, MaterialsDir + "/M_R1_WreckTrim.mat");

            // 5. M_R1_GoldFoil
            var matFoil = new Material(litShader) { name = "M_R1_GoldFoil", enableInstancing = true };
            matFoil.SetTexture("_BaseMap", foilAlbedo);
            matFoil.SetColor("_BaseColor", new Color(1.05f, 1.02f, 0.96f, 1f));
            matFoil.SetTexture("_BumpMap", foilNormal);
            matFoil.SetFloat("_BumpScale", 1.45f);
            matFoil.SetTexture("_MaskMap", foilMask);
            matFoil.SetFloat("_Metallic", 1.0f);
            matFoil.SetFloat("_Smoothness", 1.02f);
            matFoil.SetFloat("_OcclusionStrength", 0.85f);
            matFoil.SetTexture("_BakeMap", wreckBake);
            matFoil.SetFloat("_UseUV1ForBake", 1.0f);
            matFoil.SetColor("_EdgeWearColor", new Color(0.98f, 0.85f, 0.50f, 1f));
            matFoil.SetFloat("_EdgeWearStrength", 0.55f);
            matFoil.SetFloat("_EdgeWearThreshold", 0.55f);
            matFoil.SetFloat("_EdgeWearMetallic", 0.95f);
            matFoil.SetFloat("_EdgeWearSmoothness", 0.84f);
            matFoil.SetColor("_DustColor", new Color(0.58f, 0.59f, 0.62f, 1f));
            matFoil.SetFloat("_DustStrength", 0.28f);
            matFoil.SetFloat("_DustGroundHeight", 0.22f);
            matFoil.SetFloat("_DustGroundFade", 0.28f);
            matFoil.SetFloat("_DustGroundStrength", 0.45f);
            SaveOrReplaceAsset(matFoil, MaterialsDir + "/M_R1_GoldFoil.mat");

            // 6. M_R1_Terrain
            var matTerrain = new Material(terrainShader) { name = "M_R1_Terrain", enableInstancing = true };
            matTerrain.SetTexture("_BaseMap", regAlbedo);
            matTerrain.SetColor("_BaseColor", new Color(0.96f, 0.97f, 1.0f, 1f));
            matTerrain.SetColor("_SlopeColor", new Color(0.76f, 0.77f, 0.81f, 1f));
            matTerrain.SetTexture("_BumpMap", regNormal);
            matTerrain.SetTexture("_MaskMap", regMask);
            matTerrain.SetFloat("_NearTiling", 0.58f);
            matTerrain.SetFloat("_MacroTiling", 0.11f);
            matTerrain.SetFloat("_NearBumpScale", 1.38f);
            matTerrain.SetFloat("_MacroBumpScale", 0.42f);
            matTerrain.SetFloat("_NearFadeStart", 4.5f);
            matTerrain.SetFloat("_NearFadeEnd", 18.0f);
            matTerrain.SetFloat("_FineGrainStrength", 0.08f);
            matTerrain.SetFloat("_SmoothnessScale", 0.88f);
            matTerrain.SetFloat("_OcclusionStrength", 0.90f);
            matTerrain.SetFloat("_TriplanarSharpness", 5.0f);
            matTerrain.SetFloat("_SlopeThreshold", 0.88f);
            matTerrain.SetFloat("_SlopeBlendRange", 0.14f);
            matTerrain.SetFloat("_TrackStrength", 1.65f);
            matTerrain.SetFloat("_TrackDarken", 0.14f);
            matTerrain.SetFloat("_TrackWidth", 0.30f);
            matTerrain.SetFloat("_TrackGauge", 0.58f);
            // Rover is placed at (-1.35, h, -0.55) with Yaw = +28 deg (0.488 rad), so trail extends backward (-X, -Z)
            matTerrain.SetVector("_TrackOriginAndYaw", new Vector4(-1.35f, -0.55f, Mathf.PI + 28f * Mathf.Deg2Rad, 18.0f));
            matTerrain.SetVector("_TrackCurve", new Vector4(0.025f, 0.0f, -0.8f, Mathf.PI + 12f * Mathf.Deg2Rad));
            SaveOrReplaceAsset(matTerrain, MaterialsDir + "/M_R1_Terrain.mat");

            AssetDatabase.SaveAssets();
        }

        private static float SampleLabTerrainHeight(float x, float z)
        {
            // Keep the central 4.5m stage very calm so assets sit cleanly, while sculpting craters and ridges around it
            float distFromCenter = Mathf.Sqrt(x * x + z * z);
            float tOuter = Mathf.Clamp01((distFromCenter - 3.2f) / 6.5f);
            float outerWeight = tOuter * tOuter * (3f - 2f * tOuter);

            // Gentle macro undulation
            float h = (Mathf.Sin(x * 0.18f + 0.4f) * Mathf.Cos(z * 0.16f - 0.3f)) * 0.04f;
            h += (Mathf.Sin(x * 0.12f - z * 0.14f) * 0.16f + Mathf.Cos(x * 0.16f + z * 0.13f) * 0.11f) * outerWeight;

            // Sculpted lunar craters (bowl + raised bevelled rim)
            h += EvalCrater(x, z, -8.2f, 9.8f, 4.2f, 0.58f);
            h += EvalCrater(x, z, 9.5f, 11.2f, 5.0f, 0.68f);
            h += EvalCrater(x, z, -10.5f, -7.2f, 3.6f, 0.45f);
            h += EvalCrater(x, z, 8.2f, -8.5f, 3.8f, 0.48f);
            h += EvalCrater(x, z, 1.5f, 13.5f, 4.6f, 0.55f);

            // Background lunar ridge along +Z horizon (z > 8m) for wide shot silhouette
            if (z > 8.0f)
            {
                float ridgeT = Mathf.Clamp01((z - 8.0f) / 11.5f);
                ridgeT = ridgeT * ridgeT * (3f - 2f * ridgeT);
                float peakMound = Mathf.Exp(-((x - 2.4f) * (x - 2.4f)) / 18.0f) * 1.45f;
                float ridgeProfile = 1.45f + peakMound + 0.55f * Mathf.Sin(x * 0.22f + 0.7f) + 0.28f * Mathf.Cos(x * 0.46f - 0.4f);
                h += ridgeT * ridgeProfile;
            }

            // Soft displaced regolith berm around the half-buried Wreck Panel at (2.05, 1.55)
            float dxWreck = x - 2.05f;
            float dzWreck = z - 1.55f;
            float dWreck = Mathf.Sqrt(dxWreck * dxWreck + dzWreck * dzWreck);
            if (dWreck < 2.2f)
            {
                float berm = Mathf.Exp(-((dWreck - 0.85f) * (dWreck - 0.85f)) / 0.32f) * 0.11f;
                h += berm;
            }

            return h;
        }

        private static float EvalCrater(float x, float z, float cx, float cz, float radius, float depth)
        {
            float dx = x - cx;
            float dz = z - cz;
            float r = Mathf.Sqrt(dx * dx + dz * dz) / radius;
            if (r > 1.8f)
            {
                return 0f;
            }

            float bowl = r < 1.0f ? -(1.0f - r * r) * (1.0f - r * r) * depth : 0f;
            float rim = Mathf.Exp(-((r - 1.06f) * (r - 1.06f)) / 0.065f) * (depth * 0.42f);
            return bowl + rim;
        }

        private static Mesh BuildTerrainMesh()
        {
            const int gridRes = 160;
            const float patchSize = 40f;
            const float halfSize = patchSize * 0.5f;

            int vertCount = (gridRes + 1) * (gridRes + 1);
            var vertices = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var triangles = new int[gridRes * gridRes * 6];

            for (int iz = 0; iz <= gridRes; iz++)
            {
                float fz = (float)iz / gridRes;
                float z = -halfSize + fz * patchSize;
                for (int ix = 0; ix <= gridRes; ix++)
                {
                    float fx = (float)ix / gridRes;
                    float x = -halfSize + fx * patchSize;
                    int idx = iz * (gridRes + 1) + ix;
                    vertices[idx] = new Vector3(x, SampleLabTerrainHeight(x, z), z);
                    uvs[idx] = new Vector2(fx * 10f, fz * 10f);
                }
            }

            int t = 0;
            for (int iz = 0; iz < gridRes; iz++)
            {
                for (int ix = 0; ix < gridRes; ix++)
                {
                    int i00 = iz * (gridRes + 1) + ix;
                    int i10 = i00 + 1;
                    int i01 = i00 + (gridRes + 1);
                    int i11 = i01 + 1;

                    triangles[t++] = i00;
                    triangles[t++] = i01;
                    triangles[t++] = i10;

                    triangles[t++] = i10;
                    triangles[t++] = i01;
                    triangles[t++] = i11;
                }
            }

            var mesh = new Mesh
            {
                name = "R1_TerrainPatchMesh",
                indexFormat = IndexFormat.UInt32,
                vertices = vertices,
                uv = uvs,
                triangles = triangles,
            };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            SaveOrReplaceAsset(mesh, TerrainMeshPath);
            return AssetDatabase.LoadAssetAtPath<Mesh>(TerrainMeshPath);
        }

        private static VolumeProfile BuildR1VolumeProfile()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "R1VolumeProfile";
            SaveOrReplaceAsset(profile, VolumeProfilePath);
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);

            // Clear existing components if any
            profile.components.Clear();

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.18f);
            bloom.intensity.Override(0.38f);
            bloom.scatter.Override(0.65f);
            bloom.tint.Override(new Color(1.0f, 0.96f, 0.90f, 1f));
            bloom.highQualityFiltering.Override(true);

            var colorAdjust = profile.Add<ColorAdjustments>(true);
            colorAdjust.postExposure.Override(0.24f);
            colorAdjust.contrast.Override(11f);
            colorAdjust.saturation.Override(-4f);
            colorAdjust.colorFilter.Override(new Color(0.98f, 0.99f, 1.0f, 1f));

            var splitToning = profile.Add<SplitToning>(true);
            splitToning.shadows.Override(new Color(0.45f, 0.50f, 0.58f, 1f));
            splitToning.highlights.Override(new Color(1.0f, 0.94f, 0.85f, 1f));
            splitToning.balance.Override(-14f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.16f);
            vignette.smoothness.Override(0.45f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void BuildLabScene(Mesh terrainMesh, VolumeProfile volumeProfile, Cubemap envCubemap)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            WorldSettings worldSettings = AssetDatabase.LoadAssetAtPath<WorldSettings>(WorldPaths.Settings);
            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(WorldPaths.SkyMaterial);
            Material earthMat = AssetDatabase.LoadAssetAtPath<Material>(WorldPaths.EarthMaterial);

            // 1. Lighting & Atmosphere matching Main.unity's night
            var lightingRoot = new GameObject("Lighting").transform;
            var earthlightGo = new GameObject("Earthlight");
            earthlightGo.transform.SetParent(lightingRoot, false);
            var earthlight = earthlightGo.AddComponent<Light>();

            var fillGo = new GameObject("Fill Light");
            fillGo.transform.SetParent(lightingRoot, false);
            var fillLight = fillGo.AddComponent<Light>();

            if (worldSettings != null)
            {
                WorldAtmosphere.Apply(worldSettings.Atmosphere, worldSettings.Sky, earthlight, fillLight);
                SkyShaderGlobals.Apply(worldSettings.Sky);
            }

            if (skyMat != null)
            {
                RenderSettings.skybox = skyMat;
            }

            if (envCubemap != null)
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = envCubemap;
                RenderSettings.reflectionIntensity = 1.0f;
            }

            // Earth in the lunar night sky
            if (worldSettings != null && earthMat != null)
            {
                var earthGo = new GameObject("Earth");
                earthGo.transform.SetParent(lightingRoot, false);
                Mesh earthMesh = EarthMeshBuilder.Build(worldSettings.Sky, worldSettings.Seed);
                earthGo.AddComponent<MeshFilter>().sharedMesh = earthMesh;
                var earthRenderer = earthGo.AddComponent<MeshRenderer>();
                earthRenderer.sharedMaterial = earthMat;
                earthRenderer.shadowCastingMode = ShadowCastingMode.Off;
                earthRenderer.receiveShadows = false;
            }

            // 2. Post-Processing Volume tuned for R1
            var volumeGo = new GameObject("R1 PostProcess Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = volumeProfile;

            // 3. 40 x 40 m R1 Regolith Terrain Patch
            var terrainGo = new GameObject("R1_TerrainPatch_40x40");
            terrainGo.AddComponent<MeshFilter>().sharedMesh = terrainMesh;
            var terrainRenderer = terrainGo.AddComponent<MeshRenderer>();
            terrainRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/M_R1_Terrain.mat");
            terrainRenderer.shadowCastingMode = ShadowCastingMode.On;
            terrainRenderer.receiveShadows = true;

            // 4. Three R1 Hero Assets + Background Rocks + Today's 07 for Scale
            Material matCrate = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/M_R1_SupplyCrate.mat");
            Material matRubber = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/M_R1_Rubber.mat");
            Material matRock = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/M_R1_Rock.mat");
            Material matWreck = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/M_R1_WreckTrim.mat");
            Material matFoil = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/M_R1_GoldFoil.mat");

            // (1) Crew Supply Crate (upright on its 4 rubber corner feet)
            Vector3 cratePos = new Vector3(0.68f, SampleLabTerrainHeight(0.68f, -1.12f), -1.12f);
            GameObject crateInst = InstantiateFbx(ModelsDir + "/R1_SupplyCrate.fbx", "R1_SupplyCrate", cratePos, Quaternion.Euler(0f, -26f, 0f));
            AssignRendererMaterials(crateInst, new[] { matCrate, matRubber });

            // (2) Cluster of Three Moon Rocks (upright with tall 1.48m monolith)
            Vector3 rockPos = new Vector3(-2.10f, SampleLabTerrainHeight(-2.10f, 2.25f) - 0.02f, 2.25f);
            GameObject rockInst = InstantiateFbx(ModelsDir + "/R1_RockCluster.fbx", "R1_RockCluster", rockPos, Quaternion.Euler(0f, 16f, 0f));
            AssignRendererMaterials(rockInst, new[] { matRock });

            // (3) Wreck Panel with Torn Edge (tilted at 42 deg over the regolith berm to show curved hull + torn edge + gold foil)
            Vector3 wreckPos = new Vector3(2.05f, SampleLabTerrainHeight(2.05f, 1.55f) + 0.08f, 1.55f);
            Quaternion wreckRot = Quaternion.Euler(0f, -34f, 0f) * Quaternion.Euler(-42f, 0f, 0f);
            GameObject wreckInst = InstantiateFbx(ModelsDir + "/R1_WreckPanel.fbx", "R1_WreckPanel", wreckPos, wreckRot);
            AssignRendererMaterials(wreckInst, new[] { matWreck, matFoil });

            // Secondary background rock clusters for wide shot depth across the 40x40m patch
            Vector3 bgRock1Pos = new Vector3(-7.2f, SampleLabTerrainHeight(-7.2f, 10.5f) - 0.04f, 10.5f);
            GameObject bgRock1 = InstantiateFbx(ModelsDir + "/R1_RockCluster.fbx", "R1_RockCluster_BG_Left", bgRock1Pos, Quaternion.Euler(0f, 68f, 0f));
            bgRock1.transform.localScale = Vector3.one * 1.25f;
            AssignRendererMaterials(bgRock1, new[] { matRock });

            Vector3 bgRock2Pos = new Vector3(6.8f, SampleLabTerrainHeight(6.8f, 8.8f) - 0.04f, 8.8f);
            GameObject bgRock2 = InstantiateFbx(ModelsDir + "/R1_RockCluster.fbx", "R1_RockCluster_BG_Right", bgRock2Pos, Quaternion.Euler(0f, -115f, 0f));
            bgRock2.transform.localScale = Vector3.one * 0.78f;
            AssignRendererMaterials(bgRock2, new[] { matRock });

            // (4) Today's 07 for Scale + Headlamp
            Vector3 roverPos = new Vector3(-1.35f, SampleLabTerrainHeight(-1.35f, -0.55f), -0.55f);
            SpawnRover07WithHeadlamp(roverPos, Quaternion.Euler(0f, 28f, 0f));

            // 5. Scene Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 600f;
            cam.fieldOfView = 44f;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;

            float baseH = SampleLabTerrainHeight(0f, 0f);
            camGo.transform.position = new Vector3(0f, baseH, 0f) + Poses[3].CameraOffset;
            camGo.transform.LookAt(new Vector3(0f, baseH, 0f) + Poses[3].LookAtOffset, Vector3.up);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[R1Lab] Saved {ScenePath}");
        }

        private static GameObject InstantiateFbx(string assetPath, string name, Vector3 position, Quaternion rotation)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing model asset at {assetPath}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        private static void AssignRendererMaterials(GameObject root, Material[] materials)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                int slotCount = r.sharedMaterials.Length;
                if (slotCount <= 1)
                {
                    r.sharedMaterial = materials[0];
                }
                else
                {
                    var assigned = new Material[slotCount];
                    for (int i = 0; i < slotCount; i++)
                    {
                        assigned[i] = materials[Mathf.Min(i, materials.Length - 1)];
                    }
                    r.sharedMaterials = assigned;
                }
            }
        }

        private static GameObject SpawnRover07WithHeadlamp(Vector3 position, Quaternion rotation)
        {
            var roverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArtPaths.RoverFolder + "/RoverModel.prefab");
            if (roverPrefab == null)
            {
                throw new InvalidOperationException("RoverModel.prefab not found.");
            }

            var rover = (GameObject)PrefabUtility.InstantiatePrefab(roverPrefab);
            rover.name = "Rover_07";
            rover.transform.SetPositionAndRotation(position, rotation);

            // Attach visible kit items (LampBar, CapacitorDrums, TetherRing) so 07 looks complete
            AttachKitPrefab(rover.transform, "HeadlampSocket", ArtPaths.RoverFolder + "/Kit_LampBar.prefab");
            AttachKitPrefab(rover.transform, "DrumSocket_L", ArtPaths.RoverFolder + "/Kit_CapacitorDrum.prefab");
            AttachKitPrefab(rover.transform, "DrumSocket_R", ArtPaths.RoverFolder + "/Kit_CapacitorDrum.prefab");
            AttachKitPrefab(rover.transform, "Neck/Head/RingSocket", ArtPaths.RoverFolder + "/Kit_TetherRing.prefab");

            ApplyRoverGlowBlocks(rover);

            // Add warm amber headlamp Spot Light casting soft light toward the crate and wreck panel
            Transform lampSocket = rover.transform.Find("HeadlampSocket");
            var spotGo = new GameObject("WarmHeadlampLight");
            spotGo.transform.SetParent(lampSocket != null ? lampSocket : rover.transform, false);
            spotGo.transform.localPosition = new Vector3(0f, 0.08f, 0.12f);
            spotGo.transform.localRotation = Quaternion.Euler(7f, 14f, 0f);

            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1.0f, 0.74f, 0.36f, 1f);
            spot.intensity = 2.6f;
            spot.range = 11.0f;
            spot.spotAngle = 62f;
            spot.innerSpotAngle = 32f;
            spot.shadows = LightShadows.Soft;
            spot.shadowStrength = 0.65f;

            return rover;
        }

        private static void ApplyRoverGlowBlocks(GameObject rover)
        {
            Renderer[] renderers = rover.GetComponentsInChildren<Renderer>(true);
            var warmProp = new MaterialPropertyBlock();
            warmProp.SetColor("_EmissionColor", new Color(0.95f, 0.68f, 0.28f, 1f));
            var cyanProp = new MaterialPropertyBlock();
            cyanProp.SetColor("_EmissionColor", new Color(0.28f, 0.72f, 0.78f, 1f));

            foreach (Renderer r in renderers)
            {
                if (r.name.StartsWith("Lamp_", StringComparison.Ordinal) || r.name.StartsWith("BatteryGlass_", StringComparison.Ordinal))
                {
                    r.SetPropertyBlock(warmProp);
                }
                else if (r.name == "Glow")
                {
                    r.SetPropertyBlock(cyanProp);
                }
            }
        }

        private static void AttachKitPrefab(Transform roverRoot, string socketPath, string prefabPath)
        {
            Transform socket = roverRoot.Find(socketPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (socket == null || prefab == null)
            {
                return;
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, socket);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
        }

        private static void CaptureAndMeasureLabScene(string outDir, StringBuilder report)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            WorldSettings worldSettings = AssetDatabase.LoadAssetAtPath<WorldSettings>(WorldPaths.Settings);
            if (worldSettings != null)
            {
                SkyShaderGlobals.Apply(worldSettings.Sky);
            }

            Cubemap envCubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(EnvCubemapPath);
            if (envCubemap != null)
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = envCubemap;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Rover_07")
                {
                    ApplyRoverGlowBlocks(root);
                }
            }

            Camera cam = FindSceneCamera(scene);
            float baseH = SampleLabTerrainHeight(0f, 0f);
            Vector3 stageOrigin = new Vector3(0f, baseH, 0f);

            report.AppendLine("  \"r1Lab\": {");
            AppendSceneGeometryAndMemoryStats(scene, true, report);
            report.AppendLine("    \"poses\": {");

            for (int i = 0; i < Poses.Length; i++)
            {
                PoseSpec pose = Poses[i];
                cam.transform.position = stageOrigin + pose.CameraOffset;
                cam.transform.LookAt(stageOrigin + pose.LookAtOffset, Vector3.up);
                cam.fieldOfView = pose.FieldOfView;

                string pngPath = Path.Combine(outDir, $"r1_{pose.Id}.png");
                double avgFrameMs = RenderCaptureAndBenchmark(cam, pngPath);
                string comma = i < Poses.Length - 1 ? "," : "";
                report.AppendLine($"      \"{pose.Id}\": {{ \"frameMs1080p\": {avgFrameMs:F3}, \"file\": \"Logs/r1/r1_{pose.Id}.png\" }}{comma}");
            }

            report.AppendLine("    }");
            report.AppendLine("  },");
        }

        private static void CaptureAndMeasureMainScene(string outDir, StringBuilder report)
        {
            Scene mainScene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity", OpenSceneMode.Single);
            WorldSettings worldSettings = AssetDatabase.LoadAssetAtPath<WorldSettings>(WorldPaths.Settings);
            if (worldSettings != null)
            {
                SkyShaderGlobals.Apply(worldSettings.Sky);
                Color haze = worldSettings.Atmosphere.HazeColor.linear;
                Shader.SetGlobalVector("_MoonHazeColor", new Vector4(haze.r, haze.g, haze.b, worldSettings.Atmosphere.HazeMax));
                Shader.SetGlobalVector("_MoonHaze", new Vector4(
                    worldSettings.Atmosphere.HazeDensity,
                    1f / worldSettings.Atmosphere.HazeHeight,
                    worldSettings.Atmosphere.HazeBase,
                    0f));
                Shader.SetGlobalVector("_MoonDesaturate", new Vector4(
                    worldSettings.Atmosphere.DesaturateStart,
                    1f / Mathf.Max(worldSettings.Atmosphere.DesaturateEnd - worldSettings.Atmosphere.DesaturateStart, 1f),
                    worldSettings.Atmosphere.DesaturateAmount,
                    0f));
            }

            // Locate terrain height at (0, 0) in Main.unity and temporarily hide Home Base / Rover props within 22m
            float mainH = SampleMainSceneHeight(mainScene, worldSettings, 0f, 0f);
            Vector3 stageOrigin = new Vector3(0f, mainH, 0f);

            HideOverlappingPropsInMain(mainScene, stageOrigin, 22.0f);

            // Instantiate Main.unity's current low-poly equivalents at the exact same relative positions & rotations
            var stagingRoot = new GameObject("_MainComparisonStage");

            // (1) Today's Supply Crate (CoilCrate.prefab)
            var mainCratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArtPaths.PropFolder + "/CoilCrate.prefab");
            if (mainCratePrefab != null)
            {
                var c = (GameObject)PrefabUtility.InstantiatePrefab(mainCratePrefab, stagingRoot.transform);
                c.transform.SetPositionAndRotation(
                    new Vector3(0.68f, SampleMainSceneHeight(mainScene, worldSettings, 0.68f, -1.12f), -1.12f),
                    Quaternion.Euler(0f, -26f, 0f));
            }

            // (2) Today's Cluster of Three Moon Rocks (Rock_01, Rock_02, Rock_03)
            var rockClusterRoot = new GameObject("Main_RockCluster");
            rockClusterRoot.transform.SetParent(stagingRoot.transform, false);
            rockClusterRoot.transform.SetPositionAndRotation(
                new Vector3(-2.10f, SampleMainSceneHeight(mainScene, worldSettings, -2.10f, 2.25f), 2.25f),
                Quaternion.Euler(0f, 16f, 0f));

            SpawnMainRockChild(rockClusterRoot.transform, ArtPaths.RockFolder + "/Rock_01.prefab", new Vector3(-0.18f, 0f, 0.22f), new Vector3(1.15f, 1.30f, 1.10f), 15f);
            SpawnMainRockChild(rockClusterRoot.transform, ArtPaths.RockFolder + "/Rock_02.prefab", new Vector3(-0.68f, 0f, -0.28f), new Vector3(0.85f, 0.82f, 0.80f), -40f);
            SpawnMainRockChild(rockClusterRoot.transform, ArtPaths.RockFolder + "/Rock_03.prefab", new Vector3(0.48f, 0f, -0.26f), new Vector3(0.72f, 0.58f, 0.68f), 75f);

            // (3) Today's Wreck Panel (HullFragment_0.prefab)
            var mainWreckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArtPaths.PropFolder + "/HullFragment_0.prefab");
            if (mainWreckPrefab != null)
            {
                var w = (GameObject)PrefabUtility.InstantiatePrefab(mainWreckPrefab, stagingRoot.transform);
                w.transform.SetPositionAndRotation(
                    new Vector3(2.05f, SampleMainSceneHeight(mainScene, worldSettings, 2.05f, 1.55f), 1.55f),
                    Quaternion.Euler(0f, -32f, 0f));
            }

            // (4) Today's 07 with same headlamp
            GameObject rover = SpawnRover07WithHeadlamp(
                new Vector3(-1.35f, SampleMainSceneHeight(mainScene, worldSettings, -1.35f, -0.55f), -0.55f),
                Quaternion.Euler(0f, 28f, 0f));
            rover.transform.SetParent(stagingRoot.transform, true);

            Camera cam = FindSceneCamera(mainScene);
            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.antialiasingQuality = AntialiasingQuality.High;
            }

            report.AppendLine("  \"mainScene\": {");
            AppendSceneGeometryAndMemoryStats(mainScene, false, report);
            report.AppendLine("    \"poses\": {");

            for (int i = 0; i < Poses.Length; i++)
            {
                PoseSpec pose = Poses[i];
                cam.transform.position = stageOrigin + pose.CameraOffset;
                cam.transform.LookAt(stageOrigin + pose.LookAtOffset, Vector3.up);
                cam.fieldOfView = pose.FieldOfView;

                string pngPath = Path.Combine(outDir, $"main_{pose.Id}.png");
                double avgFrameMs = RenderCaptureAndBenchmark(cam, pngPath);
                string comma = i < Poses.Length - 1 ? "," : "";
                report.AppendLine($"      \"{pose.Id}\": {{ \"frameMs1080p\": {avgFrameMs:F3}, \"file\": \"Logs/r1/main_{pose.Id}.png\" }}{comma}");
            }

            report.AppendLine("    }");
            report.AppendLine("  },");
        }

        private static void SpawnMainRockChild(Transform parent, string path, Vector3 localPos, Vector3 localScale, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                return;
            }

            var r = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            r.transform.localPosition = localPos;
            r.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            r.transform.localScale = localScale;
        }

        private static float SampleMainSceneHeight(Scene scene, WorldSettings worldSettings, float x, float z)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                WorldSystem worldSys = root.GetComponentInChildren<WorldSystem>(true);
                if (worldSys != null)
                {
                    if (!worldSys.HasGeneratedWorld)
                    {
                        worldSys.RebuildPreview();
                    }
                    if (worldSys.Surface != null)
                    {
                        return worldSys.Surface.Sample(x, z).Height;
                    }
                }
            }
            if (worldSettings != null)
            {
                return worldSettings.CreateSurface().Sample(x, z).Height;
            }
            return 0f;
        }

        private static void HideOverlappingPropsInMain(Scene scene, Vector3 center, float radius)
        {
            float rSq = radius * radius;
            Material groundMat = AssetDatabase.LoadAssetAtPath<Material>(WorldPaths.GroundMaterial);
            Material earthMat = AssetDatabase.LoadAssetAtPath<Material>(WorldPaths.EarthMaterial);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Rover")
                {
                    root.SetActive(false);
                    continue;
                }

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    if (r.name == "Earth" || r.name == "WornTracks" || r.sharedMaterial == groundMat || r.sharedMaterial == earthMat)
                    {
                        continue;
                    }
                    if (r.name.StartsWith("Chunk", StringComparison.Ordinal) || r.name.StartsWith("Backdrop", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Vector3 bCenter = r.bounds.center;
                    Vector2 dxz = new Vector2(bCenter.x - center.x, bCenter.z - center.z);
                    if (dxz.sqrMagnitude < rSq)
                    {
                        r.enabled = false;
                    }
                }

                Light[] lights = root.GetComponentsInChildren<Light>(true);
                foreach (Light l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        continue;
                    }
                    Vector2 dxz = new Vector2(l.transform.position.x - center.x, l.transform.position.z - center.z);
                    if (dxz.sqrMagnitude < rSq)
                    {
                        l.enabled = false;
                    }
                }
            }
        }

        private static double RenderCaptureAndBenchmark(Camera cam, string outPngPath)
        {
            var rt = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.DefaultHDR)
            {
                antiAliasing = 1,
            };
            rt.Create();
            var srgbRt = RenderTexture.GetTemporary(CaptureWidth, CaptureHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var syncTex = new Texture2D(1, 1, TextureFormat.RGB24, false, false);

            RenderTexture prevTarget = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;
            cam.targetTexture = rt;

            // Warmup 12 frames with GPU readback fence
            for (int w = 0; w < 12; w++)
            {
                cam.Render();
            }
            Graphics.Blit(rt, srgbRt);
            RenderTexture.active = srgbRt;
            syncTex.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);

            // Benchmark 48 rendered frames at 1920x1080 with GPU sync fence
            const int benchFrames = 48;
            var sw = Stopwatch.StartNew();
            for (int f = 0; f < benchFrames; f++)
            {
                cam.Render();
                GL.Flush();
            }
            Graphics.Blit(rt, srgbRt);
            RenderTexture.active = srgbRt;
            syncTex.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
            sw.Stop();
            double avgMs = sw.Elapsed.TotalMilliseconds / benchFrames;
            Object.DestroyImmediate(syncTex);
            var tex = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false, false);
            tex.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
            tex.Apply();

            byte[] pngBytes = tex.EncodeToPNG();
            File.WriteAllBytes(outPngPath, pngBytes);

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            RenderTexture.ReleaseTemporary(srgbRt);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);

            Debug.Log($"[R1Lab] Captured {outPngPath} ({avgMs:F2} ms/frame @ 1080p)");
            return avgMs;
        }

        private static void AppendSceneGeometryAndMemoryStats(Scene scene, bool isR1Lab, StringBuilder sb)
        {
            long totalTris = 0;
            long totalVerts = 0;
            int activeRenderers = 0;
            var uniqueTextures = new HashSet<Texture>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (!root.activeInHierarchy)
                {
                    continue;
                }

                MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(false);
                foreach (MeshFilter mf in filters)
                {
                    Renderer r = mf.GetComponent<Renderer>();
                    if (r == null || !r.enabled || mf.sharedMesh == null || mf.gameObject.name == "Earth")
                    {
                        continue;
                    }

                    activeRenderers++;
                    totalVerts += mf.sharedMesh.vertexCount;
                    totalTris += mf.sharedMesh.triangles.Length / 3;

                    foreach (Material mat in r.sharedMaterials)
                    {
                        if (mat == null)
                        {
                            continue;
                        }
                        CollectMaterialTextures(mat, uniqueTextures);
                    }
                }
            }

            long texMemoryBytes = 0;
            foreach (Texture tex in uniqueTextures)
            {
                texMemoryBytes += Profiler.GetRuntimeMemorySizeLong(tex);
            }

            sb.AppendLine($"    \"activeRenderers\": {activeRenderers},");
            sb.AppendLine($"    \"totalVertices\": {totalVerts},");
            sb.AppendLine($"    \"totalTriangles\": {totalTris},");
            sb.AppendLine($"    \"uniqueTextures\": {uniqueTextures.Count},");
            sb.AppendLine($"    \"textureMemoryBytes\": {texMemoryBytes},");
            sb.AppendLine($"    \"textureMemoryMB\": {(texMemoryBytes / (1024.0 * 1024.0)):F3},");

            if (isR1Lab)
            {
                AppendMeshStat(ModelsDir + "/R1_SupplyCrate.fbx", "supplyCrate", sb);
                AppendMeshStat(ModelsDir + "/R1_RockCluster.fbx", "rockCluster", sb);
                AppendMeshStat(ModelsDir + "/R1_WreckPanel.fbx", "wreckPanel", sb);
            }
            else
            {
                AppendPrefabTriStat(ArtPaths.PropFolder + "/CoilCrate.prefab", "mainCoilCrate", sb);
                AppendPrefabTriStat(ArtPaths.RockFolder + "/Rock_01.prefab", "mainRock01", sb);
                AppendPrefabTriStat(ArtPaths.PropFolder + "/HullFragment_0.prefab", "mainHullFragment0", sb);
                AppendPrefabTriStat(ArtPaths.RoverFolder + "/RoverModel.prefab", "rover07", sb);
            }
        }

        private static void AppendMeshStat(string assetPath, string key, StringBuilder sb)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            long tris = 0;
            long verts = 0;
            if (go != null)
            {
                foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh != null)
                    {
                        verts += mf.sharedMesh.vertexCount;
                        tris += mf.sharedMesh.triangles.Length / 3;
                    }
                }
            }
            sb.AppendLine($"    \"{key}\": {{ \"vertices\": {verts}, \"triangles\": {tris} }},");
        }

        private static void AppendPrefabTriStat(string assetPath, string key, StringBuilder sb)
        {
            AppendMeshStat(assetPath, key, sb);
        }

        private static void CollectMaterialTextures(Material mat, HashSet<Texture> set)
        {
            Shader s = mat.shader;
            if (s == null)
            {
                return;
            }

            int count = s.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                if (s.GetPropertyType(i) == ShaderPropertyType.Texture)
                {
                    string propName = s.GetPropertyName(i);
                    Texture t = mat.GetTexture(propName);
                    if (t != null)
                    {
                        set.Add(t);
                    }
                }
            }
        }

        private static void AppendWorldAudit(StringBuilder sb)
        {
            string[] allGeneratedPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Generated" });
            int roverPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.RoverFolder }).Length;
            int rockPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.RockFolder }).Length;
            int basePrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.BaseFolder }).Length;
            int sitePrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.SitesFolder }).Length;
            int landmarkPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.LandmarksFolder }).Length;
            int relicPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.RelicFolder }).Length;
            int friendPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.FriendFolder }).Length;
            int pickupPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.PickupFolder }).Length;
            int relayPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.RelayFolder }).Length;
            int propPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { ArtPaths.PropFolder }).Length;

            sb.AppendLine("  \"worldAudit\": {");
            sb.AppendLine($"    \"totalGeneratedPrefabs\": {allGeneratedPrefabs.Length},");
            sb.AppendLine($"    \"roverPrefabs\": {roverPrefabs},");
            sb.AppendLine($"    \"rockPrefabs\": {rockPrefabs},");
            sb.AppendLine($"    \"basePrefabs\": {basePrefabs},");
            sb.AppendLine($"    \"sitePrefabs\": {sitePrefabs},");
            sb.AppendLine($"    \"landmarkPrefabs\": {landmarkPrefabs},");
            sb.AppendLine($"    \"relicPrefabs\": {relicPrefabs},");
            sb.AppendLine($"    \"friendPrefabs\": {friendPrefabs},");
            sb.AppendLine($"    \"pickupPrefabs\": {pickupPrefabs},");
            sb.AppendLine($"    \"relayPrefabs\": {relayPrefabs},");
            sb.AppendLine($"    \"propPrefabs\": {propPrefabs}");
            sb.AppendLine("  }");
        }

        private static Camera FindSceneCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Camera cam = root.GetComponentInChildren<Camera>(true);
                if (cam != null)
                {
                    return cam;
                }
            }
            throw new InvalidOperationException($"No Camera found in scene {scene.name}");
        }

        private static Texture2D LoadTex(string fileName)
        {
            string path = TexturesDir + "/" + fileName;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                throw new InvalidOperationException($"Missing texture at {path}");
            }
            return tex;
        }

        private static void SaveOrReplaceAsset(Object asset, string path)
        {
            Object existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(asset);
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
            }
        }
    }
}
