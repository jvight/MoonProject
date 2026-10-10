using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Editor.Builders;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Wires World's <see cref="HomeLightingSystem"/> to the base once Gameplay has built it (M3-16): the Glow_ glass
    /// segments of Art's PowerCable_Home, _Bay and _Lift on the lander, ordered out from the tower, and a pilot halo
    /// at the top of the first radio tower stage. Runs after the Gameplay contributor (order 500).
    /// </summary>
    public sealed class WorldHomeWiringContributor : ISceneContributor
    {
        private const string CablePrefix = "PowerCable_";
        private const string GlowPrefix = "Glow_";
        private const string FirstTowerStage = "RadioTower_L1";
        private const string BeaconSocket = "BeaconSocket";
        private const string LanderName = "Lander";
        private const string DockAnchor = "DockAnchor";

        // Home's places 07 drives to from its dock (Art's lander anchors): the bay, the shelf, the tower.
        private static readonly string[] HomeDestinations = { "WorkshopAnchor", "ShelfAnchor", "TowerAnchor" };

        // The lander faces the pad's centre: the dock's track runs this far forward to meet the outward tracks.
        private const float PadCentreReach = 10f;

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
            AssignCable(serialized, "_liftCable", Segments(gameplay, CablePrefix + "Lift"));
            serialized.FindProperty("_pilot").objectReferenceValue = CreatePilot(context, gameplay);
            AssignPools(context, serialized, gameplay);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CreateHomeMarks(context, gameplay);
        }

        /// <summary>
        /// Home's marks in the dust (M3-18 V4), in the lander's frame on the flat pad: 07's worn tracks from the dock
        /// to the bay, the shelf and the tower and out toward the pad's centre (where the tracks to the depot and the
        /// canyon start), the landing-blast scar under the lander and a few bootprints by the ladder.
        /// </summary>
        private static void CreateHomeMarks(SceneBuildContext context, Transform gameplay)
        {
            WorldSettings world = context.LoadAsset<WorldSettings>(WorldPaths.Settings);
            GroundMarkSettings settings = world.GroundMarks;
            Transform lander = Find(gameplay, LanderName);
            Vector2 dock = Local(lander, Find(lander, DockAnchor));
            var marks = new GroundMarkMesh();
            uint seed = (uint)world.Seed;
            Func<float, float, float> flat = (x, z) => 0f;
            for (int i = 0; i < HomeDestinations.Length; i++)
            {
                GroundMarks.AddTrack(marks, dock, Local(lander, Find(lander, HomeDestinations[i])), settings,
                    seed + (uint)i, flat);
            }

            GroundMarks.AddTrack(marks, dock, dock + Vector2.up * PadCentreReach, settings, seed + 7u, flat);
            GroundMarks.AddScar(marks, Vector3.zero, settings, seed);
            Vector3 heading = Quaternion.Euler(0f, settings.BootprintHeading, 0f) * Vector3.forward;
            GroundMarks.AddBootprints(marks, settings.BootprintStart, heading, settings);
            Mesh mesh = marks.ToMesh("HomeGroundMarks");
            GeneratedAssets.CreateOrReplace(mesh, WorldPaths.HomeGroundMarks);
            GameObject host = context.CreateChild("HomeGroundMarks", lander);
            host.AddComponent<MeshFilter>().sharedMesh = context.LoadAsset<Mesh>(WorldPaths.HomeGroundMarks);
            var renderer = host.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = context.LoadAsset<Material>(WorldPaths.GroundMarkMaterial);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Vector2 Local(Transform lander, Transform node)
        {
            Vector3 local = lander.InverseTransformPoint(node.position);
            return new Vector2(local.x, local.z);
        }

        /// <summary>One shadowless light per Home Lighting pool, hung from its anchor in the base (M3-18 V3).</summary>
        private static void AssignPools(SceneBuildContext context, SerializedObject serialized, Transform gameplay)
        {
            IReadOnlyList<LightPoolTuning> pools =
                context.LoadAsset<WorldSettings>(WorldPaths.Settings).HomeLighting.Pools;
            SerializedProperty property = serialized.FindProperty("_pools");
            property.arraySize = pools.Count;
            for (int i = 0; i < pools.Count; i++)
            {
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
