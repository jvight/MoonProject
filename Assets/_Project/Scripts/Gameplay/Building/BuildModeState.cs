namespace MoonProject.Gameplay
{
    /// <summary>
    /// State of <see cref="BuildModeController"/> (0.5.7 slice 6, BUILD-MODE).
    /// </summary>
    public enum BuildModeState
    {
        /// <summary>Build mode is inactive; normal rover exploration and tools run.</summary>
        Closed = 0,

        /// <summary>The build strip is open and the player is choosing a known structure.</summary>
        Menu = 1,

        /// <summary>A structure is selected and its hologram placement ghost tracks the terrain aim point.</summary>
        Placing = 2,

        /// <summary>07 is actively welding a newly placed or resumed unfinished structure into service.</summary>
        Welding = 3,
    }
}
