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

        /// <summary>Bit i set when required item i is held (version 2).</summary>
        public int items;

        /// <summary>True once it has answered a ping.</summary>
        public bool discovered;

        /// <summary>True once it has greeted 07 coming home for the first time (version 2).</summary>
        public bool welcomed;
    }
}
