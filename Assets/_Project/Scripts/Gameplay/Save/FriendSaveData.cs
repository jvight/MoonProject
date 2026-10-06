using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One friend in the "gameplay.friends" section (JsonUtility DTO; field names are the JSON keys).
    /// </summary>
    [Serializable]
    public sealed class FriendSaveData
    {
        /// <summary>FriendDefinition id, e.g. "tilly".</summary>
        public string id;

        /// <summary><see cref="FriendState"/> as an int.</summary>
        public int state;

        /// <summary>Bit i set when part i is gathered.</summary>
        public int parts;

        /// <summary>True once it has answered a ping.</summary>
        public bool discovered;
    }
}
