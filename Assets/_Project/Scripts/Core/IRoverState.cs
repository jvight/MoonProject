using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// Read-only telemetry of the player rover, registered in the <see cref="GameContext"/> by the Rover domain.
    /// Audio, camera, gameplay and UI read it every frame instead of reaching into rover components.
    /// </summary>
    public interface IRoverState
    {
        /// <summary>Interpolated world position of the rover body (visual, not raw physics).</summary>
        Vector3 Position { get; }

        /// <summary>Visual body rotation (yaw from steering, pitch/roll from ground alignment).</summary>
        Quaternion Rotation { get; }

        Vector3 Velocity { get; }

        /// <summary>Horizontal speed in m/s.</summary>
        float Speed { get; }

        /// <summary>Horizontal speed divided by the current top speed, 0..1.</summary>
        float NormalizedSpeed { get; }

        /// <summary>Current drive input after smoothing: x = steer, y = throttle, each -1..1.</summary>
        Vector2 DriveInput { get; }

        bool IsGrounded { get; }

        /// <summary>Seconds since the rover last left the ground (0 while grounded).</summary>
        float AirTime { get; }

        /// <summary>Ground normal under the rover (Vector3.up while airborne).</summary>
        Vector3 GroundNormal { get; }
    }
}
