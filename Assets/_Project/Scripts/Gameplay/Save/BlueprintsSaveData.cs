using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.blueprints" (JsonUtility DTO): the kits whose tube the lab's reader has decoded (the one
    /// known from the start is not listed), and where the other found tubes are.
    /// </summary>
    [Serializable]
    public sealed class BlueprintsSaveData
    {
        public string[] found = Array.Empty<string>();

        /// <summary>Tubes found but still riding on 07, not yet taken by the reader.</summary>
        public string[] carried = Array.Empty<string>();

        /// <summary>Tubes the reader has taken but not finished decoding: they decode again from the start.</summary>
        public string[] reading = Array.Empty<string>();

        /// <summary>Decoded kits whose sheet Kenji's plotter has drawn in the bay.</summary>
        public string[] drawn = Array.Empty<string>();
    }
}
