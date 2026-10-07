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
        public const string Tapes = DataFolder + "/RadioTapes.asset";
        public const string MixTuning = DataFolder + "/AudioMixTuning.asset";
        public const string RoverTuning = DataFolder + "/RoverAudioTuning.asset";
        public const string RadioTuning = DataFolder + "/RadioTuning.asset";
        public const string GameplayTuning = DataFolder + "/GameplayAudioTuning.asset";
        public const string UiTuning = DataFolder + "/UiAudioTuning.asset";
        public const string FriendTuning = DataFolder + "/FriendAudioTuning.asset";
        public const string JumpTuning = DataFolder + "/JumpAudioTuning.asset";

        /// <summary>Written by tools/audio/build_sfx.py (project-relative).</summary>
        public const string SfxManifest = "tools/audio/sfx_manifest.json";

        /// <summary>Written by the music box's tools/music (project-relative).</summary>
        public const string MusicPlaylist = "tools/music/playlist.json";

        /// <summary>Cassette tracks, written by the music box (project-relative).</summary>
        public const string MusicTapes = "tools/music/tapes.json";

        /// <summary>Friends' music-box jingles, written by the music box (project-relative).</summary>
        public const string MusicJingles = "tools/music/jingles.json";

        public static string ProjectFile(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }
    }
}
