using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.blueprints" (JsonUtility DTO): the kits learned (every fragment of their set scanned) and
    /// the ones whose sheet Kenji's plotter has drawn.
    /// </summary>
    [Serializable]
    public sealed class BlueprintsSaveData
    {
        /// <summary>The learned kits (a save from before scanning: the kits whose tube was decoded).</summary>
        public string[] found = Array.Empty<string>();

        /// <summary>
        /// A save from before scanning only: tubes found but still riding on 07. Read once, as learned; never written.
        /// </summary>
        public string[] carried = Array.Empty<string>();

        /// <summary>
        /// A save from before scanning only: tubes in the lab's reader. Read once, as learned; never written.
        /// </summary>
        public string[] reading = Array.Empty<string>();

        /// <summary>Learned kits whose sheet Kenji's plotter has drawn in the bay.</summary>
        public string[] drawn = Array.Empty<string>();
    }
}
