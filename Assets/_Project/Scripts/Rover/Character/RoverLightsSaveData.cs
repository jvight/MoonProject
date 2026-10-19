using System;

namespace MoonProject.Rover
{
    /// <summary>
    /// Save section "rover.lights" (JsonUtility DTO): whether the player switched 07's lamps off with the lights key.
    /// A save without it loads with the lamps on.
    /// </summary>
    [Serializable]
    public sealed class RoverLightsSaveData
    {
        /// <summary>The player switched 07's lamps off.</summary>
        public bool lightsOff;
    }
}
