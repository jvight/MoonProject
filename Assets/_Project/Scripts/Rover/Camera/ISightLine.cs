using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>How far lines run clear of solid scenery and where there is room (the camera's clearance).</summary>
    public interface ISightLine
    {
        /// <summary>
        /// Distance (m) from <paramref name="from"/> toward <paramref name="to"/> that a ball of
        /// <paramref name="radius"/> (0 for a thin line) travels before it meets an obstacle, or the full distance
        /// when the way is clear.
        /// </summary>
        float Clear(Vector3 from, Vector3 to, float radius);

        /// <summary>True when nothing solid lies within <paramref name="radius"/> of the point.</summary>
        bool IsFree(Vector3 point, float radius);
    }
}
