using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.crates" (JsonUtility DTO; field names are the JSON keys): the crew supply crates already
    /// cut open (docs/features/M3-25 T4), by site id.
    /// </summary>
    [Serializable]
    public sealed class CratesSaveData
    {
        public string[] opened = Array.Empty<string>();
    }
}
