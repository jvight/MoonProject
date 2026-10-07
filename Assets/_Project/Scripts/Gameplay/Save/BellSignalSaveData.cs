using System;

namespace MoonProject.Gameplay
{
    /// <summary>Save section "gameplay.bell_signals" (JsonUtility DTO; field names are the JSON keys).</summary>
    [Serializable]
    public sealed class BellSignalSaveData
    {
        /// <summary>Core <c>BellSignalTarget</c> of the current signal as an int (with a target id).</summary>
        public int targetKind;

        /// <summary>Id of what Bell points at now; empty when she points at nothing.</summary>
        public string targetId = string.Empty;

        /// <summary>Signals found so far (picks Bell's next "found" line).</summary>
        public int found;
    }
}
