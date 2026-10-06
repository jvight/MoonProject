using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Creates the UI tuning asset with its code defaults when it does not exist yet. An existing asset keeps every
    /// hand-tuned value; it only gains the default prompt entry of an interaction kind added since it was created
    /// (new settings sections arrive with their code defaults on their own).
    /// </summary>
    internal static class UiTuningBuilder
    {
        public const string BuilderPath = "UI/Tuning";
        public const int BuilderOrder = 600;

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
    }
}
