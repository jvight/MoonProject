using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// One ruin piece in the Lumen station ruins cluster: its R1 prefab id, local XZ offset from the cluster's
    /// shaped centre, yaw in degrees clockwise from +Z, and clearance radius kept around it.
    /// </summary>
    [Serializable]
    public struct RuinPlacement
    {
        [Tooltip("R1 ruin prefab name under Generated/Art/R1/Models/Ruins (e.g. FallenSolarArray).")]
        [SerializeField] private string _id;

        [Tooltip("XZ offset from the ruins cluster's centre, metres (+X east, +Z north).")]
        [SerializeField] private Vector2 _offset;

        [Tooltip("Yaw of the ruin piece, degrees clockwise from +Z.")]
        [SerializeField] private float _yaw;

        [Tooltip("Clearance radius of the ruin piece's footprint, metres.")]
        [SerializeField] private float _clearanceRadius;

        public RuinPlacement(string id, Vector2 offset, float yaw, float clearanceRadius)
        {
            _id = id;
            _offset = offset;
            _yaw = yaw;
            _clearanceRadius = clearanceRadius;
        }

        public string Id => _id;

        public Vector2 Offset => _offset;

        public float Yaw => _yaw;

        public float ClearanceRadius => _clearanceRadius;
    }
}
