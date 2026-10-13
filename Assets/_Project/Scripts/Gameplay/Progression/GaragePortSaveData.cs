using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.garage_port" (JsonUtility DTO): whether Kenji's garage port has fitted 07's tether ring
    /// and Ro's tape deck, whether its place card has shown and whether the "the tether is gone" line has been said.
    /// Kenji's spare crate keeps its own state with the sealed containers.
    /// </summary>
    [Serializable]
    public sealed class GaragePortSaveData
    {
        public bool ringFitted;
        public bool deckFitted;
        public bool placeSeen;
        public bool tetherHinted;
    }
}
