using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using MoonProject.Editor.SceneBuild;
using MoonProject.Gameplay.Map;

namespace MoonProject.Gameplay.Editor.Map
{
    /// <summary>
    /// The crew's map in <c>Main.unity</c> (docs/features/M3-07): a "CrewMap" object under the gameplay root holding
    /// <see cref="MapSystem"/>, added to the systems right after <see cref="GameplaySystem"/> (whose parts it reads) and
    /// before UI (which draws the map). Its tuning is created once with the code defaults and never overwritten.
    /// </summary>
    public sealed class MapSceneContributor : ISceneContributor
    {
        public const string TuningPath = GameplayAssetPaths.TuningFolder + "/MapTuning.asset";

        public int Order => 520;

        [MoonBuilder("Gameplay/Map Tuning", GameplayTuningBuilder.BuilderOrder + 1)]
        public static void BuildTuning()
        {
            EnsureTuning();
        }

        private static MapTuning EnsureTuning()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<MapTuning>(TuningPath);
            return tuning != null ? tuning
                : GeneratedAssets.CreateOrReplace(ScriptableObject.CreateInstance<MapTuning>(), TuningPath);
        }

        public void Contribute(SceneBuildContext context)
        {
            GameplaySystem gameplay = null;
            foreach (MonoBehaviour system in context.Systems)
            {
                gameplay = system as GameplaySystem ?? gameplay;
            }

            if (gameplay == null)
            {
                throw new InvalidOperationException("The Gameplay contributor added no GameplaySystem.");
            }

            var visuals = context.LoadAsset<GameplayVisuals>(GameplayAssetPaths.Visuals);
            GameObject host = context.CreateChild("CrewMap", context.GameplayRoot.transform);
            var map = host.AddComponent<MapSystem>();
            map.Wire(gameplay, EnsureTuning(), visuals.Flash);
            context.AddSystem(map);
        }
    }
}
