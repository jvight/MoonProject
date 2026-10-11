using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// Puts 07's wake sounds (<see cref="WakeAudio"/>) into Main.unity, right after the Audio contributor (order 560),
    /// so the <see cref="AudioDirector"/> it resolves its cues from has initialised first.
    /// </summary>
    public sealed class WakeAudioSceneContributor : ISceneContributor
    {
        public int Order => 560;

        public void Contribute(SceneBuildContext context)
        {
            AudioDirector director = null;
            foreach (MonoBehaviour system in context.Systems)
            {
                director = system as AudioDirector ?? director;
            }

            if (director == null)
            {
                throw new InvalidOperationException("The Audio contributor added no AudioDirector.");
            }

            var wake = context.CreateChild("WakeAudio", context.AudioRoot.transform).AddComponent<WakeAudio>();
            var serialized = new SerializedObject(wake);
            Assign(serialized, "_director", director);
            Assign(serialized, "_tuning", context.LoadAsset<RoverAudioTuning>(AudioAssetPaths.RoverTuning));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(wake);
        }

        private static void Assign(SerializedObject serialized, string field, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WakeAudio)} has no serialized field '{field}'; update {nameof(WakeAudioSceneContributor)}.");
            }

            property.objectReferenceValue = value;
        }
    }
}
