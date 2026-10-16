using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Rover.Face.Editor
{
    /// <summary>
    /// Creates <c>Assets/_Project/Data/Tuning/FaceTuning.asset</c> from code defaults when missing, without touching
    /// <c>RoverTuningBuilder</c>.
    /// </summary>
    public static class FaceTuningBuilder
    {
        public const string TuningFolder = "Assets/_Project/Data/Tuning";

        [MoonBuilder("Rover/FaceTuning", 301)]
        public static void Build()
        {
            Ensure<FaceTuning>(FaceTuning.AssetPath);
            Ensure<FaceDirectorTuning>(FaceDirectorTuning.AssetPath);
            AssetDatabase.SaveAssets();
        }

        private static void Ensure<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                return;
            }

            GeneratedAssets.EnsureFolder(TuningFolder);
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
        }
    }
}
