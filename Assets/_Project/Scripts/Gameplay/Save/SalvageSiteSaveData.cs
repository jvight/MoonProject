using System;

namespace MoonProject.Gameplay
{
    /// <summary>One site in the "gameplay.salvage" section (JsonUtility DTO; field names are the JSON keys).</summary>
    [Serializable]
    public sealed class SalvageSiteSaveData
    {
        /// <summary>The site's anchor id, e.g. "site.kestrel".</summary>
        public string id;

        /// <summary>Numbers of the pieces folded into 07: they never come back.</summary>
        public int[] taken = Array.Empty<int>();

        /// <summary>Pieces part-cut, or cut through and held on the wreck.</summary>
        public SalvagePieceSaveData[] pieces = Array.Empty<SalvagePieceSaveData>();
    }
}
