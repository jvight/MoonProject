using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.salvage" (JsonUtility DTO; field names are the JSON keys): every site's taken, loose and
    /// part-cut pieces, matched by site id.
    /// </summary>
    [Serializable]
    public sealed class SalvageSaveData
    {
        public SalvageSiteSaveData[] sites = Array.Empty<SalvageSiteSaveData>();
    }
}
