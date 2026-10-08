using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.salvage" (JsonUtility DTO; field names are the JSON keys): every site's taken, loose and
    /// part-cut pieces, matched by site id, and the trail bits already folded into 07.
    /// </summary>
    [Serializable]
    public sealed class SalvageSaveData
    {
        public SalvageSiteSaveData[] sites = Array.Empty<SalvageSiteSaveData>();

        /// <summary>Indices (catalog order) of the Kestrel trail bits 07 has driven through.</summary>
        public int[] trail = Array.Empty<int>();
    }
}
