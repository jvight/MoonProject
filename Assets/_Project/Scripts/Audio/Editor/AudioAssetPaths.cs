using System.IO;
using UnityEngine;

namespace MoonProject.Audio.Editor
{
    /// <summary>Where the Audio domain's builders read their inputs and write their assets.</summary>
    internal static class AudioAssetPaths
    {
        public const string DataFolder = "Assets/_Project/Data/Audio";
        public const string Library = DataFolder + "/AudioLibrary.asset";
        public const string Playlist = DataFolder + "/RadioPlaylist.asset";
        public const string MixTuning = DataFolder + "/AudioMixTuning.asset";
        public const string RoverTuning = DataFolder + "/RoverAudioTuning.asset";
        public const string RadioTuning = DataFolder + "/RadioTuning.asset";
        public const string GameplayTuning = DataFolder + "/GameplayAudioTuning.asset";

        /// <summary>Written by tools/audio/build_sfx.py (project-relative).</summary>
        public const string SfxManifest = "tools/audio/sfx_manifest.json";

        /// <summary>Written by the music box's tools/music (project-relative).</summary>
        public const string MusicPlaylist = "tools/music/playlist.json";

        public static string ProjectFile(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }
    }
}
