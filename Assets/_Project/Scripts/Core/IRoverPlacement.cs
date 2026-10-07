using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// Moves 07 somewhere else in one step, registered in the <see cref="GameContext"/> by the Rover domain. Only for
    /// moments the player cannot see (the radio-hop calls it once, while the view is fully faded out): the physics
    /// body, the visual rig and the follow camera are snapped, never eased.
    /// </summary>
    public interface IRoverPlacement
    {
        /// <summary>
        /// Puts 07 at rest on the surface at <paramref name="position"/>, facing <paramref name="rotation"/>'s yaw. The
        /// velocity is zeroed and the stuck/landing detectors are reset.
        /// </summary>
        void PlaceAt(Vector3 position, Quaternion rotation);
    }
}
