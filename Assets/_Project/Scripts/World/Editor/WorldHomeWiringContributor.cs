using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Art;
using MoonProject.Art.Editor;
using MoonProject.Editor.Builders;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Wires World's <see cref="HomeLightingSystem"/> to the base once Gameplay has built it (M3-16): the Glow_ glass
    /// segments of Art's PowerCable_Home, _Bay and _Chart on the lander, ordered out from the tower, and a pilot halo
    /// at the top of the first radio tower stage; and World's <see cref="LabLights"/> to the habitat lander's belly
    /// lift and lab (docs/ARCHITECTURE.md, "Contract: the lab (M3-19)"). Runs after the Gameplay contributor (order
    /// 500).
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
        private const string DockAnchor = "DockAnchor";

        // Home's places 07 drives to from its dock (Art's lander nodes): the bay, the belly lift, the tower.
        private static readonly string[] HomeDestinations =
        {
            "WorkshopAnchor", BaseModelBuilder.BellyLiftName, "TowerAnchor",
        };

        // The lander faces the pad's centre: the dock's track runs this far forward to meet the outward tracks.
        private const float PadCentreReach = 10f;

        public int Order => 650;

        public void Contribute(SceneBuildContext context)
        {
            HomeLightingSystem lighting = null;
            LabLights lab = null;
            foreach (MonoBehaviour system in context.Systems)
            {
                lighting = system as HomeLightingSystem ?? lighting;
                lab = system as LabLights ?? lab;
            }

            if (lighting == null || lab == null)
            {
                throw new InvalidOperationException("The World contributor added no HomeLightingSystem or LabLights.");
            }

            Transform gameplay = context.GameplayRoot.transform;
            var serialized = new SerializedObject(lighting);
            AssignCable(serialized, "_homeCable", Segments(gameplay, CablePrefix + "Home"));
            AssignCable(serialized, "_bayCable", Segments(gameplay, CablePrefix + "Bay"));
            AssignCable(serialized, "_chartCable", Segments(gameplay, CablePrefix + "Chart"));
            serialized.FindProperty("_pilot").objectReferenceValue = CreatePilot(context, gameplay);
            AssignPools(context, serialized, gameplay);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            WireLab(lab, Find(gameplay, LanderName));
            CreateHomeMarks(context, gameplay);
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

        /// <summary>
        /// Hands <see cref="LabLights"/> the lander's lab and belly lift nodes (Art's lab contract).
        /// </summary>
        private static void WireLab(LabLights lights, Transform lander)
        {
            var lab = new SerializedObject(lights);
            Transform belly = Child(lander, BaseModelBuilder.BellyLiftName);
            Transform room = Child(lander, BaseModelBuilder.LabName);
            Transform board = Child(room, BaseModelBuilder.StationPrefix + "Board");
            Transform keepsakes = Child(room, BaseModelBuilder.StationPrefix + "Keepsakes");
            Transform table = Find(room, BaseModelBuilder.LightTableName);
            Transform carriage = Find(table, BaseModelBuilder.ChartPenCarriageName);
            lab.FindProperty("_home").objectReferenceValue = lander;
            lab.FindProperty("_bellyLamp").objectReferenceValue = Glow(Child(belly, BaseModelBuilder.BellyLampName));
            lab.FindProperty("_hatchRim").objectReferenceValue = Glow(Child(belly, BaseModelBuilder.HatchRimName));
            lab.FindProperty("_cabinLamps").objectReferenceValue = Glow(Child(room, BaseModelBuilder.CabinLampsName));
            lab.FindProperty("_bigPorthole").objectReferenceValue =
                Glow(Child(lander, BaseModelBuilder.BigPortholeName));
            lab.FindProperty("_tableGlow").objectReferenceValue = Glow(Child(table, BaseModelBuilder.LightTableGlowName));
            lab.FindProperty("_penCarriage").objectReferenceValue = carriage;
            lab.FindProperty("_penHead").objectReferenceValue = Child(carriage, BaseModelBuilder.ChartPenHeadName);
            lab.FindProperty("_drawWidth").floatValue = BaseModelBuilder.ChartDrawWidth;
            lab.FindProperty("_drawHeight").floatValue = BaseModelBuilder.ChartDrawHeight;
            AssignGlows(lab, "_portholeLamps", Child(lander, BaseModelBuilder.PortholeLampsName),
                BaseModelBuilder.LampPrefix, BaseModelBuilder.PortholeLampCount);
            AssignGlows(lab, "_boardLamps", board, BaseModelBuilder.LampPrefix, BaseModelBuilder.BoardNodeCount);
            SerializedProperty sheets = lab.FindProperty("_boardSheets");
            sheets.arraySize = BaseModelBuilder.BoardNodeCount;
            for (int i = 0; i < BaseModelBuilder.BoardNodeCount; i++)
            {
                sheets.GetArrayElementAtIndex(i).objectReferenceValue =
                    Child(board, BaseModelBuilder.SheetPrefix + i).gameObject;
            }

            Transform panel = Child(board, BaseModelBuilder.RecoveryPanelName);
            string[] rows = { "_powerRow", "_linkRow", "_dishRow", "_answerRow" };
            for (int row = 0; row < rows.Length; row++)
            {
                AssignGlows(lab, rows[row], Child(panel, BaseModelBuilder.RecoveryRowName(row)),
                    BaseModelBuilder.LampPrefix, BaseModelBuilder.RecoveryRowSockets(row));
            }

            AssignGlows(lab, "_readingLamps", keepsakes, BaseModelBuilder.ReadingLampPrefix,
                BaseModelBuilder.ReadingLampCount);
            SerializedProperty slots = lab.FindProperty("_keepsakeSlots");
            SerializedProperty sections = lab.FindProperty("_slotSections");
            slots.arraySize = BaseModelBuilder.KeepsakeSlotCount;
            sections.arraySize = BaseModelBuilder.KeepsakeSlotCount;
            for (int i = 0; i < BaseModelBuilder.KeepsakeSlotCount; i++)
            {
                slots.GetArrayElementAtIndex(i).objectReferenceValue = Child(keepsakes, BaseModelBuilder.SlotPrefix + i);
                sections.GetArrayElementAtIndex(i).intValue = BaseModelBuilder.KeepsakeSection(i);
            }

            lab.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Glow renderers <paramref name="prefix"/>0..count-1, direct children of <paramref name="parent"/>.</summary>
        private static void AssignGlows(SerializedObject serialized, string field, Transform parent, string prefix,
            int count)
        {
            SerializedProperty property = serialized.FindProperty(field);
            property.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = Glow(Child(parent, prefix + i));
            }
        }

        private static Renderer Glow(Transform node)
        {
            var renderer = node.GetComponent<Renderer>();
            return renderer != null ? renderer
                : throw new InvalidOperationException($"{node.name} has no renderer (Art's lab contract).");
        }

        /// <summary>The direct child <paramref name="name"/> of <paramref name="parent"/>, or a wiring error.</summary>
        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child != null ? child
                : throw new InvalidOperationException($"{parent.name} has no '{name}' (Art's lab contract, " +
                                                      "docs/ARCHITECTURE.md): rebuild Art's base first.");
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
            Vector2 dockBack = dock - Vector2.up * 3.5f;
            for (int i = 0; i < HomeDestinations.Length; i++)
            {
                GroundMarks.AddTrack(marks, dockBack, Local(lander, Find(lander, HomeDestinations[i])), settings,
                    seed + (uint)i, flat);
            }

            GroundMarks.AddTrack(marks, dockBack, dock + Vector2.up * PadCentreReach, settings, seed + 7u, flat);
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
