using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// The rover's interaction points, registered in the <see cref="GameContext"/> by the Rover domain next to
    /// <see cref="IRoverState"/>. Gameplay attaches beams and cargo here and asks 07 to look at things; the rover
    /// owns how that looks (eased gaze, body language).
    /// </summary>
    public interface IRoverRig
    {
        /// <summary>Lens centre of 07's eye; +Z is the gaze. Sonar and tether beams leave from here.</summary>
        Transform TetherOrigin { get; }

        /// <summary>Where the cargo bed upgrade attaches.</summary>
        Transform CargoSocket { get; }

        /// <summary>The rover's physics body (the hidden sphere). Read it or apply reaction forces; never move it.</summary>
        Rigidbody PhysicsBody { get; }

        /// <summary>
        /// Asks 07 to look at <paramref name="worldPosition"/>. The highest-priority active request wins; ties go to
        /// the most recent. Calling again with the same owner updates its target.
        /// </summary>
        void SetGazeTarget(object owner, Vector3 worldPosition, int priority);

        /// <summary>Withdraws <paramref name="owner"/>'s gaze request (no-op if it has none).</summary>
        void ClearGazeTarget(object owner);
    }
}
