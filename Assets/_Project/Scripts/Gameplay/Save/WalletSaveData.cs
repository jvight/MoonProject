using System;

namespace MoonProject.Gameplay
{
    /// <summary>Save section "gameplay.wallet" (JsonUtility DTO; field names are the JSON keys).</summary>
    [Serializable]
    public sealed class WalletSaveData
    {
        public int balance;
    }
}
