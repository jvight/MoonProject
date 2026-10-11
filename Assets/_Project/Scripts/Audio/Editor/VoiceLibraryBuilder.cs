using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// Builds <c>Assets/_Project/Data/Audio/VoiceLibrary.asset</c> (one recording per voiced ticker line) from
    /// tools/voice/voice_manifest.json. A missing file or clip fails the build: a line the manifest voices must play.
    /// </summary>
    internal static class VoiceLibraryBuilder
    {
        public const string BuilderPath = "Audio/Voice Lines";
        public const int BuilderOrder = 426;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            string manifestFile = AudioAssetPaths.ProjectFile(AudioAssetPaths.VoiceManifest);
            if (!File.Exists(manifestFile))
            {
                throw new FileNotFoundException($"{BuilderPath}: {AudioAssetPaths.VoiceManifest} is missing; render " +
                                                "the lines with tools/voice/build_voice.py.", manifestFile);
            }

            var manifest = JsonUtility.FromJson<VoiceManifest>(File.ReadAllText(manifestFile));
            if (manifest == null || manifest.lines == null || manifest.version != VoiceManifest.SupportedVersion)
            {
                throw new InvalidOperationException($"{BuilderPath}: unsupported {AudioAssetPaths.VoiceManifest} " +
                                                    $"(expected version {VoiceManifest.SupportedVersion}).");
            }

            var lines = new VoiceLine[manifest.lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                VoiceManifest.Line entry = manifest.lines[i];
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(entry.file);
                if (clip == null)
                {
                    throw new InvalidOperationException($"{BuilderPath}: line '{entry.key}' clip {entry.file} is " +
                                                        "missing or not imported.");
                }

                lines[i] = new VoiceLine(entry.key, entry.voice, clip);
            }

            var library = ScriptableObject.CreateInstance<VoiceLibrary>();
            library.Populate(lines);
            library = GeneratedAssets.CreateOrReplace(library, AudioAssetPaths.Voices);
            string problem = library.FindProblem();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: {problem}.");
            }

            Debug.Log($"{BuilderPath}: {lines.Length} line(s) -> {AudioAssetPaths.Voices}");
        }
    }
}
