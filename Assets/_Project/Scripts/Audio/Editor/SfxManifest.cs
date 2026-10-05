using System;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// JsonUtility mirror of tools/audio/sfx_manifest.json. Field names must equal the JSON keys, hence the public
    /// camelCase fields (this type exists only to be deserialised).
    /// </summary>
    [Serializable]
    internal sealed class SfxManifest
    {
        public const int SupportedVersion = 1;

        public int version;
        public string audioRoot;
        public Cue[] cues;

        [Serializable]
        internal sealed class Cue
        {
            public string id;
            public string category;
            public string bus;
            public bool spatial;
            public bool loop;
            public string[] files;
            public string[] sha256;
            public float volumeMin;
            public float volumeMax;
            public float pitchMin;
            public float pitchMax;
        }
    }
}
