namespace MoonProject.Core.Events
{
    /// <summary>
    /// App put the title up over the loaded world (<see cref="IGameFlow"/> is in Title). <see cref="HasSave"/> is
    /// false on a fresh install: Rover holds the stargaze frame on 07's closed lens; with a save, the high wide shot of
    /// home.
    /// </summary>
    public readonly struct TitleShown
    {
        public TitleShown(bool hasSave)
        {
            HasSave = hasSave;
        }

        public bool HasSave { get; }
    }

    /// <summary>
    /// Progress was written to disk (a successful <see cref="Save.ISaveService.SaveNow"/>). The dock's lamp and the
    /// HUD answer with a small sign that the game is safe (docs/design/polish-pass.md). Saves come at checkpoints, on
    /// pause and on quit, so a listener that shows something should let a burst of saves read as one.
    /// </summary>
    public readonly struct GameSaved
    {
    }

    /// <summary>
    /// The player chose Continue on the title. App moves the flow to Playing; Rover eases from the title frame into the
    /// chase frame and plays the short wake.
    /// </summary>
    public readonly struct ContinueRequested
    {
    }

    /// <summary>
    /// The player chose "Quit to title" in the pause menu, once the screen has faded. App saves and reloads the scene,
    /// which boots into the title over the same save.
    /// </summary>
    public readonly struct QuitToTitleRequested
    {
    }

    /// <summary>
    /// Play begins (Title -> Playing, or a boot that goes straight into play). Published once per session by App, its
    /// only publisher (docs/design/cutscenes.md §1.8). <see cref="FreshGame"/> is true when there is no progress yet
    /// (New Game, or a first boot): Rover plays the opening; otherwise the short wake.
    /// </summary>
    public readonly struct SessionStarted
    {
        public SessionStarted(bool freshGame)
        {
            FreshGame = freshGame;
        }

        public bool FreshGame { get; }
    }
}
