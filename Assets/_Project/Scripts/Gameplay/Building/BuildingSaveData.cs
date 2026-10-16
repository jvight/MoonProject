using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.building" (JsonUtility DTO, version 1): all structures placed in 07's base.
    /// </summary>
    [Serializable]
    public sealed class BuildingSaveData
    {
        public PlacedStructureSaveData[] structures = Array.Empty<PlacedStructureSaveData>();
        public string[] known = Array.Empty<string>();
    }
}
