using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.battery" (JsonUtility DTO, docs/features/M3-20 §9): the charge, the capacity, the drums,
    /// the crawl, the Low line's shown count and whether the HUD arc has revealed itself. A save without it loads full.
    /// </summary>
    [Serializable]
    public sealed class BatterySaveData
    {
        public int cells;
        public float charge;
        public float drums;
        public bool lowPower;
        public bool arcRevealed;
        public int lowLinesShown;
    }
}
