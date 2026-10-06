using System;

namespace MoonProject.Gameplay
{
    /// <summary>Save section "gameplay.upgrades" (JsonUtility DTO): bought levels, matched by upgrade id.</summary>
    [Serializable]
    public sealed class UpgradesSaveData
    {
        public UpgradeSaveData[] upgrades = Array.Empty<UpgradeSaveData>();
    }
}
