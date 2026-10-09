using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Art;
using MoonProject.Art.Editor;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// The World's part of Main.unity: the earthlight and its fill, the post-processing volume, the skybox, fog and
    /// ambient (saved into the scene), The Peak's beacon, and the <see cref="WorldSystem"/> that generates the
    /// terrain, the rocks and Earth at boot.
    /// Runs first (order 200) because rover, audio and gameplay read ITerrainQuery and IWorldLayout.
    /// </summary>
    public sealed class WorldSceneContributor : ISceneContributor
    {
        public int Order => 200;

        public void Contribute(SceneBuildContext context)
        {
            var settings = context.LoadAsset<WorldSettings>(WorldPaths.Settings);
            var groundMaterial = context.LoadAsset<Material>(WorldPaths.GroundMaterial);
            var paletteMaterial = context.LoadAsset<Material>(ArtPaths.LowPolyMaterial);
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
            CreateCanyonGlow(context, settings, root);

            var world = context.CreateChild("World", root).AddComponent<WorldSystem>();
            var serialized = new SerializedObject(world);
            Assign(serialized, "_settings", settings);
            Assign(serialized, "_groundMaterial", groundMaterial);
            Assign(serialized, "_paletteMaterial", paletteMaterial);
            Assign(serialized, "_earthMaterial", earthMaterial);
            AssignRocks(context, serialized, "_pebbleRocks", WorldPaths.PebbleRocks);
            AssignRocks(context, serialized, "_boulderRocks", WorldPaths.BoulderRocks);
            Assign(serialized, "_earthlight", earthlight);
            Assign(serialized, "_fillLight", fill);
            Assign(serialized, "_peakBeacon", beacon);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(world);
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

        /// <summary>
        /// The faint warm light deep in Whispering Canyon, over the glinting ledge: seen from the base down the
        /// canyon, it pools warm light on the ledge where a relic waits.
        /// </summary>
        private static void CreateCanyonGlow(SceneBuildContext context, WorldSettings settings, Transform root)
        {
            CanyonSettings canyon = settings.Surface.Canyon;
            MoonSurface surface = settings.CreateSurface();
            Vector2 point = surface.Canyon.GlowPoint;
            Vector2 ledge = surface.Canyon.LedgeCenter;
            float height = surface.SampleHeight(ledge.x, ledge.y) + canyon.GlowHeight;
            GameObject glow = context.CreateChild("CanyonGlow", root);
            glow.transform.position = new Vector3(point.x, height, point.y);

            GameObject halo = context.CreateChild("Halo", glow.transform);
            halo.AddComponent<MeshFilter>().sharedMesh = context.LoadAsset<Mesh>(WorldPaths.BeaconHaloMesh);
            var haloRenderer = halo.AddComponent<MeshRenderer>();
            haloRenderer.sharedMaterial = context.LoadAsset<Material>(WorldPaths.CanyonGlowMaterial);
            haloRenderer.shadowCastingMode = ShadowCastingMode.Off;
            haloRenderer.receiveShadows = false;
            haloRenderer.lightProbeUsage = LightProbeUsage.Off;
            haloRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Palette.Get(PaletteSwatch.WarmLamp);
            light.range = canyon.GlowLightRange;
            light.intensity = canyon.GlowLightIntensity;
            light.shadows = LightShadows.None;
        }

        private static void AssignRocks(SceneBuildContext context, SerializedObject serialized, string field,
            string[] names)
        {
            SerializedProperty property = Require(serialized, field);
            property.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue =
                    context.LoadAsset<Mesh>($"{ArtPaths.RockFolder}/{names[i]}.asset");
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
