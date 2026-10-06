using System;

namespace MoonProject.Core.Save
{
    /// <summary>The save file (JsonUtility DTO; field names are the JSON keys).</summary>
    [Serializable]
    internal sealed class SaveEnvelope
    {
        public int formatVersion;
        public string savedAtUtc;
        public SaveEntry[] sections = Array.Empty<SaveEntry>();
    }
}
