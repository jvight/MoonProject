using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.startkit" (JsonUtility DTO, docs/design/opening-v2.md §10): whether the spare igniter is
    /// fitted to 07's cutter, whether 07 carries it in its hold (laid back in as the session starts), and whether the
    /// cutter has already sputtered without it (opening v3 slice 1.6: once per game).
    /// </summary>
    [Serializable]
    public sealed class StartKitSaveData
    {
        public bool igniterFitted;

        public bool igniterCarried;

        public bool cutterSputtered;
    }
}
