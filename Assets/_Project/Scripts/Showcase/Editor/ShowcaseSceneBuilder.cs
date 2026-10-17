using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using MoonProject.Art;
using MoonProject.Art.Editor;
using MoonProject.Editor.Automation;
using MoonProject.Editor.Builders;
using MoonProject.Editor.SceneBuild;
using MoonProject.Rover;
using MoonProject.World;
using MoonProject.World.Editor;

namespace MoonProject.Showcase.Editor
{
    /// <summary>
    /// Builds <c>Assets/_Project/Scenes/Showcase.unity</c> from an empty scene: sets up the real World
    /// (<see cref="WorldSystem"/>, <see cref="SkyCycleSystem"/>, <see cref="PeakBeacon"/>, post-processing
    /// <see cref="Volume"/>), places all 15 look-dev exhibits on the central basin floor with &gt;= 12 m spacing,
    /// wires all animated node paths, <c>Stage_1..4</c> roots, lamps, socket-mounted rover kits, ruin cluster pieces,
    /// camera rig, and Vietnamese UI Toolkit help overlay, and normalizes scene file IDs.
    /// </summary>
    public static class ShowcaseSceneBuilder
    {
        public const string ScenePath = ShowcasePaths.Scene;
        public const string BuilderPath = "Showcase Scene";
        public const string BuilderPattern = "^" + BuilderPath + "$";
        public const int BuilderOrder = 950;

        private static readonly string[] StageNodeNames =
        {
            "Stage_1",
            "Stage_2",
            "Stage_3",
            "Stage_4",
        };

        public static void BuildShowcaseScene()
        {
            BatchRunner.Run(nameof(BuildShowcaseScene), _ =>
            {
                ShowcaseTuningBuilder.Build();
                return BuilderRegistry.DiscoverAndRun(BuilderPattern);
            });
        }

        [MenuItem(BuilderWindow.BuildMenuRoot + BuilderPath, priority = 25)]
        private static void BuildFromMenu()
        {
            BuilderWindow.RunFromMenu(BuilderPattern);
        }

        [MoonBuilder(BuilderPath, BuilderOrder, ReplacesOpenScene = true)]
        public static void Build()
        {
            ShowcaseTuningBuilder.Build();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var tuning = RequireAsset<ShowcaseTuning>(ShowcasePaths.Tuning);
            var controls = RequireAsset<InputActionAsset>(ShowcasePaths.InputActions);
            var worldSettings = RequireAsset<WorldSettings>(WorldPaths.Settings);

            var context = new SceneBuildContext(scene, controls);
            SetupWorld(context, worldSettings, out WorldSystem worldSystem, out SkyCycleSystem skyCycleSystem);

            MoonSurface surface = worldSettings.CreateSurface();
            ShowcaseExhibitRig[] exhibits = SetupExhibits(context, tuning, surface);
            ShowcaseCameraRig cameraRig = SetupCamera(context, tuning, exhibits);
            ShowcaseHelpOverlay helpOverlay = SetupHelpUi(context);
            SetupController(context, tuning, controls, worldSystem, skyCycleSystem, cameraRig, helpOverlay, exhibits);

            context.SortRoots();
            GeneratedAssets.EnsureFolder(Path.GetDirectoryName(ScenePath)?.Replace('\\', '/'));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Saving {ScenePath} failed.");
            }

            SceneFileIds.NormalizeFile(ScenePath);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            BatchRunner.Log($"showcase scene: saved {ScenePath} with {exhibits.Length} exhibits");
        }

