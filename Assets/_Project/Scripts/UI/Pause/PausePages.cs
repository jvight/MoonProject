namespace MoonProject.UI
{
    /// <summary>
    /// Which page of the pause menu is up, and where focus goes: the main buttons, or one of the calm questions (quit,
    /// start a new game) that replace them. A question always opens on its safe answer (stay, keep going), and Esc
    /// or that answer steps back to the button that asked. Starting over is confirmed at most once.
    /// </summary>
    internal sealed class PausePages
    {
        public PausePage Current { get; private set; }

        /// <summary>True once the player confirmed a new game (the scene is about to reload).</summary>
        public bool StartingOver { get; private set; }

        /// <summary>Where focus rests on the current page.</summary>
        public PauseFocus DefaultFocus
        {
            get
            {
                switch (Current)
                {
                    case PausePage.Quit:
                        return PauseFocus.QuitStay;
                    case PausePage.NewGame:
                        return PauseFocus.NewGameKeep;
                    default:
                        return PauseFocus.Resume;
                }
            }
        }

        /// <summary>The menu opened: always on the main buttons.</summary>
        public void Reset()
        {
            Current = PausePage.Main;
        }

        /// <summary>Puts <paramref name="question"/> up; false unless the main buttons were showing.</summary>
        public bool Ask(PausePage question)
        {
            if (Current != PausePage.Main || question == PausePage.Main)
            {
                return false;
            }

            Current = question;
            return true;
        }

        /// <summary>
        /// Esc, Stay or Keep going: back to the main buttons. Returns the button to focus there (the one that asked),
        /// or false when no question was up.
        /// </summary>
        public bool Back(out PauseFocus focus)
        {
            switch (Current)
            {
                case PausePage.Quit:
                    focus = PauseFocus.Quit;
                    break;
                case PausePage.NewGame:
                    focus = PauseFocus.NewGame;
                    break;
                default:
                    focus = PauseFocus.Resume;
                    return false;
            }

            Current = PausePage.Main;
            return true;
        }

        /// <summary>Start over: true only the first time, and only while the new-game question is up.</summary>
        public bool ConfirmNewGame()
        {
            if (Current != PausePage.NewGame || StartingOver)
            {
                return false;
            }

            StartingOver = true;
            return true;
        }
    }
}
