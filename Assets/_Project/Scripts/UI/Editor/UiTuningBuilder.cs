using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using MoonProject.UI.Map;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Creates the UI tuning assets with their code defaults when they do not exist yet. An existing UI tuning asset
    /// keeps every hand-tuned value; it only gains the default prompt entry of an interaction kind added since it was
    /// created (new settings sections arrive with their code defaults on their own). An existing map view tuning asset
    /// is never touched.
    /// </summary>
    internal static class UiTuningBuilder
    {
        public const string BuilderPath = "UI/Tuning";
        public const int BuilderOrder = 600;
        public const string MapBuilderPath = "UI/Map Tuning";

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            var existing = AssetDatabase.LoadAssetAtPath<UiTuning>(UiAssetPaths.Tuning);
            if (existing != null)
            {
                int added = existing.Prompts.AddMissingDefaults();
                if (added > 0)
                {
                    EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssets();
                }

                Debug.Log($"{BuilderPath}: keeping existing {UiAssetPaths.Tuning} ({added} new prompt entries)");
                return;
            }

            GeneratedAssets.CreateOrReplace(ScriptableObject.CreateInstance<UiTuning>(), UiAssetPaths.Tuning);
            Debug.Log($"{BuilderPath}: created {UiAssetPaths.Tuning} with default values");
        }

        /// <summary>The crew map view's tuning (docs/features/M3-07), which the map's UI contributor loads.</summary>
        [MoonBuilder(MapBuilderPath, BuilderOrder + 1)]
        private static void BuildMapTuning()
        {
            if (AssetDatabase.LoadAssetAtPath<MapViewTuning>(UiAssetPaths.MapViewTuning) != null)
            {
                Debug.Log($"{MapBuilderPath}: keeping existing {UiAssetPaths.MapViewTuning}");
                return;
            }

            GeneratedAssets.CreateOrReplace(ScriptableObject.CreateInstance<MapViewTuning>(),
                UiAssetPaths.MapViewTuning);
            Debug.Log($"{MapBuilderPath}: created {UiAssetPaths.MapViewTuning} with default values");
        }
    }
}
