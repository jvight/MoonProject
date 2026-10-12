using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// <see cref="IDisplayControl"/> on Unity's screen. In the editor it changes nothing: the Game view owns the
    /// editor's size, window and sync, and a test run must not be paced by a sync.
    /// </summary>
    internal sealed class UnityDisplayControl : IDisplayControl
    {
        private const int MinWidth = 1024;

        private List<Vector2Int> _resolutions;
        private Vector2Int _native;

        /// <summary>
        /// The screen is read on first use, not when this is made: the UI system holds one as a field, and Unity does
        /// not allow reading the screen while a component is being constructed.
        /// </summary>
        private void Load()
        {
            if (_resolutions != null)
            {
                return;
            }

            var offered = new List<Vector2Int>();
            foreach (Resolution resolution in Screen.resolutions)
            {
                offered.Add(new Vector2Int(resolution.width, resolution.height));
            }

            _native = new Vector2Int(Display.main.systemWidth, Display.main.systemHeight);
            if (!offered.Contains(_native))
            {
                offered.Add(_native);
            }

            _resolutions = DisplayChoices.Distinct(offered, MinWidth);
            if (_resolutions.Count == 0)
            {
                _resolutions.Add(_native);
            }
        }

        public IReadOnlyList<Vector2Int> Resolutions
        {
            get
            {
                Load();
                return _resolutions;
            }
        }

        public Vector2Int Native
        {
            get
            {
                Load();
                return _native;
            }
        }

        public void Apply(DisplayState state)
        {
            if (Application.isEditor)
            {
                return;
            }

            Screen.SetResolution(state.Width, state.Height, Mode(state.Mode));
            QualitySettings.vSyncCount = state.VSync ? 1 : 0;
            Application.targetFrameRate = state.VSync || state.FpsCap <= 0 ? -1 : state.FpsCap;
        }

        private static FullScreenMode Mode(DisplayMode mode)
        {
            switch (mode)
            {
                case DisplayMode.Exclusive:
                    return FullScreenMode.ExclusiveFullScreen;
                case DisplayMode.Windowed:
                    return FullScreenMode.Windowed;
                default:
                    return FullScreenMode.FullScreenWindow;
            }
        }
    }
}
