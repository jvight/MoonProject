using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// The thing 07 can scan or use right now, registered by Gameplay so the camera can keep it in view without
    /// knowing about scanning or salvage. Read-only and allocation-free: poll it every frame.
    /// </summary>
    public interface IInteractionFocus
    {
        /// <summary>True, with the world point to keep in view, while a scan or interaction target is available.</summary>
        bool TryGetFocus(out Vector3 point);
    }
}
