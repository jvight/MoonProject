using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.garage_port" (JsonUtility DTO): whether Kenji's garage port has fitted 07's tether ring,
    /// whether its place card has shown and whether the "the tether is gone" line has been said. The tape deck's own
    /// state is its relic's.
    /// </summary>
    [Serializable]
    public sealed class GaragePortSaveData
    {
        public bool ringFitted;
        public bool placeSeen;
        public bool tetherHinted;
    }
}
