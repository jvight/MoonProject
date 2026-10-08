using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.wallet" from version 2 (JsonUtility DTO; field names are the JSON keys): 07's salvaged
    /// materials. Version 1 held the scrap balance (<see cref="WalletSaveData"/>), converted by
    /// <see cref="MaterialsSaveMigrations"/>.
    /// </summary>
    [Serializable]
    public sealed class MaterialsSaveData
    {
        public int metal;

        public int wiring;

        public int optics;
    }
}
