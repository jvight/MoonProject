using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.towerparts" (JsonUtility DTO): the tower's story parts 07 has picked up (docs/features/M3-16
    /// P1), split by whether they still ride on 07 or are already fed to the tower's port.
    /// </summary>
    [Serializable]
    public sealed class TowerPartsSaveData
    {
        /// <summary>Parts picked up but still riding on 07.</summary>
        public string[] carried = Array.Empty<string>();

        /// <summary>Parts fed to the tower's port.</summary>
        public string[] delivered = Array.Empty<string>();
    }
}
