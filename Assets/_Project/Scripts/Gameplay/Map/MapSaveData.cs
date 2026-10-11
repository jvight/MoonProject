using System;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// The crew's map in the save (docs/features/M3-07): places seen, places visited, places the chart pen has
    /// already inked, and whether the first look happened.
    /// </summary>
    [Serializable]
    public sealed class MapSaveData
    {
        public string[] seen = Array.Empty<string>();
        public string[] visited = Array.Empty<string>();
        public string[] inked = Array.Empty<string>();
        public bool looked;
    }
}
