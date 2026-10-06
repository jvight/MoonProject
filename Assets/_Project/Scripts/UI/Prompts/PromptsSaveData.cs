using System;

namespace MoonProject.UI
{
    /// <summary>
    /// Save section "ui.prompts", version 1: which context prompts have been taught (JsonUtility DTO).
    /// </summary>
    [Serializable]
    public sealed class PromptsSaveData
    {
        public PromptCountData[] kinds = Array.Empty<PromptCountData>();
    }
}
