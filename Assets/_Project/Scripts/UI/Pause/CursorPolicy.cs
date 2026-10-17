using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The one place that manages the mouse cursor: locked and hidden while driving (mouse look), free and visible in
    /// menus, and visible, unlocked and confined to the window while the face inventory is open. If the lock is lost
    /// while driving (alt-tab, the editor's Escape), a click or regaining focus takes it back.
    /// </summary>
    internal sealed class CursorPolicy
    {
        private bool _hasSavedInventoryState;
        private CursorLockMode _savedLockState = CursorLockMode.Locked;
        private bool _savedVisible;
        private bool _savedInMenu;

        public bool InMenu { get; private set; }

        public CursorLockMode LockState { get; private set; } = CursorLockMode.None;

        public bool IsVisible { get; private set; } = true;

        public bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        public void Drive()
        {
            _hasSavedInventoryState = false;
            InMenu = false;
            LockState = CursorLockMode.Locked;
            IsVisible = false;
            Apply();
        }

        public void Menu()
        {
            _hasSavedInventoryState = false;
            InMenu = true;
            LockState = CursorLockMode.None;
            IsVisible = true;
            Apply();
        }

        /// <summary>
        /// Saves the current cursor state and makes the cursor visible, unlocked, and confined to the game window
        /// while the face inventory is open.
        /// </summary>
        public void OpenInventory()
        {
            if (!_hasSavedInventoryState)
            {
                _savedLockState = LockState;
                _savedVisible = IsVisible;
                _savedInMenu = InMenu;
                _hasSavedInventoryState = true;
            }

            InMenu = true;
            LockState = CursorLockMode.Confined;
            IsVisible = true;
            Apply();
        }

        /// <summary>Restores the cursor state that was active before <see cref="OpenInventory"/>.</summary>
        public void CloseInventory()
        {
            if (_hasSavedInventoryState)
            {
                _hasSavedInventoryState = false;
                InMenu = _savedInMenu;
                LockState = _savedLockState;
                IsVisible = _savedVisible;
                Apply();
                return;
            }

            Drive();
        }

        /// <summary>Re-locks the cursor if it was lost while driving, or re-confines it if lost while in inventory.</summary>
        public void Recapture()
        {
            if (!InMenu && !IsLocked)
            {
                Apply();
            }
            else if (_hasSavedInventoryState && Cursor.lockState != CursorLockMode.Confined)
            {
                Apply();
            }
        }

        /// <summary>Hands the cursor back untouched (the UI is going away).</summary>
        public void Release()
        {
            _hasSavedInventoryState = false;
            InMenu = true;
            LockState = CursorLockMode.None;
            IsVisible = true;
            Apply();
        }

        private void Apply()
        {
            Cursor.lockState = LockState;
            Cursor.visible = IsVisible;
        }
    }
}
