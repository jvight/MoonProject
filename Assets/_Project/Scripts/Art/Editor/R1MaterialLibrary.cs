using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using MoonProject.Editor.Automation;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Registered <c>Art/R1Materials</c> builder (order 105) that generates all 12 R1 stylized-realism material assets
    /// under <c>Assets/_Project/Generated/Art/R1/Materials/</c>, and renders the two swatch-board captures (at home in
    /// <c>Main.unity</c> under earthlit night lighting, and under a neutral grey studio light) into <c>Logs/captures/</c>.
    /// </summary>
    public static class R1MaterialLibrary
    {
        private const int SwatchWidth = 1600;
        private const int SwatchHeight = 900;
        private const float ColumnSpacing = 1.00f;
        private const float SphereRadius = 0.35f;
        private const float CubeSize = 0.62f;
        private const float CubeBevel = 0.085f;

        private static readonly int MoonHazeColorId = Shader.PropertyToID("_MoonHazeColor");
        private static readonly int MoonHazeId = Shader.PropertyToID("_MoonHaze");
        private static readonly int MoonDesaturateId = Shader.PropertyToID("_MoonDesaturate");

        /// <summary>
        /// Batch entry point that runs the <c>Art/R1Materials</c> builder and captures both the <c>Main.unity</c>
        /// earthlit night swatch board and the neutral grey studio swatch board into <c>Logs/captures/</c>.
        /// </summary>
        public static void BuildAndCapture()
        {
            BatchRunner.Run(nameof(R1MaterialLibrary), args =>
            {
                BuildMaterials();
                string capturesDir = Path.GetFullPath("Logs/captures");
                Directory.CreateDirectory(capturesDir);
                foreach (string stale in Directory.GetFiles(capturesDir, "r1_swatch_*.png"))
                {
                    File.Delete(stale);
                }

                bool mainOk = CaptureMainSceneSwatchBoard(capturesDir);
                bool studioOk = CaptureStudioSwatchBoard(capturesDir);

                string batchOutDir = Path.Combine(args.OutputDirectory, "r1_swatches");
                Directory.CreateDirectory(batchOutDir);
                foreach (string png in Directory.GetFiles(capturesDir, "r1_swatch_*.png"))
                {
                    File.Copy(png, Path.Combine(batchOutDir, Path.GetFileName(png)), true);
                }

                return mainOk && studioOk;
            });
        }

        /// <summary>
        /// Builds all 12 R1 library materials into <c>Assets/_Project/Generated/Art/R1/Materials/</c> using
        /// <see cref="GeneratedAssets.CreateOrReplace{T}"/>. Throws <see cref="InvalidOperationException"/> if
        /// <c>R1Lit.shader</c> or any required texture asset is missing.
        /// </summary>
        [MoonBuilder("Art/R1Materials", 105)]
        public static void BuildMaterials()
        {
            Shader shader = LoadShader();
            IReadOnlyList<R1MaterialSpec> specs = R1MaterialRecipes.All;
            for (int i = 0; i < specs.Count; i++)
            {
                R1MaterialSpec spec = specs[i];
                Material material = CreateMaterial(shader, spec);
                GeneratedAssets.CreateOrReplace(material, R1MaterialRecipes.GetMaterialPath(spec));
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Loads the R1 <c>MoonProject/R1/Lit</c> shader strictly from <see cref="R1MaterialRecipes.ShaderPath"/>.
        /// Throws <see cref="InvalidOperationException"/> if the shader asset is missing.
        /// </summary>
        public static Shader LoadShader()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(R1MaterialRecipes.ShaderPath);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"R1Lit shader is missing at '{R1MaterialRecipes.ShaderPath}' ({R1MaterialRecipes.ShaderName}).");
            }

            return shader;
        }

        /// <summary>
        /// Creates an in-memory <see cref="Material"/> configured for <paramref name="spec"/>. Throws
        /// <see cref="InvalidOperationException"/> if <paramref name="shader"/> or any required texture is missing.
        /// </summary>
        public static Material CreateMaterial(
            Shader shader,
            R1MaterialSpec spec,
            Func<string, Texture2D> textureLoader = null)
        {
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"Cannot create R1 material '{spec.MaterialAssetName}': R1Lit shader is null.");
            }

            var material = new Material(shader) { name = spec.MaterialAssetName };
            try
            {
                ConfigureMaterial(material, shader, spec, textureLoader);
                return material;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(material);
                throw;
            }
        }

        /// <summary>
        /// Loads the generated library <see cref="Material"/> for <paramref name="slot"/> from
        /// <c>Assets/_Project/Generated/Art/R1/Materials/</c>.
        /// </summary>
        public static Material LoadMaterial(R1MaterialSlot slot)
        {
            R1MaterialSpec spec = R1MaterialRecipes.Get(slot);
            string matPath = R1MaterialRecipes.GetMaterialPath(spec);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                throw new InvalidOperationException(
                    $"R1 material asset is missing at '{matPath}': run the Art/R1Materials builder first.");
            }

            return mat;
        }

        /// <summary>
        /// Configures <paramref name="material"/> with the textures and PBR/procedural parameters of
        /// <paramref name="spec"/>. Throws <see cref="InvalidOperationException"/> if any required texture is missing.
        /// </summary>
        public static void ConfigureMaterial(
            Material material,
            Shader shader,
            R1MaterialSpec spec,
            Func<string, Texture2D> textureLoader = null)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"Cannot configure R1 material '{spec.MaterialAssetName}': R1Lit shader is null.");
            }

            Func<string, Texture2D> loadTex = textureLoader ?? AssetDatabase.LoadAssetAtPath<Texture2D>;

            string albedoPath = R1MaterialRecipes.GetAlbedoTexturePath(spec);
            string normalPath = R1MaterialRecipes.GetNormalTexturePath(spec);
            string maskPath = R1MaterialRecipes.GetMaskTexturePath(spec);
            string emissionPath = R1MaterialRecipes.GetEmissionTexturePath(spec);

            Texture2D baseMap = loadTex(albedoPath);
            if (baseMap == null)
            {
                throw new InvalidOperationException(
                    $"R1 material '{spec.MaterialAssetName}' is missing required albedo texture at '{albedoPath}'.");
            }

            Texture2D normalMap = loadTex(normalPath);
            if (normalMap == null)
            {
                throw new InvalidOperationException(
                    $"R1 material '{spec.MaterialAssetName}' is missing required normal texture at '{normalPath}'.");
            }

            Texture2D maskMap = loadTex(maskPath);
            if (maskMap == null)
            {
                throw new InvalidOperationException(
                    $"R1 material '{spec.MaterialAssetName}' is missing required mask texture at '{maskPath}'.");
            }

            Texture2D emissionMap = null;
            if (spec.HasEmission)
            {
                emissionMap = loadTex(emissionPath);
                if (emissionMap == null)
                {
                    throw new InvalidOperationException(
                        $"Emissive R1 material '{spec.MaterialAssetName}' is missing required emission texture at '{emissionPath}'.");
                }
            }

            material.shader = shader;
            material.name = spec.MaterialAssetName;

            material.SetTexture("_BaseMap", baseMap);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTextureOffset("_BaseMap", Vector2.zero);
            // Each surface's sRGB palette colour is baked into _BaseMap (including multi-tone slots such as R1_SolarCell);
            // _BaseColor stays white so R1LitCore does not square the colour in linear space.
            material.SetColor("_BaseColor", Color.white);

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
            material.SetFloat("_EdgeWearThreshold", 0.52f);
            material.SetFloat("_EdgeWearSharpness", 4.0f);
            material.SetFloat("_EdgeWearMetallic", spec.EdgeWearMetallic);
            material.SetFloat("_EdgeWearSmoothness", spec.EdgeWearSmoothness);

            material.SetColor("_DustColor", R1MaterialRecipes.DefaultDustColor);
            material.SetFloat("_DustAmount", spec.DustAmount);
            material.SetFloat("_DustSharpness", 2.0f);
            material.SetFloat("_DustUpThreshold", 0.12f);
            material.SetFloat("_DustOcclusionWeight", 0.42f);
            material.SetFloat("_DustSmoothness", 0.12f);

            material.SetFloat("_ColorVarScale", 1.8f);
            material.SetFloat("_ColorVarAmount", 0.025f);

            material.SetTexture("_EmissionMap", emissionMap);
            material.SetColor("_EmissionColor", spec.EmissionColor);
            material.SetFloat("_EmissionStrength", spec.EmissionStrength);
            material.SetFloat("_BloomSafeMax", R1MaterialRecipes.DefaultBloomSafeMax);
            material.SetFloat("_HotCoreThreshold", R1MaterialRecipes.DefaultHotCoreThreshold);
            material.SetFloat("_HotCorePeak", R1MaterialRecipes.DefaultHotCorePeak);

            material.globalIlluminationFlags = spec.HasEmission
                ? MaterialGlobalIlluminationFlags.RealtimeEmissive
                : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            material.enableInstancing = true;
        }

        /// <summary>
        /// Opens the built <c>Main.unity</c> scene, places the 12-slot R1 swatch board at home under the game's real
        /// earthlit night lighting (look A), and saves PNG captures into <paramref name="outputDir"/>.
        /// </summary>
        public static bool CaptureMainSceneSwatchBoard(string outputDir)
        {
            Directory.CreateDirectory(outputDir);
            EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);

            var temporary = new ArtLightingPreview.TemporaryObjects();
            var hiddenObjects = new List<GameObject>();
            foreach (string tempHideName in new[] { "[Rover]", "Bell" })
            {
                GameObject obj = FindSceneObject(tempHideName);
                if (obj != null && obj.activeSelf)
                {
                    obj.SetActive(false);
                    hiddenObjects.Add(obj);
                }
            }

            try
            {
                DynamicGI.UpdateEnvironment();
                PopulateSwatchBoard(temporary);
                WarmUpSceneRender();
                CameraPoseSet poseSet = CreateSwatchPoseSet();
                return CaptureAutomation.CapturePoses(poseSet, outputDir, "r1_swatch_main") > 0;
            }
            finally
            {
                temporary.Dispose();
                for (int i = 0; i < hiddenObjects.Count; i++)
                {
                    if (hiddenObjects[i] != null)
                    {
                        hiddenObjects[i].SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Stages the 12-slot R1 swatch board in a neutral grey studio environment (achromatic lighting and neutral
        /// grey backdrop, no blue skybox fallback) for judging surface colour, and saves PNG captures into
        /// <paramref name="outputDir"/>.
        /// </summary>
        public static bool CaptureStudioSwatchBoard(string outputDir)
        {
            Directory.CreateDirectory(outputDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var temporary = new ArtLightingPreview.TemporaryObjects();
            try
            {
                SetupNeutralStudioEnvironment(temporary);
                PopulateSwatchBoard(temporary);
                WarmUpSceneRender();
                CameraPoseSet poseSet = CreateSwatchPoseSet();
                return CaptureAutomation.CapturePoses(poseSet, outputDir, "r1_swatch_studio") > 0;
            }
            finally
            {
                temporary.Dispose();
            }
        }

        private static void PopulateSwatchBoard(ArtLightingPreview.TemporaryObjects temporary)
        {
            Mesh sphereMesh = CreateUvSphereMesh(SphereRadius, 36, 24);
            Mesh cubeMesh = CreateBevelledCubeMesh(CubeSize, CubeBevel);
            Mesh plinthMesh = CreateBevelledCubeMesh(1f, 0.03f);
            temporary.Add(sphereMesh);
            temporary.Add(cubeMesh);
            temporary.Add(plinthMesh);

            Material plinthMat = LoadMaterial(R1MaterialSlot.GreyMetal);
            GameObject plinth = ArtLightingPreview.MeshObject("SwatchPlinth", plinthMesh, plinthMat, temporary);
            plinth.transform.position = new Vector3(0f, 0.18f, 0.15f);
            plinth.transform.localScale = new Vector3(13.4f, 0.32f, 3.2f);
            temporary.Add(plinth);

            IReadOnlyList<R1MaterialSpec> specs = R1MaterialRecipes.All;
            float halfSpan = (specs.Count - 1) * 0.5f * ColumnSpacing;

            for (int i = 0; i < specs.Count; i++)
            {
                R1MaterialSpec spec = specs[i];
                Material mat = LoadMaterial(spec.Slot);
                float x = i * ColumnSpacing - halfSpan;

                // Front row: bevelled cube with baked vertex colours (R=AO, G=convex bevel curvature, B=cavity dust)
                GameObject cube = ArtLightingPreview.MeshObject(spec.SlotName + "_Cube", cubeMesh, mat, temporary);
                cube.transform.SetPositionAndRotation(
                    new Vector3(x, 0.66f, -0.42f),
                    Quaternion.Euler(-12f, 32f, 6f));
                temporary.Add(cube);

                // Back row: elevated sphere showing smooth terminator, specular highlight, top dust, and emission
                GameObject sphere = ArtLightingPreview.MeshObject(spec.SlotName + "_Sphere", sphereMesh, mat, temporary);
                sphere.transform.SetPositionAndRotation(
                    new Vector3(x, 1.16f, 0.42f),
                    Quaternion.Euler(15f, -25f, 0f));
                temporary.Add(sphere);
            }
        }

        private static CameraPoseSet CreateSwatchPoseSet()
        {
            return new CameraPoseSet
            {
                width = SwatchWidth,
                height = SwatchHeight,
                postProcessing = true,
                poses = new[]
                {
                    ArtLightingPreview.Pose(
                        "board_full",
                        new[] { 0f, 2.85f, -8.15f },
                        new[] { 0f, 0.82f, 0.0f },
                        48f),
                    ArtLightingPreview.Pose(
                        "board_left_closeup",
                        new[] { -3.00f, 2.02f, -4.15f },
                        new[] { -3.00f, 0.86f, 0.0f },
                        44f),
                    ArtLightingPreview.Pose(
                        "board_right_closeup",
                        new[] { 3.00f, 2.02f, -4.15f },
                        new[] { 3.00f, 0.86f, 0.0f },
                        44f)
                }
            };
        }

        private static void SetupNeutralStudioEnvironment(ArtLightingPreview.TemporaryObjects temporary)
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.34f, 0.34f, 0.34f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
            RenderSettings.ambientIntensity = 1.0f;

            // Reset any global moon haze/desaturation vectors left by Main.unity so studio colours are un-tinted
            Shader.SetGlobalVector(MoonHazeColorId, Vector4.zero);
            Shader.SetGlobalVector(MoonHazeId, Vector4.zero);
            Shader.SetGlobalVector(MoonDesaturateId, new Vector4(10000f, 1f, 0f, 0f));

            // Pure neutral D65 white key light + soft neutral fill light for accurate colour judgement
            Light keyLight = ArtLightingPreview.NewLight(
                "StudioKeyLight",
                LightType.Directional,
                Color.white,
                1.15f);
            keyLight.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.72f;
            RenderSettings.sun = keyLight;
            temporary.Add(keyLight.gameObject);

            Light fillLight = ArtLightingPreview.NewLight(
                "StudioFillLight",
                LightType.Directional,
                Color.white,
                0.38f);
            fillLight.transform.rotation = Quaternion.Euler(22f, 148f, 0f);
            fillLight.shadows = LightShadows.None;
            temporary.Add(fillLight.gameObject);

            DynamicGI.UpdateEnvironment();

            // Full-frustum neutral matte grey studio floor and back wall cyclorama (drops the flat blue skybox fallback)
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
            {
                var backdropMat = new Material(urpLit) { name = "R1StudioBackdrop" };
                backdropMat.SetColor("_BaseColor", new Color(0.28f, 0.28f, 0.28f, 1f));
                backdropMat.SetFloat("_Smoothness", 0.06f);
                backdropMat.SetFloat("_Metallic", 0.0f);
                temporary.Add(backdropMat);

                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "StudioFloor";
                UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
                floor.transform.position = new Vector3(0f, 0.0f, 0f);
                floor.transform.localScale = new Vector3(20f, 1f, 20f);
                floor.GetComponent<MeshRenderer>().sharedMaterial = backdropMat;
                temporary.Add(floor);

                GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Plane);
                backWall.name = "StudioBackWall";
                UnityEngine.Object.DestroyImmediate(backWall.GetComponent<Collider>());
                backWall.transform.SetPositionAndRotation(
                    new Vector3(0f, 10f, 6.5f),
                    Quaternion.Euler(-90f, 0f, 0f));
                backWall.transform.localScale = new Vector3(20f, 1f, 10f);
                backWall.GetComponent<MeshRenderer>().sharedMaterial = backdropMat;
                temporary.Add(backWall);
            }

            // Neutral tonemapping + bloom above Palette.BloomThreshold
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            temporary.Add(profile);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(Palette.BloomThreshold);
            bloom.intensity.Override(0.50f);
            bloom.scatter.Override(0.65f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var volumeObject = new GameObject("R1StudioVolume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            temporary.Add(volumeObject);
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

        private static GameObject FindSceneObject(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }

                Transform child = root.transform.Find(name);
                if (child != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Builds a UV sphere mesh with baked vertex colours (<c>R = lower-hemisphere AO</c>, <c>G = 0</c>, <c>B = 0</c>).
        /// </summary>
        public static Mesh CreateUvSphereMesh(float radius, int lonSegments, int latSegments)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
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

                    float ao = Mathf.Lerp(0.65f, 1.0f, Mathf.Clamp01(dir.y * 0.5f + 0.5f));
                    colors.Add(new Color(ao, 0f, 0f, 1f));
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
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Builds a bevelled cube mesh with baked vertex colours (<c>R = ambient occlusion</c>,
        /// <c>G = convex bevel curvature for edge wear</c>, <c>B = crease/cavity dust</c>, <c>A = 1</c>) so that
        /// edge wear visibly sits along its bevelled edges while flat face interiors stay clean.
        /// </summary>
        public static Mesh CreateBevelledCubeMesh(float size, float bevel)
        {
            float half = size * 0.5f;
            float clampedBevel = Mathf.Clamp(bevel, 0.005f, half * 0.45f);
            float inner = half - clampedBevel;

            // Non-uniform 1D sample profile concentrating vertices across both bevel shoulders and the flat face
            float[] samples = BuildBevelProfileSamples(half, inner);
            int grid = samples.Length - 1;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
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

            int stride = grid + 1;
            for (int f = 0; f < 6; f++)
            {
                int baseVertex = vertices.Count;
                Vector3 n = faceNormals[f];
                Vector3 r = faceRights[f];
                Vector3 u = faceUps[f];

                for (int iy = 0; iy <= grid; iy++)
                {
                    float py = samples[iy];
                    float tv = (py + half) / size;
                    for (int ix = 0; ix <= grid; ix++)
                    {
                        float px = samples[ix];
                        float tu = (px + half) / size;

                        Vector3 cubePt = r * px + u * py + n * half;
                        Vector3 corePt = new Vector3(
                            Mathf.Clamp(cubePt.x, -inner, inner),
                            Mathf.Clamp(cubePt.y, -inner, inner),
                            Mathf.Clamp(cubePt.z, -inner, inner));
                        Vector3 delta = cubePt - corePt;
                        Vector3 normal = delta.sqrMagnitude > 1e-6f ? delta.normalized : n;
                        Vector3 pos = corePt + normal * clampedBevel;

                        // Convex curvature G: 0 on flat face interior (|px| <= inner && |py| <= inner),
                        // rising smoothly to 1 along the 12 bevelled edges and 8 corners.
                        float bx = Mathf.Clamp01((Mathf.Abs(px) - inner) / clampedBevel);
                        float by = Mathf.Clamp01((Mathf.Abs(py) - inner) / clampedBevel);
                        float bevelEdge = Mathf.Max(bx, by);
                        float cornerBoost = bx * by * 0.2f;
                        float edgeWearG = bevelEdge > 0f
                            ? Mathf.Clamp01(Mathf.SmoothStep(0.05f, 0.90f, bevelEdge) + cornerBoost)
                            : 0f;

                        // Ambient occlusion R and cavity dust B near the bottom contact of the cube
                        float height01 = Mathf.Clamp01((pos.y + half) / size);
                        float aoR = Mathf.Lerp(0.68f, 1.0f, Mathf.SmoothStep(0f, 0.35f, height01));
                        float cavityB = (1f - edgeWearG) * Mathf.Lerp(0.45f, 0.0f, Mathf.SmoothStep(0f, 0.30f, height01));

                        vertices.Add(pos);
                        normals.Add(normal);
                        uvs.Add(new Vector2(tu, tv));
                        colors.Add(new Color(aoR, edgeWearG, cavityB, 1f));
                    }
                }

                for (int iy = 0; iy < grid; iy++)
                {
                    for (int ix = 0; ix < grid; ix++)
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
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float[] BuildBevelProfileSamples(float half, float inner)
        {
            const int BevelSteps = 5;
            const int FlatSteps = 8;
            var list = new List<float>(BevelSteps * 2 + FlatSteps + 1);

            for (int i = 0; i < BevelSteps; i++)
            {
                float t = (float)i / BevelSteps;
                list.Add(Mathf.Lerp(-half, -inner, t));
            }

            for (int i = 0; i <= FlatSteps; i++)
            {
                float t = (float)i / FlatSteps;
                list.Add(Mathf.Lerp(-inner, inner, t));
            }

            for (int i = 1; i <= BevelSteps; i++)
            {
                float t = (float)i / BevelSteps;
                list.Add(Mathf.Lerp(inner, half, t));
            }

            return list.ToArray();
        }
    }
}
