using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Art;
using MoonProject.Art.Editor;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Wires World's <see cref="HomeLightingSystem"/> to the base once Gameplay has built it (M3-16): the Glow_ glass
    /// segments of Art's PowerCable_Home, _Bay and _Chart on the lander, ordered out from the tower, and a pilot halo
    /// at the top of the first radio tower stage. Runs after the Gameplay contributor (order 500).
    /// </summary>
    public sealed class WorldHomeWiringContributor : ISceneContributor
    {
        private const string CablePrefix = "PowerCable_";
        private const string GlowPrefix = "Glow_";
        private const string FirstTowerStage = "RadioTower_L1";
        private const string BeaconSocket = "BeaconSocket";
        private const string LanderName = "Lander";

        // Gameplay's fallen supply pod (Art's SupplyPod) and its cracked beacon glass, which World lights (0.5.5).
        private const string SupplyPodName = "SupplyPod";
        private const string PodGlowName = "Glow";

        public int Order => 650;

        public void Contribute(SceneBuildContext context)
        {
            HomeLightingSystem lighting = null;
            foreach (MonoBehaviour system in context.Systems)
            {
                lighting = system as HomeLightingSystem ?? lighting;
            }

            if (lighting == null)
            {
                throw new InvalidOperationException("The World contributor added no HomeLightingSystem.");
            }

            Transform gameplay = context.GameplayRoot.transform;
            var serialized = new SerializedObject(lighting);
            AssignCable(serialized, "_homeCable", Segments(gameplay, CablePrefix + "Home"));
            AssignCable(serialized, "_bayCable", Segments(gameplay, CablePrefix + "Bay"));
            AssignCable(serialized, "_chartCable", Segments(gameplay, CablePrefix + "Chart"));
            serialized.FindProperty("_pilot").objectReferenceValue = CreatePilot(context, gameplay);
            AssignPools(context, serialized, gameplay);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CreatePodBeacon(context, gameplay);
        }

        /// <summary>
        /// The fallen supply pod's locator beacon (0.5.5): World's halo and warm light hung on Art's beacon glass
        /// (SupplyPod/Glow, the pod Gameplay stands under its root), so they follow the pod to the ledge, and the
        /// <see cref="PodBeacon"/> that pulses all three.
        /// </summary>
        private static void CreatePodBeacon(SceneBuildContext context, Transform gameplay)
        {
            WorldSettings world = context.LoadAsset<WorldSettings>(WorldPaths.Settings);
            Transform glass = Find(Find(gameplay, SupplyPodName), PodGlowName);
            GameObject halo = context.CreateChild("BeaconHalo", glass);
            halo.AddComponent<MeshFilter>().sharedMesh = context.LoadAsset<Mesh>(WorldPaths.BeaconHaloMesh);
            var haloRenderer = halo.AddComponent<MeshRenderer>();
            haloRenderer.sharedMaterial = context.LoadAsset<Material>(WorldPaths.CanyonGlowMaterial);
            haloRenderer.shadowCastingMode = ShadowCastingMode.Off;
            haloRenderer.receiveShadows = false;
            haloRenderer.lightProbeUsage = LightProbeUsage.Off;
            haloRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var light = halo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Palette.Get(PaletteSwatch.WarmLamp);
            light.range = world.Surface.Canyon.GlowLightRange;
            light.shadows = LightShadows.None;

            var beacon = context.CreateChild("PodBeacon", context.WorldRoot.transform).AddComponent<PodBeacon>();
            var serialized = new SerializedObject(beacon);
            serialized.FindProperty("_settings").objectReferenceValue = world;
            serialized.FindProperty("_glass").objectReferenceValue = glass.GetComponent<Renderer>();
            serialized.FindProperty("_halo").objectReferenceValue = haloRenderer;
            serialized.FindProperty("_light").objectReferenceValue = light;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(beacon);
        }

        /// <summary>One shadowless light per Home Lighting pool, hung from its anchor in the base (M3-18 V3).</summary>
        private static void AssignPools(SceneBuildContext context, SerializedObject serialized, Transform gameplay)
        {
            IReadOnlyList<LightPoolTuning> pools =
                context.LoadAsset<WorldSettings>(WorldPaths.Settings).HomeLighting.Pools;
            SerializedProperty property = serialized.FindProperty("_pools");
            SerializedProperty discs = serialized.FindProperty("_poolDiscs");
            property.arraySize = pools.Count;
            discs.arraySize = pools.Count;
            var discPrefab = context.LoadAsset<GameObject>(WorldPaths.LightPoolPrefab);
            for (int i = 0; i < pools.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = null;
                discs.GetArrayElementAtIndex(i).objectReferenceValue = null;
                if (pools[i].Disc)
                {
                    GameObject disc = UnityEngine.Object.Instantiate(discPrefab, Find(gameplay, pools[i].Anchor), false);
                    disc.name = "LightPoolDisc_" + i;
                    disc.transform.localPosition = pools[i].Offset;
                    disc.transform.localScale = Vector3.one * pools[i].Range;
                    var discRenderer = disc.GetComponent<Renderer>();
                    discRenderer.enabled = false;
                    discs.GetArrayElementAtIndex(i).objectReferenceValue = discRenderer;
                    continue;
                }

                GameObject host = context.CreateChild("LightPool_" + i, Find(gameplay, pools[i].Anchor));
                host.transform.localPosition = pools[i].Offset;
                var light = host.AddComponent<Light>();
                light.type = LightType.Point;
                light.shadows = LightShadows.None;
                light.intensity = 0f;
                light.enabled = false;
                property.GetArrayElementAtIndex(i).objectReferenceValue = light;
            }
        }

        private static Renderer CreatePilot(SceneBuildContext context, Transform gameplay)
        {
            Transform socket = Find(Find(gameplay, FirstTowerStage), BeaconSocket);
            GameObject pilot = context.CreateChild("TowerPilot", socket);
            pilot.AddComponent<MeshFilter>().sharedMesh = context.LoadAsset<Mesh>(WorldPaths.BeaconHaloMesh);
            var halo = pilot.AddComponent<MeshRenderer>();
            halo.sharedMaterial = context.LoadAsset<Material>(WorldPaths.TowerPilotMaterial);
            halo.shadowCastingMode = ShadowCastingMode.Off;
            halo.receiveShadows = false;
            halo.lightProbeUsage = LightProbeUsage.Off;
            halo.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return halo;
        }

        private static List<Renderer> Segments(Transform gameplay, string cableName)
        {
            Transform cable = Find(gameplay, cableName);
            var segments = new List<Renderer>();
            for (int i = 0; ; i++)
            {
                Transform glow = cable.Find(GlowPrefix + i);
                if (glow == null)
                {
                    break;
                }

                var renderer = glow.GetComponent<Renderer>();
                segments.Add(renderer != null ? renderer
                    : throw new InvalidOperationException($"{cableName}/{glow.name} has no renderer."));
            }

            if (segments.Count == 0)
            {
                throw new InvalidOperationException($"{cableName} has no {GlowPrefix}0..n segments.");
            }

            return segments;
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            throw new InvalidOperationException($"No '{name}' under {root.name}: rebuild Art's base and Gameplay first.");
        }

        private static void AssignCable(SerializedObject serialized, string field, List<Renderer> segments)
        {
            SerializedProperty property = serialized.FindProperty(field);
            property.arraySize = segments.Count;
            for (int i = 0; i < segments.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = segments[i];
            }
        }
    }
}
