using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.relay" (JsonUtility DTO): relay.1's repair. A spare carried in the hold is laid back into
    /// it as the session starts (the hold keeps no story parts of its own).
    /// </summary>
    [Serializable]
    public sealed class RelaySaveData
    {
        public bool spareTaken;

        public bool spareCarried;

        public bool fitted;
    }
}
