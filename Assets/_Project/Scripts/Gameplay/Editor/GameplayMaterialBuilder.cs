using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// Writes one SoftGlow material per <see cref="GlowRole"/> (recipes in <see cref="GlowMaterials"/>), the scrap and
    /// friend-part glint materials, and the <see cref="GameplayVisuals"/> asset that hands them to the runtime.
    /// Rewritten in place on every run.
    /// </summary>
    internal static class GameplayMaterialBuilder
    {
        public const string BuilderPath = "Gameplay/Materials";
        public const int BuilderOrder = 505;

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
                GeneratedAssets.CreateOrReplace(GlintMaterials.Create(glint), GameplayAssetPaths.GlintMaterial),
                GeneratedAssets.CreateOrReplace(GlintMaterials.CreatePart(glint),
                    GameplayAssetPaths.PartGlintMaterial), Write(shader, GlowRole.FriendPillar),
                Write(shader, GlowRole.Spark), Write(shader, GlowRole.HomeHalo));
            GeneratedAssets.CreateOrReplace(visuals, GameplayAssetPaths.Visuals);
            AssetDatabase.SaveAssets();
            Debug.Log($"{BuilderPath}: wrote {Enum.GetValues(typeof(GlowRole)).Length + 2} materials and " +
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
