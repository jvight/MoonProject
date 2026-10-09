using System;

namespace MoonProject.UI
{
    /// <summary>
    /// Save section "ui.prompts", version 1: which context prompts (and the Look up hint) have been taught (JsonUtility DTO).
    /// </summary>
    [Serializable]
    public sealed class PromptsSaveData
    {
        public PromptCountData[] kinds = Array.Empty<PromptCountData>();

        /// <summary>The one-time Look up hint has been shown in this save.</summary>
        public bool lookUpHinted;
    }
}
