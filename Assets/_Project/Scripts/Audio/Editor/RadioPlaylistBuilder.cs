using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// Builds <c>Assets/_Project/Data/Audio/RadioPlaylist.asset</c> from the music box's tools/music/playlist.json.
    /// Until the music is rendered the playlist is written empty with a warning; the RadioStation then reports the
    /// empty playlist as an error at runtime.
    /// </summary>
    internal static class RadioPlaylistBuilder
    {
        public const string BuilderPath = "Audio/Radio Playlist";
        public const int BuilderOrder = 420;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            string manifestFile = AudioAssetPaths.ProjectFile(AudioAssetPaths.MusicPlaylist);
            RadioTrack[] tracks;
            if (File.Exists(manifestFile))
            {
                tracks = ReadTracks(manifestFile);
            }
            else
            {
                Debug.LogWarning($"{BuilderPath}: {AudioAssetPaths.MusicPlaylist} does not exist yet (music not " +
                                 "rendered); writing an empty playlist.");
                tracks = Array.Empty<RadioTrack>();
            }

            var playlist = ScriptableObject.CreateInstance<RadioPlaylist>();
            playlist.Populate(tracks);
            GeneratedAssets.CreateOrReplace(playlist, AudioAssetPaths.Playlist);
            Debug.Log($"{BuilderPath}: {tracks.Length} track(s) -> {AudioAssetPaths.Playlist}");
        }

        private static RadioTrack[] ReadTracks(string manifestFile)
        {
            var manifest = JsonUtility.FromJson<MusicPlaylistManifest>(File.ReadAllText(manifestFile));
            if (manifest == null || manifest.tracks == null ||
                manifest.version != MusicPlaylistManifest.SupportedVersion)
            {
                throw new InvalidOperationException($"{BuilderPath}: unsupported {AudioAssetPaths.MusicPlaylist} " +
                                                    $"(expected version {MusicPlaylistManifest.SupportedVersion}).");
            }

            var tracks = new RadioTrack[manifest.tracks.Length];
            for (int i = 0; i < tracks.Length; i++)
            {
                MusicPlaylistManifest.Track entry = manifest.tracks[i];
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(entry.file);
                if (clip == null)
                {
                    throw new InvalidOperationException($"{BuilderPath}: track '{entry.id}' clip {entry.file} " +
                                                        "is missing or not imported.");
                }

                tracks[i] = new RadioTrack(entry.id, entry.title, clip, entry.bpm);
            }

            return tracks;
        }
    }
}
