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
    /// Batch Editor capture runner for the 6 Opening v2 R1 story prop deliverables alongside Rover 07
    /// (<c>RoverModel.prefab</c>) in both Day C (low golden lunar sun) and Night A / Eclipse lighting.
    /// Outputs PNG captures to <c>Logs/captures/r1props_*.png</c>.
    /// </summary>
    public static class R1StoryPropsCapture
    {
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 720;

        private static readonly int MoonHazeColorId = Shader.PropertyToID("_MoonHazeColor");
        private static readonly int MoonHazeId = Shader.PropertyToID("_MoonHaze");
        private static readonly int MoonDesaturateId = Shader.PropertyToID("_MoonDesaturate");
        private static readonly int BuildModeGlowId = Shader.PropertyToID("_BuildModeGlow");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// <summary>
        /// Batch entry point invoked via:
        /// <c>python tools/unity_batch.py -- -executeMethod MoonProject.Art.Editor.R1StoryPropsCapture.Capture</c>
        /// </summary>
        public static void Capture()
        {
            BatchRunner.Run(nameof(R1StoryPropsCapture), args =>
            {
                AssetDatabase.Refresh();
                R1MaterialLibrary.BuildMaterials();
                R1ModelBuilder.Build();

                string capturesDir = Path.GetFullPath("Logs/captures");
                Directory.CreateDirectory(capturesDir);

                bool prevAsync = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = false;
                try
                {
                    CaptureDeliverable1_PostAndCable(capturesDir, false);
                    CaptureDeliverable1_PostAndCable(capturesDir, true);

                    CaptureDeliverable2_LanderPortAndBeacon(capturesDir, false);
                    CaptureDeliverable2_LanderPortAndBeacon(capturesDir, true);

                    CaptureDeliverable3_BuildApron(capturesDir, false);
                    CaptureDeliverable3_BuildApron(capturesDir, true);

                    CaptureDeliverable4_AirlockReadout(capturesDir, false);
                    CaptureDeliverable4_AirlockReadout(capturesDir, true);

                    CaptureDeliverable5_DeadAntennaAndPillar(capturesDir, false);
                    CaptureDeliverable5_DeadAntennaAndPillar(capturesDir, true);

                    CaptureDeliverable6_PeakBeacon(capturesDir, false);
                    CaptureDeliverable6_PeakBeacon(capturesDir, true);

                    CaptureDeliverable7_OpenCrateAndFragments(capturesDir, false);
                    CaptureDeliverable7_OpenCrateAndFragments(capturesDir, true);
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = prevAsync;
                }

                string batchOutDir = Path.Combine(args.OutputDirectory, "r1_story_props");
                Directory.CreateDirectory(batchOutDir);
                foreach (string png in Directory.GetFiles(capturesDir, "r1props_*.png"))
                {
                    File.Copy(png, Path.Combine(batchOutDir, Path.GetFileName(png)), true);
                }

                return true;
            });
        }

        /// <summary>
        /// Dedicated fast batch entry point for capturing <c>Crate_M_Open</c>, <c>PadFragment_CoilPlate</c>,
        /// and <c>PadFragment_GuideRail</c> next to Rover 07 in eclipse light with 07's lamps:
        /// <c>python tools/unity_batch.py -- -executeMethod MoonProject.Art.Editor.R1StoryPropsCapture.CaptureLanderCrateAndFragments</c>
        /// </summary>
        public static void CaptureLanderCrateAndFragments()
        {
            BatchRunner.Run(nameof(CaptureLanderCrateAndFragments), args =>
            {
                AssetDatabase.Refresh();
                R1MaterialLibrary.BuildMaterials();
                R1ModelBuilder.Build();

                string capturesDir = Path.GetFullPath("Logs/captures");
                Directory.CreateDirectory(capturesDir);

                bool prevAsync = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = false;
                try
                {
                    CaptureDeliverable7_OpenCrateAndFragments(capturesDir, false);
                    CaptureDeliverable7_OpenCrateAndFragments(capturesDir, true);
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = prevAsync;
                }

                return true;
            });
        }

        private static void CaptureDeliverable1_PostAndCable(string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);
                SpawnRover07(temp, new Vector3(-1.15f, 0f, 0.10f), 162f, nightEclipse);

                SpawnProp(R1StoryPropsBuilder.DeadChargingPostPrefabPath, new Vector3(0.55f, 0f, 0.25f), 0f, temp);
                SpawnProp(R1StoryPropsBuilder.GroundCableEndPrefabPath, new Vector3(0.55f, 0f, -0.05f), 180f, temp);
                SpawnProp(R1StoryPropsBuilder.GroundCableSegmentPrefabPath, new Vector3(1.55f, 0f, -0.55f), 28f, temp);
                SpawnProp(R1StoryPropsBuilder.GroundCableSegmentPrefabPath, new Vector3(5.30f, 0f, 6.52f), 28f, temp);

                string suffix = nightEclipse ? "nightA" : "dayC";
                RenderShot(
                    new Vector3(-0.15f, 1.48f, -3.55f),
                    new Vector3(0.30f, 0.58f, 0.05f),
                    42f,
                    bg,
                    Path.Combine(outDir, $"r1props_01_post_and_cable_{suffix}.png"));
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void CaptureDeliverable2_LanderPortAndBeacon(string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);
                SpawnRover07(temp, new Vector3(-1.35f, 0f, -0.10f), 160f, nightEclipse);

                // Lander hull section backing plinth for mounting the service port and beacon
                Mesh plinthMesh = R1MaterialLibrary.CreateBevelledCubeMesh(1.0f, 0.04f);
                temp.Add(plinthMesh);
                GameObject hullWall = ArtLightingPreview.MeshObject(
                    "LanderHullBacking",
                    plinthMesh,
                    R1MaterialLibrary.LoadMaterial(R1MaterialSlot.CreamEnamel),
                    temp);
                hullWall.transform.position = new Vector3(0.65f, 1.05f, 0.52f);
                hullWall.transform.localScale = new Vector3(1.30f, 2.10f, 0.36f);
                temp.Add(hullWall);

                SpawnProp(R1StoryPropsBuilder.LanderServicePortPrefabPath, new Vector3(0.65f, 0.32f, 0.22f), 0f, temp);
                SpawnProp(R1StoryPropsBuilder.LanderBeaconPrefabPath, new Vector3(0.65f, 2.10f, 0.48f), 0f, temp);
                SpawnProp(R1StoryPropsBuilder.LanderBeaconPrefabPath, new Vector3(1.75f, 0.0f, -0.15f), 0f, temp);

                if (nightEclipse)
                {
                    Light beaconPoint = ArtLightingPreview.NewLight(
                        "LanderBeaconWarmGlow",
                        LightType.Point,
                        new Color(1.0f, 0.68f, 0.28f, 1f),
                        2.2f);
                    beaconPoint.transform.position = new Vector3(0.65f, 2.55f, 0.15f);
                    beaconPoint.range = 6.5f;
                    beaconPoint.shadows = LightShadows.None;
                    temp.Add(beaconPoint.gameObject);
                }

                string suffix = nightEclipse ? "nightA" : "dayC";
                RenderShot(
                    new Vector3(0.10f, 1.45f, -3.50f),
                    new Vector3(0.25f, 1.02f, 0.15f),
                    44f,
                    bg,
                    Path.Combine(outDir, $"r1props_02_lander_port_and_beacon_{suffix}.png"));
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void CaptureDeliverable3_BuildApron(string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);
                SpawnRover07(temp, new Vector3(-2.35f, 0f, 0.20f), 165f, nightEclipse);

                // Left apron: normal unlit (_BuildModeGlow = 0)
                SpawnProp(R1StoryPropsBuilder.BuildApronMarkingsPrefabPath, new Vector3(-2.35f, 0.005f, 0.20f), 0f, temp);

                // Right apron: build mode active (_BuildModeGlow = 1)
                GameObject apronOn = SpawnProp(R1StoryPropsBuilder.BuildApronMarkingsPrefabPath, new Vector3(2.35f, 0.005f, 0.20f), 0f, temp);
                Transform outline = apronOn.transform.Find("BayGlowOutline");
                if (outline != null)
                {
                    var block = new MaterialPropertyBlock();
                    block.SetFloat(BuildModeGlowId, 1.0f);
                    outline.GetComponent<Renderer>().SetPropertyBlock(block);
                }

                string suffix = nightEclipse ? "nightA" : "dayC";
                RenderShot(
                    new Vector3(0.0f, 4.10f, -5.50f),
                    new Vector3(0.0f, 0.30f, 0.20f),
                    48f,
                    bg,
                    Path.Combine(outDir, $"r1props_03_build_apron_{suffix}.png"));
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void CaptureDeliverable4_AirlockReadout(string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);
                SpawnRover07(temp, new Vector3(-1.45f, 0f, -0.35f), 152f, nightEclipse);

                // HabitatModules placed so its -Z front airlock door (-3.45, 1.25, -1.42) & AirlockReadout (-4.35, 1.37, -1.392)
                // sit right next to Rover 07 at X = 0.20..1.10, Z = 0.55 facing -Z toward the camera
                SpawnProp(R1StoryPropsBuilder.HabitatModulesPrefabPath, new Vector3(4.55f, 0f, 1.95f), 0f, temp);

                // Standalone AirlockReadoutPanel at Rover 07 eye level between 07 and the airlock door
                SpawnProp(R1StoryPropsBuilder.AirlockReadoutPanelPrefabPath, new Vector3(-0.35f, 0.92f, -0.45f), 0f, temp);

                string suffix = nightEclipse ? "nightA" : "dayC";
                RenderShot(
                    new Vector3(-0.05f, 1.42f, -3.05f),
                    new Vector3(0.15f, 1.18f, 0.25f),
                    42f,
                    bg,
                    Path.Combine(outDir, $"r1props_04_airlock_readout_{suffix}.png"));
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void CaptureDeliverable5_DeadAntennaAndPillar(string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);
                SpawnRover07(temp, new Vector3(-1.65f, 0f, -0.20f), 160f, nightEclipse);

                SpawnProp(R1StoryPropsBuilder.DeadAntennaMastPrefabPath, new Vector3(0.25f, 0f, 0.15f), 180f, temp);
                SpawnProp(R1StoryPropsBuilder.DeadAntennaMastPrefabPath, new Vector3(2.35f, 0f, 1.10f), 155f, temp);
                SpawnProp(R1StoryPropsBuilder.SignalPillarPrefabPath, new Vector3(2.35f, 0f, 1.10f), 0f, temp);

                string suffix = nightEclipse ? "nightA" : "dayC";
                RenderShot(
                    new Vector3(-0.20f, 1.85f, -5.80f),
                    new Vector3(0.55f, 2.35f, 0.40f),
                    52f,
                    bg,
                    Path.Combine(outDir, $"r1props_05_dead_antenna_and_pillar_{suffix}.png"));
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void CaptureDeliverable6_PeakBeacon(string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);
                SpawnRover07(temp, new Vector3(-1.55f, 0f, -0.40f), 160f, nightEclipse);

                GameObject peakBeacon = SpawnProp(R1StoryPropsBuilder.PeakBeaconPrefabPath, new Vector3(0.85f, 0f, 0.40f), 180f, temp);

                Transform lamp = peakBeacon.transform.Find("Lamp");
                if (lamp != null)
                {
                    var block = new MaterialPropertyBlock();
                    block.SetVector(EmissionColorId, new Vector4(1.35f, 1.35f, 1.35f, 1f));
                    lamp.GetComponent<Renderer>().SetPropertyBlock(block);
                }

                if (nightEclipse)
                {
                    Light redPoint = ArtLightingPreview.NewLight(
                        "PeakBeaconRedGlow",
                        LightType.Point,
                        Palette.Get(PaletteSwatch.AlertSoft),
                        3.5f);
                    redPoint.transform.position = new Vector3(0.85f, 6.72f, 0.15f);
                    redPoint.range = 9.0f;
                    redPoint.shadows = LightShadows.None;
                    temp.Add(redPoint.gameObject);
                }

                string suffix = nightEclipse ? "nightA" : "dayC";
                RenderShot(
                    new Vector3(-0.35f, 2.85f, -8.80f),
                    new Vector3(0.10f, 3.25f, 0.20f),
                    48f,
                    bg,
                    Path.Combine(outDir, $"r1props_06_peak_beacon_{suffix}.png"));
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void CaptureDeliverable7_OpenCrateAndFragments(string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);
                // Rover 07 positioned at (-1.55, 0, -0.35) angled toward the open crate & pad fragments
                GameObject rover = SpawnRover07(temp, new Vector3(-1.55f, 0f, -0.35f), 118f, nightEclipse);

                // Place Crate_M_Open, PadFragment_CoilPlate, and PadFragment_GuideRail in front of Rover 07's lamps
                SpawnProp(R1StoryPropsBuilder.CrateMOpenPrefabPath, new Vector3(0.15f, 0f, -0.35f), 165f, temp);
                SpawnProp(R1StoryPropsBuilder.PadFragmentCoilPlatePrefabPath, new Vector3(0.95f, 0f, -0.25f), 18f, temp);
                SpawnProp(R1StoryPropsBuilder.PadFragmentGuideRailPrefabPath, new Vector3(1.55f, 0f, -0.15f), -16f, temp);

                if (nightEclipse)
                {
                    AddRoverHeadlamps(rover.transform, new Vector3(0.75f, 0.18f, -0.25f), temp);
                }

                string suffix = nightEclipse ? "nightA" : "dayC";

                // 1. Close-up of Crate_M_Open next to Rover 07 showing Igniter, MetalBundle, WiringSpool & RimGlow
                RenderShot(
                    new Vector3(0.05f, 0.92f, -1.65f),
                    new Vector3(0.05f, 0.24f, -0.35f),
                    38f,
                    bg,
                    Path.Combine(outDir, $"r1props_07_crate_m_open_{suffix}.png"));

                // 2. Close-up of PadFragment_CoilPlate and PadFragment_GuideRail next to Rover 07
                RenderShot(
                    new Vector3(0.95f, 1.05f, -2.05f),
                    new Vector3(0.95f, 0.18f, -0.22f),
                    40f,
                    bg,
                    Path.Combine(outDir, $"r1props_08_pad_fragments_{suffix}.png"));

                // 3. 8 m distance readability shot in eclipse dark under Rover 07's headlamps
                RenderShot(
                    new Vector3(-1.85f, 2.25f, -7.85f),
                    new Vector3(0.25f, 0.35f, -0.25f),
                    36f,
                    bg,
                    Path.Combine(outDir, $"r1props_09_crate_and_fragments_8m_{suffix}.png"));

                // 4. 3 m distance shot of Crate_M_Open under Rover 07's headlamps
                RenderShot(
                    new Vector3(-0.35f, 1.35f, -3.10f),
                    new Vector3(0.15f, 0.24f, -0.35f),
                    36f,
                    bg,
                    Path.Combine(outDir, $"r1props_10_crate_m_open_3m_{suffix}.png"));
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void AddRoverHeadlamps(
            Transform roverTransform,
            Vector3 targetPoint,
            ArtLightingPreview.TemporaryObjects temp)
        {
            Vector3[] lampOffsets =
            {
                new Vector3(-0.34f, 0.88f, 0.72f),
                new Vector3(0.34f, 0.88f, 0.72f),
            };
            for (int i = 0; i < lampOffsets.Length; i++)
            {
                Light headlamp = ArtLightingPreview.NewLight(
                    $"Rover07Headlamp_{i}",
                    LightType.Spot,
                    new Color(1.00f, 0.88f, 0.68f, 1f),
                    8.5f);
                Vector3 worldPos = roverTransform.TransformPoint(lampOffsets[i]);
                headlamp.transform.position = worldPos;
                headlamp.transform.rotation = Quaternion.LookRotation((targetPoint - worldPos).normalized, Vector3.up);
                headlamp.range = 16.0f;
                headlamp.spotAngle = 58.0f;
                headlamp.innerSpotAngle = 32.0f;
                headlamp.shadows = LightShadows.Soft;
                temp.Add(headlamp.gameObject);
            }
        }

        private static Color SetupEnvironment(ArtLightingPreview.TemporaryObjects temp, bool nightEclipse)
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
                // Night A / Eclipse: deep indigo shadow ambient + low cool earthlight rim
                RenderSettings.ambientSkyColor = new Color(0.11f, 0.14f, 0.22f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.08f, 0.10f, 0.16f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.05f, 0.06f, 0.10f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.05f, 0.07f, 0.11f, 1f);

                Light earthLight = ArtLightingPreview.NewLight(
                    "NightAEarthlight",
                    LightType.Directional,
                    new Color(0.58f, 0.68f, 0.95f, 1f),
                    0.48f);
                earthLight.transform.rotation = Quaternion.Euler(28f, 145f, 0f);
                earthLight.shadows = LightShadows.Soft;
                earthLight.shadowStrength = 0.75f;
                RenderSettings.sun = earthLight;
                temp.Add(earthLight.gameObject);

                Light rimLight = ArtLightingPreview.NewLight(
                    "NightARimFill",
                    LightType.Directional,
                    new Color(0.42f, 0.52f, 0.78f, 1f),
                    0.22f);
                rimLight.transform.rotation = Quaternion.Euler(22f, -35f, 0f);
                rimLight.shadows = LightShadows.None;
                temp.Add(rimLight.gameObject);
            }
            else
            {
                // Day C: low golden sun (18 deg elevation) + cool lunar sky fill
                RenderSettings.ambientSkyColor = new Color(0.32f, 0.35f, 0.42f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.25f, 0.26f, 0.30f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.18f, 0.18f, 0.20f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.14f, 0.16f, 0.20f, 1f);

                Light sunLight = ArtLightingPreview.NewLight(
                    "DayCGoldenSun",
                    LightType.Directional,
                    new Color(1.00f, 0.91f, 0.76f, 1f),
                    1.35f);
                sunLight.transform.rotation = Quaternion.Euler(18f, 138f, 0f);
                sunLight.shadows = LightShadows.Soft;
                sunLight.shadowStrength = 0.78f;
                RenderSettings.sun = sunLight;
                temp.Add(sunLight.gameObject);

                Light fillLight = ArtLightingPreview.NewLight(
                    "DayCCoolFill",
                    LightType.Directional,
                    new Color(0.70f, 0.78f, 0.94f, 1f),
                    0.42f);
                fillLight.transform.rotation = Quaternion.Euler(35f, -45f, 0f);
                fillLight.shadows = LightShadows.None;
                temp.Add(fillLight.gameObject);
            }

            DynamicGI.UpdateEnvironment();

            // Lunar regolith floor plane using R1_Basalt
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

            var volumeObj = new GameObject("R1StoryPropsVolume");
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
            GameObject rover = InstantiatePrefab(R1RoverCapture.RoverPrefabPath, temp);
            rover.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDeg, 0f));

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

        private static GameObject SpawnProp(
            string prefabPath,
            Vector3 position,
            float yawDeg,
            ArtLightingPreview.TemporaryObjects temp)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"R1StoryPropsCapture: Missing generated prefab at '{prefabPath}'.");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(0f, yawDeg, 0f) * prefab.transform.localRotation);
            temp.Add(instance);
            return instance;
        }

        private static GameObject InstantiatePrefab(string prefabPath, ArtLightingPreview.TemporaryObjects temp)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"R1StoryPropsCapture: Missing generated prefab at '{prefabPath}'.");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            temp.Add(instance);
            return instance;
        }

        private static void RenderShot(Vector3 camPos, Vector3 lookAt, float fov, Color bgColor, string outputPath)
        {
            var camObj = new GameObject("[R1StoryPropsCamera]") { hideFlags = HideFlags.HideAndDontSave };
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
    }
}
