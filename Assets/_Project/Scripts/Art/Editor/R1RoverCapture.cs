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
using MoonProject.Rover;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Batch Editor capture helper for 07 v3 (<c>Assets/_Project/Generated/Art/R1/Models/Rover/RoverModel.prefab</c>).
    /// Captures the generated 07 prefab under neutral studio light, under the game's earthlit night at home in
    /// <c>Main.unity</c>, and in the four orthographic turnaround views composited with <c>turnaround.png</c> into
    /// <c>Logs/renders/compare_unity.png</c>.
    /// </summary>
    public static class R1RoverCapture
    {
        public const string RoverPrefabPath = "Assets/_Project/Generated/Art/R1/Models/Rover/RoverModel.prefab";

        private const int PerspectiveSize = 1024;
        private const int OrthoRenderSize = 1024;
        private const float OrthoHalfHeightM = 1.20f;
        private const float OrthoScaleM = 2.40f;
        private const float OrthoTargetY = 0.78f;
        private const float OrthoCamDist = 6.0f;

        private const int TurnaroundWidth = 1376;
        private const int TurnaroundHeight = 768;
        private const float TurnaroundPxPerM = 194.0f;
        private const int TurnaroundGroundYFromTop = 539;
        private const int FrontCx = 164;
        private const int SideCx = 502;
        private const int BackCx = 865;
        private const int TopCx = 1194;
        private const int TopCyFromTop = 366;

        private static readonly Color32 TurnaroundBgSrgb = new Color32(52, 60, 71, 255);
        private static readonly int MoonHazeColorId = Shader.PropertyToID("_MoonHazeColor");
        private static readonly int MoonHazeId = Shader.PropertyToID("_MoonHaze");
        private static readonly int MoonDesaturateId = Shader.PropertyToID("_MoonDesaturate");

        /// <summary>
        /// Batch entry point invoked via:
        /// <c>python tools/unity_batch.py exec --method MoonProject.Art.Editor.R1RoverCapture.Capture</c>
        /// </summary>
        public static void Capture()
        {
            BatchRunner.Run(nameof(R1RoverCapture), args =>
            {
                AssetDatabase.Refresh();
                R1MaterialLibrary.BuildMaterials();
                R1ModelBuilder.Build();

                var roverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoverPrefabPath);
                if (roverPrefab == null)
                {
                    Debug.LogError($"R1RoverCapture: Missing generated prefab at '{RoverPrefabPath}'.");
                    return false;
                }

                string rendersDir = Path.GetFullPath("Logs/renders");
                Directory.CreateDirectory(rendersDir);

                bool studioOk = CaptureStudioAndTurnaround(roverPrefab, rendersDir);
                bool mainOk = CaptureMainSceneEarthlitNight(roverPrefab, rendersDir);

                string batchOutDir = Path.Combine(args.OutputDirectory, "r1_rover");
                Directory.CreateDirectory(batchOutDir);
                foreach (string name in new[]
                {
                    "compare_unity.png",
                    "unity_studio_front_3q.png",
                    "unity_studio_back_3q.png",
                    "unity_upgraded_front_3q.png",
                    "unity_upgraded_back_3q.png",
                    "unity_main_front_3q.png",
                    "unity_main_back_3q.png",
                })
                {
                    string src = Path.Combine(rendersDir, name);
                    if (File.Exists(src))
                    {
                        File.Copy(src, Path.Combine(batchOutDir, name), true);
                    }
                }

                return studioOk && mainOk;
            });
        }

        private static bool CaptureStudioAndTurnaround(GameObject roverPrefab, string rendersDir)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temporary = new ArtLightingPreview.TemporaryObjects();
            bool prevAsync = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            try
            {
                SetupNeutralStudioLighting(temporary, out GameObject floorObj, out GameObject wallObj);
                var roverInstance = (GameObject)PrefabUtility.InstantiatePrefab(roverPrefab);
                roverInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                temporary.Add(roverInstance);

                ApplyProceduralCrtFace(roverInstance, temporary);

                // 1. Capture 3/4 front and 3/4 back studio renders with neutral studio backdrop active
                RenderPerspectiveShot(
                    new Vector3(-1.85f, 1.38f, 2.25f),
                    new Vector3(0.0f, 0.78f, 0.08f),
                    36f,
                    PerspectiveSize,
                    PerspectiveSize,
                    Path.Combine(rendersDir, "unity_studio_front_3q.png"));

                RenderPerspectiveShot(
                    new Vector3(-1.95f, 1.42f, -2.25f),
                    new Vector3(0.0f, 0.76f, -0.05f),
                    36f,
                    PerspectiveSize,
                    PerspectiveSize,
                    Path.Combine(rendersDir, "unity_studio_back_3q.png"));

                // 2. Hide floor/wall backdrop and capture the 4 orthographic turnaround views over slate background
                if (floorObj != null)
                {
                    floorObj.SetActive(false);
                }

                if (wallObj != null)
                {
                    wallObj.SetActive(false);
                }

                Color bgSrgb = new Color(
                    TurnaroundBgSrgb.r / 255f,
                    TurnaroundBgSrgb.g / 255f,
                    TurnaroundBgSrgb.b / 255f,
                    1f);

                // Note: In Unity's left-handed camera space:
                // - Front camera at (0, y, +dist) looking -Z has camera.right = -X (so rover's left cheek Eye is on +u right);
                //   flipX=true puts Eye/Antenna on the left (-u), matching turnaround.png Front view.
                // - Side camera at (-dist, y, 0) looking +X at the left flank has camera.right = -Z (rear on right);
                //   flipX=true makes the rover face right (+Z on right) while showing left-flank Eye & SagePatch_RL.
                // - Back camera at (0, y, -dist) looking +Z has camera.right = +X (Eye on left);
                //   flipX=true puts Eye/Antenna on the right (+u), matching turnaround.png Back view.
                // - Top camera at (0, +dist, 0) looking -Y with up=(0,0,-1) has front +Z at bottom and camera.right = -X;
                //   flipX=true puts Antenna/Wheel_RL on the left (-u) and DigScoop at bottom, matching turnaround.png Top view.
                Texture2D frontTex = RenderOrthoView(
                    new Vector3(0f, OrthoTargetY, OrthoCamDist),
                    new Vector3(0f, OrthoTargetY, 0f),
                    Vector3.up,
                    bgSrgb,
                    true);
                temporary.Add(frontTex);

                Texture2D sideTex = RenderOrthoView(
                    new Vector3(-OrthoCamDist, OrthoTargetY, 0f),
                    new Vector3(0f, OrthoTargetY, 0f),
                    Vector3.up,
                    bgSrgb,
                    true);
                temporary.Add(sideTex);

                Texture2D backTex = RenderOrthoView(
                    new Vector3(0f, OrthoTargetY, -OrthoCamDist),
                    new Vector3(0f, OrthoTargetY, 0f),
                    Vector3.up,
                    bgSrgb,
                    true);
                temporary.Add(backTex);

                Texture2D topTex = RenderOrthoView(
                    new Vector3(0f, OrthoCamDist, 0f),
                    new Vector3(0f, 0f, 0f),
                    Vector3.back,
                    bgSrgb,
                    true);
                temporary.Add(topTex);

                string comparePath = Path.Combine(rendersDir, "compare_unity.png");
                bool compareOk = BuildTurnaroundComparison(frontTex, sideTex, backTex, topTex, comparePath, temporary);

                // 3. Re-enable backdrop, mount all 10 R1 upgrade kits + Kit_TapeDeck, and capture upgraded studio shots
                if (floorObj != null)
                {
                    floorObj.SetActive(true);
                }

                if (wallObj != null)
                {
                    wallObj.SetActive(true);
                }

                MountAllUpgrades(roverInstance, temporary);

                RenderPerspectiveShot(
                    new Vector3(-1.95f, 1.45f, 2.38f),
                    new Vector3(0.0f, 0.78f, 0.05f),
                    36f,
                    PerspectiveSize,
                    PerspectiveSize,
                    Path.Combine(rendersDir, "unity_upgraded_front_3q.png"));

                RenderPerspectiveShot(
                    new Vector3(2.05f, 1.48f, -2.42f),
                    new Vector3(0.0f, 0.76f, -0.08f),
                    36f,
                    PerspectiveSize,
                    PerspectiveSize,
                    Path.Combine(rendersDir, "unity_upgraded_back_3q.png"));

                return compareOk;
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = prevAsync;
                temporary.Dispose();
            }
        }

        private static void MountAllUpgrades(GameObject roverInstance, ArtLightingPreview.TemporaryObjects temporary)
        {
            foreach (string flippedSocket in new[] { "DrumSocket_L", "BinSocket_L", "Neck/NeckLift/Head/RingSocket" })
            {
                Transform s = roverInstance.transform.Find(flippedSocket);
                if (s != null)
                {
                    s.localRotation = Quaternion.Euler(0f, 180f, 0f);
                }
            }

            const string folder = "Assets/_Project/Generated/Art/R1/Models/Rover/";
            (string kit, string socketPath)[] mounts =
            {
                ("Kit_LampBar", "HeadlampSocket"),
                ("Kit_CapacitorDrum", "DrumSocket_L"),
                ("Kit_CapacitorDrum", "DrumSocket_R"),
                ("Kit_BatterySpine", "SpineSocket"),
                ("Kit_TetherRing", "Neck/NeckLift/Head/RingSocket"),
                ("Kit_CargoRack", "CargoSocket"),
                ("Kit_SideBin", "BinSocket_L"),
                ("Kit_SideBin", "BinSocket_R"),
                ("HoverCoils", "CoilSocket"),
                ("FocusLens", "Neck/NeckLift/Head/Eye/LensSocket"),
                ("LensGlass", "Neck/NeckLift/Head/Eye"),
                ("Kit_TapeDeck", "Neck/NeckLift/Head/DeckSocket"),
            };

            foreach ((string kitName, string socketPath) in mounts)
            {
                Transform socket = roverInstance.transform.Find(socketPath);
                GameObject kitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + kitName + ".prefab");
                if (socket == null || kitPrefab == null)
                {
                    continue;
                }

                var kitInstance = (GameObject)PrefabUtility.InstantiatePrefab(kitPrefab, socket);
                kitInstance.transform.localPosition = Vector3.zero;
                kitInstance.transform.localRotation = Quaternion.identity;
                kitInstance.transform.localScale = Vector3.one;
                temporary.Add(kitInstance);
            }

            foreach (string giftPath in new[] { "Body/Decal07Fresh", "SolarWing/CellFilled", "Antenna/Pennant" })
            {
                Transform gift = roverInstance.transform.Find(giftPath);
                if (gift != null)
                {
                    gift.gameObject.SetActive(true);
                }
            }
        }

        private static bool CaptureMainSceneEarthlitNight(GameObject roverPrefab, string rendersDir)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CaptureAutomation.MainScenePath) == null)
            {
                Debug.LogError($"R1RoverCapture: Missing scene at '{CaptureAutomation.MainScenePath}'.");
                return false;
            }

            EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
            var temporary = new ArtLightingPreview.TemporaryObjects();
            var hiddenObjects = new List<GameObject>();
            bool prevAsync = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            try
            {
                GameObject existingRover = FindSceneObject("[Rover]");
                if (existingRover != null && existingRover.activeSelf)
                {
                    existingRover.SetActive(false);
                    hiddenObjects.Add(existingRover);
                }

                DynamicGI.UpdateEnvironment();

                Vector3 homePos = new Vector3(0.4f, 0.0f, 2.2f);
                Quaternion homeRot = Quaternion.Euler(0f, 185f, 0f);
                var roverInstance = (GameObject)PrefabUtility.InstantiatePrefab(roverPrefab);
                roverInstance.transform.SetPositionAndRotation(homePos, homeRot);
                temporary.Add(roverInstance);

                ApplyProceduralCrtFace(roverInstance, temporary);

                // Warm home porch fill light + rover headlamp spot so earthlit night & warm base glow both read clearly
                Light homeWarmLight = ArtLightingPreview.NewLight(
                    "R1HomePorchWarmLight",
                    LightType.Point,
                    new Color(1.0f, 0.72f, 0.42f, 1f),
                    3.8f);
                homeWarmLight.transform.position = homePos + homeRot * new Vector3(-2.1f, 1.85f, 1.6f);
                homeWarmLight.range = 9.0f;
                homeWarmLight.shadows = LightShadows.Soft;
                temporary.Add(homeWarmLight.gameObject);

                Light moonRimFill = ArtLightingPreview.NewLight(
                    "R1EarthlightRimFill",
                    LightType.Point,
                    new Color(0.62f, 0.72f, 0.96f, 1f),
                    2.2f);
                moonRimFill.transform.position = homePos + homeRot * new Vector3(2.2f, 2.1f, -2.2f);
                moonRimFill.range = 10.0f;
                moonRimFill.shadows = LightShadows.None;
                temporary.Add(moonRimFill.gameObject);

                Vector3 frontCamPos = homePos + homeRot * new Vector3(-1.85f, 1.38f, 2.25f);
                Vector3 frontLookAt = homePos + homeRot * new Vector3(0.0f, 0.78f, 0.08f);
                RenderPerspectiveShot(
                    frontCamPos,
                    frontLookAt,
                    36f,
                    PerspectiveSize,
                    PerspectiveSize,
                    Path.Combine(rendersDir, "unity_main_front_3q.png"));

                Vector3 backCamPos = homePos + homeRot * new Vector3(-1.95f, 1.42f, -2.25f);
                Vector3 backLookAt = homePos + homeRot * new Vector3(0.0f, 0.76f, -0.05f);
                RenderPerspectiveShot(
                    backCamPos,
                    backLookAt,
                    36f,
                    PerspectiveSize,
                    PerspectiveSize,
                    Path.Combine(rendersDir, "unity_main_back_3q.png"));

                return true;
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = prevAsync;
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

        private static void SetupNeutralStudioLighting(
            ArtLightingPreview.TemporaryObjects temporary,
            out GameObject floorObj,
            out GameObject wallObj)
        {
            floorObj = null;
            wallObj = null;

            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.38f, 0.40f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.28f, 0.30f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.20f, 0.22f, 1f);
            RenderSettings.ambientIntensity = 1.0f;

            Shader.SetGlobalVector(MoonHazeColorId, Vector4.zero);
            Shader.SetGlobalVector(MoonHazeId, Vector4.zero);
            Shader.SetGlobalVector(MoonDesaturateId, new Vector4(10000f, 1f, 0f, 0f));

            Light keyLight = ArtLightingPreview.NewLight(
                "R1RoverStudioKey",
                LightType.Directional,
                new Color(1.0f, 0.98f, 0.95f, 1f),
                1.25f);
            keyLight.transform.rotation = Quaternion.Euler(42f, 145f, 0f);
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.68f;
            RenderSettings.sun = keyLight;
            temporary.Add(keyLight.gameObject);

            Light fillLight = ArtLightingPreview.NewLight(
                "R1RoverStudioFill",
                LightType.Directional,
                new Color(0.90f, 0.94f, 1.0f, 1f),
                0.55f);
            fillLight.transform.rotation = Quaternion.Euler(28f, -135f, 0f);
            fillLight.shadows = LightShadows.None;
            temporary.Add(fillLight.gameObject);

            Light backRim = ArtLightingPreview.NewLight(
                "R1RoverStudioRim",
                LightType.Directional,
                new Color(0.98f, 0.96f, 0.92f, 1f),
                0.52f);
            backRim.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
            backRim.shadows = LightShadows.None;
            temporary.Add(backRim.gameObject);

            DynamicGI.UpdateEnvironment();

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
            {
                var backdropMat = new Material(urpLit) { name = "R1RoverStudioBackdrop" };
                backdropMat.SetColor("_BaseColor", new Color(0.22f, 0.25f, 0.29f, 1f));
                backdropMat.SetFloat("_Smoothness", 0.08f);
                backdropMat.SetFloat("_Metallic", 0.0f);
                temporary.Add(backdropMat);

                floorObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floorObj.name = "R1RoverStudioFloor";
                UnityEngine.Object.DestroyImmediate(floorObj.GetComponent<Collider>());
                floorObj.transform.position = Vector3.zero;
                floorObj.transform.localScale = new Vector3(20f, 1f, 20f);
                floorObj.GetComponent<MeshRenderer>().sharedMaterial = backdropMat;
                temporary.Add(floorObj);

                wallObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                wallObj.name = "R1RoverStudioWall";
                UnityEngine.Object.DestroyImmediate(wallObj.GetComponent<Collider>());
                wallObj.transform.SetPositionAndRotation(
                    new Vector3(0f, 10f, -6.5f),
                    Quaternion.Euler(90f, 0f, 0f));
                wallObj.transform.localScale = new Vector3(20f, 1f, 10f);
                wallObj.GetComponent<MeshRenderer>().sharedMaterial = backdropMat;
                temporary.Add(wallObj);
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            temporary.Add(profile);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.45f);
            bloom.scatter.Override(0.65f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var volumeObj = new GameObject("R1RoverStudioVolume");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            temporary.Add(volumeObj);
        }

        private static void ApplyProceduralCrtFace(
            GameObject roverInstance,
            ArtLightingPreview.TemporaryObjects temporary)
        {
            Transform screenNode = FindChildRecursive(roverInstance.transform, "Screen");
            if (screenNode == null)
            {
                return;
            }

            var renderer = screenNode.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var tuning = ScriptableObject.CreateInstance<FaceTuning>();
            temporary.Add(tuning);

            var display = screenNode.GetComponent<FaceDisplay>();
            if (display == null)
            {
                display = screenNode.gameObject.AddComponent<FaceDisplay>();
            }

            display.Initialize(tuning, renderer, 42);
            display.State.SnapToFeeling(FaceFeeling.Calm);
            display.ApplyToRenderer();
        }

        private static void RenderPerspectiveShot(
            Vector3 camPos,
            Vector3 lookAt,
            float fov,
            int width,
            int height,
            string outputPath)
        {
            var camObj = new GameObject("[R1RoverPerspCamera]") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(
                    TurnaroundBgSrgb.r / 255f,
                    TurnaroundBgSrgb.g / 255f,
                    TurnaroundBgSrgb.b / 255f,
                    1f);
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

                Texture2D tex = RenderCameraToTexture(cam, width, height);
                try
                {
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
                UnityEngine.Object.DestroyImmediate(camObj);
            }
        }

        private static Texture2D RenderOrthoView(
            Vector3 camPos,
            Vector3 lookAt,
            Vector3 up,
            Color bgSrgb,
            bool flipHorizontal)
        {
            var camObj = new GameObject("[R1RoverOrthoCamera]") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = OrthoHalfHeightM;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = bgSrgb;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 50f;
                cam.allowHDR = true;
                cam.transform.position = camPos;
                cam.transform.rotation = Quaternion.LookRotation(lookAt - camPos, up);

                UniversalAdditionalCameraData camData = cam.GetUniversalAdditionalCameraData();
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.None;
                camData.renderShadows = true;

                Texture2D raw = RenderCameraToTexture(cam, OrthoRenderSize, OrthoRenderSize);
                if (!flipHorizontal)
                {
                    return raw;
                }

                Color32[] src = raw.GetPixels32();
                var dst = new Color32[src.Length];
                for (int y = 0; y < OrthoRenderSize; y++)
                {
                    int rowStart = y * OrthoRenderSize;
                    for (int x = 0; x < OrthoRenderSize; x++)
                    {
                        dst[rowStart + x] = src[rowStart + (OrthoRenderSize - 1 - x)];
                    }
                }

                raw.SetPixels32(dst);
                raw.Apply(false);
                return raw;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camObj);
            }
        }

        private static Texture2D RenderCameraToTexture(Camera cam, int width, int height)
        {
            var warmRt = RenderTexture.GetTemporary(
                64,
                64,
                24,
                RenderTextureFormat.DefaultHDR,
                RenderTextureReadWrite.Default);
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

            var rt = RenderTexture.GetTemporary(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            RenderTexture prevActive = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                cam.Render();
                cam.targetTexture = null;

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply(false);
                return tex;
            }
            finally
            {
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static bool BuildTurnaroundComparison(
            Texture2D frontTex,
            Texture2D sideTex,
            Texture2D backTex,
            Texture2D topTex,
            string outputPath,
            ArtLightingPreview.TemporaryObjects temporary)
        {
            string repoRoot = Path.GetFullPath(".");
            string parentDir = Directory.GetParent(repoRoot)?.FullName ?? repoRoot;
            string conceptPath = Path.Combine(parentDir, "agy-face", "Logs", "v3", "turnaround.png");
            if (!File.Exists(conceptPath))
            {
                Debug.LogError($"R1RoverCapture: Concept turnaround not found at '{conceptPath}'.");
                return false;
            }

            var conceptTex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            temporary.Add(conceptTex);
            if (!conceptTex.LoadImage(File.ReadAllBytes(conceptPath)))
            {
                Debug.LogError($"R1RoverCapture: Failed to decode '{conceptPath}'.");
                return false;
            }

            int totalHeight = TurnaroundHeight * 2;
            var compTex = new Texture2D(TurnaroundWidth, totalHeight, TextureFormat.RGBA32, false, false);
            temporary.Add(compTex);

            var compPixels = new Color32[TurnaroundWidth * totalHeight];
            for (int i = 0; i < compPixels.Length; i++)
            {
                compPixels[i] = TurnaroundBgSrgb;
            }

            // Copy concept turnaround into top half (y in [TurnaroundHeight .. 2*TurnaroundHeight - 1] in Unity bottom-up coords)
            Color32[] conceptPixels = conceptTex.GetPixels32();
            int copyW = Mathf.Min(TurnaroundWidth, conceptTex.width);
            int copyH = Mathf.Min(TurnaroundHeight, conceptTex.height);
            for (int y = 0; y < copyH; y++)
            {
                int srcRow = y * conceptTex.width;
                int dstRow = (TurnaroundHeight + y) * TurnaroundWidth;
                for (int x = 0; x < copyW; x++)
                {
                    compPixels[dstRow + x] = conceptPixels[srcRow + x];
                }
            }

            // Scale ortho renders from 426.67 px/m down to 194.0 px/m (scaledSize = 466 px)
            float pxPerMRender = OrthoRenderSize / OrthoScaleM;
            float scaleRatio = TurnaroundPxPerM / pxPerMRender;
            int scaledSize = Mathf.RoundToInt(OrthoRenderSize * scaleRatio);
            float halfScaled = scaledSize * 0.5f;
            int groundRowFromTopInScaled = Mathf.RoundToInt(halfScaled + OrthoTargetY * TurnaroundPxPerM);
            int sideViewsTopFromTop = TurnaroundGroundYFromTop - groundRowFromTopInScaled;
            int topViewTopFromTop = Mathf.RoundToInt(TurnaroundRoverTopCyOffset(halfScaled));

            PasteScaledOrthoView(compPixels, frontTex, scaledSize, Mathf.RoundToInt(FrontCx - halfScaled), sideViewsTopFromTop);
            PasteScaledOrthoView(compPixels, sideTex, scaledSize, Mathf.RoundToInt(SideCx - halfScaled), sideViewsTopFromTop);
            PasteScaledOrthoView(compPixels, backTex, scaledSize, Mathf.RoundToInt(BackCx - halfScaled), sideViewsTopFromTop);
            PasteScaledOrthoView(compPixels, topTex, scaledSize, Mathf.RoundToInt(TopCx - halfScaled), topViewTopFromTop);

            // Subtle alignment dashed lines on the bottom half at Ground (0.0m), Tyre Top (0.70m), Deck (0.91m), Head Top (1.55m)
            DrawDashedAlignmentLine(compPixels, 0.00f, new Color32(110, 125, 145, 255));
            DrawDashedAlignmentLine(compPixels, 0.70f, new Color32(94, 110, 130, 255));
            DrawDashedAlignmentLine(compPixels, 0.91f, new Color32(94, 110, 130, 255));
            DrawDashedAlignmentLine(compPixels, 1.55f, new Color32(125, 140, 160, 255));

            // 1 px separator line between top concept row and bottom Unity row
            int sepRow = TurnaroundHeight * TurnaroundWidth;
            var sepCol = new Color32(82, 92, 107, 255);
            for (int x = 0; x < TurnaroundWidth; x++)
            {
                compPixels[sepRow + x] = sepCol;
            }

            compTex.SetPixels32(compPixels);
            compTex.Apply(false);
            File.WriteAllBytes(outputPath, compTex.EncodeToPNG());
            BatchRunner.Log($"capture {outputPath}");
            return true;
        }

        private static float TurnaroundRoverTopCyOffset(float halfScaled)
        {
            return TopCyFromTop - halfScaled;
        }

        private static void PasteScaledOrthoView(
            Color32[] destSheet,
            Texture2D srcTex,
            int scaledSize,
            int destLeftX,
            int destTopFromTop)
        {
            Color[] srcPixels = srcTex.GetPixels();
            int srcSize = srcTex.width;
            for (int sy = 0; sy < scaledSize; sy++)
            {
                // sy is row from top (0 = top of scaled view)
                int sheetRowFromTop = destTopFromTop + sy;
                if (sheetRowFromTop < 0 || sheetRowFromTop >= TurnaroundHeight)
                {
                    continue;
                }

                // Bottom half in Unity bottom-up coords has y = (TurnaroundHeight - 1 - sheetRowFromTop)
                int dstY = TurnaroundHeight - 1 - sheetRowFromTop;
                float vNorm = 1f - ((sy + 0.5f) / scaledSize);
                float fy = Mathf.Clamp(vNorm * srcSize - 0.5f, 0f, srcSize - 1f);
                int y0 = Mathf.FloorToInt(fy);
                int y1 = Mathf.Min(y0 + 1, srcSize - 1);
                float wy = fy - y0;

                for (int sx = 0; sx < scaledSize; sx++)
                {
                    int dstX = destLeftX + sx;
                    if (dstX < 0 || dstX >= TurnaroundWidth)
                    {
                        continue;
                    }

                    float uNorm = (sx + 0.5f) / scaledSize;
                    float fx = Mathf.Clamp(uNorm * srcSize - 0.5f, 0f, srcSize - 1f);
                    int x0 = Mathf.FloorToInt(fx);
                    int x1 = Mathf.Min(x0 + 1, srcSize - 1);
                    float wx = fx - x0;

                    Color c00 = srcPixels[y0 * srcSize + x0];
                    Color c10 = srcPixels[y0 * srcSize + x1];
                    Color c01 = srcPixels[y1 * srcSize + x0];
                    Color c11 = srcPixels[y1 * srcSize + x1];
                    Color c = Color.Lerp(Color.Lerp(c00, c10, wx), Color.Lerp(c01, c11, wx), wy);

                    // Skip pure background pixels so neighboring views never overwrite each other
                    int dr = Mathf.Abs(Mathf.RoundToInt(c.r * 255f) - TurnaroundBgSrgb.r);
                    int dg = Mathf.Abs(Mathf.RoundToInt(c.g * 255f) - TurnaroundBgSrgb.g);
                    int db = Mathf.Abs(Mathf.RoundToInt(c.b * 255f) - TurnaroundBgSrgb.b);
                    if (dr <= 2 && dg <= 2 && db <= 2)
                    {
                        continue;
                    }

                    destSheet[dstY * TurnaroundWidth + dstX] = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255),
                        255);
                }
            }
        }

        private static void DrawDashedAlignmentLine(Color32[] destSheet, float heightMeters, Color32 lineColor)
        {
            int rowFromTop = Mathf.RoundToInt(TurnaroundGroundYFromTop - heightMeters * TurnaroundPxPerM);
            if (rowFromTop < 0 || rowFromTop >= TurnaroundHeight)
            {
                return;
            }

            int dstY = TurnaroundHeight - 1 - rowFromTop;
            int rowOffset = dstY * TurnaroundWidth;
            for (int x = 10; x < 1020; x += 10)
            {
                for (int dx = 0; dx < 5 && (x + dx) < 1020; dx++)
                {
                    Color32 cur = destSheet[rowOffset + x + dx];
                    if (Mathf.Abs(cur.r - TurnaroundBgSrgb.r) <= 3 &&
                        Mathf.Abs(cur.g - TurnaroundBgSrgb.g) <= 3 &&
                        Mathf.Abs(cur.b - TurnaroundBgSrgb.b) <= 3)
                    {
                        destSheet[rowOffset + x + dx] = lineColor;
                    }
                }
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

                Transform child = FindChildRecursive(root.transform, name);
                if (child != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform hit = FindChildRecursive(parent.GetChild(i), name);
                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }
    }
}
