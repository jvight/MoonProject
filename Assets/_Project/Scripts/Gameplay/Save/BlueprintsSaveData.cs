using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.blueprints" (JsonUtility DTO): the kits whose blueprint 07 has found and pinned on Kenji's
    /// board (the one pinned from the start is not listed).
    /// </summary>
    [Serializable]
    public sealed class BlueprintsSaveData
    {
        public string[] found = Array.Empty<string>();

        /// <summary>Tubes found but still riding on 07, not yet delivered to the plotter.</summary>
        public string[] carried = Array.Empty<string>();
    }
}
