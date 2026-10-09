using System;

namespace MoonProject.Core.Save
{
    /// <summary>The save file (JsonUtility DTO; field names are the JSON keys).</summary>
    [Serializable]
    internal sealed class SaveEnvelope
    {
        public int formatVersion;

        /// <summary>Application.version of the build that wrote the file; null in saves from before 0.4.1.</summary>
        public string contentVersion;

        public string savedAtUtc;
        public SaveEntry[] sections = Array.Empty<SaveEntry>();
    }
}
