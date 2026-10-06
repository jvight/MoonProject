using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// One frame of rover telemetry as the audio model needs it (built from IRoverState, no allocation).
    /// </summary>
    public readonly struct RoverAudioInput
    {
        public RoverAudioInput(float normalizedSpeed, float throttle, bool isGrounded, Vector3 groundNormal)
        {
            NormalizedSpeed = normalizedSpeed;
            Throttle = throttle;
            IsGrounded = isGrounded;
            GroundNormal = groundNormal;
        }

        /// <summary>Horizontal speed / top speed, 0..1.</summary>
        public float NormalizedSpeed { get; }

        /// <summary>Throttle magnitude 0..1 (forward or reverse both load the motor).</summary>
        public float Throttle { get; }

        public bool IsGrounded { get; }

        public Vector3 GroundNormal { get; }
    }
}
