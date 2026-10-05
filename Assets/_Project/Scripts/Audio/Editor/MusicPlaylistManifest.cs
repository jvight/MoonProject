using System;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// JsonUtility mirror of the music box's tools/music/playlist.json (only the keys the radio needs; extra keys are
    /// ignored). Field names must equal the JSON keys, hence the public camelCase fields.
    /// </summary>
    [Serializable]
    internal sealed class MusicPlaylistManifest
    {
        public const int SupportedVersion = 1;

        public int version;
        public Track[] tracks;

        [Serializable]
        internal sealed class Track
        {
            public string id;
            public string file;
            public string title;
            public float bpm;
        }
    }
}
