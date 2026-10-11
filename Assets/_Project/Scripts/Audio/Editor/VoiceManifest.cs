using System;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// JsonUtility mirror of tools/voice/voice_manifest.json (only the keys Audio needs; loudness and timing are for
    /// the tool's own gates). Field names must equal the JSON keys, hence the public camelCase fields.
    /// </summary>
    [Serializable]
    internal sealed class VoiceManifest
    {
        public const int SupportedVersion = 1;

        public int version;
        public Line[] lines;

        [Serializable]
        internal sealed class Line
        {
            public string key;
            public string voice;
            public string file;
        }
    }
}
