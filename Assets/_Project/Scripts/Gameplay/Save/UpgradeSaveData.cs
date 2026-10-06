using System;

namespace MoonProject.Gameplay
{
    /// <summary>One upgrade in the "gameplay.upgrades" section (JsonUtility DTO).</summary>
    [Serializable]
    public sealed class UpgradeSaveData
    {
        /// <summary>UpgradeDefinition id, e.g. "radio_tower".</summary>
        public string id;

        /// <summary>Levels bought (0 = none).</summary>
        public int level;
    }
}
