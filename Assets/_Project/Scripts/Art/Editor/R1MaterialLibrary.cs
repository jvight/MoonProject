using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonProject.Editor.Automation;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Builds, validates, and loads the 11 R1 stylized-realism material assets under
    /// <c>Assets/_Project/Art/R1/Materials/</c>, and renders the night-lit swatch board capture into
    /// <c>Logs/captures/</c>.
    /// </summary>
    public static class R1MaterialLibrary
    {
        private const int SwatchWidth = 1600;
        private const int SwatchHeight = 900;
        private const float SwatchSpacing = 1.15f;
        private const float SphereRadius = 0.36f;
        private const float CubeSize = 0.64f;
        private const float CubeBevel = 0.065f;

        /// <summary>
        /// Batch entry point that creates or updates the 11 R1 material assets and captures the night-lit swatch board.
        /// </summary>
        public static void BuildAndCapture()
        {
            BatchRunner.Run(nameof(R1MaterialLibrary), args =>
            {
                BuildMaterials();
                string capturesDir = Path.GetFullPath("Logs/captures");
                bool ok = CaptureSwatchBoardTo(capturesDir);
                string batchOutDir = Path.Combine(args.OutputDirectory, "r1_swatches");
                Directory.CreateDirectory(batchOutDir);
                foreach (string png in Directory.GetFiles(capturesDir, "r1_swatch_*.png"))
                {
                    File.Copy(png, Path.Combine(batchOutDir, Path.GetFileName(png)), true);
                }

                return ok;
            });
        }

        /// <summary>
        /// Creates or updates all 11 R1 material assets under <see cref="R1MaterialCatalog.MaterialsFolder"/>.
        /// </summary>
        public static void BuildMaterials()
        {
            EnsureFolderExists(R1MaterialCatalog.RootFolder);
            EnsureFolderExists(R1MaterialCatalog.MaterialsFolder);
            EnsureFolderExists(R1MaterialCatalog.TexturesFolder);

            Shader shader = LoadShader();
            IReadOnlyList<R1MaterialSpec> specs = R1MaterialCatalog.All;
            for (int i = 0; i < specs.Count; i++)
            {
                R1MaterialSpec spec = specs[i];
                string matPath = R1MaterialCatalog.GetMaterialPath(spec);
                Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (existing == null)
                {
                    var created = new Material(shader) { name = spec.MaterialAssetName };
                    ConfigureMaterial(created, shader, spec);
                    AssetDatabase.CreateAsset(created, matPath);
                }
                else
                {
                    ConfigureMaterial(existing, shader, spec);
                    EditorUtility.SetDirty(existing);
                }
            }

            AssetDatabase.SaveAssets();
            OriginalDiskBytes.Clear();
        }

        /// <summary>
        /// Loads the R1 <c>MoonProject/R1/Lit</c> shader from its asset path or shader registry.
        /// </summary>
        public static Shader LoadShader()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(R1MaterialCatalog.ShaderPath);
            if (shader == null)
            {
                shader = Shader.Find(R1MaterialCatalog.ShaderName);
            }

            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"R1Lit shader is missing at '{R1MaterialCatalog.ShaderPath}' ({R1MaterialCatalog.ShaderName}).");
            }

            return shader;
        }

        private static readonly Dictionary<string, byte[]> OriginalDiskBytes = new Dictionary<string, byte[]>();

        static R1MaterialLibrary()
        {
            EditorApplication.quitting += RestoreOriginalDiskBytes;
        }

        /// <summary>
        /// Ensures every loaded R1 material asset has its shader and textures bound in memory (needed in batch runs
        /// where untracked <c>.meta</c> files were regenerated with fresh local GUIDs).
        /// </summary>
        public static void EnsureInMemoryBindings()
        {
            Shader shader = LoadShader();
            IReadOnlyList<R1MaterialSpec> specs = R1MaterialCatalog.All;
            for (int i = 0; i < specs.Count; i++)
            {
                R1MaterialSpec spec = specs[i];
                string matPath = R1MaterialCatalog.GetMaterialPath(spec);
                if (!OriginalDiskBytes.ContainsKey(matPath) && File.Exists(matPath))
                {
                    OriginalDiskBytes[matPath] = File.ReadAllBytes(matPath);
                }

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    continue;
                }

                bool needsRebind = mat.shader != shader
                    || mat.GetTexture("_BaseMap") == null
                    || mat.GetTexture("_BumpMap") == null
                    || mat.GetTexture("_MaskMap") == null
                    || (spec.HasEmission && mat.GetTexture("_EmissionMap") == null);
                if (needsRebind)
                {
                    ConfigureMaterial(mat, shader, spec);
                }
            }
        }

        private static void RestoreOriginalDiskBytes()
        {
            foreach (KeyValuePair<string, byte[]> pair in OriginalDiskBytes)
            {
                if (File.Exists(pair.Key))
                {
                    File.WriteAllBytes(pair.Key, pair.Value);
                }
            }
        }

        /// <summary>Loads the library <see cref="Material"/> for <paramref name="slot"/>.</summary>
        public static Material LoadMaterial(R1MaterialSlot slot)
        {
            R1MaterialSpec spec = R1MaterialCatalog.Get(slot);
            string matPath = R1MaterialCatalog.GetMaterialPath(spec);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                throw new InvalidOperationException($"R1 material asset is missing at '{matPath}'.");
            }

            Shader shader = LoadShader();
            if (mat.shader != shader
                || mat.GetTexture("_BaseMap") == null
                || mat.GetTexture("_BumpMap") == null
                || mat.GetTexture("_MaskMap") == null
                || (spec.HasEmission && mat.GetTexture("_EmissionMap") == null))
            {
                ConfigureMaterial(mat, shader, spec);
            }

            return mat;
        }

        /// <summary>
        /// Configures <paramref name="material"/> with the textures and PBR/procedural parameters of
        /// <paramref name="spec"/>.
        /// </summary>
        public static void ConfigureMaterial(Material material, Shader shader, R1MaterialSpec spec)
        {
            material.shader = shader;
            material.name = spec.MaterialAssetName;

            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialCatalog.GetAlbedoTexturePath(spec));
            var normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialCatalog.GetNormalTexturePath(spec));
            var maskMap = AssetDatabase.LoadAssetAtPath<Texture2D>(R1MaterialCatalog.GetMaskTexturePath(spec));
            string emissionPath = R1MaterialCatalog.GetEmissionTexturePath(spec);
            Texture2D emissionMap = emissionPath != null
                ? AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath)
                : null;

            material.SetTexture("_BaseMap", baseMap);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetColor("_BaseColor", spec.BaseColor);

            material.SetTexture("_BumpMap", normalMap);
            material.SetFloat("_BumpScale", spec.BumpScale);

            material.SetTexture("_MaskMap", maskMap);
            material.SetFloat("_MetallicScale", spec.MetallicScale);
            material.SetFloat("_SmoothnessScale", 1.0f);
            material.SetFloat("_SmoothnessMin", spec.SmoothnessMin);
            material.SetFloat("_SmoothnessMax", spec.SmoothnessMax);
            material.SetFloat("_OcclusionStrength", 1.0f);

            material.SetColor("_EdgeWearColor", spec.EdgeWearColor);
            material.SetFloat("_EdgeWearAmount", spec.EdgeWearAmount);
            material.SetFloat("_EdgeWearThreshold", 0.56f);
            material.SetFloat("_EdgeWearSharpness", 3.5f);
            material.SetFloat("_EdgeWearMetallic", spec.EdgeWearMetallic);
            material.SetFloat("_EdgeWearSmoothness", spec.EdgeWearSmoothness);

            material.SetColor("_DustColor", R1MaterialCatalog.DefaultDustColor);
            material.SetFloat("_DustAmount", spec.DustAmount);
            material.SetFloat("_DustSharpness", 2.0f);
            material.SetFloat("_DustUpThreshold", 0.12f);
            material.SetFloat("_DustOcclusionWeight", 0.42f);
            material.SetFloat("_DustSmoothness", 0.12f);

            material.SetFloat("_ColorVarScale", 1.8f);
            material.SetFloat("_ColorVarAmount", 0.035f);

            material.SetTexture("_EmissionMap", emissionMap);
            material.SetColor("_EmissionColor", spec.EmissionColor);
            material.SetFloat("_EmissionStrength", spec.EmissionStrength);
            material.SetFloat("_BloomSafeMax", R1MaterialCatalog.DefaultBloomSafeMax);
            material.SetFloat("_HotCoreThreshold", R1MaterialCatalog.DefaultHotCoreThreshold);
            material.SetFloat("_HotCorePeak", R1MaterialCatalog.DefaultHotCorePeak);

            material.globalIlluminationFlags = spec.HasEmission
                ? MaterialGlobalIlluminationFlags.RealtimeEmissive
                : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            material.enableInstancing = true;
        }

        /// <summary>
        /// Stages all 11 R1 library materials on spheres and bevelled cubes under the game's lunar night lighting and
        /// saves PNG captures into <paramref name="outputDir"/>.
        /// </summary>
        public static bool CaptureSwatchBoardTo(string outputDir)
        {
            EnsureInMemoryBindings();
            Directory.CreateDirectory(outputDir);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temporary = new ArtLightingPreview.TemporaryObjects();
            try
            {
                SetupNightSwatchEnvironment(temporary);

                Mesh sphereMesh = CreateUvSphereMesh(SphereRadius, 36, 24);
                Mesh cubeMesh = CreateBevelledCubeMesh(CubeSize, CubeBevel);
                Mesh plinthMesh = CreateBevelledCubeMesh(1f, 0.03f);
                temporary.Add(sphereMesh);
                temporary.Add(cubeMesh);
                temporary.Add(plinthMesh);

                Material plinthMat = LoadMaterial(R1MaterialSlot.GreyMetal);
                GameObject plinth = ArtLightingPreview.MeshObject("SwatchPlinth", plinthMesh, plinthMat, temporary);
                plinth.transform.position = new Vector3(0f, -0.24f, 0.2f);
                plinth.transform.localScale = new Vector3(12.4f, 0.36f, 3.2f);
                temporary.Add(plinth);

                IReadOnlyList<R1MaterialSpec> specs = R1MaterialCatalog.All;
                const float ColumnSpacing = 1.02f;
                float halfSpan = (specs.Count - 1) * 0.5f * ColumnSpacing;

                for (int i = 0; i < specs.Count; i++)
                {
                    R1MaterialSpec spec = specs[i];
                    Material mat = LoadMaterial(spec.Slot);
                    float x = i * ColumnSpacing - halfSpan;

                    // Front row: bevelled cube tilted so top face (dust), front-left/right faces, and chamfers (edge wear) read clearly
                    GameObject cube = ArtLightingPreview.MeshObject(spec.SlotName + "_Cube", cubeMesh, mat, temporary);
                    cube.transform.SetPositionAndRotation(
                        new Vector3(x, 0.32f, -0.42f),
                        Quaternion.Euler(-10f, 32f, 6f));
                    temporary.Add(cube);

                    // Back row: elevated sphere showing smooth terminator, specular highlight, top dust, and emission
                    GameObject sphere = ArtLightingPreview.MeshObject(spec.SlotName + "_Sphere", sphereMesh, mat, temporary);
                    sphere.transform.SetPositionAndRotation(
                        new Vector3(x, 0.82f, 0.42f),
                        Quaternion.Euler(15f, -25f, 0f));
                    temporary.Add(sphere);
                }

                // Warm up Unity's light/shader pipeline once so the first captured pose has all lights registered
                WarmUpSceneRender();

                var poseSet = new CameraPoseSet
                {
                    width = SwatchWidth,
                    height = SwatchHeight,
                    postProcessing = true,
                    poses = new[]
                    {
                        ArtLightingPreview.Pose(
                            "board_full",
                            new[] { 0f, 2.45f, -7.65f },
                            new[] { 0f, 0.48f, 0.0f },
                            48f),
                        ArtLightingPreview.Pose(
                            "board_left_closeup",
                            new[] { -3.10f, 1.62f, -3.95f },
                            new[] { -2.85f, 0.52f, 0.0f },
                            42f),
                        ArtLightingPreview.Pose(
                            "board_right_closeup",
                            new[] { 3.10f, 1.62f, -3.95f },
                            new[] { 2.85f, 0.52f, 0.0f },
                            42f)
                    }
                };

                return CaptureAutomation.CapturePoses(poseSet, outputDir, "r1_swatch") > 0;
            }
            finally
            {
                temporary.Dispose();
            }
        }

        private static void WarmUpSceneRender()
        {
            var camObj = new GameObject("[R1WarmUpCamera]") { hideFlags = HideFlags.HideAndDontSave };
            var rt = RenderTexture.GetTemporary(64, 64, 24, RenderTextureFormat.DefaultHDR);
            try
            {
                var cam = camObj.AddComponent<Camera>();
                cam.transform.position = new Vector3(0f, 2.25f, -6.65f);
                cam.transform.LookAt(new Vector3(0f, 0.48f, 0f));
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(camObj);
            }
        }

        private static void SetupNightSwatchEnvironment(ArtLightingPreview.TemporaryObjects temporary)
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.22f, 0.35f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.12f, 0.15f, 0.24f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.08f, 0.14f, 1f);

            // Cold directional Earthlight matching the lunar basin night
            Light earthlight = ArtLightingPreview.NewLight(
                "Earthlight",
                LightType.Directional,
                new Color(0.72f, 0.80f, 1.00f, 1f),
                0.95f);
            earthlight.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
            earthlight.shadows = LightShadows.Soft;
            RenderSettings.sun = earthlight;
            temporary.Add(earthlight.gameObject);

            // Warm work lamps across the board span so all 11 columns receive consistent warm-vs-cold contrast
            Light warmLeft = ArtLightingPreview.NewLight(
                "WarmWorkLightLeft",
                LightType.Point,
                new Color(1.00f, 0.72f, 0.42f, 1f),
                6.5f);
            warmLeft.transform.position = new Vector3(-3.6f, 2.3f, -2.4f);
            warmLeft.range = 14f;
            warmLeft.shadows = LightShadows.Soft;
            temporary.Add(warmLeft.gameObject);

            Light warmCenter = ArtLightingPreview.NewLight(
                "WarmWorkLightCenter",
                LightType.Point,
                new Color(1.00f, 0.74f, 0.45f, 1f),
                5.0f);
            warmCenter.transform.position = new Vector3(0.0f, 2.4f, -2.5f);
            warmCenter.range = 14f;
            warmCenter.shadows = LightShadows.None;
            temporary.Add(warmCenter.gameObject);

            Light warmRight = ArtLightingPreview.NewLight(
                "WarmWorkLightRight",
                LightType.Point,
                new Color(1.00f, 0.74f, 0.46f, 1f),
                6.5f);
            warmRight.transform.position = new Vector3(3.6f, 2.3f, -2.4f);
            warmRight.range = 14f;
            warmRight.shadows = LightShadows.None;
            temporary.Add(warmRight.gameObject);

            // Post-processing volume matching Palette.BloomThreshold (1.25) + Neutral tonemapping
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            temporary.Add(profile);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(Palette.BloomThreshold);
            bloom.intensity.Override(0.65f);
            bloom.scatter.Override(0.65f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var volumeObject = new GameObject("R1SwatchVolume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            temporary.Add(volumeObject);
        }

        private static Mesh CreateUvSphereMesh(float radius, int lonSegments, int latSegments)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            for (int lat = 0; lat <= latSegments; lat++)
            {
                float v = (float)lat / latSegments;
                float theta = v * Mathf.PI;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                for (int lon = 0; lon <= lonSegments; lon++)
                {
                    float u = (float)lon / lonSegments;
                    float phi = u * Mathf.PI * 2f;
                    var dir = new Vector3(
                        sinTheta * Mathf.Cos(phi),
                        cosTheta,
                        sinTheta * Mathf.Sin(phi));
                    vertices.Add(dir * radius);
                    normals.Add(dir);
                    uvs.Add(new Vector2(u, 1f - v));
                }
            }

            int stride = lonSegments + 1;
            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int i0 = lat * stride + lon;
                    int i1 = i0 + 1;
                    int i2 = i0 + stride;
                    int i3 = i2 + 1;

                    triangles.Add(i0);
                    triangles.Add(i1);
                    triangles.Add(i2);

                    triangles.Add(i1);
                    triangles.Add(i3);
                    triangles.Add(i2);
                }
            }

            var mesh = new Mesh { name = "R1SwatchSphere" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateBevelledCubeMesh(float size, float bevel)
        {
            const int Grid = 10;
            float half = size * 0.5f;
            float inner = Mathf.Max(half - bevel, half * 0.5f);

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            Vector3[] faceNormals =
            {
                Vector3.forward,
                Vector3.back,
                Vector3.right,
                Vector3.left,
                Vector3.up,
                Vector3.down
            };
            Vector3[] faceRights =
            {
                Vector3.left,
                Vector3.right,
                Vector3.forward,
                Vector3.back,
                Vector3.right,
                Vector3.right
            };
            Vector3[] faceUps =
            {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.forward,
                Vector3.back
            };

            int stride = Grid + 1;
            for (int f = 0; f < 6; f++)
            {
                int baseVertex = vertices.Count;
                Vector3 n = faceNormals[f];
                Vector3 r = faceRights[f];
                Vector3 u = faceUps[f];

                for (int iy = 0; iy <= Grid; iy++)
                {
                    float tv = (float)iy / Grid;
                    float py = Mathf.Lerp(-half, half, tv);
                    for (int ix = 0; ix <= Grid; ix++)
                    {
                        float tu = (float)ix / Grid;
                        float px = Mathf.Lerp(-half, half, tu);

                        Vector3 cubePt = r * px + u * py + n * half;
                        Vector3 corePt = new Vector3(
                            Mathf.Clamp(cubePt.x, -inner, inner),
                            Mathf.Clamp(cubePt.y, -inner, inner),
                            Mathf.Clamp(cubePt.z, -inner, inner));
                        Vector3 delta = cubePt - corePt;
                        Vector3 normal = delta.sqrMagnitude > 1e-6f ? delta.normalized : n;
                        Vector3 pos = corePt + normal * bevel;

                        vertices.Add(pos);
                        normals.Add(normal);
                        uvs.Add(new Vector2(tu, tv));
                    }
                }

                for (int iy = 0; iy < Grid; iy++)
                {
                    for (int ix = 0; ix < Grid; ix++)
                    {
                        int i0 = baseVertex + iy * stride + ix;
                        int i1 = i0 + 1;
                        int i2 = i0 + stride;
                        int i3 = i2 + 1;

                        triangles.Add(i0);
                        triangles.Add(i2);
                        triangles.Add(i1);

                        triangles.Add(i1);
                        triangles.Add(i2);
                        triangles.Add(i3);
                    }
                }
            }

            var mesh = new Mesh { name = "R1SwatchBevelCube" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void EnsureFolderExists(string assetFolder)
        {
            string normalized = assetFolder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            string parent = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolderExists(parent);
                AssetDatabase.CreateFolder(parent, Path.GetFileName(normalized));
            }
        }
    }
}
