using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Somewhere that can recharge 07: the built pads now, lit relays and hex contacts later. The range ring counts
    /// every point a source lists as a way home and a base to range out from.
    /// </summary>
    public interface IChargePointSource
    {
        /// <summary>Adds to <paramref name="points"/> where 07 can recharge now.</summary>
        void CopyChargePoints(List<Vector3> points);
    }
}
