using System;

namespace MoonProject.Gameplay
{
    /// <summary>Save section "gameplay.relics" (JsonUtility DTO): every relic's state, matched by id.</summary>
    [Serializable]
    public sealed class RelicsSaveData
    {
        public RelicSaveData[] relics = Array.Empty<RelicSaveData>();
    }
}
