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
    }
}
