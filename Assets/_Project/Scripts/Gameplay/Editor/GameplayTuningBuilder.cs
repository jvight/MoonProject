using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// Creates the gameplay tuning assets with their code defaults when they do not exist yet. Existing assets are
    /// never touched, so hand-tuned values survive every rebuild.
    /// </summary>
    internal static class GameplayTuningBuilder
    {
        public const string BuilderPath = "Gameplay/Tuning";
        public const int BuilderOrder = 500;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            EnsureExists<ScrapTuning>(GameplayAssetPaths.ScrapTuning);
            EnsureExists<SonarTuning>(GameplayAssetPaths.SonarTuning);
            EnsureExists<RelicTuning>(GameplayAssetPaths.RelicTuning);
            EnsureExists<RelicPlacementTuning>(GameplayAssetPaths.RelicPlacement);
            EnsureExists<ExcavationTuning>(GameplayAssetPaths.ExcavationTuning);
            EnsureExists<TetherTuning>(GameplayAssetPaths.TetherTuning);
            EnsureExists<BaseTuning>(GameplayAssetPaths.BaseTuning);
            EnsureExists<RadioTowerTuning>(GameplayAssetPaths.RadioTowerTuning);
            EnsureExists<WorkshopTuning>(GameplayAssetPaths.WorkshopTuning);
            EnsureExists<FriendTuning>(GameplayAssetPaths.FriendTuning);
        }

        private static void EnsureExists<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                Debug.Log($"{BuilderPath}: keeping existing {path}");
                return;
            }

            GeneratedAssets.CreateOrReplace(ScriptableObject.CreateInstance<T>(), path);
            Debug.Log($"{BuilderPath}: created {path} with default values");
        }
    }
}
