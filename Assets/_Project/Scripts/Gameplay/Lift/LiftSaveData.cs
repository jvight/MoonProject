using System;

namespace MoonProject.Gameplay.Lift
{
    /// <summary>Save section "gameplay.lift" (JsonUtility DTO): whether 07 has restored the crew's lift.</summary>
    [Serializable]
    public sealed class LiftSaveData
    {
        public bool restored;
    }
}
