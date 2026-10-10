using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
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
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
