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
}
