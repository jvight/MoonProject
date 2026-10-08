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

        int ArmCount { get; }

        /// <summary>The joint transform of arm <paramref name="arm"/> (0-based), for posing it.</summary>
        Transform GetArmJoint(int arm, RoverBayJoint joint);
    }
}
