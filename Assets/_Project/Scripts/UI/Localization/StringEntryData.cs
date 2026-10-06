using System;

namespace MoonProject.UI
{
    /// <summary>One localized string (JsonUtility DTO; field names are the JSON keys).</summary>
    [Serializable]
    public sealed class StringEntryData
    {
        /// <summary>Stable dotted lower-case key, e.g. "ui.pause.resume".</summary>
        public string key;

        public string text;
    }
}
