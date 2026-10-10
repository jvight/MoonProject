using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.progression" (JsonUtility DTO): the base's wake-up (docs/features/M3-16) beyond what
    /// other sections already hold: whether 07 has woken and gone out, whether it has salvaged anything, and the
    /// places that have greeted it.
    /// </summary>
    [Serializable]
    public sealed class ProgressionSaveData
    {
        public bool woke;
        public bool salvaged;
        public string[] places = Array.Empty<string>();
    }
}
