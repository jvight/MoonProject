using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonProject.Editor.Automation;
using MoonProject.Rover;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Batch Editor capture runner for The Long Mare (<c>0.6</c>) R1 environment kit showcase sheet
    /// (<c>docs/art/concepts/0.6-mare/kit/kit_sheet.png</c>), rendering all 4 kit groups alongside Rover 07
    /// (<c>1.59 m</c> scale reference) under Low Sun Day C and Earthlit Night A lighting.
    /// </summary>
    public static class R1MareKitCapture
    {
        private const int PanelWidth = 1280;
        private const int PanelHeight = 720;
        private const int DividerPx = 8;

        private static readonly int MoonHazeColorId = Shader.PropertyToID("_MoonHazeColor");
        private static readonly int MoonHazeId = Shader.PropertyToID("_MoonHaze");
        private static readonly int MoonDesaturateId = Shader.PropertyToID("_MoonDesaturate");

        /// <summary>
        /// Batch entry point invoked via:
        /// <c>python tools/unity_batch.py exec --project . --method MoonProject.Art.Editor.R1MareKitCapture.BuildAndCapture</c>
        /// </summary>
        public static void BuildAndCapture()
        {
            BatchRunner.Run(nameof(R1MareKitCapture), args =>
            {
                AssetDatabase.Refresh();
                Shader litShader = R1MaterialLibrary.LoadShader();
                R1MaterialLibrary.BuildMaterials();
                R1MareKitBuilder.BuildMareMaterials(litShader);
                R1ModelBuilder.Build();
                AssetDatabase.SaveAssets();

                string capturesDir = Path.GetFullPath("Logs/captures");
                Directory.CreateDirectory(capturesDir);

                string kitDocsDir = Path.GetFullPath("docs/art/concepts/0.6-mare/kit");
                Directory.CreateDirectory(kitDocsDir);

                string dayOverviewPath = Path.Combine(capturesDir, "mare_kit_dayC_overview.png");
                string dayCloseupPath = Path.Combine(capturesDir, "mare_kit_dayC_closeup.png");
                string nightOverviewPath = Path.Combine(capturesDir, "mare_kit_nightA_overview.png");
                string nightCloseupPath = Path.Combine(capturesDir, "mare_kit_nightA_closeup.png");

                bool prevAsync = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = false;
                try
                {
                    CaptureMareStage(dayOverviewPath, dayCloseupPath, nightMode: false);
                    CaptureMareStage(nightOverviewPath, nightCloseupPath, nightMode: true);
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = prevAsync;
                }

                string sheetOutPath = Path.Combine(kitDocsDir, "kit_sheet.png");
                ComposeTwoByTwoSheet(
                    dayOverviewPath,
                    dayCloseupPath,
                    nightOverviewPath,
                    nightCloseupPath,
                    sheetOutPath);

                string logSheetCopy = Path.Combine(capturesDir, "mare_kit_sheet.png");
                File.Copy(sheetOutPath, logSheetCopy, true);
                BatchRunner.Log($"capture {sheetOutPath}");
                return true;
            });
        }

        private static void CaptureMareStage(string overviewPath, string closeupPath, bool nightMode)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupMareEnvironment(temp, nightMode);

                // 1. Ground & Decals: MareGround_Patch under hero area + Decal_MareSwirl + Decal_MareEjectaRay
                SpawnProp(R1MareKitBuilder.MareGroundPatchPrefabPath, new Vector3(0f, -0.02f, 2.0f), 0f, temp);
                SpawnProp(R1MareKitBuilder.DecalMareSwirlPrefabPath, new Vector3(1.1f, 0.005f, 1.2f), -36f, temp);
                SpawnProp(R1MareKitBuilder.DecalMareEjectaRayPrefabPath, new Vector3(-3.6f, 0.004f, 2.4f), 22f, temp);

                // 2. Rille Pass Modular Pieces (4 chunks framing the west-rim channel along the back horizon,
                //    rotated so their stepped basalt cliff faces and talus aprons face -Z toward the camera & low sun)
                SpawnProp(R1MareKitBuilder.RilleMouthCornerPrefabPath, new Vector3(-8.4f, 0f, 9.2f), -68f, temp);
                SpawnProp(R1MareKitBuilder.RilleWallStraightPrefabPath, new Vector3(-1.4f, 0f, 10.2f), -90f, temp);
                SpawnProp(R1MareKitBuilder.RilleWallCurvePrefabPath, new Vector3(6.8f, 0f, 9.6f), -102f, temp);
                SpawnProp(R1MareKitBuilder.RilleSlopeRampPrefabPath, new Vector3(8.6f, 0f, 3.8f), -25f, temp);

                // 3. Rock Kit: 6 dark Mare basalt boulders/outcrops (casting long raking shadows to the right)
                //    + 2 dark Ilmenite drill-target outcrops (facing the camera & low sun to catch bronze-black/gunmetal glint)
                SpawnProp(R1MareKitBuilder.MareBasaltRockPrefabPaths[0], new Vector3(-5.8f, 0f, 0.4f), 24f, temp);
                SpawnProp(R1MareKitBuilder.MareBasaltRockPrefabPaths[1], new Vector3(-4.6f, 0f, 2.8f), -34f, temp);
                SpawnProp(R1MareKitBuilder.MareBasaltRockPrefabPaths[2], new Vector3(-6.4f, 0f, 5.2f), 18f, temp);
                SpawnProp(R1MareKitBuilder.MareBasaltRockPrefabPaths[3], new Vector3(-2.8f, 0f, 5.8f), -16f, temp);
                SpawnProp(R1MareKitBuilder.MareBasaltRockPrefabPaths[4], new Vector3(5.6f, 0f, 5.4f), 38f, temp);
                SpawnProp(R1MareKitBuilder.MareBasaltRockPrefabPaths[5], new Vector3(-3.4f, 0f, -1.6f), 58f, temp);

                // Two dark Ilmenite (FeTiO3) outcrops with their stepped IlmeniteVein (+Z local) angled toward camera (-Z)
                SpawnProp(R1MareKitBuilder.IlmeniteOutcrop00PrefabPath, new Vector3(-2.35f, 0f, 0.85f), 156f, temp);
                SpawnProp(R1MareKitBuilder.IlmeniteOutcrop01PrefabPath, new Vector3(2.85f, 0f, 4.40f), -152f, temp);

                // 4. Mare Derelict Set:
                //    - Stopped Convoy (3 haul bots in a clear diagonal line on the right, middle with StrappedLoad)
                //    - Crashed Survey Drone (4 De Laval RCS nozzles in furrow on front-left)
                //    - Half-Buried Tardigrade Capsule (front-center-right, TinyWindow facing camera)
                SpawnProp(R1MareKitBuilder.DerelictMareConvoyPrefabPath, new Vector3(2.85f, 0f, 0.65f), -142f, temp);
                SpawnProp(R1MareKitBuilder.DerelictMareSurveyDronePrefabPath, new Vector3(-1.85f, 0f, -1.75f), 146f, temp);
                GameObject capsule = SpawnProp(
                    R1MareKitBuilder.DerelictTardigradeCapsulePrefabPath,
                    new Vector3(0.95f, 0f, -2.05f),
                    162f,
                    temp);

                // Subtle micro-glow at TinyWindow at night so the tiny Beresheet window reads cleanly without bloom blowout
                if (nightMode)
                {
                    Transform winNode = capsule.transform.Find("Window_Anchor");
                    Vector3 winPos = winNode != null ? winNode.position : capsule.transform.position + new Vector3(0f, 0.26f, -0.25f);
                    Light tinyWinGlow = ArtLightingPreview.NewLight(
                        "TardigradeWindowMicroGlow",
                        LightType.Point,
                        new Color(1.00f, 0.72f, 0.30f, 1f),
                        0.16f);
                    tinyWinGlow.transform.position = winPos + new Vector3(0f, 0.03f, -0.06f);
                    tinyWinGlow.range = 0.55f;
                    tinyWinGlow.shadows = LightShadows.None;
                    temp.Add(tinyWinGlow.gameObject);
                }

                // 5. Rover 07 (1.59 m tall scale reference) in center-foreground at 3/4 front view facing toward camera-left
                SpawnRover07WithHeadlamp(temp, new Vector3(-0.15f, 0f, -0.85f), 148f, nightMode);

                // Shot 1: Wide Kit Overview (all 4 kit groups + Rover 07 for scale)
                RenderShot(
                    new Vector3(-0.20f, 3.85f, -8.40f),
                    new Vector3(0.10f, 0.75f, 1.80f),
                    45f,
                    bg,
                    overviewPath);

                // Shot 2: Hero Close-Up (Rover 07 in 3/4 front view framed between IlmeniteOutcrop_00,
                //         Derelict_MareConvoy with StrappedLoad, Derelict_MareSurveyDrone, and Derelict_TardigradeCapsule)
                RenderShot(
                    new Vector3(-0.15f, 2.05f, -5.20f),
                    new Vector3(0.20f, 0.55f, 0.15f),
                    42f,
                    bg,
                    closeupPath);
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static Color SetupMareEnvironment(ArtLightingPreview.TemporaryObjects temp, bool nightMode)
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            Shader.SetGlobalVector(MoonHazeColorId, Vector4.zero);
            Shader.SetGlobalVector(MoonHazeId, Vector4.zero);
            Shader.SetGlobalVector(MoonDesaturateId, new Vector4(10000f, 1f, 0f, 0f));

            Color bgColor;
            if (nightMode)
            {
                // Night A (Earthlit Mare): #05070E deep indigo-black sky + #9AB0D2 cool Earthshine key from front-left
                RenderSettings.ambientSkyColor = new Color(0.13f, 0.17f, 0.27f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.09f, 0.12f, 0.20f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.05f, 0.06f, 0.11f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.02f, 0.03f, 0.06f, 1f);

                Light earthLight = ArtLightingPreview.NewLight(
                    "NightAEarthshine",
                    LightType.Directional,
                    new Color(0.62f, 0.72f, 0.90f, 1f),
                    0.68f);
                earthLight.transform.rotation = Quaternion.Euler(24f, 34f, 0f);
                earthLight.shadows = LightShadows.Soft;
                earthLight.shadowStrength = 0.76f;
                RenderSettings.sun = earthLight;
                temp.Add(earthLight.gameObject);

                Light rimLight = ArtLightingPreview.NewLight(
                    "NightARimFill",
                    LightType.Directional,
                    new Color(0.44f, 0.54f, 0.78f, 1f),
                    0.26f);
                rimLight.transform.rotation = Quaternion.Euler(20f, -135f, 0f);
                rimLight.shadows = LightShadows.None;
                temp.Add(rimLight.gameObject);
            }
            else
            {
                // Low Sun Day C: 10.5 deg grazing polar sun (#FFF4E4) shining from front-left (yaw +46 deg)
                // so camera-facing surfaces are brightly raked and cast long crisp shadows across the Mare plain to +X/+Z
                RenderSettings.ambientSkyColor = new Color(0.30f, 0.34f, 0.44f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.22f, 0.25f, 0.34f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.14f, 0.16f, 0.22f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.03f, 0.04f, 0.08f, 1f);

                Light lowSun = ArtLightingPreview.NewLight(
                    "DayCLowPolarSun",
                    LightType.Directional,
                    new Color(1.00f, 0.95f, 0.86f, 1f),
                    1.75f);
                lowSun.transform.rotation = Quaternion.Euler(10.5f, 46f, 0f);
                lowSun.shadows = LightShadows.Soft;
                lowSun.shadowStrength = 0.85f;
                RenderSettings.sun = lowSun;
                temp.Add(lowSun.gameObject);

                Light earthFill = ArtLightingPreview.NewLight(
                    "DayCEarthshineFill",
                    LightType.Directional,
                    new Color(0.60f, 0.70f, 0.88f, 1f),
                    0.42f);
                earthFill.transform.rotation = Quaternion.Euler(34f, -48f, 0f);
                earthFill.shadows = LightShadows.None;
                temp.Add(earthFill.gameObject);
            }

            DynamicGI.UpdateEnvironment();

            // Broad Mare basalt sea floor using R1_MareBasalt
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "MareBasaltFloor";
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(16f, 1f, 16f);
            var mareBasaltMat = AssetDatabase.LoadAssetAtPath<Material>(R1MareKitBuilder.MareBasaltMatPath);
            floor.GetComponent<MeshRenderer>().sharedMaterial = mareBasaltMat != null
                ? mareBasaltMat
                : R1MaterialLibrary.LoadMaterial(R1MaterialSlot.Basalt);
            temp.Add(floor);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            temp.Add(profile);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(Palette.BloomThreshold);
            bloom.intensity.Override(nightMode ? 0.50f : 0.38f);
            bloom.scatter.Override(0.62f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var volumeObj = new GameObject("R1MareKitVolume");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            temp.Add(volumeObj);

            return bgColor;
        }

        private static GameObject SpawnRover07WithHeadlamp(
            ArtLightingPreview.TemporaryObjects temp,
            Vector3 position,
            float yawDeg,
            bool nightMode)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(R1RoverCapture.RoverPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"R1MareKitCapture: Missing Rover prefab at '{R1RoverCapture.RoverPrefabPath}'.");
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

            if (nightMode)
            {
                // Warm amber eye fill + headlamp cone illuminating the foreground derelicts & Ilmenite outcrop
                Light eyeFill = ArtLightingPreview.NewLight(
                    "Rover07EyeWarmFill",
                    LightType.Point,
                    new Color(1.0f, 0.70f, 0.32f, 1f),
                    1.25f);
                eyeFill.transform.position = position + Quaternion.Euler(0f, yawDeg, 0f) * new Vector3(0f, 1.22f, 1.05f);
                eyeFill.range = 4.2f;
                eyeFill.shadows = LightShadows.None;
                temp.Add(eyeFill.gameObject);

                // Headlamp cone aimed across MareOutcrop_Ilmenite_00 & Derelict_MareSurveyDrone
                Light headlampLeft = ArtLightingPreview.NewLight(
                    "Rover07HeadlampConeA",
                    LightType.Spot,
                    new Color(1.0f, 0.90f, 0.72f, 1f),
                    4.8f);
                Vector3 lampOrigin = position + new Vector3(0f, 1.10f, 0.10f);
                headlampLeft.transform.position = lampOrigin;
                headlampLeft.transform.rotation = Quaternion.LookRotation(new Vector3(-2.1f, 0.45f, 0.65f) - lampOrigin, Vector3.up);
                headlampLeft.range = 12f;
                headlampLeft.spotAngle = 64f;
                headlampLeft.innerSpotAngle = 32f;
                headlampLeft.shadows = LightShadows.Soft;
                temp.Add(headlampLeft.gameObject);

                // Secondary fill cone toward Derelict_MareConvoy so the strapped load reads at night
                Light headlampRight = ArtLightingPreview.NewLight(
                    "Rover07HeadlampConeB",
                    LightType.Spot,
                    new Color(1.0f, 0.88f, 0.70f, 1f),
                    3.6f);
                headlampRight.transform.position = lampOrigin;
                headlampRight.transform.rotation = Quaternion.LookRotation(new Vector3(2.6f, 0.50f, 0.65f) - lampOrigin, Vector3.up);
                headlampRight.range = 12f;
                headlampRight.spotAngle = 62f;
                headlampRight.innerSpotAngle = 30f;
                headlampRight.shadows = LightShadows.Soft;
                temp.Add(headlampRight.gameObject);
            }

            return rover;
        }

        private static GameObject SpawnProp(
            string prefabPath,
            Vector3 position,
            float yawDeg,
            ArtLightingPreview.TemporaryObjects temp)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"R1MareKitCapture: Missing generated prefab at '{prefabPath}'.");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(0f, yawDeg, 0f) * prefab.transform.localRotation);
            temp.Add(instance);
            return instance;
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

        private static void RenderShot(Vector3 camPos, Vector3 lookAt, float fov, Color bgColor, string outputPath)
        {
            var camObj = new GameObject("[R1MareKitCamera]") { hideFlags = HideFlags.HideAndDontSave };
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

                var rt = RenderTexture.GetTemporary(PanelWidth, PanelHeight, 24, RenderTextureFormat.DefaultHDR);
                RenderTexture prevActive = RenderTexture.active;
                try
                {
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = null;

                    RenderTexture.active = rt;
                    var tex = new Texture2D(PanelWidth, PanelHeight, TextureFormat.RGB24, false, false);
                    try
                    {
                        tex.ReadPixels(new Rect(0, 0, PanelWidth, PanelHeight), 0, 0, false);
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

        private static void ComposeTwoByTwoSheet(
            string topLeftPath,
            string topRightPath,
            string bottomLeftPath,
            string bottomRightPath,
            string outPath)
        {
            Texture2D tl = LoadPng(topLeftPath);
            Texture2D tr = LoadPng(topRightPath);
            Texture2D bl = LoadPng(bottomLeftPath);
            Texture2D br = LoadPng(bottomRightPath);
            try
            {
                int totalW = PanelWidth * 2 + DividerPx;
                int totalH = PanelHeight * 2 + DividerPx;
                var canvas = new Texture2D(totalW, totalH, TextureFormat.RGB24, false, false);
                try
                {
                    var bgPixels = new Color32[totalW * totalH];
                    var dividerColor = new Color32(12, 15, 24, 255);
                    for (int i = 0; i < bgPixels.Length; i++)
                    {
                        bgPixels[i] = dividerColor;
                    }

                    canvas.SetPixels32(bgPixels);

                    // Top row (y = PanelHeight + DividerPx) = Day C (Low Sun)
                    canvas.SetPixels(0, PanelHeight + DividerPx, PanelWidth, PanelHeight, tl.GetPixels());
                    canvas.SetPixels(PanelWidth + DividerPx, PanelHeight + DividerPx, PanelWidth, PanelHeight, tr.GetPixels());

                    // Bottom row (y = 0) = Night A (Earthlit)
                    canvas.SetPixels(0, 0, PanelWidth, PanelHeight, bl.GetPixels());
                    canvas.SetPixels(PanelWidth + DividerPx, 0, PanelWidth, PanelHeight, br.GetPixels());

                    canvas.Apply(false);
                    File.WriteAllBytes(outPath, canvas.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(canvas);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tl);
                UnityEngine.Object.DestroyImmediate(tr);
                UnityEngine.Object.DestroyImmediate(bl);
                UnityEngine.Object.DestroyImmediate(br);
            }
        }

        private static Texture2D LoadPng(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGB24, false, false);
            tex.LoadImage(bytes, false);
            return tex;
        }
    }
}
