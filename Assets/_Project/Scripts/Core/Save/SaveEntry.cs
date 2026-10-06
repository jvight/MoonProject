using System;

namespace MoonProject.Core.Save
{
    /// <summary>One section in the save file (JsonUtility DTO; field names are the JSON keys).</summary>
    [Serializable]
    internal sealed class SaveEntry
    {
        public string key;
        public int version;
        public string json;
    }
}
