using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>How 07 is placed when the scene is built: the first frame the player sees.</summary>
    [Serializable]
    public sealed class SpawnSettings
    {
        [Tooltip("Heading (deg, clockwise from +Z) 07 faces on the base pad. The camera starts behind it, so this "
            + "decides what fills the first frame: 355 puts Earth (bearing 338) and The Peak in the opening shot "
            + "(design ruling 8). Rebuild the scene after changing it.")]
        [Range(0f, 360f)]
        [SerializeField] private float _yaw = 355f;

        public float Yaw => _yaw;
    }
}
