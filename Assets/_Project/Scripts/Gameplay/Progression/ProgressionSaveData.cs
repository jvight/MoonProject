using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.progression" (JsonUtility DTO): the base's wake-up (docs/features/M3-16) beyond what
    /// other sections already hold: whether 07 has woken and gone out, whether it has salvaged anything, whether it
    /// has listened yet (its first listen's line is shown once), and the places that have greeted it.
    /// </summary>
    [Serializable]
    public sealed class ProgressionSaveData
    {
        public bool woke;
        public bool salvaged;

        /// <summary>07 has stood at the chasm's lip and looked across at Bell's light (M3-16 P2).</summary>
        public bool chasmSeen;

        /// <summary>07 has listened at least once (first-hour v2 §3): its first line is not said again.</summary>
        public bool listened;
        public string[] places = Array.Empty<string>();
    }
}
