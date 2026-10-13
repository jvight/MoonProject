using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Art.Editor;
using MoonProject.Editor.Builders;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// Writes one SoftGlow material per <see cref="GlowRole"/> (recipes in <see cref="GlowMaterials"/>), the salvage
    /// and friend-part glint materials, and the <see cref="GameplayVisuals"/> asset that hands them to the runtime.
    /// Rewritten in place on every run.
    /// </summary>
    internal static class GameplayMaterialBuilder
    {
        public const string BuilderPath = "Gameplay/Materials";
        public const int BuilderOrder = 505;

        /// <summary>
        /// Art's station pads, one per <see cref="BaseModelBuilder.PadRadii"/> size, each checked for the lamps
        /// <see cref="StationPad"/> lights.
        /// </summary>
        private static GameObject[] Pads()
        {
            var pads = new GameObject[BaseModelBuilder.PadRadii.Length];
            for (int i = 0; i < pads.Length; i++)
            {
                string path = ArtPaths.BaseFolder + "/" + BaseModelBuilder.PadName(i) + ".prefab";
                pads[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (pads[i] == null)
                {
                    throw new InvalidOperationException($"{BuilderPath}: Art's pad {path} is missing (run Art/Base).");
                }

                for (int lamp = 0; lamp < StationPad.LampCount; lamp++)
                {
                    if (pads[i].transform.Find(StationPad.LampPrefix + lamp) == null)
                    {
                        throw new InvalidOperationException(
                            $"{BuilderPath}: Art's pad {path} has no {StationPad.LampPrefix}{lamp}.");
                    }
                }
            }

            return pads;
        }

        /// <summary>Art's tower components (Part_TowerCoil, ...), each prefab named as its part's id.</summary>
        private static GameObject[] ComponentModels()
        {
            var parts = new GameObject[SiteModelBuilder.StoryPartNames.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string path = ArtPaths.PickupFolder + "/" + SiteModelBuilder.StoryPartNames[i] + ".prefab";
                parts[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (parts[i] == null)
                {
                    throw new InvalidOperationException($"{BuilderPath}: Art's component {path} is missing.");
                }
            }

            return parts;
        }

        /// <summary>Art's sealed containers, the finds' homes (0.5.5 §2).</summary>
        private static GameObject[] ContainerModels()
        {
            string[] names =
            {
                LootContainerBuilder.CoilCrateName, LootContainerBuilder.TapeCaseName,
                LootContainerBuilder.JunctionBoxName, LootContainerBuilder.SupplyPodName,
                LootContainerBuilder.TillySledName, LootContainerBuilder.CabinetName,
            };
            var containers = new GameObject[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string path = ArtPaths.PropFolder + "/" + names[i] + ".prefab";
                containers[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (containers[i] == null)
                {
                    throw new InvalidOperationException($"{BuilderPath}: Art's container {path} is missing.");
                }
            }

            return containers;
        }

        /// <summary>Art's blueprint tube, for a tube waiting on the ground at a world anchor.</summary>
        private static GameObject TubeModel()
        {
            string path = ArtPaths.PickupFolder + "/" + SiteModelBuilder.BlueprintTubeName + ".prefab";
            var tube = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return tube != null ? tube
                : throw new InvalidOperationException($"{BuilderPath}: Art's blueprint tube {path} is missing.");
        }

        private static GameObject GaragePortModel()
        {
            string path = ArtPaths.SitesFolder + "/" + GaragePortBuilder.PortName + ".prefab";
            var port = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return port != null ? port
                : throw new InvalidOperationException($"{BuilderPath}: Art's garage port {path} is missing.");
        }

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            Shader shader = LoadShader(GameplayAssetPaths.Shader);
            Shader glint = LoadShader(GameplayAssetPaths.GlintShader);

            var visuals = ScriptableObject.CreateInstance<GameplayVisuals>();
            visuals.Populate(Write(shader, GlowRole.SonarRing), Write(shader, GlowRole.SiteRing),
                Write(shader, GlowRole.SitePillar), Write(shader, GlowRole.TractorBeam),
                Write(shader, GlowRole.TetherBeam), Write(shader, GlowRole.Flash), Write(shader, GlowRole.RelicHalo),
                Write(shader, GlowRole.Dust), Write(shader, GlowRole.WarmRing), Write(shader, GlowRole.WarmGlow),
                GeneratedAssets.CreateOrReplace(GlintMaterials.CreatePart(glint),
                    GameplayAssetPaths.PartGlintMaterial), Write(shader, GlowRole.FriendPillar),
                Write(shader, GlowRole.Spark), Write(shader, GlowRole.HomeHalo),
                Write(shader, GlowRole.LinkPulse));
            visuals.PopulatePads(Pads(), BaseModelBuilder.PadRadii);
            visuals.PopulateComponents(ComponentModels());
            visuals.PopulateContainers(ContainerModels());
            visuals.PopulateTube(TubeModel());
            visuals.PopulateGaragePort(GaragePortModel());
            GeneratedAssets.CreateOrReplace(visuals, GameplayAssetPaths.Visuals);
            AssetDatabase.SaveAssets();
            Debug.Log($"{BuilderPath}: wrote {Enum.GetValues(typeof(GlowRole)).Length + 1} materials and " +
                      GameplayAssetPaths.Visuals);
        }

        private static Shader LoadShader(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null)
            {
                throw new InvalidOperationException($"{BuilderPath}: shader missing at {path}.");
            }

            return shader;
        }

        private static Material Write(Shader shader, GlowRole role)
        {
            return GeneratedAssets.CreateOrReplace(GlowMaterials.Create(shader, role),
                GameplayAssetPaths.Material(role));
        }
    }
}
