using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using MoonProject.Gameplay.Lift;
using MoonProject.Gameplay.Map;

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
            EnsureExists<SonarTuning>(GameplayAssetPaths.SonarTuning);
            EnsureExists<RelicTuning>(GameplayAssetPaths.RelicTuning);
            EnsureExists<ExcavationTuning>(GameplayAssetPaths.ExcavationTuning);
            EnsureExists<TetherTuning>(GameplayAssetPaths.TetherTuning);
            EnsureExists<BaseTuning>(GameplayAssetPaths.BaseTuning);
            EnsureExists<RadioTowerTuning>(GameplayAssetPaths.RadioTowerTuning);
            EnsureExists<WorkshopTuning>(GameplayAssetPaths.WorkshopTuning);
            EnsureExists<FriendTuning>(GameplayAssetPaths.FriendTuning);
            EnsureExists<CassetteTuning>(GameplayAssetPaths.CassetteTuning);
            EnsureExists<LogCacheTuning>(GameplayAssetPaths.LogCacheTuning);
            EnsureExists<BellTuning>(GameplayAssetPaths.BellTuning);
            EnsureExists<RelayTuning>(GameplayAssetPaths.RelayTuning);
            EnsureExists<GlintTuning>(GameplayAssetPaths.GlintTuning);
            EnsureExists<SalvageTuning>(GameplayAssetPaths.SalvageTuning);
            EnsureExists<ProgressionTuning>(GameplayAssetPaths.ProgressionTuning);
            EnsureExists<LiftTuning>(GameplayAssetPaths.LiftTuning);
            EnsureExists<BatteryTuning>(GameplayAssetPaths.BatteryTuning);
            EnsureExists<CargoTuning>(GameplayAssetPaths.CargoTuning);
            EnsureExists<CutterTuning>(GameplayAssetPaths.CutterTuning);
            EnsureExists<ScanTuning>(GameplayAssetPaths.ScanTuning);
            EnsureExists<BuildingTuning>(GameplayAssetPaths.BuildingTuning);
            EnsureExists<MapTuning>(GameplayAssetPaths.MapTuning);
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
