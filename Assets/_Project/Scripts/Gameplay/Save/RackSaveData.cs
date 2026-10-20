using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.rack" (JsonUtility DTO): the socket rack at the pad (<see cref="SocketRack"/>), by bay id.
    /// </summary>
    [Serializable]
    public sealed class RackSaveData
    {
        /// <summary>The rack stands at the pad.</summary>
        public bool shown;

        /// <summary>Bays whose part 07 has seen by a scan.</summary>
        public string[] seen;

        /// <summary>Bays whose part is fitted on 07.</summary>
        public string[] filled;
    }
}
