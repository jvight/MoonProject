using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Creates the UI tuning assets with their code defaults when they do not exist yet. An existing UI tuning asset
    /// keeps every hand-tuned value; it only gains the default prompt entry of an interaction kind added since it was
    /// created (new settings sections arrive with their code defaults on their own), and the databank's wireframe
    /// material, a generated asset no code default can name.
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
                bool wired = AssignWireMaterial(existing);
                if (added > 0 || wired)
                {
                    EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssets();
                }

                Debug.Log($"{BuilderPath}: keeping existing {UiAssetPaths.Tuning} ({added} new prompt entries)");
                return;
            }

            var created = ScriptableObject.CreateInstance<UiTuning>();
            AssignWireMaterial(created);
            GeneratedAssets.CreateOrReplace(created, UiAssetPaths.Tuning);
            Debug.Log($"{BuilderPath}: created {UiAssetPaths.Tuning} with default values");
        }

        /// <summary>Points the databank at the wireframe material; true when it changed.</summary>
        private static bool AssignWireMaterial(UiTuning tuning)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(UiAssetPaths.WireMaterial);
            if (material == null)
            {
                throw new System.InvalidOperationException(
                    $"{BuilderPath}: the databank's wireframe material is missing at {UiAssetPaths.WireMaterial}.");
            }

            var serialized = new SerializedObject(tuning);
            SerializedProperty property = serialized.FindProperty("_databank._wireMaterial");
            if (property.objectReferenceValue == material)
            {
                return false;
            }

            property.objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
    }
}