        private static void SetupWorld(
            SceneBuildContext context,
            WorldSettings settings,
            out WorldSystem worldSystem,
            out SkyCycleSystem skyCycleSystem)
        {
            var groundMaterial = RequireAsset<Material>(WorldPaths.GroundMaterial);
            var paletteMaterial = RequireAsset<Material>(WorldPaths.R1BasaltMaterial);
            var earthMaterial = RequireAsset<Material>(WorldPaths.EarthMaterial);
            var skyMaterial = RequireAsset<Material>(WorldPaths.SkyMaterial);
            var profile = RequireAsset<VolumeProfile>(WorldPaths.VolumeProfile);
            Transform root = context.WorldRoot.transform;

            var earthlight = context.CreateChild("Earthlight", root).AddComponent<Light>();
            var fill = context.CreateChild("Fill Light", root).AddComponent<Light>();
            var volume = context.CreateChild("Post Processing", root).AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            RenderSettings.skybox = skyMaterial;
            WorldAtmosphere.Apply(settings.Atmosphere, settings.Sky, earthlight, fill);

            PeakBeacon beacon = CreateBeacon(context, settings, root);

            worldSystem = context.CreateChild("World", root).AddComponent<WorldSystem>();
            var serialized = new SerializedObject(worldSystem);
            AssignObject(serialized, "_settings", settings);
            AssignObject(serialized, "_groundMaterial", groundMaterial);
            AssignObject(serialized, "_paletteMaterial", paletteMaterial);
            AssignObject(serialized, "_earthMaterial", earthMaterial);
            AssignObject(serialized, "_groundMarkMaterial", RequireAsset<Material>(WorldPaths.GroundMarkMaterial));
            AssignRocks(context, serialized, "_pebbleRocks", WorldPaths.PebbleRocks);
            AssignRocks(context, serialized, "_boulderRocks", WorldPaths.BoulderRocks);
            AssignObject(serialized, "_earthlight", earthlight);
            AssignObject(serialized, "_fillLight", fill);
            AssignObject(serialized, "_peakBeacon", beacon);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            skyCycleSystem = context.CreateChild("Sky Cycle", root).AddComponent<SkyCycleSystem>();
            var skySerialized = new SerializedObject(skyCycleSystem);
            AssignObject(skySerialized, "_settings", settings);
            AssignObject(skySerialized, "_keyLight", earthlight);
            AssignObject(skySerialized, "_fillLight", fill);
            skySerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PeakBeacon CreateBeacon(SceneBuildContext context, WorldSettings settings, Transform root)
        {
            GameObject node = context.CreateChild(PeakBeacon.NodeName, root);
            node.transform.position = settings.CreateSurface().PeakSummit;
            GameObject model = context.InstantiatePrefab(
                RequireAsset<GameObject>(WorldPaths.BeaconModelPrefab),
                node.transform,
                Vector3.zero,
                Quaternion.identity);

            MeshRenderer lamp = null;
            MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].gameObject.name == PeakBeaconBuilder.LampNodeName)
                {
                    lamp = renderers[i];
                    break;
                }
            }

            if (lamp == null)
            {
                throw new InvalidOperationException($"Peak beacon prefab has no '{PeakBeaconBuilder.LampNodeName}' renderer.");
            }

            GameObject halo = context.CreateChild("Halo", node.transform);
            halo.transform.localPosition = new Vector3(0f, PeakBeaconBuilder.LampHeight, 0f);
            halo.AddComponent<MeshFilter>().sharedMesh = RequireAsset<Mesh>(WorldPaths.BeaconHaloMesh);
            var haloRenderer = halo.AddComponent<MeshRenderer>();
            haloRenderer.sharedMaterial = RequireAsset<Material>(WorldPaths.BeaconHaloMaterial);
            haloRenderer.shadowCastingMode = ShadowCastingMode.Off;
            haloRenderer.receiveShadows = false;
            haloRenderer.lightProbeUsage = LightProbeUsage.Off;
            haloRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            var beacon = node.AddComponent<PeakBeacon>();
            var serialized = new SerializedObject(beacon);
            AssignObject(serialized, "_lamp", lamp);
            AssignObject(serialized, "_halo", haloRenderer);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return beacon;
        }

        private static ShowcaseExhibitRig[] SetupExhibits(
            SceneBuildContext context,
            ShowcaseTuning tuning,
            MoonSurface surface)
        {
            Transform exhibitsRoot = context.Root("Exhibits").transform;
            var rigs = new ShowcaseExhibitRig[10];

            rigs[0] = BuildRoverExhibit(context, exhibitsRoot, tuning, surface);
            rigs[1] = BuildCargoLanderExhibit(context, exhibitsRoot, tuning, surface);
            rigs[2] = BuildChargingPadExhibit(context, exhibitsRoot, tuning, surface);
            rigs[3] = BuildSolarArrayExhibit(context, exhibitsRoot, tuning, surface);
            rigs[4] = BuildPowerPylonExhibit(context, exhibitsRoot, tuning, surface);
            rigs[5] = BuildRockClusterExhibit(context, exhibitsRoot, tuning, surface);
            rigs[6] = BuildRoverUpgradedExhibit(context, exhibitsRoot, tuning, surface);
            rigs[7] = BuildBatteryBankExhibit(context, exhibitsRoot, tuning, surface);
            rigs[8] = BuildAntennaMastExhibit(context, exhibitsRoot, tuning, surface);
            rigs[9] = BuildRuinsCornerExhibit(context, exhibitsRoot, tuning, surface);

            return rigs;
        }

        private static ShowcaseExhibitRig BuildRoverExhibit(
            SceneBuildContext context,
            Transform parent,
            ShowcaseTuning tuning,
            MoonSurface surface)
        {
            ShowcaseExhibitDefinition def = tuning.FindExhibit(ShowcaseExhibitId.Rover07);
            GameObject root = CreateExhibitRoot(context, parent, "Exhibit_1_Rover07", def, surface);
            var rig = root.AddComponent<ShowcaseExhibitRig>();

            GameObject prefab = RequireAsset<GameObject>(ShowcasePaths.RoverPrefab);
            GameObject model = context.InstantiatePrefab(prefab, root.transform, Vector3.zero, prefab.transform.localRotation);

            RequireNode(model.transform, RoverModelNodes.CellFilled).gameObject.SetActive(false);
            RequireNode(model.transform, RoverModelNodes.Decal07Fresh).gameObject.SetActive(false);
            RequireNode(model.transform, RoverModelNodes.Pennant).gameObject.SetActive(false);
            MountKitAtSocket(context, model.transform, RoverModelNodes.Eye, ShowcasePaths.LensGlassPrefab);

            var faceTuning = RequireAsset<FaceTuning>(ShowcasePaths.FaceTuning);
            MeshRenderer screenRenderer = RequireRenderer(model.transform, "Neck/NeckLift/Head/Screen");
            var faceDisplay = model.AddComponent<FaceDisplay>();
            var faceSo = new SerializedObject(faceDisplay);
            AssignObject(faceSo, "_screenRenderer", screenRenderer);
            AssignObject(faceSo, "_tuning", faceTuning);
            faceSo.ApplyModifiedPropertiesWithoutUndo();

            Transform headTransform = RequireNode(model.transform, "Neck/NeckLift/Head");
            var lampAmberMat = RequireAsset<Material>(ShowcasePaths.LampAmberMaterial);
            MeshRenderer[] lampRenderers = FindRenderersUsingMaterial(model.transform, lampAmberMat);

            Light leftSpot = CreateAccentLight(
                context,
                RequireNode(model.transform, "Body/Headlamp_L"),
                "HeadlampSpot_L",
                LightType.Spot,
                new Vector3(0f, 0f, 0.06f),
                Quaternion.Euler(8f, 0f, 0f),
                new Color(1.0f, 0.78f, 0.42f),
                intensity: 2.2f,
                range: 9.5f,
                spotAngle: 64f);
            Light rightSpot = CreateAccentLight(
                context,
                RequireNode(model.transform, "Body/Headlamp_R"),
                "HeadlampSpot_R",
                LightType.Spot,
                new Vector3(0f, 0f, 0.06f),
                Quaternion.Euler(8f, 0f, 0f),
                new Color(1.0f, 0.78f, 0.42f),
                intensity: 2.2f,
                range: 9.5f,
                spotAngle: 64f);

            WireExhibitRig(
                rig,
                ShowcaseExhibitId.Rover07,
                tuning,
                BindNodes(model.transform, def.Nodes),
                lampRenderers,
                Array.Empty<MeshRenderer>(),
                new[] { leftSpot, rightSpot },
                new[] { 2.2f, 2.2f },
                ShowcaseTriggerKey.Action6_R,
                lampsDefaultLit: true,
                faceDisplay: faceDisplay,
                roverHeadTransform: headTransform);

            return rig;
        }

        private static ShowcaseExhibitRig BuildRoverUpgradedExhibit(
            SceneBuildContext context,
            Transform parent,
            ShowcaseTuning tuning,
            MoonSurface surface)
        {
            ShowcaseExhibitDefinition def = tuning.FindExhibit(ShowcaseExhibitId.Rover07Upgraded);
            GameObject root = CreateExhibitRoot(context, parent, "Exhibit_7_Rover07Upgraded", def, surface);
            var rig = root.AddComponent<ShowcaseExhibitRig>();

            GameObject prefab = RequireAsset<GameObject>(ShowcasePaths.RoverPrefab);
            GameObject model = context.InstantiatePrefab(prefab, root.transform, Vector3.zero, prefab.transform.localRotation);

            // Mirror left-side sockets and rear ring socket identically to RoverPrefabBuilder
            Transform drumLeft = RequireNode(model.transform, RoverModelNodes.DrumSocketLeft);
            drumLeft.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Transform binLeft = RequireNode(model.transform, RoverModelNodes.BinSocketLeft);
            binLeft.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Transform ringSocket = RequireNode(model.transform, RoverModelNodes.RingSocket);
            ringSocket.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Mount every R1 kit prefab at its socket from RoverModelNodes
            MountKitAtSocket(context, model.transform, RoverModelNodes.HeadlampSocket, ShowcasePaths.KitLampBarPrefab);
            MountKitAtSocket(context, model.transform, RoverModelNodes.DrumSocketLeft, ShowcasePaths.KitCapacitorDrumPrefab);
            MountKitAtSocket(context, model.transform, RoverModelNodes.DrumSocketRight, ShowcasePaths.KitCapacitorDrumPrefab);
            MountKitAtSocket(context, model.transform, RoverModelNodes.BinSocketLeft, ShowcasePaths.KitSideBinPrefab);
            MountKitAtSocket(context, model.transform, RoverModelNodes.BinSocketRight, ShowcasePaths.KitSideBinPrefab);
            MountKitAtSocket(context, model.transform, RoverModelNodes.CargoSocket, ShowcasePaths.KitCargoRackPrefab);
            MountKitAtSocket(context, model.transform, RoverModelNodes.SpineSocket, ShowcasePaths.KitBatterySpinePrefab);
            MountKitAtSocket(context, model.transform, RoverModelNodes.CoilSocket, ShowcasePaths.HoverCoilsPrefab);
            MountKitAtSocket(context, model.transform, RoverModelNodes.DeckSocket, ShowcasePaths.KitTapeDeckPrefab);
            MountKitAtSocket(context, model.transform, RoverModelNodes.RingSocket, ShowcasePaths.KitTetherRingPrefab);
            MountKitAtSocket(context, model.transform, RoverModelNodes.LensSocket, ShowcasePaths.FocusLensPrefab);
            MountKitAtSocket(context, model.transform, RoverModelNodes.Eye, ShowcasePaths.LensGlassPrefab);
            GameObject scanFan = MountKitAtSocket(context, model.transform, RoverModelNodes.ScanFanSocket, ShowcasePaths.ScanFanPrefab);
            foreach (Renderer fanRenderer in scanFan.GetComponentsInChildren<Renderer>(true))
            {
                fanRenderer.enabled = false;
            }

            // Activate all gift visuals on the fully upgraded 07
            RequireNode(model.transform, RoverModelNodes.CellFilled).gameObject.SetActive(true);
            RequireNode(model.transform, RoverModelNodes.Decal07Fresh).gameObject.SetActive(true);
            RequireNode(model.transform, RoverModelNodes.Pennant).gameObject.SetActive(true);

            var faceTuning = RequireAsset<FaceTuning>(ShowcasePaths.FaceTuning);
            MeshRenderer screenRenderer = RequireRenderer(model.transform, "Neck/NeckLift/Head/Screen");
            var faceDisplay = model.AddComponent<FaceDisplay>();
            var faceSo = new SerializedObject(faceDisplay);
            AssignObject(faceSo, "_screenRenderer", screenRenderer);
            AssignObject(faceSo, "_tuning", faceTuning);
            faceSo.ApplyModifiedPropertiesWithoutUndo();

            Transform headTransform = RequireNode(model.transform, "Neck/NeckLift/Head");
            var lampAmberMat = RequireAsset<Material>(ShowcasePaths.LampAmberMaterial);
            MeshRenderer[] lampRenderers = FindRenderersUsingMaterial(model.transform, lampAmberMat);

            Light leftSpot = CreateAccentLight(
                context,
                RequireNode(model.transform, "Body/Headlamp_L"),
                "HeadlampSpot_L",
                LightType.Spot,
                new Vector3(0f, 0f, 0.06f),
                Quaternion.Euler(8f, 0f, 0f),
                new Color(1.0f, 0.78f, 0.42f),
                intensity: 2.4f,
                range: 10.5f,
                spotAngle: 66f);
            Light rightSpot = CreateAccentLight(
                context,
                RequireNode(model.transform, "Body/Headlamp_R"),
                "HeadlampSpot_R",
                LightType.Spot,
                new Vector3(0f, 0f, 0.06f),
                Quaternion.Euler(8f, 0f, 0f),
                new Color(1.0f, 0.78f, 0.42f),
                intensity: 2.4f,
                range: 10.5f,
                spotAngle: 66f);
            Light roofBarGlow = CreateAccentLight(
                context,
                RequireNode(model.transform, RoverModelNodes.HeadlampSocket),
                "LampBarGlow",
                LightType.Point,
                new Vector3(0f, 0.08f, 0.12f),
                Quaternion.identity,
                new Color(1.0f, 0.76f, 0.38f),
                intensity: 1.6f,
                range: 4.5f);

            WireExhibitRig(
                rig,
                ShowcaseExhibitId.Rover07Upgraded,
                tuning,
                BindNodes(model.transform, def.Nodes),
                lampRenderers,
                Array.Empty<MeshRenderer>(),
                new[] { leftSpot, rightSpot, roofBarGlow },
                new[] { 2.4f, 2.4f, 1.6f },
                ShowcaseTriggerKey.Action6_R,
                lampsDefaultLit: true,
                faceDisplay: faceDisplay,
                roverHeadTransform: headTransform);

            return rig;
        }

        private static GameObject MountKitAtSocket(
            SceneBuildContext context,
            Transform roverModelRoot,
            string socketRelativePath,
            string kitPrefabPath)
        {
            Transform socket = RequireNode(roverModelRoot, socketRelativePath);
            var kitPrefab = RequireAsset<GameObject>(kitPrefabPath);
            GameObject instance = context.InstantiatePrefab(
                kitPrefab,
                socket,
                Vector3.zero,
                Quaternion.identity);
            instance.SetActive(true);
            return instance;
        }

        private static ShowcaseExhibitRig BuildCargoLanderExhibit(
            SceneBuildContext context,
            Transform parent,
            ShowcaseTuning tuning,
            MoonSurface surface)
        {
            ShowcaseExhibitDefinition def = tuning.FindExhibit(ShowcaseExhibitId.CargoLander);
            GameObject root = CreateExhibitRoot(context, parent, "Exhibit_2_CargoLander", def, surface);
            var rig = root.AddComponent<ShowcaseExhibitRig>();

            GameObject prefab = RequireAsset<GameObject>(ShowcasePaths.CargoLanderPrefab);
            GameObject model = context.InstantiatePrefab(prefab, root.transform, Vector3.zero, prefab.transform.localRotation);

            MeshRenderer[] bayLamps =
            {
                RequireRenderer(model.transform, "BayLight_1"),
                RequireRenderer(model.transform, "BayLight_2"),
                RequireRenderer(model.transform, "BayLight_3"),
                RequireRenderer(model.transform, "BayLight_4"),
            };

            GameObject[] brokenObjects =
            {
                RequireNode(model.transform, "Bay_CableGap").gameObject,
                RequireNode(model.transform, "Bay_ConnectorSocket").gameObject,
            };
            GameObject[] repairedObjects =
            {
                RequireNode(model.transform, "Bay_CableFixed").gameObject,
                RequireNode(model.transform, "Bay_Connector").gameObject,
            };
            brokenObjects[0].SetActive(false);
            brokenObjects[1].SetActive(false);
            repairedObjects[0].SetActive(true);
            repairedObjects[1].SetActive(true);

            Light bayFrontLight = CreateAccentLight(
                context,
                root.transform,
                "BayPointLight_Front",
                LightType.Point,
                new Vector3(0f, 1.95f, 1.55f),
                Quaternion.identity,
                new Color(1.0f, 0.74f, 0.36f),
                intensity: 2.8f,
                range: 6.5f);
            Light bayInnerLight = CreateAccentLight(
                context,
                root.transform,
                "BayPointLight_Inner",
                LightType.Point,
                new Vector3(0f, 1.55f, 0.25f),
                Quaternion.identity,
                new Color(1.0f, 0.72f, 0.34f),
                intensity: 2.2f,
                range: 5.5f);

            WireExhibitRig(
                rig,
                ShowcaseExhibitId.CargoLander,
                tuning,
                BindNodes(model.transform, def.Nodes),
                bayLamps,
                Array.Empty<MeshRenderer>(),
                new[] { bayFrontLight, bayInnerLight },
                new[] { 2.8f, 2.2f },
                ShowcaseTriggerKey.Action5_G,
                lampsDefaultLit: true,
                brokenBayObjects: brokenObjects,
                repairedBayObjects: repairedObjects);

            return rig;
        }

        private static ShowcaseExhibitRig BuildChargingPadExhibit(
            SceneBuildContext context,
            Transform parent,
            ShowcaseTuning tuning,
            MoonSurface surface)
        {
            ShowcaseExhibitDefinition def = tuning.FindExhibit(ShowcaseExhibitId.ChargingPad);
            GameObject root = CreateExhibitRoot(context, parent, "Exhibit_3_ChargingPad", def, surface);
            var rig = root.AddComponent<ShowcaseExhibitRig>();

            GameObject prefab = RequireAsset<GameObject>(ShowcasePaths.ChargingPadPrefab);
            GameObject model = context.InstantiatePrefab(prefab, root.transform, Vector3.zero, prefab.transform.localRotation);

            MeshRenderer[] totemBars =
            {
                RequireRenderer(model.transform, "Stage_4/TotemBar_1"),
                RequireRenderer(model.transform, "Stage_4/TotemBar_2"),
                RequireRenderer(model.transform, "Stage_4/TotemBar_3"),
                RequireRenderer(model.transform, "Stage_4/TotemBar_4"),
            };
            MeshRenderer[] guideStrips =
            {
                RequireRenderer(model.transform, "Stage_4/GuideStrip_L"),
                RequireRenderer(model.transform, "Stage_4/GuideStrip_R"),
            };

            Light padGlow = CreateAccentLight(
                context,
                root.transform,
                "ChargingPadGlow",
                LightType.Point,
                new Vector3(0f, 1.05f, -0.65f),
                Quaternion.identity,
                new Color(1.0f, 0.74f, 0.36f),
                intensity: 1.6f,
                range: 4.5f);

            BindStages(model.transform, out Transform[] stageRoots, out Vector3[] stageRestPositions);

            WireExhibitRig(
                rig,
                ShowcaseExhibitId.ChargingPad,
                tuning,
                BindNodes(model.transform, def.Nodes),
                totemBars,
                guideStrips,
                new[] { padGlow },
                new[] { 1.6f },
                ShowcaseTriggerKey.Action3_C,
                lampsDefaultLit: true,
                stageRoots: stageRoots,
                stageRestPositions: stageRestPositions);

            return rig;
        }

        private static ShowcaseExhibitRig BuildSolarArrayExhibit(
            SceneBuildContext context,
            Transform parent,
            ShowcaseTuning tuning,
            MoonSurface surface)
        {
            ShowcaseExhibitDefinition def = tuning.FindExhibit(ShowcaseExhibitId.SolarArray);
            GameObject root = CreateExhibitRoot(context, parent, "Exhibit_4_SolarArray", def, surface);
            var rig = root.AddComponent<ShowcaseExhibitRig>();

            GameObject prefab = RequireAsset<GameObject>(ShowcasePaths.SolarArrayPrefab);
            GameObject model = context.InstantiatePrefab(prefab, root.transform, Vector3.zero, prefab.transform.localRotation);

            Transform stage4 = RequireNode(model.transform, "Stage_4");
            MeshRenderer[] statusLamp =
            {
                RequireRenderer(model.transform, "Stage_4/StatusLamp"),
            };

            Light lampLight = CreateAccentLight(
                context,
                root.transform,
                "SolarStatusGlow",
                LightType.Point,
                new Vector3(0f, 0.85f, 0.32f),
                Quaternion.identity,
                new Color(1.0f, 0.74f, 0.36f),
                intensity: 1.1f,
                range: 3.2f);

            BindStages(model.transform, out Transform[] stageRoots, out Vector3[] stageRestPositions);

            WireExhibitRig(
                rig,
                ShowcaseExhibitId.SolarArray,
                tuning,
                BindNodes(model.transform, def.Nodes),
                statusLamp,
                Array.Empty<MeshRenderer>(),
                new[] { lampLight },
                new[] { 1.1f },
                ShowcaseTriggerKey.Action3_C,
                lampsDefaultLit: true,
                solarStage4Transform: stage4,
                stageRoots: stageRoots,
                stageRestPositions: stageRestPositions);

            return rig;
        }

        private static ShowcaseExhibitRig BuildPowerPylonExhibit(
            SceneBuildContext context,
            Transform parent,
            ShowcaseTuning tuning,
            MoonSurface surface)
        {
            ShowcaseExhibitDefinition def = tuning.FindExhibit(ShowcaseExhibitId.PowerPylon);
            GameObject root = CreateExhibitRoot(context, parent, "Exhibit_5_PowerPylon", def, surface);
            var rig = root.AddComponent<ShowcaseExhibitRig>();

            GameObject prefab = RequireAsset<GameObject>(ShowcasePaths.PowerPylonPrefab);
            GameObject model = context.InstantiatePrefab(prefab, root.transform, Vector3.zero, prefab.transform.localRotation);

            MeshRenderer[] pathLamp =
            {
                RequireRenderer(model.transform, "Stage_4/PathLamp"),
            };

            Light pylonLight = CreateAccentLight(
                context,
                root.transform,
                "PylonPathLight",
                LightType.Point,
                new Vector3(0.32f, 1.92f, 0.05f),
                Quaternion.identity,
                new Color(1.0f, 0.75f, 0.38f),
                intensity: 2.2f,
                range: 5.8f);

            BindStages(model.transform, out Transform[] stageRoots, out Vector3[] stageRestPositions);

            WireExhibitRig(
                rig,
                ShowcaseExhibitId.PowerPylon,
                tuning,
                BindNodes(model.transform, def.Nodes),
                pathLamp,
                Array.Empty<MeshRenderer>(),
                new[] { pylonLight },
                new[] { 2.2f },
                ShowcaseTriggerKey.Action3_C,
                lampsDefaultLit: true,
                stageRoots: stageRoots,
                stageRestPositions: stageRestPositions);

            return rig;
        }

        private static ShowcaseExhibitRig BuildRockClusterExhibit(
            SceneBuildContext context,
            Transform parent,
            ShowcaseTuning tuning,
            MoonSurface surface)
        {
            ShowcaseExhibitDefinition def = tuning.FindExhibit(ShowcaseExhibitId.RockCluster);
            GameObject root = CreateExhibitRoot(context, parent, "Exhibit_6_RockCluster", def, surface);
            var rig = root.AddComponent<ShowcaseExhibitRig>();

            (Vector3 offset, float yaw, Vector3 scale)[] placements =
            {
                (new Vector3(0.00f, -0.06f, 0.00f), 25f, new Vector3(1.35f, 1.10f, 1.25f)),
                (new Vector3(-1.35f, -0.05f, 0.45f), 110f, new Vector3(1.05f, 0.92f, 1.10f)),
                (new Vector3(1.25f, -0.04f, 0.55f), 215f, new Vector3(0.95f, 0.88f, 1.02f)),
                (new Vector3(-0.65f, -0.04f, 1.35f), 64f, new Vector3(0.76f, 0.72f, 0.82f)),
                (new Vector3(0.75f, -0.04f, 1.40f), 310f, new Vector3(0.72f, 0.68f, 0.78f)),
                (new Vector3(-1.75f, -0.03f, -0.75f), 148f, new Vector3(0.84f, 0.76f, 0.88f)),
                (new Vector3(1.65f, -0.03f, -0.65f), 192f, new Vector3(0.80f, 0.74f, 0.85f)),
                (new Vector3(-0.45f, -0.02f, -1.25f), 42f, new Vector3(0.58f, 0.52f, 0.62f)),
                (new Vector3(0.55f, -0.02f, -1.15f), 275f, new Vector3(0.52f, 0.48f, 0.56f)),
                (new Vector3(0.10f, -0.02f, 1.95f), 130f, new Vector3(0.44f, 0.40f, 0.48f)),
            };

            for (int i = 0; i < ShowcasePaths.RockPrefabs.Length && i < placements.Length; i++)
            {
                var rockPrefab = RequireAsset<GameObject>(ShowcasePaths.RockPrefabs[i]);
                (Vector3 offset, float yaw, Vector3 scale) p = placements[i];
                Quaternion rot = Quaternion.Euler(0f, p.yaw, 0f) * rockPrefab.transform.localRotation;
                GameObject rockInstance = context.InstantiatePrefab(rockPrefab, root.transform, p.offset, rot);
                rockInstance.transform.localScale = p.scale;
            }

            WireExhibitRig(
                rig,
                ShowcaseExhibitId.RockCluster,
                tuning,
                Array.Empty<ShowcaseBoundNode>(),
                Array.Empty<MeshRenderer>(),
                Array.Empty<MeshRenderer>(),
                Array.Empty<Light>(),
                Array.Empty<float>(),
                ShowcaseTriggerKey.None,
                lampsDefaultLit: false);

            return rig;
        }

        private static ShowcaseExhibitRig BuildBatteryBankExhibit(
            SceneBuildContext context,
            Transform parent,
            ShowcaseTuning tuning,
            MoonSurface surface)
        {
            return BuildStageStructureExhibit(
                context,
                parent,
                tuning,
                surface,
                ShowcaseExhibitId.BatteryBank,
                "Exhibit_8_BatteryBank",
                ShowcasePaths.BatteryBankPrefab,
                ShowcaseTriggerKey.Action4_V,
                new Vector3(0f, 1.65f, 0.45f),
                lightIntensity: 1.8f,
                lightRange: 4.8f);
        }

        private static ShowcaseExhibitRig BuildAntennaMastExhibit(
            SceneBuildContext context,
            Transform parent,
            ShowcaseTuning tuning,
            MoonSurface surface)
        {
            return BuildStageStructureExhibit(
                context,
                parent,
                tuning,
                surface,
                ShowcaseExhibitId.AntennaMast,
                "Exhibit_9_AntennaMast",
                ShowcasePaths.AntennaMastPrefab,
                ShowcaseTriggerKey.Action4_V,
                new Vector3(0f, 3.15f, 0.35f),
                lightIntensity: 2.0f,
                lightRange: 5.8f);
        }

        private static ShowcaseExhibitRig BuildStageStructureExhibit(
            SceneBuildContext context,
            Transform parent,
            ShowcaseTuning tuning,
            MoonSurface surface,
            ShowcaseExhibitId exhibitId,
            string rootName,
            string prefabPath,
            ShowcaseTriggerKey lampTriggerKey,
            Vector3 accentLightLocalPos,
            float lightIntensity,
            float lightRange)
        {
            ShowcaseExhibitDefinition def = tuning.FindExhibit(exhibitId);
            GameObject root = CreateExhibitRoot(context, parent, rootName, def, surface);
            var rig = root.AddComponent<ShowcaseExhibitRig>();

            GameObject prefab = RequireAsset<GameObject>(prefabPath);
            GameObject model = context.InstantiatePrefab(prefab, root.transform, Vector3.zero, prefab.transform.localRotation);

            var lampAmberMat = RequireAsset<Material>(ShowcasePaths.LampAmberMaterial);
            MeshRenderer[] lampRenderers = FindRenderersUsingMaterial(model.transform, lampAmberMat);

            Light glowLight = CreateAccentLight(
                context,
                root.transform,
                $"{exhibitId}Glow",
                LightType.Point,
                accentLightLocalPos,
                Quaternion.identity,
                new Color(1.0f, 0.74f, 0.36f),
                intensity: lightIntensity,
                range: lightRange);

            BindStages(model.transform, out Transform[] stageRoots, out Vector3[] stageRestPositions);

            WireExhibitRig(
                rig,
                exhibitId,
                tuning,
                BindNodes(model.transform, def.Nodes),
                lampRenderers,
                Array.Empty<MeshRenderer>(),
                new[] { glowLight },
                new[] { lightIntensity },
                lampTriggerKey,
                lampsDefaultLit: true,
                stageRoots: stageRoots,
                stageRestPositions: stageRestPositions);

            return rig;
        }

        private static ShowcaseExhibitRig BuildRuinsCornerExhibit(
            SceneBuildContext context,
            Transform parent,
            ShowcaseTuning tuning,
            MoonSurface surface)
        {
            ShowcaseExhibitDefinition def = tuning.FindExhibit(ShowcaseExhibitId.RuinsCorner);
            GameObject root = CreateExhibitRoot(context, parent, "Exhibit_10_RuinsCorner", def, surface);
            var rig = root.AddComponent<ShowcaseExhibitRig>();

            ShowcaseRuinPiecePlacement[] pieces = tuning.RuinPieces;
            if (pieces != null)
            {
                for (int i = 0; i < pieces.Length; i++)
                {
                    ShowcaseRuinPiecePlacement piece = pieces[i];
                    if (piece == null)
                    {
                        continue;
                    }

                    string prefabPath = ResolveRuinPrefabPath(piece.PieceId);
                    GameObject prefab = RequireAsset<GameObject>(prefabPath);

                    Vector3 localPos = piece.LocalOffset;
                    Vector3 worldXZ = root.transform.TransformPoint(new Vector3(localPos.x, 0f, localPos.z));
                    localPos.y = surface.SampleHeight(worldXZ.x, worldXZ.z) - root.transform.position.y;

                    Quaternion localRot = Quaternion.Euler(0f, piece.LocalYawDegrees, 0f) * prefab.transform.localRotation;
                    GameObject instance = context.InstantiatePrefab(prefab, root.transform, localPos, localRot);

                    if (piece.PieceId == ShowcaseRuinPieceId.HabitatModules)
                    {
                        EnsureAirlockCrtStatusLook(instance.transform);
                    }
                }
            }

            var lampAmberMat = RequireAsset<Material>(ShowcasePaths.LampAmberMaterial);
            MeshRenderer[] lampRenderers = FindRenderersUsingMaterial(root.transform, lampAmberMat);

            Light workshopLight = CreateAccentLight(
                context,
                root.transform,
                "RuinsWorkshopGlow",
                LightType.Point,
                new Vector3(5.6f, 1.65f, 0.25f),
                Quaternion.identity,
                new Color(1.0f, 0.74f, 0.36f),
                intensity: 2.1f,
                range: 7.5f);
            Light habitatAirlockLight = CreateAccentLight(
                context,
                root.transform,
                "RuinsAirlockGlow",
                LightType.Point,
                new Vector3(-3.8f, 1.35f, -0.60f),
                Quaternion.identity,
                new Color(1.0f, 0.76f, 0.40f),
                intensity: 1.8f,
                range: 6.5f);
            Light towerJunctionLight = CreateAccentLight(
                context,
                root.transform,
                "RuinsTowerGlow",
                LightType.Point,
                new Vector3(1.6f, 1.85f, 8.20f),
                Quaternion.identity,
                new Color(1.0f, 0.72f, 0.34f),
                intensity: 1.6f,
                range: 6.0f);

            WireExhibitRig(
                rig,
                ShowcaseExhibitId.RuinsCorner,
                tuning,
                Array.Empty<ShowcaseBoundNode>(),
                lampRenderers,
                Array.Empty<MeshRenderer>(),
                new[] { workshopLight, habitatAirlockLight, towerJunctionLight },
                new[] { 2.1f, 1.8f, 1.6f },
                ShowcaseTriggerKey.Action1_Z,
                lampsDefaultLit: true);

            return rig;
        }

        private static string ResolveRuinPrefabPath(ShowcaseRuinPieceId pieceId)
        {
            switch (pieceId)
            {
                case ShowcaseRuinPieceId.HabitatModules:
                    return ShowcasePaths.HabitatModulesPrefab;
                case ShowcaseRuinPieceId.RadioTower:
                    return ShowcasePaths.RadioTowerPrefab;
                case ShowcaseRuinPieceId.KenjiWorkshop:
                    return ShowcasePaths.KenjiWorkshopPrefab;
                case ShowcaseRuinPieceId.FallenSolarArray:
                    return ShowcasePaths.FallenSolarArrayPrefab;
                case ShowcaseRuinPieceId.ScatterSet:
                    return ShowcasePaths.ScatterSetPrefab;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pieceId), pieceId, null);
            }
        }

        private static void EnsureAirlockCrtStatusLook(Transform habitatRoot)
        {
            MeshRenderer panelRenderer = RequireRenderer(habitatRoot, "AirlockPanel");
            var crtMat = AssetDatabase.LoadAssetAtPath<Material>(ShowcasePaths.CrtFaceMaterial);
            if (crtMat == null)
            {
                crtMat = RequireAsset<Material>(ShowcasePaths.LampAmberMaterial);
            }

            Material[] shared = panelRenderer.sharedMaterials;
            if (shared != null && shared.Length > 2 && shared[2] != crtMat)
            {
                shared[2] = crtMat;
                panelRenderer.sharedMaterials = shared;
            }
        }

        private static ShowcaseCameraRig SetupCamera(
            SceneBuildContext context,
            ShowcaseTuning tuning,
            ShowcaseExhibitRig[] exhibits)
        {
            GameObject camRoot = context.Root("Showcase Camera");
            camRoot.tag = "MainCamera";

            var camera = camRoot.AddComponent<Camera>();
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 4000f;
            camRoot.AddComponent<AudioListener>();

            var urpData = camRoot.AddComponent<UniversalAdditionalCameraData>();
            urpData.renderPostProcessing = true;
            urpData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            urpData.antialiasingQuality = AntialiasingQuality.High;

            var rig = camRoot.AddComponent<ShowcaseCameraRig>();
            var so = new SerializedObject(rig);
            AssignObject(so, "_camera", camera);
            AssignObject(so, "_tuning", tuning);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (exhibits.Length > 0 && exhibits[0] != null)
            {
                ShowcaseExhibitDefinition def = exhibits[0].Definition ?? tuning.FindExhibit(ShowcaseExhibitId.Rover07);
                if (def != null)
                {
                    Quaternion rot = Quaternion.Euler(def.DefaultOrbitPitch, def.DefaultOrbitYaw, 0f);
                    Vector3 pivot = exhibits[0].transform.position + def.OrbitPivotOffset;
                    camRoot.transform.SetPositionAndRotation(
                        pivot - (rot * Vector3.forward) * def.DefaultOrbitDistance,
                        rot);
                }
            }

            return rig;
        }

        private static ShowcaseHelpOverlay SetupHelpUi(SceneBuildContext context)
        {
            GameObject uiRoot = context.Root("Showcase UI");
            var uiDocument = uiRoot.AddComponent<UIDocument>();
            uiDocument.panelSettings = RequireAsset<PanelSettings>(ShowcasePaths.PanelSettings);

            var overlay = uiRoot.AddComponent<ShowcaseHelpOverlay>();
            var so = new SerializedObject(overlay);
            AssignObject(so, "_uiDocument", uiDocument);
            AssignObject(so, "_regularFontAsset", AssetDatabase.LoadAssetAtPath<FontAsset>(ShowcasePaths.FontRegularSdf));
            AssignObject(so, "_semiBoldFontAsset", AssetDatabase.LoadAssetAtPath<FontAsset>(ShowcasePaths.FontSemiBoldSdf));
            AssignObject(so, "_fallbackRegularFont", RequireAsset<Font>(ShowcasePaths.FontRegularTtf));
            so.ApplyModifiedPropertiesWithoutUndo();

            return overlay;
        }

        private static void SetupController(
            SceneBuildContext context,
            ShowcaseTuning tuning,
            InputActionAsset controls,
            WorldSystem worldSystem,
            SkyCycleSystem skyCycleSystem,
            ShowcaseCameraRig cameraRig,
            ShowcaseHelpOverlay helpOverlay,
            ShowcaseExhibitRig[] exhibits)
        {
            GameObject root = context.Root("Showcase");
            var controller = root.AddComponent<ShowcaseController>();
            var gameControls = RequireAsset<InputActionAsset>(ShowcasePaths.GameInputActions);
            var so = new SerializedObject(controller);
            AssignObject(so, "_tuning", tuning);
            AssignObject(so, "_inputActions", controls);
            AssignObject(so, "_gameInputActions", gameControls);
            AssignObject(so, "_worldSystem", worldSystem);
            AssignObject(so, "_skyCycleSystem", skyCycleSystem);
            AssignObject(so, "_cameraRig", cameraRig);
            AssignObject(so, "_helpOverlay", helpOverlay);

            SerializedProperty exhibitsProp = so.FindProperty("_exhibits");
            exhibitsProp.arraySize = exhibits.Length;
            for (int i = 0; i < exhibits.Length; i++)
            {
                exhibitsProp.GetArrayElementAtIndex(i).objectReferenceValue = exhibits[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateExhibitRoot(
            SceneBuildContext context,
            Transform parent,
            string name,
            ShowcaseExhibitDefinition def,
            MoonSurface surface)
        {
            GameObject go = context.CreateChild(name, parent);
            Vector3 pos = def != null ? def.PositionOffset : Vector3.zero;
            pos.y = surface.SampleHeight(pos.x, pos.z);
            float yaw = def != null ? def.YawDegrees : 0f;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        private static ShowcaseBoundNode[] BindNodes(Transform modelRoot, ShowcaseNodeRule[] rules)
        {
            if (rules == null || rules.Length == 0)
            {
                return Array.Empty<ShowcaseBoundNode>();
            }

            var bound = new ShowcaseBoundNode[rules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                ShowcaseNodeRule rule = rules[i];
                Transform target = RequireNode(modelRoot, rule.NodePath);
                bound[i] = new ShowcaseBoundNode(rule.RuleId, target, target.localPosition, target.localRotation);
            }

            return bound;
        }

        private static void BindStages(
            Transform modelRoot,
            out Transform[] stageRoots,
            out Vector3[] stageRestPositions)
        {
            stageRoots = new Transform[StageNodeNames.Length];
            stageRestPositions = new Vector3[StageNodeNames.Length];
            for (int i = 0; i < StageNodeNames.Length; i++)
            {
                Transform stage = RequireNode(modelRoot, StageNodeNames[i]);
                stageRoots[i] = stage;
                stageRestPositions[i] = stage.localPosition;
            }
        }

        private static Light CreateAccentLight(
            SceneBuildContext context,
            Transform parent,
            string name,
            LightType type,
            Vector3 localPos,
            Quaternion localRot,
            Color color,
            float intensity,
            float range,
            float spotAngle = 60f)
        {
            GameObject go = context.CreateChild(name, parent);
            go.transform.SetLocalPositionAndRotation(localPos, localRot);
            var light = go.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = spotAngle;
            light.shadows = LightShadows.None;
            return light;
        }

        private static void WireExhibitRig(
            ShowcaseExhibitRig rig,
            ShowcaseExhibitId exhibitId,
            ShowcaseTuning tuning,
            ShowcaseBoundNode[] boundNodes,
            MeshRenderer[] lampRenderers,
            MeshRenderer[] secondaryLampRenderers,
            Light[] accentLights,
            float[] accentMaxIntensities,
            ShowcaseTriggerKey lampTriggerKey,
            bool lampsDefaultLit,
            FaceDisplay faceDisplay = null,
            Transform roverHeadTransform = null,
            GameObject[] brokenBayObjects = null,
            GameObject[] repairedBayObjects = null,
            Transform solarStage4Transform = null,
            Transform[] stageRoots = null,
            Vector3[] stageRestPositions = null)
        {
            var so = new SerializedObject(rig);
            so.FindProperty("_exhibitId").enumValueIndex = (int)exhibitId;
            AssignObject(so, "_tuning", tuning);
            so.FindProperty("_lampTriggerKey").enumValueIndex = (int)lampTriggerKey;
            so.FindProperty("_lampsDefaultLit").boolValue = lampsDefaultLit;
            AssignObject(so, "_faceDisplay", faceDisplay);
            AssignObject(so, "_roverHeadTransform", roverHeadTransform);
            AssignObject(so, "_solarStage4Transform", solarStage4Transform);

            SerializedProperty boundProp = so.FindProperty("_boundNodes");
            boundProp.arraySize = boundNodes.Length;
            for (int i = 0; i < boundNodes.Length; i++)
            {
                SerializedProperty elem = boundProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("_ruleId").stringValue = boundNodes[i].RuleId;
                elem.FindPropertyRelative("_target").objectReferenceValue = boundNodes[i].Target;
                elem.FindPropertyRelative("_restLocalPosition").vector3Value = boundNodes[i].RestLocalPosition;
                elem.FindPropertyRelative("_restLocalRotation").quaternionValue = boundNodes[i].RestLocalRotation;
            }

            AssignObjectArray(so, "_stageRoots", stageRoots ?? Array.Empty<Transform>());
            SerializedProperty stageRestProp = so.FindProperty("_stageRestLocalPositions");
            Vector3[] restPositions = stageRestPositions ?? Array.Empty<Vector3>();
            stageRestProp.arraySize = restPositions.Length;
            for (int i = 0; i < restPositions.Length; i++)
            {
                stageRestProp.GetArrayElementAtIndex(i).vector3Value = restPositions[i];
            }

            AssignObjectArray(so, "_lampRenderers", lampRenderers);
            AssignObjectArray(so, "_secondaryLampRenderers", secondaryLampRenderers);
            AssignObjectArray(so, "_accentLights", accentLights);
            AssignObjectArray(so, "_brokenBayObjects", brokenBayObjects ?? Array.Empty<GameObject>());
            AssignObjectArray(so, "_repairedBayObjects", repairedBayObjects ?? Array.Empty<GameObject>());

            SerializedProperty intensitiesProp = so.FindProperty("_accentLightMaxIntensities");
            intensitiesProp.arraySize = accentMaxIntensities.Length;
            for (int i = 0; i < accentMaxIntensities.Length; i++)
            {
                intensitiesProp.GetArrayElementAtIndex(i).floatValue = accentMaxIntensities[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform RequireNode(Transform root, string relativePath)
        {
            Transform found = root.Find(relativePath);
            if (found != null)
            {
                return found;
            }

            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i].name == relativePath)
                {
                    return descendants[i];
                }
            }

            throw new InvalidOperationException(
                $"Node '{relativePath}' not found under '{root.name}' while building Showcase.unity.");
        }

        private static MeshRenderer RequireRenderer(Transform root, string relativePath)
        {
            Transform node = RequireNode(root, relativePath);
            var renderer = node.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                throw new InvalidOperationException(
                    $"Node '{relativePath}' under '{root.name}' has no MeshRenderer.");
            }

            return renderer;
        }

        private static MeshRenderer[] FindRenderersUsingMaterial(Transform root, Material targetMaterial)
        {
            MeshRenderer[] all = root.GetComponentsInChildren<MeshRenderer>(true);
            var matches = new List<MeshRenderer>();
            for (int i = 0; i < all.Length; i++)
            {
                Material[] mats = all[i].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == targetMaterial)
                    {
                        matches.Add(all[i]);
                        break;
                    }
                }
            }

            return matches.ToArray();
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Required asset missing at '{path}' ({typeof(T).Name}).");
            }

            return asset;
        }

        private static void AssignObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty prop = serialized.FindProperty(propertyName);
            if (prop == null)
            {
                throw new InvalidOperationException(
                    $"{serialized.targetObject.GetType().Name} has no serialized field '{propertyName}'.");
            }

            prop.objectReferenceValue = value;
        }

        private static void AssignObjectArray<T>(SerializedObject serialized, string propertyName, T[] values)
            where T : UnityEngine.Object
        {
            SerializedProperty prop = serialized.FindProperty(propertyName);
            if (prop == null)
            {
                throw new InvalidOperationException(
                    $"{serialized.targetObject.GetType().Name} has no serialized field '{propertyName}'.");
            }

            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void AssignRocks(
            SceneBuildContext context,
            SerializedObject serialized,
            string field,
            string[] names)
        {
            SerializedProperty list = serialized.FindProperty(field);
            list.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue =
                    context.LoadAsset<Mesh>($"{WorldPaths.R1RocksFolder}/{names[i]}.fbx");
            }
        }
    }
}
