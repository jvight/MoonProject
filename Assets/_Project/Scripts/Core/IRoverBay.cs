using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// Kenji's Rover Bay, registered in the <see cref="GameContext"/> by the Gameplay domain so the Rover domain can
    /// drive the gantry arms that fit kit onto 07 (VISION ruling 14: 07 has no hands). Fixed after initialisation.
    /// </summary>
    public interface IRoverBay
    {
        /// <summary>World position of the turntable's centre, where 07 parks to be fitted.</summary>
        Vector3 TurntablePosition { get; }

        /// <summary>World rotation 07 faces on the turntable.</summary>
        Quaternion TurntableRotation { get; }

        /// <summary>The turntable disc under 07; turning it turns 07 to show a fitted piece.</summary>
        Transform Turntable { get; }

        /// <summary>
        /// The floor arm that rises through the turntable's slot to fit kit under 07's belly (the Hover-Jump coils).
        /// </summary>
        Transform FloorLift { get; }

        /// <summary>The floor arm's tip, where a belly piece rides up to its socket.</summary>
        Transform FloorTip { get; }

        int ArmCount { get; }

        /// <summary>The joint transform of arm <paramref name="arm"/> (0-based), for posing it.</summary>
        Transform GetArmJoint(int arm, RoverBayJoint joint);
    }
}
