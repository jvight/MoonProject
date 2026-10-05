using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// Creates the audio tuning assets (mix, rover, radio) with their code defaults when they do not exist yet.
    /// Existing assets are left untouched so hand-tuned values survive every rebuild.
    /// </summary>
    internal static class AudioTuningBuilder
    {
        public const string BuilderPath = "Audio/Tuning";
        public const int BuilderOrder = 400;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            EnsureExists<AudioMixTuning>(AudioAssetPaths.MixTuning);
            EnsureExists<RoverAudioTuning>(AudioAssetPaths.RoverTuning);
            EnsureExists<RadioTuning>(AudioAssetPaths.RadioTuning);
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
