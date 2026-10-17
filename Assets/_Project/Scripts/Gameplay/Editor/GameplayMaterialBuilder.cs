using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Art.Editor;
using MoonProject.Editor.Builders;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// Writes one SoftGlow material per <see cref="GlowRole"/> (recipes in <see cref="GlowMaterials"/>), the glint
    /// material, and the <see cref="GameplayVisuals"/> asset that hands them, Art's R1 crates and the hologram
    /// materials to the runtime. Rewritten in place on every run.
    /// </summary>
    internal static class GameplayMaterialBuilder
    {
        public const string BuilderPath = "Gameplay/Materials";
        public const int BuilderOrder = 505;

        /// <summary>Art's R1 crates (opening-v2 §4), each named as its prefab: the supply crates' models.</summary>
        private static readonly string[] CrateNames = { ContainerKinds.CrateS, ContainerKinds.CrateM,
            ContainerKinds.CrateL };

        [MoonBuilder(BuilderPath, BuilderOrder)]
        internal static void Build()
        {
            Shader shader = LoadShader(GameplayAssetPaths.Shader);
            Shader glint = LoadShader(GameplayAssetPaths.GlintShader);

            var visuals = ScriptableObject.CreateInstance<GameplayVisuals>();
            visuals.Populate(Write(shader, GlowRole.WeldBeam), Write(shader, GlowRole.Flash),
                Write(shader, GlowRole.PieceHalo),
                GeneratedAssets.CreateOrReplace(GlintMaterials.CreatePart(glint), GameplayAssetPaths.PartGlintMaterial),
                Write(shader, GlowRole.Spark));
            visuals.PopulateContainers(CrateModels());
            visuals.PopulateHologram(HologramMaterialBuilder.LoadMaterial(false),
                HologramMaterialBuilder.LoadMaterial(true));
            GeneratedAssets.CreateOrReplace(visuals, GameplayAssetPaths.Visuals);
            AssetDatabase.SaveAssets();
            Debug.Log($"{BuilderPath}: wrote {Enum.GetValues(typeof(GlowRole)).Length + 1} materials and " +
                      GameplayAssetPaths.Visuals);
        }

        private static GameObject[] CrateModels()
        {
            var crates = new GameObject[CrateNames.Length];
            for (int i = 0; i < crates.Length; i++)
            {
                string path = GameplayAssetPaths.CratePrefab(CrateNames[i]);
                crates[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (crates[i] == null)
                {
                    throw new InvalidOperationException($"{BuilderPath}: Art's crate {path} is missing.");
                }
            }

            return crates;
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
