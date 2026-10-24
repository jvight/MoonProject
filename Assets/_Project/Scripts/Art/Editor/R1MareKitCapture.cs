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
    /// (<c>docs/art/concepts/0.6-mare/kit/kit_sheet.png</c>), rendering a wide establishing shot of the kit
    /// laid out on the mare ground under a low sun (long shadows, Earth low on the horizon) plus close hero shots
    /// under Low Sun Day C and Earthlit Night A lighting.
    /// </summary>
    public static class R1MareKitCapture
    {
        private const int PanelWidth = 1280;
        private const int PanelHeight = 720;
        private const int DividerPx = 8;

        private const string LofiEarthShaderPath = "Assets/_Project/Shaders/World/LofiEarth.shader";
        private const string EarthAlbedoTexturePath = "Assets/_Project/Art/R1/Textures/T_R1_Earth_Albedo.png";

        private static readonly int MoonHazeColorId = Shader.PropertyToID("_MoonHazeColor");
        private static readonly int MoonHazeId = Shader.PropertyToID("_MoonHaze");
        private static readonly int MoonDesaturateId = Shader.PropertyToID("_MoonDesaturate");

        private static readonly int MoonEarthDirectionId = Shader.PropertyToID("_MoonEarthDirection");
        private static readonly int MoonEarthAxisXId = Shader.PropertyToID("_MoonEarthAxisX");
        private static readonly int MoonEarthAxisYId = Shader.PropertyToID("_MoonEarthAxisY");
        private static readonly int MoonEarthAxisZId = Shader.PropertyToID("_MoonEarthAxisZ");
        private static readonly int MoonEarthSunId = Shader.PropertyToID("_MoonEarthSun");
        private static readonly int MoonEarthGlowId = Shader.PropertyToID("_MoonEarthGlow");
        private static readonly int MoonEarthAtmosphereId = Shader.PropertyToID("_MoonEarthAtmosphere");
        private static readonly int MoonEarthOceanId = Shader.PropertyToID("_MoonEarthOcean");
        private static readonly int MoonEarthLandId = Shader.PropertyToID("_MoonEarthLand");
        private static readonly int MoonEarthCloudId = Shader.PropertyToID("_MoonEarthCloud");
        private static readonly int MoonEarthAuroraId = Shader.PropertyToID("_MoonEarthAurora");
        private static readonly int MoonEarthAuroraBandId = Shader.PropertyToID("_MoonEarthAuroraBand");
        private static readonly int MoonEclipseRingId = Shader.PropertyToID("_MoonEclipseRing");

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
                    nightOverviewPath,
                    dayCloseupPath,
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
                SetupLowEarthAndStars(temp, nightMode);

                // 1. Ground & Decals:
                //    -MareGround_Patch swatches under hero area
                //    -Sinuous feathered Decal_MareSwirl S-curve winding from foreground across the Mare plain toward Earth
                //    -Soft streaky Decal_MareEjectaRay wayfinding rays flanking the swirl
                SpawnProp(R1MareKitBuilder.MareGroundPatchPrefabPath, new Vector3(0f, -0.055f, 0.0f), 0f, temp);
                SpawnProp(R1MareKitBuilder.MareGroundPatchPrefabPath, new Vector3(0f, -0.055f, 12.0f), 90f, temp);

                SpawnProp(R1MareKitBuilder.DecalMareSwirlPrefabPath, new Vector3(0.45f, 0.016f, 0.60f), -18f, temp);
                SpawnProp(R1MareKitBuilder.DecalMareSwirlPrefabPath, new Vector3(4.80f, 0.015f, 4.20f), 22f, temp);
                SpawnProp(R1MareKitBuilder.DecalMareSwirlPrefabPath, new Vector3(3.20f, 0.014f, 16.50f), -12f, temp);
                SpawnProp(R1MareKitBuilder.DecalMareEjectaRayPrefabPath, new Vector3(-3.20f, 0.014f, 1.80f), 16f, temp);
                SpawnProp(R1MareKitBuilder.DecalMareEjectaRayPrefabPath, new Vector3(6.60f, 0.014f, 3.80f), -18f, temp);

                // 2. Rille Pass Modular Pieces (4 chunks framing the West-Rim Rille Pass along the left midground,
                //    oriented along the channel axis so their weathered basalt cliff walls and talus aprons read naturally)
                SpawnProp(R1MareKitBuilder.RilleSlopeRampPrefabPath, new Vector3(-11.5f, 0f, 7.5f), -72f, temp);
                SpawnProp(R1MareKitBuilder.RilleMouthCornerPrefabPath, new Vector3(-10.8f, 0f, 14.5f), -38f, temp);
                SpawnProp(R1MareKitBuilder.RilleWallStraightPrefabPath, new Vector3(-12.4f, 0f, 22.5f), -28f, temp);
                SpawnProp(R1MareKitBuilder.RilleWallCurvePrefabPath, new Vector3(-10.5f, 0f, 31.0f), -44f, temp);

                // 3. Mare Rock Kit:
                //    - 6 natural rounded-weathered basalt boulders/outcrops (MareRock_00..05) of varied sizes laid out across
                //      the Mare ground with open spacing so their rounded silhouettes and long low-sun shadows are unmistakable
                //    - 2 dark Ilmenite drill-target outcrops (MareOutcrop_Ilmenite_00..01) catching a bronze-gunmetal glint
                SpawnProp(R1MareKitBuilder.MareBasaltRockPrefabPaths[0], new Vector3(-3.45f, 0f, -1.10f), 24f, temp);
                SpawnProp(R1MareKitBuilder.MareBasaltRockPrefabPaths[1], new Vector3(-4.65f, 0f, 2.15f), -34f, temp);
                SpawnProp(R1MareKitBuilder.MareBasaltRockPrefabPaths[2], new Vector3(-5.80f, 0f, 6.20f), 18f, temp);
                SpawnProp(R1MareKitBuilder.MareBasaltRockPrefabPaths[3], new Vector3(-1.85f, 0f, 5.40f), -16f, temp);
                SpawnProp(R1MareKitBuilder.MareBasaltRockPrefabPaths[4], new Vector3(6.40f, 0f, 4.80f), 38f, temp);
                SpawnProp(R1MareKitBuilder.MareBasaltRockPrefabPaths[5], new Vector3(4.90f, 0f, -1.40f), -42f, temp);

                SpawnProp(R1MareKitBuilder.IlmeniteOutcrop00PrefabPath, new Vector3(-2.30f, 0f, 1.35f), 154f, temp);
                SpawnProp(R1MareKitBuilder.IlmeniteOutcrop01PrefabPath, new Vector3(3.10f, 0f, 6.40f), -150f, temp);

                // 4. Mare Derelict Set:
                //    - Stopped Convoy (3 haul bots softened in 07's rounded shape language, middle with StrappedLoad)
                //    - Crashed Survey Drone (4 De Laval RCS nozzles in plowed furrow on front-left)
                //    - Half-Buried Tardigrade Capsule (front-center-right, TinyWindow facing camera)
                SpawnProp(R1MareKitBuilder.DerelictMareConvoyPrefabPath, new Vector3(2.95f, 0f, 1.10f), -144f, temp);
                SpawnProp(R1MareKitBuilder.DerelictMareSurveyDronePrefabPath, new Vector3(-1.85f, 0f, -1.75f), 146f, temp);
                GameObject capsule = SpawnProp(
                    R1MareKitBuilder.DerelictTardigradeCapsulePrefabPath,
                    new Vector3(0.82f, 0f, -2.25f),
                    162f,
                    temp);

                if (nightMode)
                {
                    Transform winNode = capsule.transform.Find("Window_Anchor");
                    Vector3 winPos = winNode != null ? winNode.position : capsule.transform.position + new Vector3(0f, 0.26f, -0.25f);
                    Light tinyWinGlow = ArtLightingPreview.NewLight(
                        "TardigradeWindowMicroGlow",
                        LightType.Point,
                        new Color(1.00f, 0.72f, 0.30f, 1f),
                        0.22f);
                    tinyWinGlow.transform.position = winPos + new Vector3(0f, 0.03f, -0.06f);
                    tinyWinGlow.range = 0.65f;
                    tinyWinGlow.shadows = LightShadows.None;
                    temp.Add(tinyWinGlow.gameObject);
                }

                // 5. Rover 07 (1.59 m tall scale reference) in center-foreground at 3/4 view
                SpawnRover07WithHeadlamp(temp, new Vector3(-0.20f, 0f, -0.75f), 148f, nightMode);

                // Shot 1: Wide Establishing Shot (kit laid out on the mare ground under a low sun, long shadows, Earth low on horizon)
                RenderShot(
                    new Vector3(0.20f, 3.25f, -12.20f),
                    new Vector3(0.15f, 0.95f, 11.00f),
                    52f,
                    bg,
                    overviewPath);

                // Shot 2: Close Hero Shot (close inspection of natural basalt rocks, Ilmenite outcrop, softened convoy, drone, capsule, swirl, and 07)
                RenderShot(
                    new Vector3(-0.18f, 1.88f, -5.25f),
                    new Vector3(0.22f, 0.62f, 0.85f),
                    44f,
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
                // Night A (Earthlit Mare): #05070E deep indigo-black sky + cool blue Earthshine key
                RenderSettings.ambientSkyColor = new Color(0.20f, 0.28f, 0.46f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.15f, 0.22f, 0.36f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.10f, 0.14f, 0.24f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.015f, 0.025f, 0.055f, 1f);

                Light earthLight = ArtLightingPreview.NewLight(
                    "NightAEarthshine",
                    LightType.Directional,
                    new Color(0.62f, 0.76f, 0.98f, 1f),
                    1.35f);
                earthLight.transform.rotation = Quaternion.Euler(16f, -42f, 0f);
                earthLight.shadows = LightShadows.Soft;
                earthLight.shadowStrength = 0.76f;
                RenderSettings.sun = earthLight;
                temp.Add(earthLight.gameObject);

                Light rimLight = ArtLightingPreview.NewLight(
                    "NightARimFill",
                    LightType.Directional,
                    new Color(0.48f, 0.62f, 0.88f, 1f),
                    0.55f);
                rimLight.transform.rotation = Quaternion.Euler(22f, 48f, 0f);
                rimLight.shadows = LightShadows.None;
                temp.Add(rimLight.gameObject);
            }
            else
            {
                // Low Sun Day C: 9.5 deg grazing polar sun (#FFF4E2) shining from front-right (yaw -58 deg)
                // casting long crisp shadows across the Mare plain and pale swirl while keeping all basalt forms clearly readable
                RenderSettings.ambientSkyColor = new Color(0.36f, 0.41f, 0.52f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.28f, 0.32f, 0.42f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.18f, 0.22f, 0.30f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.025f, 0.035f, 0.065f, 1f);

                Light lowSun = ArtLightingPreview.NewLight(
                    "DayCLowPolarSun",
                    LightType.Directional,
                    new Color(1.00f, 0.95f, 0.86f, 1f),
                    3.20f);
                lowSun.transform.rotation = Quaternion.Euler(9.5f, -58f, 0f);
                lowSun.shadows = LightShadows.Soft;
                lowSun.shadowStrength = 0.88f;
                RenderSettings.sun = lowSun;
                temp.Add(lowSun.gameObject);

                Light earthFill = ArtLightingPreview.NewLight(
                    "DayCEarthshineFill",
                    LightType.Directional,
                    new Color(0.66f, 0.76f, 0.94f, 1f),
                    0.68f);
                earthFill.transform.rotation = Quaternion.Euler(24f, 52f, 0f);
                earthFill.shadows = LightShadows.None;
                temp.Add(earthFill.gameObject);
            }

            DynamicGI.UpdateEnvironment();

            // Expansive Mare basalt sea floor extending to the distant horizon with clean dark vertex colors
            // (preventing Unity's default (1,1,1,1) vertex color from triggering 100% edge wear on the flat floor)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "MareBasaltFloor";
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.transform.position = new Vector3(0f, -0.02f, 35f);
            floor.transform.localScale = new Vector3(24f, 1f, 24f);
            var floorMf = floor.GetComponent<MeshFilter>();
            if (floorMf != null && floorMf.sharedMesh != null)
            {
                Mesh floorMesh = UnityEngine.Object.Instantiate(floorMf.sharedMesh);
                floorMesh.name = "MareBasaltFloorMesh";
                Vector2[] uvs = floorMesh.uv;
                for (int i = 0; i < uvs.Length; i++)
                {
                    uvs[i] *= 36f;
                }

                floorMesh.uv = uvs;
                var vcols = new Color[floorMesh.vertexCount];
                var darkFloorCol = new Color(0.84f, 0.0f, 0.06f, 1.0f);
                for (int i = 0; i < vcols.Length; i++)
                {
                    vcols[i] = darkFloorCol;
                }

                floorMesh.colors = vcols;
                floorMf.sharedMesh = floorMesh;
                temp.Add(floorMesh);
            }

            var mareBasaltMat = AssetDatabase.LoadAssetAtPath<Material>(R1MareKitBuilder.MareBasaltMatPath);
            floor.GetComponent<MeshRenderer>().sharedMaterial = mareBasaltMat != null
                ? mareBasaltMat
                : R1MaterialLibrary.LoadMaterial(R1MaterialSlot.Basalt);
            temp.Add(floor);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            temp.Add(profile);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(Palette.BloomThreshold);
            bloom.intensity.Override(nightMode ? 0.48f : 0.35f);
            bloom.scatter.Override(0.60f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var volumeObj = new GameObject("R1MareKitVolume");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            temp.Add(volumeObj);

            return bgColor;
        }

        private static void SetupLowEarthAndStars(ArtLightingPreview.TemporaryObjects temp, bool nightMode)
        {
            Shader earthShader = AssetDatabase.LoadAssetAtPath<Shader>(LofiEarthShaderPath);
            Texture2D earthTex = AssetDatabase.LoadAssetAtPath<Texture2D>(EarthAlbedoTexturePath);
            if (earthShader != null)
            {
                // Huge low Earth sitting right on the Mare horizon
                Vector3 earthDir = new Vector3(0.16f, 0.068f, 0.985f).normalized;
                float angularRadius = 0.165f;
                Vector3 axisZ = -earthDir;
                Vector3 axisX = Vector3.Cross(Vector3.up, axisZ).normalized;
                Vector3 axisY = Vector3.Cross(axisZ, axisX).normalized;
                Vector3 sunDir = new Vector3(-0.76f, 0.26f, -0.59f).normalized;

                Shader.SetGlobalVector(MoonEarthDirectionId, new Vector4(earthDir.x, earthDir.y, earthDir.z, angularRadius));
                Shader.SetGlobalVector(MoonEarthAxisXId, new Vector4(axisX.x, axisX.y, axisX.z, Mathf.Tan(angularRadius)));
                Shader.SetGlobalVector(MoonEarthAxisYId, new Vector4(axisY.x, axisY.y, axisY.z, 0f));
                Shader.SetGlobalVector(MoonEarthAxisZId, new Vector4(axisZ.x, axisZ.y, axisZ.z, 0f));
                Shader.SetGlobalVector(MoonEarthSunId, new Vector4(sunDir.x, sunDir.y, sunDir.z, 0.035f));
                Shader.SetGlobalVector(MoonEarthGlowId, new Vector4(nightMode ? 1.05f : 0.95f, 0f, 2.68f, 0f));
                Shader.SetGlobalVector(MoonEarthAtmosphereId, new Vector4(0.18f, 0.54f, 0.98f, 3.4f));
                Shader.SetGlobalVector(MoonEarthOceanId, new Vector4(0.05f, 0.24f, 0.54f, 1f));
                Shader.SetGlobalVector(MoonEarthLandId, new Vector4(0.20f, 0.44f, 0.24f, 1f));
                Shader.SetGlobalVector(MoonEarthCloudId, new Vector4(0.90f, 0.94f, 0.98f, 1f));
                Shader.SetGlobalVector(MoonEarthAuroraId, new Vector4(0.06f, 0.34f, 0.20f, 0f));
                Shader.SetGlobalVector(MoonEarthAuroraBandId, new Vector4(0.82f, 0.06f, 0.5f, 0f));
                Shader.SetGlobalVector(MoonEclipseRingId, new Vector4(0.90f, 0.30f, 0.10f, 0.04f));

                var earthMat = new Material(earthShader) { name = "MareLowEarthMat" };
                if (earthTex != null)
                {
                    earthMat.SetTexture("_EarthAlbedoMap", earthTex);
                }

                temp.Add(earthMat);

                var quadMesh = new Mesh { name = "MareLowEarthQuad" };
                quadMesh.vertices = new[]
                {
                    new Vector3(-1.15f, -1.15f, 0f),
                    new Vector3(1.15f, -1.15f, 0f),
                    new Vector3(1.15f, 1.15f, 0f),
                    new Vector3(-1.15f, 1.15f, 0f),
                };
                quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                quadMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 4000f);
                temp.Add(quadMesh);

                var earthObj = new GameObject("MareLowEarth");
                var mf = earthObj.AddComponent<MeshFilter>();
                mf.sharedMesh = quadMesh;
                var mr = earthObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = earthMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                temp.Add(earthObj);
            }
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
                Light eyeFill = ArtLightingPreview.NewLight(
                    "Rover07EyeWarmFill",
                    LightType.Point,
                    new Color(1.0f, 0.72f, 0.34f, 1f),
                    1.45f);
                eyeFill.transform.position = position + Quaternion.Euler(0f, yawDeg, 0f) * new Vector3(0f, 1.22f, 1.05f);
                eyeFill.range = 4.8f;
                eyeFill.shadows = LightShadows.None;
                temp.Add(eyeFill.gameObject);

                Light headlampLeft = ArtLightingPreview.NewLight(
                    "Rover07HeadlampConeA",
                    LightType.Spot,
                    new Color(1.0f, 0.90f, 0.72f, 1f),
                    5.6f);
                Vector3 lampOrigin = position + new Vector3(0f, 1.10f, 0.10f);
                headlampLeft.transform.position = lampOrigin;
                headlampLeft.transform.rotation = Quaternion.LookRotation(new Vector3(-2.1f, 0.45f, 0.65f) - lampOrigin, Vector3.up);
                headlampLeft.range = 14f;
                headlampLeft.spotAngle = 68f;
                headlampLeft.innerSpotAngle = 34f;
                headlampLeft.shadows = LightShadows.Soft;
                temp.Add(headlampLeft.gameObject);

                Light headlampRight = ArtLightingPreview.NewLight(
                    "Rover07HeadlampConeB",
                    LightType.Spot,
                    new Color(1.0f, 0.88f, 0.70f, 1f),
                    4.4f);
                headlampRight.transform.position = lampOrigin;
                headlampRight.transform.rotation = Quaternion.LookRotation(new Vector3(2.6f, 0.50f, 0.65f) - lampOrigin, Vector3.up);
                headlampRight.range = 14f;
                headlampRight.spotAngle = 66f;
                headlampRight.innerSpotAngle = 32f;
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

                    // Top row: Wide Establishing Shots (Left = Low Sun Day C, Right = Earthlit Night A)
                    canvas.SetPixels(0, PanelHeight + DividerPx, PanelWidth, PanelHeight, tl.GetPixels());
                    canvas.SetPixels(PanelWidth + DividerPx, PanelHeight + DividerPx, PanelWidth, PanelHeight, tr.GetPixels());

                    // Bottom row: Close Hero Shots (Left = Low Sun Day C, Right = Earthlit Night A)
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
