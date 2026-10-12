namespace MoonProject.UI
{
    /// <summary>How the game's window sits on the screen (the settings' Display page).</summary>
    internal enum DisplayMode
    {
        /// <summary>Borderless fullscreen: the desktop's resolution, instant alt-tab. The default.</summary>
        Borderless = 0,

        /// <summary>Exclusive fullscreen at the chosen resolution.</summary>
        Exclusive = 1,

        Windowed = 2,
    }
}
