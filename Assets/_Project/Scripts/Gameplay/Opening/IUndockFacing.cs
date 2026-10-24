using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Where 07 should face as a pad lets it go on its own (the charge done, no hand on the wheel): the rover eases
    /// round on the pad toward it, so the chase camera frames what comes next.
    /// </summary>
    public interface IUndockFacing
    {
        /// <summary>The point to face now; false when 07 keeps the dock's heading.</summary>
        bool TryGetUndockFacing(out Vector3 point);
    }
}
