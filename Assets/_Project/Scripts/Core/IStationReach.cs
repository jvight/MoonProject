using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// How far home reaches (docs/features/M3-06): the radio tower's circle plus every lit relay mast linked back to
    /// it. Registered in the <see cref="GameContext"/> by the Gameplay domain; the soundscape, the radio, the map and
    /// the camera read it. Allocation-free: safe to poll every frame.
    /// </summary>
    public interface IStationReach
    {
        /// <summary>True when <paramref name="position"/> is inside the reach of home or of a lit, linked mast.</summary>
        bool IsInReach(Vector3 position);

        /// <summary>Horizontal distance in metres from <paramref name="position"/> to the nearest lit node.</summary>
        float DistanceToNearestNode(Vector3 position);

        /// <summary>Lit nodes, home included.</summary>
        int LitCount { get; }

        /// <summary>All nodes, home first; fixed after initialisation.</summary>
        int NodeCount { get; }

        RelayNode GetNode(int index);
    }
}
