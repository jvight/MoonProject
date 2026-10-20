using System.Collections.Generic;
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
    /// ambient (saved into the scene), The Peak's beacon, the Lumen ruins, the opening's places
    /// (<see cref="OpeningLayoutPlacer"/>) and the <see cref="WorldSystem"/> that generates the terrain, the rocks and
    /// Earth at boot.
    /// Runs first (order 200) because rover, audio and gameplay read ITerrainQuery and IWorldLayout.
    /// </summary>
    public sealed class WorldSceneContributor : ISceneContributor
    {
        public const string RuinsRootName = "Ruins";

        // The ruin whose fallen wings glint, the material its cells wear, and how far off the cells its glint floats.
        private const string FallenSolarArrayName = "FallenSolarArray";
        private const string SolarCellMaterial = "R1_SolarCell";

        // The ruin whose power module still blinks at dusk, and Art's node for the lamp's lens on it.
        private const string HabitatModulesName = "HabitatModules";
        private const string ModuleLampNode = "PowerModule_StatusLamp";
        private const string LampLensMaterial = "R1_LampAmber";
        private const float GlintLift = 0.03f;

        /// <summary>[World]/ArrayGlint: the fallen solar array's glints, beside the ruins.</summary>
        public const string ArrayGlintName = "ArrayGlint";

        /// <summary>The [World] child holding the habitat power module lamp's halo and its blink.</summary>
        public const string ModuleLampName = "ModuleLamp";

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

            RenderSettings.skybox = skyMaterial;
            WorldAtmosphere.Apply(settings.Atmosphere, settings.Sky, earthlight, fill);

            PeakBeacon beacon = CreateBeacon(context, settings, root);
            CreateRuins(context, settings, root);
            OpeningLayoutPlacer.Place(context, settings, root);

            var world = context.CreateChild("World", root).AddComponent<WorldSystem>();
            var serialized = new SerializedObject(world);
            Assign(serialized, "_settings", settings);
            Assign(serialized, "_groundMaterial", groundMaterial);
            Assign(serialized, "_paletteMaterial", paletteMaterial);
            Assign(serialized, "_earthMaterial", earthMaterial);
            Assign(serialized, "_groundMarkMaterial", context.LoadAsset<Material>(WorldPaths.GroundMarkMaterial));
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
                if (piece.Id == FallenSolarArrayName)
                {
                    StandArrayGlint(context, settings, instance, root);
                }

                if (piece.Id == HabitatModulesName)
                {
                    StandModuleLamp(context, settings, instance, root);
                }
            }
        }

        /// <summary>
        /// A sun-white glint halo over the middle of each fallen wing's solar cells and the <see cref="ArrayGlint"/>
        /// that flares them in the sunlight, under [World]/ArrayGlint: kept out of the ruin itself, whose renderers are
        /// its scan targets' and which Gameplay reads as a ruin piece. Each glint is a cracked cell that throws the
        /// golden-hour Sun (where the Sun holds from the first charge to power-on, opening v3 R4) at the apron's eye,
        /// so it calls from the base, as the retroreflector's facet does.
        /// </summary>
        private static void StandArrayGlint(SceneBuildContext context, WorldSettings settings, GameObject array,
            Transform root)
        {
            var mesh = context.LoadAsset<Mesh>(WorldPaths.BeaconHaloMesh);
            var material = context.LoadAsset<Material>(WorldPaths.ArrayGlintMaterial);
            GameObject holder = context.CreateChild(ArrayGlintName, root);
            MoonSurface surface = settings.CreateSurface();
            Vector2 apron = settings.Surface.Sites.ApronCenter;
            float eyeHeight = surface.SampleHeight(apron.x, apron.y) + settings.Landmarks.ReflectorEyeHeight;
            var eye = new Vector3(apron.x, eyeHeight, apron.y);
            Vector3 towardSun = OpeningLayoutPlacer.GoldenHourSun(settings);
            var halos = new List<Renderer>();
            var normals = new List<Vector3>();
            foreach (MeshRenderer wing in array.GetComponentsInChildren<MeshRenderer>())
            {
                if (!TryCellsMiddle(wing, out Vector3 middle))
                {
                    continue;
                }

                Vector3 facet = (towardSun + (eye - middle).normalized).normalized;
                GameObject halo = context.CreateChild("Glint", holder.transform);
                halo.transform.SetPositionAndRotation(middle + facet * GlintLift, Quaternion.identity);
                halo.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = halo.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                halos.Add(renderer);
                normals.Add(facet);
            }

            if (halos.Count == 0)
            {
                throw new System.InvalidOperationException($"World scene build: Art's {FallenSolarArrayName} has no " +
                    $"{SolarCellMaterial} cells to glint.");
            }

            var glint = holder.AddComponent<ArrayGlint>();
            var serialized = new SerializedObject(glint);
            serialized.FindProperty("_settings").objectReferenceValue = settings;
            SerializedProperty haloList = serialized.FindProperty("_halos");
            SerializedProperty normalList = serialized.FindProperty("_normals");
            haloList.arraySize = halos.Count;
            normalList.arraySize = normals.Count;
            for (int i = 0; i < halos.Count; i++)
            {
                haloList.GetArrayElementAtIndex(i).objectReferenceValue = halos[i];
                normalList.GetArrayElementAtIndex(i).vector3Value = normals[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(glint);
        }

        /// <summary>
        /// The habitat power module's status lamp under [World]/ModuleLamp: Art's lens on the ruin and a small amber
        /// halo floated out in front of it, kept out of the ruin itself (its renderers are its scan targets'), and the
        /// <see cref="ModuleStatusLamp"/> that blinks them from dusk.
        /// </summary>
        private static void StandModuleLamp(SceneBuildContext context, WorldSettings settings, GameObject habitat,
            Transform root)
        {
            Transform node = habitat.transform.Find(ModuleLampNode);
            var lens = node != null ? node.GetComponent<MeshRenderer>() : null;
            Mesh lensMesh = lens != null ? lens.GetComponent<MeshFilter>().sharedMesh : null;
            int slot = lensMesh != null ? LensSlot(lens, lensMesh) : -1;
            if (slot < 0)
            {
                throw new System.InvalidOperationException($"World scene build: Art's {HabitatModulesName} has no " +
                    $"{ModuleLampNode} with an {LampLensMaterial} lens to blink.");
            }

            // The lens bulges out of its bezel: its middle sits in front of the node's pivot, on the bulkhead.
            Vector3 lensMiddle = lensMesh.GetSubMesh(slot).bounds.center;
            Vector3 facing = node.TransformDirection(lensMiddle);
            if (facing.sqrMagnitude <= 0f)
            {
                throw new System.InvalidOperationException($"World scene build: Art's {ModuleLampNode} has its " +
                    "pivot in its middle, so the way it faces cannot be read.");
            }

            GameObject holder = context.CreateChild(ModuleLampName, root);
            GameObject halo = context.CreateChild("Halo", holder.transform);
            Vector3 lift = facing.normalized * settings.Landmarks.ModuleLamp.HaloLift;
            halo.transform.SetPositionAndRotation(node.TransformPoint(lensMiddle) + lift, Quaternion.identity);
            halo.AddComponent<MeshFilter>().sharedMesh = context.LoadAsset<Mesh>(WorldPaths.BeaconHaloMesh);
            var haloRenderer = halo.AddComponent<MeshRenderer>();
            haloRenderer.sharedMaterial = context.LoadAsset<Material>(WorldPaths.ModuleLampHaloMaterial);
            haloRenderer.shadowCastingMode = ShadowCastingMode.Off;
            haloRenderer.receiveShadows = false;
            haloRenderer.lightProbeUsage = LightProbeUsage.Off;
            haloRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            var lamp = holder.AddComponent<ModuleStatusLamp>();
            var serialized = new SerializedObject(lamp);
            serialized.FindProperty("_settings").objectReferenceValue = settings;
            serialized.FindProperty("_lens").objectReferenceValue = lens;
            serialized.FindProperty("_lensSlot").intValue = slot;
            serialized.FindProperty("_halo").objectReferenceValue = haloRenderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(lamp);
        }

        /// <summary>
        /// The material slot of <paramref name="lens"/> that wears the amber lens, found by name (the bezel shares the
        /// node, in whatever slot order Art exports); -1 when none does.
        /// </summary>
        private static int LensSlot(MeshRenderer lens, Mesh mesh)
        {
            Material[] materials = lens.sharedMaterials;
            for (int slot = 0; slot < materials.Length && slot < mesh.subMeshCount; slot++)
            {
                if (materials[slot] != null && materials[slot].name == LampLensMaterial)
                {
                    return slot;
                }
            }

            return -1;
        }

        /// <summary>
        /// The area-weighted middle (world space) of <paramref name="renderer"/>'s solar-cell faces; false when it has
        /// none.
        /// </summary>
        private static bool TryCellsMiddle(MeshRenderer renderer, out Vector3 middle)
        {
            middle = Vector3.zero;
            Material[] materials = renderer.sharedMaterials;
            var filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
            {
                return false;
            }

            Vector3[] vertices = mesh.vertices;
            Matrix4x4 toWorld = renderer.transform.localToWorldMatrix;
            float area = 0f;
            for (int slot = 0; slot < materials.Length && slot < mesh.subMeshCount; slot++)
            {
                if (materials[slot] == null || materials[slot].name != SolarCellMaterial)
                {
                    continue;
                }

                int[] triangles = mesh.GetTriangles(slot);
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    Vector3 a = toWorld.MultiplyPoint3x4(vertices[triangles[t]]);
                    Vector3 b = toWorld.MultiplyPoint3x4(vertices[triangles[t + 1]]);
                    Vector3 c = toWorld.MultiplyPoint3x4(vertices[triangles[t + 2]]);
                    float faceArea = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                    middle += (a + b + c) / 3f * faceArea;
                    area += faceArea;
                }
            }

            if (area <= 0f)
            {
                return false;
            }

            middle /= area;
            return true;
        }

        /// <summary>
        /// The beacon node on the summit: Art's R1 Peak beacon, a halo at its lamp and the <see cref="PeakBeacon"/>
        /// that breathes them.
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
