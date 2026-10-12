using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.wallet" from version 3 (JsonUtility DTO; field names are the JSON keys): where the
    /// materials are (docs/features/M3-25 §6). Arrays are indexed by material; a save from an older build with fewer
    /// materials pads the rest with 0. Version 2 held one shared total (<see cref="MaterialsSaveDataV2"/>), version 1 the
    /// scrap balance (<see cref="WalletSaveData"/>), converted by <see cref="MaterialsSaveMigrations"/>.
    /// </summary>
    [Serializable]
    public sealed class MaterialsSaveData
    {
        /// <summary>Units in 07's hold, per material.</summary>
        public int[] hold = Array.Empty<int>();

        /// <summary>The hold's slots when saved (so a load never squeezes it before the kits are restored).</summary>
        public int holdSlots;

        /// <summary>Units on home's shelves, per material.</summary>
        public int[] home = Array.Empty<int>();

        /// <summary>Units in overflow crates beside the shelves, per material (migration only; never deleted).</summary>
        public int[] overflow = Array.Empty<int>();

        /// <summary>Shelves standing, the built-in ones included.</summary>
        public int shelvesBuilt;

        /// <summary>Bundles waiting in the world because the hold was full.</summary>
        public WaitingBundleSaveData[] waiting = Array.Empty<WaitingBundleSaveData>();
    }
}
