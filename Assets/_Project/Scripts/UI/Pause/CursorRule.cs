using UnityEngine;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The cursor rule (pure, EditMode-tested). Every screen the mouse can click shows the cursor: the title and the
    /// pause menu, 07's face screen and the build menu's cards. Once a structure is picked the ghost follows the
    /// camera's aim, and the camera follows the mouse, so placing and welding capture the cursor exactly like driving.
    /// </summary>
    internal static class CursorRule
    {
        /// <summary>The mode for this frame: a menu wins, then the face screen, then the build menu.</summary>
        public static CursorMode Decide(bool menuOpen, bool screenOpen, BuildModeState build)
        {
            if (menuOpen)
            {
                return CursorMode.Menu;
            }

            return screenOpen || build == BuildModeState.Menu ? CursorMode.Screen : CursorMode.Drive;
        }

        /// <summary>True when the cursor shows and the pointer is free to click.</summary>
        public static bool IsFree(CursorMode mode)
        {
            return mode != CursorMode.Drive;
        }

        public static CursorLockMode LockOf(CursorMode mode)
        {
            switch (mode)
            {
                case CursorMode.Drive:
                    return CursorLockMode.Locked;
                case CursorMode.Screen:
                    return CursorLockMode.Confined;
                default:
                    return CursorLockMode.None;
            }
        }
    }
}
