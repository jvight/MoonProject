using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Rover.Editor
{
    /// <summary>
    /// Creates the Rover tuning assets from the code defaults (the shipped feel) when they are missing. Existing
    /// assets are never touched, so tuning done in the inspector survives every rebuild.
    /// </summary>
    public static class RoverTuningBuilder
    {
        [MoonBuilder("Rover/Tuning", 300)]
        public static void Build()
        {
            Ensure<RoverTuning>(RoverAssetPaths.RoverTuning);
            Ensure<RoverRigTuning>(RoverAssetPaths.RigTuning);
            Ensure<RoverFxTuning>(RoverAssetPaths.FxTuning);
            Ensure<RoverCameraTuning>(RoverAssetPaths.CameraTuning);
            Ensure<RoverCharacterTuning>(RoverAssetPaths.CharacterTuning);
            AssetDatabase.SaveAssets();
        }

        private static void Ensure<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                return;
            }

            GeneratedAssets.EnsureFolder(RoverAssetPaths.TuningFolder);
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
        }
    }
}
