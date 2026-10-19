using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Creates the UI tuning assets with their code defaults when they do not exist yet. An existing UI tuning asset
    /// keeps every hand-tuned value; it only gains the default prompt entry of an interaction kind added since it was
    /// created (new settings sections arrive with their code defaults on their own), and the generated assets no code
    /// default can name: the databank's wireframe material and the maker's mark the face traces as it boots.
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
                bool wired = AssignWireMaterial(existing) | AssignSplashLayers(existing) |
                             AssignMakerLinework(existing);
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
            AssignSplashLayers(created);
            AssignMakerLinework(created);
            GeneratedAssets.CreateOrReplace(created, UiAssetPaths.Tuning);
            Debug.Log($"{BuilderPath}: created {UiAssetPaths.Tuning} with default values");
        }

        /// <summary>Points the boot splash at its four studio layers; true when any changed.</summary>
        private static bool AssignSplashLayers(UiTuning tuning)
        {
            var serialized = new SerializedObject(tuning);
            bool changed = false;
            foreach (string layer in UiAssetPaths.SplashLayers)
            {
                string path = UiAssetPaths.SplashLayer(layer);
                var image = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (image == null)
                {
                    throw new System.InvalidOperationException($"{BuilderPath}: the splash layer {path} is missing.");
                }

                SerializedProperty property = serialized.FindProperty("_title._splash._" + layer);
                if (property.objectReferenceValue != image)
                {
                    property.objectReferenceValue = image;
                    changed = true;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        /// <summary>Points the boot face at the maker's mark; true when it changed.</summary>
        private static bool AssignMakerLinework(UiTuning tuning)
        {
            var linework = AssetDatabase.LoadAssetAtPath<MakerLinework>(UiAssetPaths.MakerLinework);
            if (linework == null)
            {
                throw new System.InvalidOperationException(
                    $"{BuilderPath}: the maker's mark is missing at {UiAssetPaths.MakerLinework} (UI/MakerLinework).");
            }

            var serialized = new SerializedObject(tuning);
            SerializedProperty property = serialized.FindProperty("_bootFace._linework");
            if (property.objectReferenceValue == linework)
            {
                return false;
            }

            property.objectReferenceValue = linework;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
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
