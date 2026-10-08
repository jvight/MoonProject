using System;

namespace MoonProject.Gameplay
{
    /// <summary>One site in the "gameplay.salvage" section (JsonUtility DTO; field names are the JSON keys).</summary>
    [Serializable]
    public sealed class SalvageSiteSaveData
    {
        /// <summary>The site's anchor id, e.g. "site.depot".</summary>
        public string id;

        /// <summary>True once it has answered a ping (its marker keeps breathing while it still answers).</summary>
        public bool discovered;

        /// <summary>Numbers of the pieces folded into 07: they never come back.</summary>
        public int[] taken = Array.Empty<int>();

        /// <summary>Pieces part-cut, or dragged clear and lying loose.</summary>
        public SalvagePieceSaveData[] pieces = Array.Empty<SalvagePieceSaveData>();
    }
}
