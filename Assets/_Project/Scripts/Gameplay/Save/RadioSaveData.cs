using System;

namespace MoonProject.Gameplay
{
    /// <summary>Save section "gameplay.radio" (JsonUtility DTO; field names are the JSON keys).</summary>
    [Serializable]
    public sealed class RadioSaveData
    {
        /// <summary>Collected cassette ids, in the order they were collected.</summary>
        public string[] tapes = Array.Empty<string>();

        /// <summary>Core <c>RadioChannel</c> as an int.</summary>
        public int channel;

        /// <summary>The Tape Deck's chosen cassette id; empty when none.</summary>
        public string selectedTape = string.Empty;

        /// <summary>Bell is repaired: the dial exists.</summary>
        public bool dialUnlocked;
    }
}
