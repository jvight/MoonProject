namespace MoonProject.UI
{
    /// <summary>A page of the pause or title menu (see <see cref="PausePages"/>).</summary>
    internal enum PausePage
    {
        Main,
        Quit,
        NewGame,

        /// <summary>The first boot's language step, before the title's buttons.</summary>
        Language,
    }
}
