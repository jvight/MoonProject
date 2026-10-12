using System;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// JsonUtility mirror of tools/audio/chirp_syllables.json. Field names must equal the JSON keys, hence the public
    /// camelCase fields. Moods are in <see cref="MoonProject.Core.ChirpMood"/> order.
    /// </summary>
    [Serializable]
    internal sealed class ChirpSyllablesData
    {
        public const int SupportedVersion = 1;

        public int version;
        public Mood[] moods;

        [Serializable]
        internal sealed class Mood
        {
            public string mood;
            public string cue;
            public Variant[] variants;
        }

        [Serializable]
        internal sealed class Variant
        {
            public float[] onsets;
            public float[] pitches;
            public float[] lengths;
        }
    }
}
