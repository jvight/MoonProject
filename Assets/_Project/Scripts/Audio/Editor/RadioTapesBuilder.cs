using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// Builds <c>Assets/_Project/Data/Audio/RadioTapes.asset</c> (one radio track per cassette) from the music box's
    /// tools/music/tapes.json. A missing file or clip fails the build: the radio needs every tape the game can hand
    /// out.
    /// </summary>
    internal static class RadioTapesBuilder
    {
        public const string BuilderPath = "Audio/Radio Tapes";
        public const int BuilderOrder = 425;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            string manifestFile = AudioAssetPaths.ProjectFile(AudioAssetPaths.MusicTapes);
            if (!File.Exists(manifestFile))
            {
                throw new FileNotFoundException($"{BuilderPath}: {AudioAssetPaths.MusicTapes} is missing; render the " +
                                                "tapes with tools/music.", manifestFile);
            }

            var manifest = JsonUtility.FromJson<MusicPlaylistManifest>(File.ReadAllText(manifestFile));
            if (manifest == null || manifest.tracks == null ||
                manifest.version != MusicPlaylistManifest.SupportedVersion)
            {
                throw new InvalidOperationException($"{BuilderPath}: unsupported {AudioAssetPaths.MusicTapes} " +
                                                    $"(expected version {MusicPlaylistManifest.SupportedVersion}).");
            }

            var tracks = new RadioTrack[manifest.tracks.Length];
            for (int i = 0; i < tracks.Length; i++)
            {
                MusicPlaylistManifest.Track entry = manifest.tracks[i];
                if (string.IsNullOrEmpty(entry.tape))
                {
                    throw new InvalidOperationException($"{BuilderPath}: track '{entry.id}' has no cassette id.");
                }

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(entry.file);
                if (clip == null)
                {
                    throw new InvalidOperationException($"{BuilderPath}: tape '{entry.tape}' clip {entry.file} is " +
                                                        "missing or not imported.");
                }

                tracks[i] = new RadioTrack(entry.id, entry.title, clip, entry.bpm, entry.tape);
            }

            var library = ScriptableObject.CreateInstance<RadioTapeLibrary>();
            library.Populate(tracks);
            library = GeneratedAssets.CreateOrReplace(library, AudioAssetPaths.Tapes);
            string problem = library.FindProblem();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: {problem}.");
            }

            Debug.Log($"{BuilderPath}: {tracks.Length} tape(s) -> {AudioAssetPaths.Tapes}");
        }
    }
}
