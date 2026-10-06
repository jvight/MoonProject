using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The one place that manages the mouse cursor: locked and hidden while driving (mouse look), free and visible in
    /// menus. If the lock is lost while driving (alt-tab, the editor's Escape), a click or regaining focus takes it
    /// back.
    /// </summary>
    internal sealed class CursorPolicy
    {
        public bool InMenu { get; private set; }

        public bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        public void Drive()
        {
            InMenu = false;
            Apply();
        }

        public void Menu()
        {
            InMenu = true;
            Apply();
        }

        /// <summary>Re-locks the cursor if it was lost while driving.</summary>
        public void Recapture()
        {
            if (!InMenu && !IsLocked)
            {
                Apply();
            }
        }

        /// <summary>Hands the cursor back untouched (the UI is going away).</summary>
        public void Release()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Apply()
        {
            Cursor.lockState = InMenu ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = InMenu;
        }
    }
}
