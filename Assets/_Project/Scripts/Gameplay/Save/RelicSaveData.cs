using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>One relic in the "gameplay.relics" section (JsonUtility DTO; field names are the JSON keys).</summary>
    [Serializable]
    public sealed class RelicSaveData
    {
        /// <summary>RelicDefinition id (content contract), e.g. "rubber_duck".</summary>
        public string id;

        /// <summary><see cref="RelicState"/> as an int; a relic caught mid-deposit is saved as displayed.</summary>
        public int state;

        /// <summary>Excavation progress 0..1 (kept when the player lets go early).</summary>
        public float progress;

        /// <summary>True once the relic has answered a sonar ping (its site marker keeps breathing).</summary>
        public bool discovered;

        /// <summary>World pose of a surfacing or loose relic.</summary>
        public Vector3 position;

        public Quaternion rotation = Quaternion.identity;

        /// <summary>Museum shelf slot of a displayed relic, -1 otherwise.</summary>
        public int slot = -1;
    }
}
