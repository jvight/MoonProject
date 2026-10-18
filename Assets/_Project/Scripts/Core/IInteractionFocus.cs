using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// What 07 can scan or use right now, registered by Gameplay so the camera can keep it in view without
    /// knowing about scanning or salvage. Read-only and allocation-free: poll it every frame.
    /// </summary>
    public interface IInteractionFocus
    {
        /// <summary>
        /// Fills <paramref name="points"/> with the world points to keep in view (the main one first; every part
        /// 07 can scan from here, so whichever the view picks next is shown too) and returns how many, 0 for none.
        /// </summary>
        int CopyFocus(Vector3[] points);
    }
}
