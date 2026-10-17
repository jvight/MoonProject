using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// Builds <c>Assets/_Project/Data/Audio/TitleMusicLibrary.asset</c> from <c>tools/music/title.json</c>.
    /// Preserves any existing selected variant ID and fade timings when rebuilding, defaulting to
    /// <see cref="TitleMusicLibrary.DefaultTrackId"/> (<c>title_a_turning_moon</c>).
    /// </summary>
    internal static class TitleMusicBuilder
    {
        public const string BuilderPath = "Audio/Title Music";
        public const int BuilderOrder = 428;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            string manifestFile = AudioAssetPaths.ProjectFile(AudioAssetPaths.MusicTitle);
            if (!File.Exists(manifestFile))
            {
                throw new FileNotFoundException(
                    $"{BuilderPath}: {AudioAssetPaths.MusicTitle} is missing; render the title themes with tools/music.",
                    manifestFile);
            }

            var manifest = JsonUtility.FromJson<MusicPlaylistManifest>(File.ReadAllText(manifestFile));
            if (manifest == null || manifest.tracks == null ||
                manifest.version != MusicPlaylistManifest.SupportedVersion)
            {
                throw new InvalidOperationException($"{BuilderPath}: unsupported {AudioAssetPaths.MusicTitle} " +
                                                    $"(expected version {MusicPlaylistManifest.SupportedVersion}).");
            }

            var tracks = new RadioTrack[manifest.tracks.Length];
            for (int i = 0; i < tracks.Length; i++)
            {
                MusicPlaylistManifest.Track entry = manifest.tracks[i];
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(entry.file);
                if (clip == null)
                {
                    throw new InvalidOperationException($"{BuilderPath}: title track '{entry.id}' clip {entry.file} " +
                                                        "is missing or not imported.");
                }

                tracks[i] = new RadioTrack(entry.id, entry.title, clip, entry.bpm);
            }

            var existing = AssetDatabase.LoadAssetAtPath<TitleMusicLibrary>(AudioAssetPaths.TitleMusic);
            string selectedId = existing != null && !string.IsNullOrEmpty(existing.SelectedId)
                ? existing.SelectedId
                : TitleMusicLibrary.DefaultTrackId;
            float fadeIn = existing != null ? existing.FadeInSeconds : 2f;
            float fadeOut = existing != null ? existing.FadeOutSeconds : 2.5f;
            float volume = existing != null ? existing.Volume : 1f;

            var library = ScriptableObject.CreateInstance<TitleMusicLibrary>();
            library.Populate(tracks, selectedId, fadeIn, fadeOut, volume);
            library = GeneratedAssets.CreateOrReplace(library, AudioAssetPaths.TitleMusic);
            string problem = library.FindProblem();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: {problem}.");
            }

            var mixTuning = AssetDatabase.LoadAssetAtPath<AudioMixTuning>(AudioAssetPaths.MixTuning);
            if (mixTuning != null && mixTuning.TitleMusic != library)
            {
                var serialized = new SerializedObject(mixTuning);
                SerializedProperty prop = serialized.FindProperty("_titleMusic");
                if (prop != null)
                {
                    prop.objectReferenceValue = library;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(mixTuning);
                }
            }

            Debug.Log($"{BuilderPath}: {tracks.Length} variant(s), default '{library.SelectedId}' -> {AudioAssetPaths.TitleMusic}");
        }
    }
}
