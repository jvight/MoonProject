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

                    CaptureDeliverable8_CableKit(capturesDir, false);
                    CaptureDeliverable8_CableKit(capturesDir, true);

                    CaptureDeliverable9_OldLander(capturesDir, false);
                    CaptureDeliverable9_OldLander(capturesDir, true);
                    CaptureDeliverable10_CargoCrateOpen(capturesDir, false);
                    CaptureDeliverable10_CargoCrateOpen(capturesDir, true);
                    CaptureDeliverable11_BasinLifeLineup(capturesDir, false);
                    CaptureDeliverable11_BasinLifeLineup(capturesDir, true);
                    CaptureDeliverable12_GeophoneLine(capturesDir, false);
                    CaptureDeliverable12_GeophoneLine(capturesDir, true);
                    CaptureDeliverable13_EraSortiesLineup(capturesDir, false);
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
        /// Dedicated fast batch entry point for capturing <c>Prop_OldLander</c> and <c>Decal_OldLanderBlast</c>
        /// next to Rover 07 in Day C and Eclipse light, plus the 60 m silhouette shot:
        /// <c>python tools/unity_batch.py exec --project . --method MoonProject.Art.Editor.R1StoryPropsCapture.CaptureOldLander</c>
        /// </summary>
        public static void CaptureOldLander()
        {
            BatchRunner.Run(nameof(CaptureOldLander), args =>
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
                    CaptureDeliverable9_OldLander(capturesDir, false);
                    CaptureDeliverable9_OldLander(capturesDir, true);
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = prevAsync;
                }

                return true;
            });
        }

        /// <summary>
        /// Dedicated fast batch entry point for capturing the 6 Basin Life crew trace props
        /// (<c>Prop_MissionFlag</c>, <c>Prop_SeismicStation</c>, <c>Prop_Retroreflector</c>,
        /// <c>Prop_SurveyTripod</c>, <c>Prop_SampleRack</c>, <c>Decal_BootprintTrail</c>) next to Rover 07
        /// in Day C and in Eclipse light:
        /// <c>python tools/unity_batch.py exec --project . --method MoonProject.Art.Editor.R1StoryPropsCapture.CaptureBasinLifeLineup</c>
        /// </summary>
        public static void CaptureBasinLifeLineup()
        {
            BatchRunner.Run(nameof(CaptureBasinLifeLineup), args =>
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
                    CaptureDeliverable11_BasinLifeLineup(capturesDir, false);
                    CaptureDeliverable11_BasinLifeLineup(capturesDir, true);
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = prevAsync;
                }

                return true;
            });
        }

        /// <summary>
        /// Dedicated fast batch entry point for capturing the 6-piece R1 ground power-cable kit
        /// (<c>Cable_Straight_2m</c>, <c>Cable_Bend_45</c>, <c>Cable_Bend_90</c>, <c>Cable_BrokenEnd</c>,
        /// <c>Cable_Plug</c>, <c>Cable_Splice_Box</c>) laid out next to Rover 07 in Day C and in Eclipse light:
        /// <c>python tools/unity_batch.py exec --project . --method MoonProject.Art.Editor.R1StoryPropsCapture.CaptureCableKit</c>
        /// </summary>
        public static void CaptureCableKit()
        {
            BatchRunner.Run(nameof(CaptureCableKit), args =>
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
                    CaptureDeliverable8_CableKit(capturesDir, false);
                    CaptureDeliverable8_CableKit(capturesDir, true);
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = prevAsync;
                }

                return true;
            });
        }

        /// <summary>
        /// Dedicated fast batch entry point for capturing <c>Crate_Cargo_Open</c> next to Rover 07 in Day C and Eclipse
        /// light, plus the 4 m behind / 2 m up socket shots:
        /// <c>python tools/unity_batch.py -- -executeMethod MoonProject.Art.Editor.R1StoryPropsCapture.CaptureCargoCrateOpen</c>
        /// </summary>
        public static void CaptureCargoCrateOpen()
        {
            BatchRunner.Run(nameof(CaptureCargoCrateOpen), args =>
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
                    CaptureDeliverable10_CargoCrateOpen(capturesDir, false);
                    CaptureDeliverable10_CargoCrateOpen(capturesDir, true);
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = prevAsync;
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

        private static void CaptureDeliverable8_CableKit(string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);
                if (!nightEclipse && RenderSettings.sun != null)
                {
                    RenderSettings.sun.intensity = 2.0f;
                    RenderSettings.sun.transform.rotation = Quaternion.Euler(26f, -32f, 0f);
                }

                // Rover 07 positioned on the right at (1.35, 0, 0.20) in 3/4 front view (yaw -138 deg) facing the cable kit:
                // its Day C shadow falls off-screen to +X while its CRT face and headlamps point across the kit.
                GameObject rover = SpawnRover07(temp, new Vector3(1.35f, 0f, 0.20f), -138f, nightEclipse);

                // DeadChargingPost on the front-left (-1.35, 0, -0.55) so its shadow falls behind the kit (-Z), with Cable_Plug seated
                SpawnProp(R1StoryPropsBuilder.DeadChargingPostPrefabPath, new Vector3(-1.35f, 0f, 0.55f), 165f, temp);
                SpawnProp(R1StoryPropsBuilder.CablePlugPrefabPath, new Vector3(-1.14f, 0f, -0.24f), -15f, temp);

                // Sunlit foreground layout of all 6 cable kit pieces next to Rover 07:
                // 1. Cable_Straight_2m running across the front showing regolith sinkage, top dust layer, and centered stake
                SpawnProp(R1StoryPropsBuilder.CableStraight2mPrefabPath, new Vector3(-0.05f, 0f, -0.68f), 90f, temp);

                // 2. Cable_Bend_45 and Cable_Bend_90 flanking the upper row with their ground stakes
                SpawnProp(R1StoryPropsBuilder.CableBend45PrefabPath, new Vector3(-0.45f, 0f, 0.12f), 35f, temp);
                SpawnProp(R1StoryPropsBuilder.CableBend90PrefabPath, new Vector3(0.28f, 0f, 0.15f), -45f, temp);

                // 3. Cable_Plug in foreground-left showing the cream/orange handle, grey locking collar, and copper barrel + 3 pins
                SpawnProp(R1StoryPropsBuilder.CablePlugPrefabPath, new Vector3(-0.52f, 0f, -0.32f), 125f, temp);

                // 4. Cable_Splice_Box in foreground-center showing dusty cream/orange housing, gold foil, and dead dark-glass lamp
                SpawnProp(R1StoryPropsBuilder.CableSpliceBoxPrefabPath, new Vector3(0.06f, 0f, -0.28f), 78f, temp);

                // 5. Cable_BrokenEnd in foreground-right showing cracked sleeve and splayed R1_Copper strands
                SpawnProp(R1StoryPropsBuilder.CableBrokenEndPrefabPath, new Vector3(0.65f, 0f, -0.28f), -135f, temp);

                if (nightEclipse)
                {
                    AddRoverHeadlamps(rover.transform, new Vector3(0.02f, 0.12f, -0.32f), temp);
                }

                string suffix = nightEclipse ? "eclipse" : "dayC";

                // 1. Full kit overview next to Rover 07
                RenderShot(
                    new Vector3(0.05f, 1.52f, -2.75f),
                    new Vector3(0.05f, 0.30f, -0.15f),
                    46f,
                    bg,
                    Path.Combine(outDir, $"r1props_cable_kit_{suffix}.png"));

                // 2. Close-up detail shot of Cable_Plug, Cable_Splice_Box, Cable_BrokenEnd, and staked Cable_Straight_2m
                RenderShot(
                    new Vector3(0.04f, 0.88f, -1.65f),
                    new Vector3(0.05f, 0.12f, -0.32f),
                    40f,
                    bg,
                    Path.Combine(outDir, $"r1props_cable_kit_detail_{suffix}.png"));
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void CaptureDeliverable9_OldLander(string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);
                if (!nightEclipse && RenderSettings.sun != null)
                {
                    RenderSettings.sun.intensity = 1.65f;
                    RenderSettings.sun.transform.rotation = Quaternion.Euler(22f, -38f, 0f);
                }

                // Rover 07 parked next to Prop_OldLander in 3/4 view
                GameObject rover = SpawnRover07(temp, new Vector3(-2.15f, 0f, -0.35f), 132f, nightEclipse);

                // Decal_OldLanderBlast directly under Prop_OldLander at (0.65, 0, 0.15)
                Vector3 landerPos = new Vector3(0.65f, 0f, 0.15f);
                float landerYaw = 168f;
                SpawnProp(R1StoryPropsBuilder.DecalOldLanderBlastPrefabPath, landerPos, landerYaw, temp);
                SpawnProp(R1StoryPropsBuilder.PropOldLanderPrefabPath, landerPos, landerYaw, temp);

                if (nightEclipse)
                {
                    AddRoverHeadlamps(rover.transform, new Vector3(0.55f, 0.95f, 0.10f), temp);
                }

                string suffix = nightEclipse ? "eclipse" : "dayC";

                // 1. Overview next to Rover 07 showing ~3 m height, ~8 deg tilt, half-buried footpad, blast decal, and mast/solar/antenna
                RenderShot(
                    new Vector3(-0.35f, 1.68f, -4.45f),
                    new Vector3(-0.20f, 1.18f, 0.05f),
                    44f,
                    bg,
                    Path.Combine(outDir, $"r1props_old_lander_{suffix}.png"));

                // 2. Detail close-up showing sun-faded tan thermal boxes with S-3 stencil, TV camera mirror hood, folded scoop arm, and removed-part spots
                RenderShot(
                    new Vector3(0.55f, 1.25f, -2.05f),
                    new Vector3(0.62f, 0.95f, 0.10f),
                    38f,
                    bg,
                    Path.Combine(outDir, $"r1props_old_lander_detail_{suffix}.png"));

                // 3. 60 m shot in both golden light (dayC) and eclipse so the Scout's struts and silhouette can be inspected at 60 m
                Vector3 target60 = new Vector3(0.0f, 1.35f, 0.15f);
                Vector3 cam60 = target60 + new Vector3(0.0f, 0.45f, -60.0f);
                RenderShot(
                    cam60,
                    target60,
                    12f,
                    bg,
                    Path.Combine(outDir, $"r1props_old_lander_60m_{suffix}.png"));

                if (!nightEclipse)
                {
                    RenderShot(
                        cam60,
                        target60,
                        12f,
                        bg,
                        Path.Combine(outDir, "r1props_old_lander_60m_silhouette.png"));
                }
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void CaptureDeliverable10_CargoCrateOpen(string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);
                // Rover 07 positioned to the left (-X) of Crate_Cargo_Open, angled toward the crate
                GameObject rover = SpawnRover07(temp, new Vector3(-1.85f, 0f, -0.30f), 115f, nightEclipse);

                // Place Crate_Cargo_Open at origin (0, 0, 0) unscaled (true size 1.4 x 0.9 x 0.7 m shell)
                GameObject crate = SpawnProp(R1StoryPropsBuilder.CrateCargoOpenPrefabPath, Vector3.zero, 0f, temp);

                if (nightEclipse)
                {
                    AddRoverHeadlamps(rover.transform, new Vector3(0.20f, 0.38f, 0.0f), temp);
                }

                string suffix = nightEclipse ? "nightA" : "dayC";

                // 1. Crate_Cargo_Open next to Rover 07 in Day C and Eclipse light
                RenderShot(
                    new Vector3(-0.35f, 1.55f, -3.40f),
                    new Vector3(-0.15f, 0.42f, 0.0f),
                    42f,
                    bg,
                    Path.Combine(outDir, $"r1props_11_cargo_crate_open_{suffix}.png"));

                // 2. Capture from 4 m behind (0, 2, -4) and 2 m up showing both empty sockets & foam cradle cut-outs
                RenderShot(
                    new Vector3(0.0f, 2.0f, -4.0f),
                    new Vector3(0.15f, 0.38f, 0.0f),
                    38f,
                    bg,
                    Path.Combine(outDir, $"r1props_12_cargo_crate_sockets_4m_2m_{suffix}.png"));

                // 3. Place PadFragment_CoilPlate on PartSocket_0 and PadFragment_GuideRail on PartSocket_1
                //    and capture from 4 m behind (0, 2, -4) and 2 m up showing both parts above the rim
                Transform socket0 = crate.transform.Find("PartSocket_0");
                Transform socket1 = crate.transform.Find("PartSocket_1");
                if (socket0 != null && socket1 != null)
                {
                    SpawnProp(R1StoryPropsBuilder.PadFragmentCoilPlatePrefabPath, socket0.position, 0f, temp);
                    SpawnProp(R1StoryPropsBuilder.PadFragmentGuideRailPrefabPath, socket1.position, 0f, temp);

                    RenderShot(
                        new Vector3(0.0f, 2.0f, -4.0f),
                        new Vector3(0.15f, 0.42f, 0.0f),
                        38f,
                        bg,
                        Path.Combine(outDir, $"r1props_13_cargo_crate_with_parts_4m_2m_{suffix}.png"));

                    // Also capture from (0, 2, +4) behind the closed rear wall to verify above-rim visibility over Wall_Back
                    RenderShot(
                        new Vector3(0.0f, 2.0f, 4.0f),
                        new Vector3(0.0f, 0.42f, 0.0f),
                        38f,
                        bg,
                        Path.Combine(outDir, $"r1props_14_cargo_crate_rear_rim_4m_2m_{suffix}.png"));
                }
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void CaptureDeliverable11_BasinLifeLineup(string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);
                if (!nightEclipse && RenderSettings.sun != null)
                {
                    RenderSettings.sun.intensity = 2.0f;
                    RenderSettings.sun.transform.rotation = Quaternion.Euler(24f, -34f, 0f);
                }

                // Rover 07 on the back-right in 3/4 front view facing the 6 Basin Life props
                GameObject rover = SpawnRover07(temp, new Vector3(1.65f, 0f, 0.55f), -132f, nightEclipse);

                // 1. Prop_MissionFlag: back-left (1.61 m tall, sun-bleached flag hanging from horizontal top rod)
                SpawnProp(R1StoryPropsBuilder.PropMissionFlagPrefabPath, new Vector3(-1.45f, 0f, 0.40f), 12f, temp);

                // 2. Prop_SampleRack: mid-left (A-frame rack with rake, tongs, scoop, 2 racked core tubes + 1 fallen tube)
                SpawnProp(R1StoryPropsBuilder.PropSampleRackPrefabPath, new Vector3(-0.55f, 0f, 0.32f), 8f, temp);

                // 3. Prop_Retroreflector: mid-center (tilted 3x3 corner-cube prism array catching grazing light)
                SpawnProp(R1StoryPropsBuilder.PropRetroreflectorPrefabPath, new Vector3(0.35f, 0f, 0.25f), -14f, temp);

                // 4. Prop_SeismicStation: front-left (GoldFoil shroud over seismometer + cable to squat central box with dead lamp)
                SpawnProp(R1StoryPropsBuilder.PropSeismicStationPrefabPath, new Vector3(-1.05f, 0f, -0.32f), 14f, temp);

                // 5. Prop_SurveyTripod: front-right (fallen survey tripod with theodolite head and buckled leg)
                SpawnProp(R1StoryPropsBuilder.PropSurveyTripodPrefabPath, new Vector3(0.55f, 0f, -0.35f), -16f, temp);

                // 6. Decal_BootprintTrail: foreground path of two crossing EVA bootprint trails pressed into the ground
                SpawnProp(R1StoryPropsBuilder.DecalBootprintTrailPrefabPath, new Vector3(-0.15f, 0.001f, -1.05f), 86f, temp);

                if (nightEclipse)
                {
                    AddRoverHeadlamps(rover.transform, new Vector3(-0.15f, 0.18f, -0.25f), temp);
                }

                string suffix = nightEclipse ? "eclipse" : "dayC";
                RenderShot(
                    new Vector3(-0.05f, 1.55f, -3.25f),
                    new Vector3(-0.05f, 0.35f, -0.10f),
                    48f,
                    bg,
                    Path.Combine(outDir, $"r1props_basinlife_lineup_{suffix}.png"));

                // 2. Day C only: the bootprints beside 07 with the sun 9 deg above the horizon, the light they read in
                if (!nightEclipse && RenderSettings.sun != null)
                {
                    RenderSettings.sun.transform.rotation = Quaternion.Euler(9f, -34f, 0f);
                    RenderShot(
                        new Vector3(-0.60f, 1.05f, -2.85f),
                        new Vector3(0.25f, 0.20f, -0.75f),
                        46f,
                        bg,
                        Path.Combine(outDir, "r1props_basinlife_bootprints_grazing_dayC.png"));
                }
            }
            finally
            {
                temp.Dispose();
            }
        }

        /// <summary>
        /// Dedicated fast batch entry point for capturing the seismic station's geophone line
        /// (<c>Prop_GeophoneLine</c>, <c>Geophone_Sensor</c>, <c>Geophone_Ribbon_10m</c>, <c>Geophone_Plug</c>)
        /// next to <c>Prop_SeismicStation</c> and Rover 07 in Day C light:
        /// <c>python tools/unity_batch.py exec --project . --method MoonProject.Art.Editor.R1StoryPropsCapture.CaptureGeophoneLine</c>
        /// </summary>
        public static void CaptureGeophoneLine()
        {
            BatchRunner.Run(nameof(CaptureGeophoneLine), args =>
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
                    CaptureDeliverable12_GeophoneLine(capturesDir, false);
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = prevAsync;
                }

                return true;
            });
        }

        private static void CaptureDeliverable12_GeophoneLine(string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);
                if (!nightEclipse && RenderSettings.sun != null)
                {
                    RenderSettings.sun.intensity = 2.0f;
                    RenderSettings.sun.transform.rotation = Quaternion.Euler(24f, -34f, 0f);
                }

                // Rover 07 on the back-right in 3/4 front view facing Prop_SeismicStation and the geophone line
                GameObject rover = SpawnRover07(temp, new Vector3(1.55f, 0f, 0.45f), -136f, nightEclipse);

                // Prop_SeismicStation on the left at (-1.35, 0, -0.05) so its central box sits around X = -1.0
                SpawnProp(R1StoryPropsBuilder.PropSeismicStationPrefabPath, new Vector3(-1.35f, 0f, -0.05f), 8f, temp);

                // Assembled Prop_GeophoneLine running from Prop_SeismicStation's central box (+X) across the foreground (-Z)
                // Yaw -90 deg puts local -Z (plug at -1.35) at X = -0.95 and local +Z (Sensor_1..3) running right to X = +2.0
                SpawnProp(R1StoryPropsBuilder.PropGeophoneLinePrefabPath, new Vector3(0.40f, 0f, -0.20f), -90f, temp);

                // Modular kit pieces laid out in the foreground for close inspection:
                // Geophone_Ribbon_10m running across the mid-ground behind the line
                SpawnProp(R1StoryPropsBuilder.GeophoneRibbon10mPrefabPath, new Vector3(0.20f, 0f, 0.22f), 90f, temp);

                // Geophone_Plug and 3 Geophone_Sensor instances (showing Stencil_1, Stencil_2, Stencil_3) in front-center
                SpawnProp(R1StoryPropsBuilder.GeophonePlugPrefabPath, new Vector3(-0.62f, 0f, -0.58f), -70f, temp);

                GameObject s1 = SpawnProp(R1StoryPropsBuilder.GeophoneSensorPrefabPath, new Vector3(-0.18f, 0f, -0.58f), 15f, temp);
                SetOnlySensorStencilActive(s1, 1);

                GameObject s2 = SpawnProp(R1StoryPropsBuilder.GeophoneSensorPrefabPath, new Vector3(0.18f, 0f, -0.58f), 0f, temp);
                SetOnlySensorStencilActive(s2, 2);

                GameObject s3 = SpawnProp(R1StoryPropsBuilder.GeophoneSensorPrefabPath, new Vector3(0.54f, 0f, -0.58f), -15f, temp);
                SetOnlySensorStencilActive(s3, 3);

                if (nightEclipse)
                {
                    AddRoverHeadlamps(rover.transform, new Vector3(0.05f, 0.10f, -0.35f), temp);
                }

                string suffix = nightEclipse ? "eclipse" : "dayC";

                // 1. Overview shot showing Prop_SeismicStation, Rover 07, Prop_GeophoneLine, Geophone_Ribbon_10m, Geophone_Plug, and 3 sensors
                RenderShot(
                    new Vector3(0.05f, 1.38f, -2.55f),
                    new Vector3(0.02f, 0.26f, -0.15f),
                    46f,
                    bg,
                    Path.Combine(outDir, $"r1props_geophone_line_{suffix}.png"));

                // 2. Close-up detail shot showing the 3 sensors (stencils 1/2/3, faded orange cap, 3 spikes), flat ribbon cable, and station plug
                RenderShot(
                    new Vector3(0.02f, 0.58f, -1.28f),
                    new Vector3(-0.02f, 0.06f, -0.44f),
                    38f,
                    bg,
                    Path.Combine(outDir, $"r1props_geophone_line_detail_{suffix}.png"));
            }
            finally
            {
                temp.Dispose();
            }
        }

        /// <summary>
        /// Dedicated fast batch entry point for capturing the reworked 1967 Era-P (<c>Prop_OldLander</c>) and 1971 Era-S
        /// (<c>Prop_SeismicStation</c>, <c>Prop_GeophoneLine</c>, <c>Geophone_Sensor</c>, <c>Geophone_Ribbon_10m</c>,
        /// <c>Geophone_Plug</c>, <c>Prop_Retroreflector</c>, <c>Prop_SampleRack</c>) sortie props next to Rover 07 and
        /// a Lumen-era crate in Day C light so the era contrast (P/S foil, bare metal, and white paint vs. L cream and coral)
        /// reads clearly:
        /// <c>python tools/unity_batch.py exec --project . --method MoonProject.Art.Editor.R1StoryPropsCapture.CaptureEraSortiesLineup</c>
        /// </summary>
        public static void CaptureEraSortiesLineup()
        {
            BatchRunner.Run(nameof(CaptureEraSortiesLineup), args =>
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
                    CaptureDeliverable13_EraSortiesLineup(capturesDir, false);
                    CaptureDeliverable9_OldLander(capturesDir, false);
                    CaptureDeliverable11_BasinLifeLineup(capturesDir, false);
                    CaptureDeliverable12_GeophoneLine(capturesDir, false);
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = prevAsync;
                }

                return true;
            });
        }

        private static void CaptureDeliverable13_EraSortiesLineup(string outDir, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);
                if (!nightEclipse && RenderSettings.sun != null)
                {
                    RenderSettings.sun.intensity = 2.0f;
                    RenderSettings.sun.transform.rotation = Quaternion.Euler(24f, -34f, 0f);
                }

                // Era L (2050s/2058) reference hardware on the right (+X):
                // Rover 07 (warm cream & coral-orange) and a Lumen-era crate (Crate_M_Open, warm cream & coral-orange)
                SpawnRover07(temp, new Vector3(1.85f, 0f, 0.55f), -142f, nightEclipse);
                SpawnProp(R1StoryPropsBuilder.CrateMOpenPrefabPath, new Vector3(1.05f, 0f, -0.35f), 165f, temp);

                // Era P (1967) Scout 3 lander on the back-left (-X):
                // Bare aluminium spaceframe & legs, chalked R1_WhitePaint thermal boxes & planar array, R1_SilverFoil mirror patches
                Vector3 landerPos = new Vector3(-1.65f, 0f, 0.85f);
                SpawnProp(R1StoryPropsBuilder.DecalOldLanderBlastPrefabPath, landerPos, 156f, temp);
                SpawnProp(R1StoryPropsBuilder.PropOldLanderPrefabPath, landerPos, 156f, temp);

                // Era S (1971) crewed sortie scientific hardware across the center and foreground:
                // 1. Prop_SeismicStation: R1_WhitePaint top, R1_SilverFoil skirt, R1_BareSteel legs, GoldFoil shroud,
                //    and bolted cream/coral Lumen junction box on its -Z face holding DeadStatusLamp
                SpawnProp(R1StoryPropsBuilder.PropSeismicStationPrefabPath, new Vector3(-0.75f, 0f, -0.32f), 8f, temp);

                // 2. Prop_SampleRack: bare aluminium rack, tools, and core tubes with black rubber grips
                SpawnProp(R1StoryPropsBuilder.PropSampleRackPrefabPath, new Vector3(-0.35f, 0f, 0.35f), 10f, temp);

                // 3. Prop_Retroreflector: bare aluminium stand & housing, GoldFoil rear face, 3x3 corner-cube array
                SpawnProp(R1StoryPropsBuilder.PropRetroreflectorPrefabPath, new Vector3(0.45f, 0f, 0.25f), -12f, temp);

                // 4. Geophone kit (Prop_GeophoneLine, Geophone_Ribbon_10m, Geophone_Plug, and 3 bare-metal Geophone_Sensor units)
                SpawnProp(R1StoryPropsBuilder.PropGeophoneLinePrefabPath, new Vector3(0.25f, 0f, -0.82f), -90f, temp);
                SpawnProp(R1StoryPropsBuilder.GeophonePlugPrefabPath, new Vector3(-0.32f, 0f, -0.62f), -70f, temp);
                GameObject s1 = SpawnProp(R1StoryPropsBuilder.GeophoneSensorPrefabPath, new Vector3(0.02f, 0f, -0.62f), 12f, temp);
                SetOnlySensorStencilActive(s1, 1);
                GameObject s2 = SpawnProp(R1StoryPropsBuilder.GeophoneSensorPrefabPath, new Vector3(0.28f, 0f, -0.62f), 0f, temp);
                SetOnlySensorStencilActive(s2, 2);
                GameObject s3 = SpawnProp(R1StoryPropsBuilder.GeophoneSensorPrefabPath, new Vector3(0.54f, 0f, -0.62f), -12f, temp);
                SetOnlySensorStencilActive(s3, 3);

                string suffix = nightEclipse ? "eclipse" : "dayC";

                // 1. Full lineup overview in Day C light showing Era P (Scout 3), Era S (Seismic Station, Sample Rack,
                //    Retroreflector, Geophone Line), and Era L (Rover 07 + Lumen Crate) side-by-side
                RenderShot(
                    new Vector3(0.05f, 1.65f, -3.65f),
                    new Vector3(0.0f, 0.68f, 0.15f),
                    48f,
                    bg,
                    Path.Combine(outDir, $"r1props_era_sorties_lineup_{suffix}.png"));

                // 2. Close-up detail shot comparing the 1971 Era-S instruments (including the Lumen junction box bolted
                //    to Prop_SeismicStation's side) directly against the warm cream & coral Lumen crate
                RenderShot(
                    new Vector3(0.10f, 0.92f, -2.05f),
                    new Vector3(0.08f, 0.25f, -0.22f),
                    42f,
                    bg,
                    Path.Combine(outDir, $"r1props_era_sorties_detail_{suffix}.png"));
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void SetOnlySensorStencilActive(GameObject sensorInstance, int number)
        {
            if (sensorInstance == null)
            {
                return;
            }

            for (int i = 1; i <= 3; i++)
            {
                Transform stencil = sensorInstance.transform.Find($"Stencil_{i}");
                if (stencil != null)
                {
                    stencil.gameObject.SetActive(i == number);
                }
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
            floor.transform.localScale = new Vector3(18f, 1f, 18f);
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
