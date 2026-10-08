using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One relay mast in the "gameplay.relays" section (JsonUtility DTO; field names are the JSON keys).
    /// </summary>
    [Serializable]
    public sealed class RelaySaveData
    {
        /// <summary>The mast's anchor id, e.g. "relay.0".</summary>
        public string id;

        /// <summary>True once 07 holds (or has installed) the mast's relay part.</summary>
        public bool part;

        /// <summary>Scrap paid to restore it (0 until a restoration began).</summary>
        public int paid;

        /// <summary>True once its restoration began: it loads restored, whatever the moment the game closed.</summary>
        public bool restored;
    }
}
