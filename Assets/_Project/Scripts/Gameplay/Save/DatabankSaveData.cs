using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.databank" (JsonUtility DTO, docs/design/databank.md §5): the unlocked entry ids in the
    /// order they unlocked, each set entry's scanned count (matched by index), and the entries already read. Ids a
    /// newer or older catalogue does not know are ignored on load.
    /// </summary>
    [Serializable]
    public sealed class DatabankSaveData
    {
        public string[] unlocked = Array.Empty<string>();

        public string[] progressIds = Array.Empty<string>();

        public int[] progressFound = Array.Empty<int>();

        public string[] read = Array.Empty<string>();
    }
}
