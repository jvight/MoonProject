using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.wallet" (JsonUtility DTO; field names are the JSON keys): 07's hold (opening-v2 §5).
    /// Indexed by material; a save from a build with fewer materials pads the rest with 0.
    /// </summary>
    [Serializable]
    public sealed class MaterialsSaveData
    {
        /// <summary>Units in 07's hold, per material.</summary>
        public int[] hold = Array.Empty<int>();

        /// <summary>The hold's slots when saved (so a load never squeezes it).</summary>
        public int holdSlots;
    }
}
