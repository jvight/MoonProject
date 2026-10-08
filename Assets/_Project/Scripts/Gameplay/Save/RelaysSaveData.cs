using System;

namespace MoonProject.Gameplay
{
    /// <summary>Save section "gameplay.relays" (JsonUtility DTO): every relay mast's progress, matched by id.</summary>
    [Serializable]
    public sealed class RelaysSaveData
    {
        public RelaySaveData[] masts = Array.Empty<RelaySaveData>();
    }
}
