using System;

namespace MoonProject.Rover
{
    /// <summary>
    /// Save section "rover.character" (JsonUtility DTO): what 07 remembers of its own moments. A save without it
    /// loads with nothing met.
    /// </summary>
    [Serializable]
    public sealed class RoverSaveData
    {
        /// <summary>07 has looked at its elder "06" by relay.2's hut (0.5.5 §1.7), so it never does again.</summary>
        public bool metElder06;
    }
}
