using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Art;
using MoonProject.Art.Editor;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// The World's part of Main.unity: the earthlight and its fill, the post-processing volume, the skybox, fog and
    /// ambient (saved into the scene), The Peak's beacon, and the <see cref="WorldSystem"/> that generates the
    /// terrain, the rocks and Earth at boot, and the <see cref="HomeLightingSystem"/> that wakes home's warm points
    /// (wired to the base by <see cref="WorldHomeWiringContributor"/>).
    /// Runs first (order 200) because rover, audio and gameplay read ITerrainQuery and IWorldLayout.
    /// </summary>
    public sealed class WorldSceneContributor : ISceneContributor
    {
        public const string RuinsRootName = "Ruins";

        /// <summary>[World]/Lander holds the landed R1 cargo lander, named <see cref="CargoLanderName"/>.</summary>
        public const string LanderRootName = "Lander";
        public const string CargoLanderName = "CargoLander";
        public const string LanderBeaconName = "Beacon";

        /// <summary>[World]/OpeningProps holds the dead charging post and the dead west mast.</summary>
        public const string OpeningPropsRootName = "OpeningProps";
        public const string WakePostName = "WakePost";
        public const string WestMastName = "WestMast";

        // The dead west mast stands on relay.1, the west rim shoulder (opening v2 §4).
        private const string WestMastAnchor = WorldAnchorIds.RelayPrefix + "1";

        public int Order => 200;

        public void Contribute(SceneBuildContext context)
        {
            var settings = context.LoadAsset<WorldSettings>(WorldPaths.Settings);
            var groundMaterial = context.LoadAsset<Material>(WorldPaths.GroundMaterial);
            var paletteMaterial = context.LoadAsset<Material>(WorldPaths.R1BasaltMaterial);
            var earthMaterial = context.LoadAsset<Material>(WorldPaths.EarthMaterial);
            var skyMaterial = context.LoadAsset<Material>(WorldPaths.SkyMaterial);
            var profile = context.LoadAsset<VolumeProfile>(WorldPaths.VolumeProfile);
            Transform root = context.WorldRoot.transform;

            var earthlight = context.CreateChild("Earthlight", root).AddComponent<Light>();
            var fill = context.CreateChild("Fill Light", root).AddComponent<Light>();
            var volume = context.CreateChild("Post Processing", root).AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            var homeGrade = context.CreateChild("Home Grade", root).AddComponent<Volume>();
            homeGrade.isGlobal = true;
            homeGrade.priority = volume.priority + 1f;
            homeGrade.weight = 0f;
            homeGrade.sharedProfile = context.LoadAsset<VolumeProfile>(WorldPaths.HomeGradeProfile);

            RenderSettings.skybox = skyMaterial;
            WorldAtmosphere.Apply(settings.Atmosphere, settings.Sky, earthlight, fill);

            PeakBeacon beacon = CreateBeacon(context, settings, root);
            CreateRuins(context, settings, root);
            CreateLander(context, settings, root);
            CreateOpeningProps(context, settings, root);

            var world = context.CreateChild("World", root).AddComponent<WorldSystem>();
            var serialized = new SerializedObject(world);
            Assign(serialized, "_settings", settings);
            Assign(serialized, "_groundMaterial", groundMaterial);
            Assign(serialized, "_paletteMaterial", paletteMaterial);
            Assign(serialized, "_earthMaterial", earthMaterial);
            Assign(serialized, "_groundMarkMaterial", context.LoadAsset<Material>(WorldPaths.GroundMarkMaterial));
            WorldPaths.AssignLandmarks(serialized);
            Assign(serialized, "_cableMaterial", context.LoadAsset<Material>(WorldPaths.CableMaterial));
            AssignRocks(context, serialized, "_pebbleRocks", WorldPaths.PebbleRocks);
            AssignRocks(context, serialized, "_boulderRocks", WorldPaths.BoulderRocks);
            Assign(serialized, "_earthlight", earthlight);
            Assign(serialized, "_fillLight", fill);
            Assign(serialized, "_peakBeacon", beacon);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(world);

            var skyCycle = context.CreateChild("Sky Cycle", root).AddComponent<SkyCycleSystem>();
            var skySerialized = new SerializedObject(skyCycle);
            Assign(skySerialized, "_settings", settings);
            Assign(skySerialized, "_keyLight", earthlight);
            Assign(skySerialized, "_fillLight", fill);
            skySerialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(skyCycle);

            var homeLighting = context.CreateChild("Home Lighting", root).AddComponent<HomeLightingSystem>();
            var lighting = new SerializedObject(homeLighting);
            Assign(lighting, "_settings", settings);
            Assign(lighting, "_homeGrade", homeGrade);
            lighting.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(homeLighting);

        }

        /// <summary>
        /// Places the R1 Lumen station ruins cluster under [World]/Ruins from <see cref="LandmarkSettings.Ruins"/>
        /// and <see cref="SiteSettings.RuinsCenter"/> at scene-build time, never at runtime.
        /// </summary>
        private static void CreateRuins(SceneBuildContext context, WorldSettings settings, Transform root)
        {
            MoonSurface surface = settings.CreateSurface();
            Vector2 center = settings.Surface.Sites.RuinsCenter;
            Transform ruinsRoot = context.CreateChild(RuinsRootName, root).transform;
            RuinPlacement[] pieces = settings.Landmarks.Ruins;
            if (pieces == null)
            {
                return;
            }

            for (int i = 0; i < pieces.Length; i++)
            {
                RuinPlacement piece = pieces[i];
                var prefab = context.LoadAsset<GameObject>(WorldPaths.RuinPrefab(piece.Id));
                Vector2 spot = center + piece.Offset;
                var position = new Vector3(spot.x, surface.SampleHeight(spot.x, spot.y), spot.y);
                Quaternion rotation = Quaternion.Euler(0f, piece.Yaw, 0f) * prefab.transform.localRotation;
                GameObject instance = context.InstantiatePrefab(prefab, ruinsRoot, position, rotation);
                instance.name = piece.Id;
                RuinColliderBuilder.Fit(instance);
            }
        }

        /// <summary>
        /// Opening v2's two dead machines (§4), stood at scene-build time from the closest R1 models until their own
        /// land: 07's dead charging post at place.wake facing the lander, and the dead relay mast on the west rim
        /// shoulder at relay.1 facing the basin. Both are unlit and solid on <see cref="Core.Layers.Prop"/>.
        /// </summary>
        private static void CreateOpeningProps(SceneBuildContext context, WorldSettings settings, Transform root)
        {
            MoonSurface surface = settings.CreateSurface();
            var anchors = new WorldAnchors(surface, settings.Surface.Canyon, settings.Relays);
            Transform props = context.CreateChild(OpeningPropsRootName, root).transform;
            StandProp(context, props, WakePostName, WorldPaths.WakePostPrefab, Require(anchors, WorldAnchorIds.PlaceWake));
            StandProp(context, props, WestMastName, WorldPaths.WestMastPrefab, Require(anchors, WestMastAnchor));
        }

        private static void StandProp(SceneBuildContext context, Transform parent, string name, string prefabPath,
            WorldAnchor anchor)
        {
            var prefab = context.LoadAsset<GameObject>(prefabPath);
            Quaternion facing = Quaternion.LookRotation(anchor.Forward, Vector3.up);
            GameObject prop = context.InstantiatePrefab(prefab, parent, anchor.Position,
                facing * prefab.transform.localRotation);
            prop.name = name;
            RuinColliderBuilder.Fit(prop);
        }

        private static WorldAnchor Require(WorldAnchors anchors, string id)
        {
            if (!anchors.TryGet(id, out WorldAnchor anchor))
            {
                throw new System.InvalidOperationException($"The world has no '{id}' anchor for an opening prop.");
            }

            return anchor;
        }

        /// <summary>
        /// Lands the R1 cargo lander on its footprint at place.lander (opening v2 §4) under [World]/Lander at
        /// scene-build time: its long side, where the service port is, faces the build apron, and it leans a little
        /// off level from a hard landing. Its parts are made solid on <see cref="Core.Layers.Prop"/>.
        /// </summary>
        private static void CreateLander(SceneBuildContext context, WorldSettings settings, Transform root)
        {
            MoonSurface surface = settings.CreateSurface();
            SiteSettings sites = settings.Surface.Sites;
            Vector2 center = sites.LanderCenter;
            Vector2 toApron = (sites.ApronCenter - center).normalized;
            var forward = new Vector3(toApron.x, 0f, toApron.y);
            Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);
            Quaternion lean = Quaternion.AngleAxis(settings.Landmarks.LanderTilt, forward);
            var prefab = context.LoadAsset<GameObject>(WorldPaths.CargoLanderPrefab);
            var position = new Vector3(center.x, surface.SampleHeight(center.x, center.y), center.y);
            Transform landerRoot = context.CreateChild(LanderRootName, root).transform;
            GameObject lander = context.InstantiatePrefab(prefab, landerRoot, position,
                lean * facing * prefab.transform.localRotation);
            lander.name = CargoLanderName;
            RuinColliderBuilder.Fit(lander);
            CreateLanderBeacon(context, settings, landerRoot, lander);
        }

        /// <summary>
        /// The lander's amber emergency beacon (opening v2 §4): World's halo and warm light just above the top of the
        /// hull, and the <see cref="LanderBeacon"/> that pulses them. No R1 lamp model exists yet, so the light is the
        /// beacon.
        /// </summary>
        private static void CreateLanderBeacon(SceneBuildContext context, WorldSettings settings, Transform landerRoot,
            GameObject lander)
        {
            LanderBeaconSettings tuning = settings.Landmarks.LanderBeacon;
            var bounds = new Bounds(lander.transform.position, Vector3.zero);
            foreach (Renderer part in lander.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(part.bounds);
            }

            GameObject halo = context.CreateChild(LanderBeaconName, landerRoot);
            halo.transform.position = new Vector3(bounds.center.x, bounds.max.y + tuning.Height, bounds.center.z);
            halo.AddComponent<MeshFilter>().sharedMesh = context.LoadAsset<Mesh>(WorldPaths.BeaconHaloMesh);
            var haloRenderer = halo.AddComponent<MeshRenderer>();
            haloRenderer.sharedMaterial = context.LoadAsset<Material>(WorldPaths.LanderBeaconMaterial);
            haloRenderer.shadowCastingMode = ShadowCastingMode.Off;
            haloRenderer.receiveShadows = false;
            haloRenderer.lightProbeUsage = LightProbeUsage.Off;
            haloRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var light = halo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = tuning.Color;
            light.range = tuning.LightRange;
            light.shadows = LightShadows.None;

            var beacon = landerRoot.gameObject.AddComponent<LanderBeacon>();
            var serialized = new SerializedObject(beacon);
            Assign(serialized, "_settings", settings);
            Assign(serialized, "_halo", haloRenderer);
            Assign(serialized, "_light", light);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(beacon);
        }

        /// <summary>
        /// The beacon node on the summit: the art-kit model (art swaps in the broken dish in M4), a halo at the lamp
        /// and the <see cref="PeakBeacon"/> that breathes them.
        /// </summary>
        private static PeakBeacon CreateBeacon(SceneBuildContext context, WorldSettings settings, Transform root)
        {
            GameObject node = context.CreateChild(PeakBeacon.NodeName, root);
            node.transform.position = settings.CreateSurface().PeakSummit;
            GameObject model = context.InstantiatePrefab(context.LoadAsset<GameObject>(WorldPaths.BeaconModelPrefab),
                node.transform, Vector3.zero, Quaternion.identity);
            MeshRenderer lamp = null;
            foreach (MeshRenderer candidate in model.GetComponentsInChildren<MeshRenderer>())
            {
                if (candidate.gameObject.name == PeakBeaconBuilder.LampNodeName)
                {
                    lamp = candidate;
                }
            }

            if (lamp == null)
            {
                throw new System.InvalidOperationException(
                    $"{WorldPaths.BeaconModelPrefab} has no '{PeakBeaconBuilder.LampNodeName}' renderer.");
            }

            GameObject halo = context.CreateChild("Halo", node.transform);
            halo.transform.position = lamp.transform.position;
            halo.AddComponent<MeshFilter>().sharedMesh = context.LoadAsset<Mesh>(WorldPaths.BeaconHaloMesh);
            var haloRenderer = halo.AddComponent<MeshRenderer>();
            haloRenderer.sharedMaterial = context.LoadAsset<Material>(WorldPaths.BeaconHaloMaterial);
            haloRenderer.shadowCastingMode = ShadowCastingMode.Off;
            haloRenderer.receiveShadows = false;
            haloRenderer.lightProbeUsage = LightProbeUsage.Off;
            haloRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            var beacon = node.AddComponent<PeakBeacon>();
            var serialized = new SerializedObject(beacon);
            Assign(serialized, "_lamp", lamp);
            Assign(serialized, "_halo", haloRenderer);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return beacon;
        }

        private static void AssignRocks(SceneBuildContext context, SerializedObject serialized, string field,
            string[] names)
        {
            SerializedProperty property = Require(serialized, field);
            property.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue =
                    context.LoadAsset<Mesh>($"{WorldPaths.R1RocksFolder}/{names[i]}.fbx");
            }
        }

        private static void Assign(SerializedObject serialized, string field, Object value)
        {
            Require(serialized, field).objectReferenceValue = value;
        }

        private static SerializedProperty Require(SerializedObject serialized, string field)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(WorldSystem)} has no serialized field '{field}'; " +
                    $"update {nameof(WorldSceneContributor)}.");
            }

            return property;
        }
    }
}
