using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>Save section "gameplay.rover" (JsonUtility DTO): where 07 was parked when the game was saved.</summary>
    [Serializable]
    public sealed class RoverSaveData
    {
        public Vector3 position;

        /// <summary>Heading in degrees around +Y (0 = facing +Z).</summary>
        public float yaw;
    }
}
