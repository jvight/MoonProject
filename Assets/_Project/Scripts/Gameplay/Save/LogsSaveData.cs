using System;

namespace MoonProject.Gameplay
{
    /// <summary>Save section "gameplay.logs" (JsonUtility DTO): the crew log caches 07 has opened, by log id.</summary>
    [Serializable]
    public sealed class LogsSaveData
    {
        public string[] found = Array.Empty<string>();
    }
}
