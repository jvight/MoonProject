namespace MoonProject.UI
{
    /// <summary>What the Display page has chosen, as it is handed to <see cref="IDisplayControl"/>.</summary>
    internal readonly struct DisplayState
    {
        public DisplayState(int width, int height, DisplayMode mode, bool vSync, int fpsCap)
        {
            Width = width;
            Height = height;
            Mode = mode;
            VSync = vSync;
            FpsCap = fpsCap;
        }

        public int Width { get; }

        public int Height { get; }

        public DisplayMode Mode { get; }

        public bool VSync { get; }

        /// <summary>Frames per second to hold when V-Sync is off; 0 is unlimited.</summary>
        public int FpsCap { get; }
    }
}
