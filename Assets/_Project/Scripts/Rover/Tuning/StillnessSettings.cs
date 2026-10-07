using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// When 07 counts as resting for <c>IRoverStillness</c> (the camera's wide shot and the soundscape of solitude):
    /// grounded, nearly stopped, no drive input beyond a dead zone and no look input beyond sensor noise.
    /// </summary>
    [Serializable]
    public sealed class StillnessSettings
    {
        [Tooltip("07 is still only below this horizontal speed (m/s).")]
        [Range(0.01f, 2f)]
        [SerializeField] private float _maxSpeed = 0.2f;

        [Tooltip("Drive input (stick or keys, 0..1) up to this counts as no input (stick drift).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _driveDeadZone = 0.05f;

        [Tooltip("Mouse movement (pixels per frame) up to this is a resting hand, not the player looking around.")]
        [Range(0f, 5f)]
        [SerializeField] private float _lookMouseDeadZone = 0.5f;

        [Tooltip("Look stick deflection (0..1) up to this counts as no input (stick drift).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _lookStickDeadZone = 0.1f;

        public float MaxSpeed => _maxSpeed;

        public float DriveDeadZone => _driveDeadZone;

        public float LookMouseDeadZone => _lookMouseDeadZone;

        public float LookStickDeadZone => _lookStickDeadZone;
    }
}
