using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.opening" (JsonUtility DTO, docs/design/opening-v2.md): what the opening remembers that no
    /// other system does: whether 07 has reached the lander, the cells left in the lander's reserve, whether the first
    /// night has fallen over a night-ready base and what open supply crates still hold, story parts
    /// included (their open lids are the containers' own save).
    /// </summary>
    [Serializable]
    public sealed class OpeningSaveData
    {
        public bool reachedLander;

        /// <summary>Cells left in the lander's one-charge reserve; below 0 means a new game (the full
        /// reserve).</summary>
        public float reserveCells = -1f;

        public bool nightFallen;

        /// <summary>07 has visited a near site since power-on (R5): its next dock begins the first dusk.</summary>
        public bool duskSiteVisited;

        /// <summary>The first dusk has begun (R6).</summary>
        public bool duskBegun;

        /// <summary>Opened supply crates that still hold what 07's hold had no room for, matched with
        /// <see cref="left"/>.
        /// </summary>
        public string[] leftIds = Array.Empty<string>();

        public Recipe[] left = Array.Empty<Recipe>();

        /// <summary>Supply crates that still hold their story part (the lander's open crate and its igniter).</summary>
        public string[] partsLeft = Array.Empty<string>();
    }
}
