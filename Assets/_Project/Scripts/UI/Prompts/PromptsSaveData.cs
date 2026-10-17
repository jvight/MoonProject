using System;

namespace MoonProject.UI
{
    /// <summary>
    /// Save section "ui.prompts", version 1: which context prompts (and the opening's chips) have been taught (JsonUtility DTO).
    /// </summary>
    [Serializable]
    public sealed class PromptsSaveData
    {
        public PromptCountData[] kinds = Array.Empty<PromptCountData>();

        /// <summary>The opening's first prompt has taught "Look up" in this save (absent in older saves: false).</summary>
        public bool lookTaught;

        /// <summary>The opening's first prompt has taught "Drive" in this save (absent in older saves: false).</summary>
        public bool driveTaught;

        /// <summary>The first Tab press has played the hold's boot (absent in older saves: false).</summary>
        public bool holdBooted;

        /// <summary>The "Tab · Hold" chip has been shown (absent in older saves: false).</summary>
        public bool holdChipShown;
    }
}
