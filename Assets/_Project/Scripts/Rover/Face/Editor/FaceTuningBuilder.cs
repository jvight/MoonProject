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
            if (AssetDatabase.LoadAssetAtPath<FaceTuning>(FaceTuning.AssetPath) != null)
            {
                return;
            }

            GeneratedAssets.EnsureFolder(TuningFolder);
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<FaceTuning>(), FaceTuning.AssetPath);
            AssetDatabase.SaveAssets();
        }
    }
}
