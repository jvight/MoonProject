namespace MoonProject.UI
{
    /// <summary>Where focus rests in the pause or title menu (see <see cref="PausePages"/>).</summary>
    internal enum PauseFocus
    {
        /// <summary>Resume in the pause menu; Continue on the title.</summary>
        Resume,
        NewGame,
        Quit,
        QuitStay,
        NewGameKeep,

        /// <summary>The first language of the first boot's language step.</summary>
        Language,
    }
}
