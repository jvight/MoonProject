using System;

namespace MoonProject.Gameplay
{
    /// <summary>Save section "gameplay.tape_deck" (JsonUtility DTO): whether 07 has picked up Tilly's tape deck.</summary>
    [Serializable]
    public sealed class TapeDeckSaveData
    {
        public bool found;
    }
}
