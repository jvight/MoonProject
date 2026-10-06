using System;

namespace MoonProject.UI
{
    /// <summary>
    /// One language's string table as stored in Assets/_Project/Data/Localization/&lt;code&gt;.json (JsonUtility DTO;
    /// field names are the JSON keys).
    /// </summary>
    [Serializable]
    public sealed class StringTableData
    {
        /// <summary>Language code, e.g. "en".</summary>
        public string language;

        /// <summary>The language's name in itself, shown in the language selector.</summary>
        public string name;

        public StringEntryData[] entries = Array.Empty<StringEntryData>();
    }
}
