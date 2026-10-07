using System;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// JsonUtility mirror of the music box's tools/music/jingles.json (only the keys Audio needs; note onsets and
    /// loudness are for other domains). Field names must equal the JSON keys, hence the public camelCase fields.
    /// </summary>
    [Serializable]
    internal sealed class MusicJinglesManifest
    {
        public const int SupportedVersion = 1;

        public int version;
        public Jingle[] jingles;

        [Serializable]
        internal sealed class Jingle
        {
            public string id;
            public string file;
            public string sha256;
        }
    }
}
