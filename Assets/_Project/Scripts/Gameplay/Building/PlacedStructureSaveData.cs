using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One placed structure entry in the "gameplay.building" save section (JsonUtility DTO; field names are JSON keys).
    /// </summary>
    [Serializable]
    public sealed class PlacedStructureSaveData
    {
        /// <summary>Structure definition id (e.g. "solar_array").</summary>
        public string id = string.Empty;

        /// <summary>World-space position of the structure's pivot.</summary>
        public Vector3 position;

        /// <summary>Heading yaw in degrees around +Y.</summary>
        public float yaw;

        /// <summary>Normalized weld progress in [0, 1].</summary>
        public float buildProgress;

        /// <summary>Stored battery energy in Watt-hours.</summary>
        public float storedCharge;
    }
}
