using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Creates the UI tuning asset with its code defaults when it does not exist yet; an existing asset is left
    /// untouched so hand-tuned values survive every rebuild.
    /// </summary>
    internal static class UiTuningBuilder
    {
        public const string BuilderPath = "UI/Tuning";
        public const int BuilderOrder = 600;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<UiTuning>(UiAssetPaths.Tuning) != null)
            {
                Debug.Log($"{BuilderPath}: keeping existing {UiAssetPaths.Tuning}");
                return;
            }

            GeneratedAssets.CreateOrReplace(ScriptableObject.CreateInstance<UiTuning>(), UiAssetPaths.Tuning);
            Debug.Log($"{BuilderPath}: created {UiAssetPaths.Tuning} with default values");
        }
    }
}
