using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.wallet" at version 1, when 07 collected scrap (JsonUtility DTO): kept only so
    /// <see cref="MaterialsSaveMigrations"/> can read an old save's balance.
    /// </summary>
    [Serializable]
    public sealed class WalletSaveData
    {
        public int balance;
    }
}
