using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Something other than the player holding 07's wheel: a cinematic autopilot or a scripted feel session.
    /// Same convention as <c>InputReader.Drive</c>: x = steer, y = throttle, magnitude at most 1. Input easing,
    /// steering and physics apply exactly as they do for the player.
    /// </summary>
    public interface IRoverDriveSource
    {
        Vector2 Drive { get; }
    }
}
