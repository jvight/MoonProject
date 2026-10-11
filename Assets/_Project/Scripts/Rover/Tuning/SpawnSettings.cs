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

        [Tooltip("Where 07 stands on the base pad (m: x and z from the pad's centre), on its charging dock: Art's "
            + "DockAnchor stands exactly here (DockSpawnTests). 4.5 m south of the centre keeps 07 and the dock clear "
            + "of the lander's long earthlight shadow, which then falls behind 07's head in the chase frame instead of "
            + "across its body (cutscenes O13). Keep x at 0 so home stays due north of the dock (the evening shot). "
            + "Rebuild the scene after changing it.")]
        [SerializeField] private Vector2 _position = new Vector2(0f, -4.5f);

        public float Yaw => _yaw;

        /// <summary>07's rest point on the base pad: x and z (m) from the pad's centre.</summary>
        public Vector2 Position => _position;
    }
}
