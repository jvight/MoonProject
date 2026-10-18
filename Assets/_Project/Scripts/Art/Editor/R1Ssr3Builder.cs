using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonProject.Editor.Automation;
using MoonProject.Editor.Builders;
using MoonProject.Rover;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Registered <c>Art/R1Ssr3</c> builder (order 107, immediately after <c>Art/R1Models</c> at 106).
    /// Assembles the combined <c>Elder06.prefab</c> (<c>Assets/_Project/Generated/Art/R1/Models/Rover/Elder06.prefab</c>)
    /// from <c>SSR3_Chassis.prefab</c> + <c>SSR3_Topper_06.prefab</c> mounted in <c>Topper_Socket</c> + the relief
    /// stencil number <c>06</c> under <c>Unit_Number</c>, settled ~4 cm (<see cref="Elder06DustSettleMeters"/>) into the dust.
    /// Also provides batch capture entry point <see cref="Capture"/> for rendering <c>Elder06</c> next to <c>07</c>
    /// (<c>RoverModel.prefab</c>) in Day C and Eclipse light.
    /// </summary>
    public static class R1Ssr3Builder
    {
        public const string RoverSourceFolder = R1MaterialRecipes.ModelsFolder + "/Rover";
        public const string RoverGeneratedFolder = ArtPaths.R1ModelsFolder + "/Rover";

        public const string Ssr3ChassisFbxPath = RoverSourceFolder + "/SSR3_Chassis.fbx";
        public const string Ssr3Topper06FbxPath = RoverSourceFolder + "/SSR3_Topper_06.fbx";
        public const string Ssr3Topper05FbxPath = RoverSourceFolder + "/SSR3_Topper_05.fbx";

        public const string Ssr3ChassisPrefabPath = RoverGeneratedFolder + "/SSR3_Chassis.prefab";
        public const string Ssr3Topper06PrefabPath = RoverGeneratedFolder + "/SSR3_Topper_06.prefab";
        public const string Ssr3Topper05PrefabPath = RoverGeneratedFolder + "/SSR3_Topper_05.prefab";

        public const string Elder06PrefabPath = RoverGeneratedFolder + "/Elder06.prefab";
        public const string Elder06UnitNumberMeshPath = RoverGeneratedFolder + "/Elder06_UnitNumber06.asset";

        public const string Elder05PrefabPath = RoverGeneratedFolder + "/Elder05.prefab";
        public const string Elder05UnitNumberMeshPath = RoverGeneratedFolder + "/Elder05_UnitNumber05.asset";

        /// <summary>Vertical settle depth (~4 cm into lunar dust) applied to <c>Elder06.prefab</c> and <c>Elder05.prefab</c>.</summary>
        public const float Elder06DustSettleMeters = 0.04f;
        public const float Elder05DustSettleMeters = 0.04f;

        private const int CaptureWidth = 1440;
        private const int CaptureHeight = 810;

        private static readonly int MoonHazeColorId = Shader.PropertyToID("_MoonHazeColor");
        private static readonly int MoonHazeId = Shader.PropertyToID("_MoonHaze");
        private static readonly int MoonDesaturateId = Shader.PropertyToID("_MoonDesaturate");

        // Normalized (rx, ry, rw, rh) glyph bars in [0..1] x [0..1] for '0', '5', and '6'
        private static readonly Vector4[] Glyph0Rects =
        {
            new Vector4(0.18f, 0.50f, 0.22f, 0.88f),
            new Vector4(0.82f, 0.50f, 0.22f, 0.88f),
            new Vector4(0.50f, 0.90f, 0.76f, 0.20f),
            new Vector4(0.50f, 0.10f, 0.76f, 0.20f),
        };

        private static readonly Vector4[] Glyph5Rects =
        {
            new Vector4(0.50f, 0.90f, 0.78f, 0.20f),
            new Vector4(0.18f, 0.70f, 0.22f, 0.48f),
            new Vector4(0.50f, 0.50f, 0.78f, 0.18f),
            new Vector4(0.82f, 0.30f, 0.22f, 0.48f),
            new Vector4(0.50f, 0.10f, 0.78f, 0.20f),
        };

        private static readonly Vector4[] Glyph6Rects =
        {
            new Vector4(0.18f, 0.50f, 0.22f, 0.88f),
            new Vector4(0.50f, 0.90f, 0.76f, 0.20f),
            new Vector4(0.50f, 0.50f, 0.76f, 0.18f),
            new Vector4(0.82f, 0.30f, 0.22f, 0.48f),
            new Vector4(0.50f, 0.10f, 0.76f, 0.20f),
        };

        [MoonBuilder("Art/R1Ssr3", 107)]
        public static void Build()
        {
            GameObject chassisPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Ssr3ChassisPrefabPath);
            GameObject topper06Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Ssr3Topper06PrefabPath);
            GameObject topper05Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Ssr3Topper05PrefabPath);
            if (chassisPrefab == null || topper06Prefab == null || topper05Prefab == null)
            {
                R1MaterialLibrary.BuildMaterials();
                R1ModelBuilder.Build();
                chassisPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Ssr3ChassisPrefabPath);
                topper06Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Ssr3Topper06PrefabPath);
                topper05Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Ssr3Topper05PrefabPath);
            }

            if (chassisPrefab == null)
            {
                throw new InvalidOperationException(
                    $"R1Ssr3Builder: Missing '{Ssr3ChassisPrefabPath}'. Ensure '{Ssr3ChassisFbxPath}' is imported.");
            }

            if (topper06Prefab == null)
            {
                throw new InvalidOperationException(
                    $"R1Ssr3Builder: Missing '{Ssr3Topper06PrefabPath}'. Ensure '{Ssr3Topper06FbxPath}' is imported.");
            }

            if (topper05Prefab == null)
            {
                throw new InvalidOperationException(
                    $"R1Ssr3Builder: Missing '{Ssr3Topper05PrefabPath}'. Ensure '{Ssr3Topper05FbxPath}' is imported.");
            }

            BuildElderPrefab(
                chassisPrefab,
                topper06Prefab,
                "SSR3_Topper_06",
                "Elder06",
                "Unit_Number_06",
                "Elder06_UnitNumber06",
                new[] { Glyph0Rects, Glyph6Rects },
                Elder06UnitNumberMeshPath,
                Elder06PrefabPath,
                Elder06DustSettleMeters,
                hullLiftMeters: 0f);

            BuildElderPrefab(
                chassisPrefab,
                topper05Prefab,
                "SSR3_Topper_05",
                "Elder05",
                "Unit_Number_05",
                "Elder05_UnitNumber05",
                new[] { Glyph0Rects, Glyph5Rects },
                Elder05UnitNumberMeshPath,
                Elder05PrefabPath,
                Elder05DustSettleMeters,
                hullLiftMeters: 0f);
        }

        private static void BuildElderPrefab(
            GameObject chassisPrefab,
            GameObject topperPrefab,
            string topperNodeName,
            string elderName,
            string unitNodeName,
            string unitMeshName,
            Vector4[][] unitGlyphs,
            string unitMeshAssetPath,
            string elderPrefabPath,
            float dustSettleMeters,
            float hullLiftMeters)
        {
            GameObject elderRoot = UnityEngine.Object.Instantiate(chassisPrefab);
            elderRoot.name = elderName;
            try
            {
                Transform topperSocket = elderRoot.transform.Find("Topper_Socket");
                if (topperSocket == null)
                {
                    throw new InvalidOperationException("R1Ssr3Builder: SSR3_Chassis is missing 'Topper_Socket'.");
                }

                Transform unitNumberSocket = elderRoot.transform.Find("Unit_Number");
                if (unitNumberSocket == null)
                {
                    throw new InvalidOperationException("R1Ssr3Builder: SSR3_Chassis is missing 'Unit_Number'.");
                }

                // 1. Mount topper inside Topper_Socket
                GameObject topperInstance = UnityEngine.Object.Instantiate(topperPrefab);
                topperInstance.name = topperNodeName;
                topperInstance.transform.SetParent(topperSocket, worldPositionStays: false);
                topperInstance.transform.localPosition = Vector3.zero;
                topperInstance.transform.localRotation = Quaternion.identity;
                topperInstance.transform.localScale = Vector3.one;

                // 2. Build and mount the unit relief stencil mesh under Unit_Number
                var unitGo = new GameObject(unitNodeName);
                unitGo.transform.SetParent(unitNumberSocket, worldPositionStays: false);
                unitGo.transform.localPosition = Vector3.zero;
                unitGo.transform.localRotation = Quaternion.identity;
                unitGo.transform.localScale = Vector3.one;

                Mesh unitMesh = BuildUnitNumberMesh(unitGo.transform, unitMeshName, unitGlyphs);
                Mesh savedMesh = GeneratedAssets.CreateOrReplace(unitMesh, unitMeshAssetPath);

                MeshFilter mf = unitGo.AddComponent<MeshFilter>();
                mf.sharedMesh = savedMesh;

                MeshRenderer mr = unitGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = R1MaterialLibrary.LoadMaterial(R1MaterialSlot.GreyMetal);

                // 3. Apply dust settle (-dustSettleMeters) to all children, and optional suspension lift (+hullLiftMeters)
                // to the upper hull nodes (Body, Headlamp, Topper_Socket, Unit_Number) while keeping wheels on ground.
                Vector3 localDustShift = elderRoot.transform.InverseTransformVector(
                    new Vector3(0f, -dustSettleMeters, 0f));
                Vector3 localHullShift = elderRoot.transform.InverseTransformVector(
                    new Vector3(0f, hullLiftMeters - dustSettleMeters, 0f));
                int childCount = elderRoot.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    Transform child = elderRoot.transform.GetChild(i);
                    bool isWheel = child.name.StartsWith("Wheel_", StringComparison.Ordinal);
                    child.localPosition += isWheel ? localDustShift : localHullShift;
                }

                GeneratedAssets.SavePrefab(elderRoot, elderPrefabPath);
            }
            catch
            {
                if (elderRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(elderRoot);
                }

                throw;
            }
        }

        /// <summary>
        /// Batch capture entry point invoked via:
        /// <c>python tools/unity_batch.py -- -executeMethod MoonProject.Art.Editor.R1Ssr3Builder.Capture</c>
        /// Captures each elder next to <c>07</c> (<c>RoverModel.prefab</c>) in Day C and Eclipse light.
        /// </summary>
        public static void Capture()
        {
            BatchRunner.Run(nameof(R1Ssr3Builder), args =>
            {
                AssetDatabase.Refresh();
                R1MaterialLibrary.BuildMaterials();
                R1ModelBuilder.Build();
                Build();

                string capturesDir = Path.GetFullPath("Logs/captures");
                Directory.CreateDirectory(capturesDir);

                bool prevAsync = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = false;
                try
                {
                    CaptureElderNextTo07(Elder06PrefabPath, "elder06", capturesDir, nightEclipse: false);
                    CaptureElderNextTo07(Elder06PrefabPath, "elder06", capturesDir, nightEclipse: true);
                    CaptureElderNextTo07(Elder05PrefabPath, "elder05", capturesDir, nightEclipse: false);
                    CaptureElderNextTo07(Elder05PrefabPath, "elder05", capturesDir, nightEclipse: true);
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = prevAsync;
                }

                string batchOutDir = Path.Combine(args.OutputDirectory, "r1_ssr3");
                Directory.CreateDirectory(batchOutDir);
                foreach (string png in Directory.GetFiles(capturesDir, "elder0*_*.png"))
                {
                    File.Copy(png, Path.Combine(batchOutDir, Path.GetFileName(png)), true);
                }

                return true;
            });
        }

        private static void CaptureElderNextTo07(string elderPrefabPath, string slug, string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupCaptureEnvironment(temp, nightEclipse);

                // Spawn Rover 07 on the left and the Elder unit on the right
                SpawnRover07(temp, new Vector3(-0.92f, 0f, 0.05f), 156f, nightEclipse);
                SpawnPrefabWithPrefabRotation(elderPrefabPath, new Vector3(0.78f, 0f, 0.08f), 148f, temp);

                string suffix = nightEclipse ? "eclipse" : "dayC";
                RenderShot(
                    new Vector3(0.02f, 1.32f, -3.15f),
                    new Vector3(-0.02f, 0.66f, 0.05f),
                    40f,
                    bg,
                    Path.Combine(outDir, $"{slug}_next_to_07_{suffix}.png"));
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static Color SetupCaptureEnvironment(ArtLightingPreview.TemporaryObjects temp, bool nightEclipse)
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            Shader.SetGlobalVector(MoonHazeColorId, Vector4.zero);
            Shader.SetGlobalVector(MoonHazeId, Vector4.zero);
            Shader.SetGlobalVector(MoonDesaturateId, new Vector4(10000f, 1f, 0f, 0f));

            Color bgColor;
            if (nightEclipse)
            {
                RenderSettings.ambientSkyColor = new Color(0.11f, 0.14f, 0.22f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.08f, 0.10f, 0.16f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.05f, 0.06f, 0.10f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.05f, 0.07f, 0.11f, 1f);

                Light earthLight = ArtLightingPreview.NewLight(
                    "EclipseEarthlight",
                    LightType.Directional,
                    new Color(0.58f, 0.68f, 0.95f, 1f),
                    0.58f);
                earthLight.transform.rotation = Quaternion.Euler(28f, 38f, 0f);
                earthLight.shadows = LightShadows.Soft;
                earthLight.shadowStrength = 0.75f;
                RenderSettings.sun = earthLight;
                temp.Add(earthLight.gameObject);

                Light rimLight = ArtLightingPreview.NewLight(
                    "EclipseRimFill",
                    LightType.Directional,
                    new Color(0.42f, 0.52f, 0.78f, 1f),
                    0.28f);
                rimLight.transform.rotation = Quaternion.Euler(22f, -142f, 0f);
                rimLight.shadows = LightShadows.None;
                temp.Add(rimLight.gameObject);
            }
            else
            {
                RenderSettings.ambientSkyColor = new Color(0.32f, 0.35f, 0.42f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.25f, 0.26f, 0.30f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.18f, 0.18f, 0.20f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.14f, 0.16f, 0.20f, 1f);

                Light sunLight = ArtLightingPreview.NewLight(
                    "DayCGoldenSun",
                    LightType.Directional,
                    new Color(1.00f, 0.91f, 0.76f, 1f),
                    1.45f);
                sunLight.transform.rotation = Quaternion.Euler(22f, 38f, 0f);
                sunLight.shadows = LightShadows.Soft;
                sunLight.shadowStrength = 0.78f;
                RenderSettings.sun = sunLight;
                temp.Add(sunLight.gameObject);

                Light fillLight = ArtLightingPreview.NewLight(
                    "DayCCoolFill",
                    LightType.Directional,
                    new Color(0.70f, 0.78f, 0.94f, 1f),
                    0.45f);
                fillLight.transform.rotation = Quaternion.Euler(35f, -142f, 0f);
                fillLight.shadows = LightShadows.None;
                temp.Add(fillLight.gameObject);
            }

            DynamicGI.UpdateEnvironment();

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "R1RegolithFloor";
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(12f, 1f, 12f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = R1MaterialLibrary.LoadMaterial(R1MaterialSlot.Basalt);
            temp.Add(floor);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            temp.Add(profile);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(Palette.BloomThreshold);
            bloom.intensity.Override(nightEclipse ? 0.65f : 0.45f);
            bloom.scatter.Override(0.65f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var volumeObj = new GameObject("R1Ssr3CaptureVolume");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            temp.Add(volumeObj);

            return bgColor;
        }

        private static GameObject SpawnRover07(
            ArtLightingPreview.TemporaryObjects temp,
            Vector3 position,
            float yawDeg,
            bool nightEclipse)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(R1RoverCapture.RoverPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"R1Ssr3Builder: Missing '{R1RoverCapture.RoverPrefabPath}'.");
            }

            var rover = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rover.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDeg, 0f));
            temp.Add(rover);

            Transform screenNode = FindChildRecursive(rover.transform, "Screen");
            if (screenNode != null)
            {
                var renderer = screenNode.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var tuning = ScriptableObject.CreateInstance<FaceTuning>();
                    temp.Add(tuning);
                    var display = screenNode.GetComponent<FaceDisplay>();
                    if (display == null)
                    {
                        display = screenNode.gameObject.AddComponent<FaceDisplay>();
                    }

                    display.Initialize(tuning, renderer, 42);
                    display.State.SnapToFeeling(FaceFeeling.Calm);
                    display.ApplyToRenderer();
                }
            }

            if (nightEclipse)
            {
                Light eyeWarmFill = ArtLightingPreview.NewLight(
                    "Rover07EyeFill",
                    LightType.Point,
                    new Color(1.0f, 0.68f, 0.30f, 1f),
                    1.3f);
                eyeWarmFill.transform.position = position + Quaternion.Euler(0f, yawDeg, 0f) * new Vector3(0f, 1.25f, 1.15f);
                eyeWarmFill.range = 4.2f;
                eyeWarmFill.shadows = LightShadows.None;
                temp.Add(eyeWarmFill.gameObject);
            }

            return rover;
        }

        private static GameObject SpawnPrefabWithPrefabRotation(
            string prefabPath,
            Vector3 position,
            float yawDeg,
            ArtLightingPreview.TemporaryObjects temp)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"R1Ssr3Builder: Missing '{prefabPath}'.");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(0f, yawDeg, 0f) * prefab.transform.localRotation);
            temp.Add(instance);
            return instance;
        }

        private static void RenderShot(Vector3 camPos, Vector3 lookAt, float fov, Color bgColor, string outputPath)
        {
            var camObj = new GameObject("[R1Ssr3CaptureCamera]") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = bgColor;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 2000f;
                cam.fieldOfView = fov;
                cam.allowHDR = true;
                cam.transform.position = camPos;
                cam.transform.rotation = Quaternion.LookRotation(lookAt - camPos, Vector3.up);

                UniversalAdditionalCameraData camData = cam.GetUniversalAdditionalCameraData();
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.renderShadows = true;

                var warmRt = RenderTexture.GetTemporary(64, 64, 24, RenderTextureFormat.DefaultHDR);
                try
                {
                    cam.targetTexture = warmRt;
                    cam.Render();
                    cam.targetTexture = null;
                }
                finally
                {
                    RenderTexture.ReleaseTemporary(warmRt);
                }

                var rt = RenderTexture.GetTemporary(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.DefaultHDR);
                RenderTexture prevActive = RenderTexture.active;
                try
                {
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = null;

                    RenderTexture.active = rt;
                    var tex = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false, false);
                    try
                    {
                        tex.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0, false);
                        tex.Apply(false);
                        File.WriteAllBytes(outputPath, tex.EncodeToPNG());
                        BatchRunner.Log($"capture {outputPath}");
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(tex);
                    }
                }
                finally
                {
                    RenderTexture.active = prevActive;
                    RenderTexture.ReleaseTemporary(rt);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camObj);
            }
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (string.Equals(root.name, name, StringComparison.Ordinal))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform hit = FindChildRecursive(root.GetChild(i), name);
                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }

        private static Mesh BuildUnitNumberMesh(
            Transform targetTransform,
            string meshName,
            Vector4[][] glyphs)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            const float centerY = 0.505f;
            const float centerZ = 0.26f;
            const float charW = 0.042f;
            const float charH = 0.068f;
            const float reliefX = 0.005f;
            const float stepZ = charW * 1.30f;

            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                float sideX = sx * 0.442f;
                float dirZ = sx > 0f ? 1f : -1f;
                float startZ = centerZ - 0.5f * stepZ * dirZ;

                for (int g = 0; g < glyphs.Length; g++)
                {
                    float czChar = startZ + g * stepZ * dirZ;
                    Vector4[] rects = glyphs[g];
                    for (int r = 0; r < rects.Length; r++)
                    {
                        Vector4 rect = rects[r];
                        float localDz = (rect.x - 0.5f) * charW * dirZ;
                        float localDy = (rect.y - 0.5f) * charH;
                        Vector3 boxCenterWorld = new Vector3(
                            sideX + sx * (reliefX * 0.4f),
                            centerY + localDy,
                            czChar + localDz);
                        Vector3 boxSizeWorld = new Vector3(
                            reliefX,
                            rect.w * charH,
                            rect.z * charW);
                        AppendWorldBox(
                            targetTransform,
                            boxCenterWorld,
                            boxSizeWorld,
                            vertices,
                            normals,
                            uvs,
                            colors,
                            triangles);
                    }
                }
            }

            var mesh = new Mesh
            {
                name = meshName,
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AppendWorldBox(
            Transform targetTransform,
            Vector3 centerWorld,
            Vector3 sizeWorld,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<Color> colors,
            List<int> triangles)
        {
            Vector3 h = sizeWorld * 0.5f;
            Vector3[] c =
            {
                centerWorld + new Vector3(-h.x, -h.y, -h.z),
                centerWorld + new Vector3( h.x, -h.y, -h.z),
                centerWorld + new Vector3( h.x,  h.y, -h.z),
                centerWorld + new Vector3(-h.x,  h.y, -h.z),
                centerWorld + new Vector3(-h.x, -h.y,  h.z),
                centerWorld + new Vector3( h.x, -h.y,  h.z),
                centerWorld + new Vector3( h.x,  h.y,  h.z),
                centerWorld + new Vector3(-h.x,  h.y,  h.z),
            };

            (int a, int b, int d, int e, Vector3 n)[] faces =
            {
                (0, 3, 2, 1, Vector3.back),
                (4, 5, 6, 7, Vector3.forward),
                (0, 4, 7, 3, Vector3.left),
                (1, 2, 6, 5, Vector3.right),
                (3, 7, 6, 2, Vector3.up),
                (0, 1, 5, 4, Vector3.down),
            };

            // Weathering vertex color: R=AO (0.82), G=wear (0.18), B=settled dust (0.42)
            var vcol = new Color(0.82f, 0.18f, 0.42f, 1.0f);

            for (int f = 0; f < faces.Length; f++)
            {
                (int i0, int i1, int i2, int i3, Vector3 worldNormal) = faces[f];
                int baseIdx = vertices.Count;
                vertices.Add(targetTransform.InverseTransformPoint(c[i0]));
                vertices.Add(targetTransform.InverseTransformPoint(c[i1]));
                vertices.Add(targetTransform.InverseTransformPoint(c[i2]));
                vertices.Add(targetTransform.InverseTransformPoint(c[i3]));

                Vector3 localNormal = targetTransform.InverseTransformDirection(worldNormal).normalized;
                normals.Add(localNormal);
                normals.Add(localNormal);
                normals.Add(localNormal);
                normals.Add(localNormal);

                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));

                colors.Add(vcol);
                colors.Add(vcol);
                colors.Add(vcol);
                colors.Add(vcol);

                triangles.Add(baseIdx + 0);
                triangles.Add(baseIdx + 1);
                triangles.Add(baseIdx + 2);
                triangles.Add(baseIdx + 0);
                triangles.Add(baseIdx + 2);
                triangles.Add(baseIdx + 3);
            }
        }
    }
}
