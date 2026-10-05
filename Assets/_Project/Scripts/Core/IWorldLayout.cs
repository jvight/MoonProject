using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// Fixed landmarks of the generated world, registered in the <see cref="GameContext"/> by the World domain.
    /// </summary>
    public interface IWorldLayout
    {
        /// <summary>Centre of the home base pad on the surface.</summary>
        Vector3 BasePosition { get; }

        /// <summary>Summit of The Peak (the endgame satellite dish stands here).</summary>
        Vector3 PeakPosition { get; }

        /// <summary>Unit vector from the world toward Earth in the sky (rover's idle gaze, sky rendering).</summary>
        Vector3 EarthDirection { get; }
    }
}
