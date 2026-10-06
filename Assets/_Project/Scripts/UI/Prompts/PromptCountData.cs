using System;

namespace MoonProject.UI
{
    /// <summary>
    /// How often one prompt was shown and its action done (JsonUtility DTO; field names are the JSON keys).
    /// </summary>
    [Serializable]
    public sealed class PromptCountData
    {
        /// <summary>
        /// The InteractionKind name (names, not numbers, so reordering the enum never scrambles saves).
        /// </summary>
        public string kind;

        public int shown;

        public int used;
    }
}
