using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// The camera the player sees the game through, registered in the <see cref="GameContext"/> by the Rover domain's
    /// camera rig. Gameplay aims from its centre ray; UI projects world points with it.
    /// </summary>
    public interface IViewCamera
    {
        Camera Camera { get; }
    }
}
