using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.rack" (JsonUtility DTO): the socket rack at the pad (<see cref="SocketRack"/>), by bay id.
    /// </summary>
    [Serializable]
    public sealed class RackSaveData
    {
        /// <summary>The release has placed the rack (its dock's pose below).</summary>
        public bool placed;

        /// <summary>The dock the release revealed the rack at.</summary>
        public Vector3 position;

        /// <summary>07's heading on that dock.</summary>
        public Quaternion rotation;

        /// <summary>Bays whose part 07 has seen by a scan.</summary>
        public string[] seen;

        /// <summary>Bays whose part is fitted on 07.</summary>
        public string[] filled;
    }
}
