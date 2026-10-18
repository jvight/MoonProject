using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The one place that manages the mouse cursor. <see cref="UISystem"/> decides the <see cref="CursorMode"/> every
    /// frame with <see cref="CursorRule"/> and hands it here; the cursor is only written when the mode changes, or when
    /// a click or regained focus takes back a lock the editor or an alt-tab took away.
    /// </summary>
    internal sealed class CursorPolicy
    {
        private CursorMode _mode = CursorMode.Menu;
        private bool _applied;

        /// <summary>The mode the cursor is in now.</summary>
        public CursorMode Mode => _mode;

        /// <summary>True while the cursor is free for a menu or a screen (not captured by the camera).</summary>
        public bool InMenu => CursorRule.IsFree(_mode);

        public CursorLockMode LockState => CursorRule.LockOf(_mode);

        public bool IsVisible => CursorRule.IsFree(_mode);

        /// <summary>Puts the cursor in <paramref name="mode"/>; nothing is written while the mode stays the same.</summary>
        public void Set(CursorMode mode)
        {
            if (_applied && mode == _mode)
            {
                return;
            }

            _mode = mode;
            _applied = true;
            Apply();
        }

        /// <summary>Writes the current mode again if the cursor drifted from it (a lost lock, a lost confinement).</summary>
        public void Recapture()
        {
            if (_applied && (Cursor.lockState != LockState || Cursor.visible != IsVisible))
            {
                Apply();
            }
        }

        /// <summary>Hands the cursor back free and visible (the UI is going away).</summary>
        public void Release()
        {
            _mode = CursorMode.Menu;
            _applied = true;
            Apply();
        }

        private void Apply()
        {
            Cursor.lockState = LockState;
            Cursor.visible = IsVisible;
        }
    }
}
