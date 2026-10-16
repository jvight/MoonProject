using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Art.Editor;
using MoonProject.Editor.Builders;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Home's marks in the dust (M3-18 V4) as the mesh <see cref="WorldPaths.HomeGroundMarks"/>, in the frame of Art's
    /// lander on the flat pad: 07's worn tracks from the dock to the bay, the shelf and the tower and out toward the
    /// pad's centre (where the tracks to the depot and the canyon start), the landing-blast scar under the lander and a
    /// few bootprints by the ladder. Reads the lander prefab (Art/Base) and the World settings (World/Settings);
    /// <see cref="WorldHomeWiringContributor"/> hangs the mesh under the lander in Main.unity.
    /// </summary>
    public static class HomeGroundMarksBuilder
    {
        public const string BuilderPath = "World/Home Ground Marks";
        public const int BuilderOrder = 250;

        /// <summary>Art's lander, whose nodes the marks run between.</summary>
        public const string LanderPrefab = ArtPaths.BaseFolder + "/" + BaseModelBuilder.LanderName + ".prefab";

        private const string DockAnchor = "DockAnchor";

        // Home's places 07 drives to from its dock (Art's lander nodes): the bay, the belly lift, the tower.
        private static readonly string[] HomeDestinations =
        {
            "WorkshopAnchor", BaseModelBuilder.BellyLiftName, "TowerAnchor",
        };

        // The lander faces the pad's centre: the dock's track runs this far forward to meet the outward tracks.
        private const float PadCentreReach = 10f;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        public static void Build()
        {
            var world = AssetDatabase.LoadAssetAtPath<WorldSettings>(WorldPaths.Settings);
            if (world == null)
            {
                throw new InvalidOperationException($"No WorldSettings at {WorldPaths.Settings}; run World/Settings.");
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LanderPrefab);
            if (prefab == null)
            {
                throw new InvalidOperationException($"No lander prefab at {LanderPrefab}; run Art/Base.");
            }

            Mesh mesh = CreateMesh(world, Find(prefab.transform, BaseModelBuilder.LanderName));
            GeneratedAssets.CreateOrReplace(mesh, WorldPaths.HomeGroundMarks);
            AssetDatabase.SaveAssets();
        }

        private static Mesh CreateMesh(WorldSettings world, Transform lander)
        {
            GroundMarkSettings settings = world.GroundMarks;
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
            return marks.ToMesh("HomeGroundMarks");
        }

        private static Vector2 Local(Transform lander, Transform node)
        {
            Vector3 local = lander.InverseTransformPoint(node.position);
            return new Vector2(local.x, local.z);
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

            throw new InvalidOperationException($"No '{name}' under {root.name} in {LanderPrefab}: rebuild Art/Base.");
        }
    }
}
