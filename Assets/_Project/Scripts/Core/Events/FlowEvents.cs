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

    /// <summary>
    /// The opening's progress points the sky follows (docs/design/opening-v2.md §2, the Director's amendment): the
    /// eclipse ends when 07 first charges on its own pad, the first day's Sun holds at golden hour until the base has
    /// power and then low above the rim until the base can store it, so the first nightfall always falls on a lit base.
    /// </summary>
    public enum OpeningMilestone
    {
        /// <summary>07 started its first charge on a pad the player built (beat B6).</summary>
        FirstCharge = 0,

        /// <summary>A built solar array powers the pad: the recovery protocol's Power step is on (beat B8).</summary>
        PowerOn = 1,

        /// <summary>A built battery bank holds charge (beat B9): the first sunset may begin.</summary>
        NightReady = 2,

        /// <summary>
        /// A built antenna mast caught the signal from beyond the west rim (beat B11, the opening's end): the pillar
        /// rises over the dead relay mast at relay.1, and Link is the next step.
        /// </summary>
        SignalWest = 3,

        /// <summary>
        /// 07 transmitted from the mast and relay.1 blinked the call back (beat R8,
        /// docs/design/opening-v3-purpose.md): the release begins. Published by the answer itself (opening v3 slice
        /// 4.1); the opening's objective flow does not derive it.
        /// </summary>
        CallAnswered = 4,
    }

    /// <summary>
    /// Published by Gameplay's objective flow when the opening reaches <see cref="Milestone"/>, and again on every
    /// session start for each milestone already reached, with <see cref="Restored"/> true, so a listener such as
    /// World's sky clock can snap to the saved state instead of playing the transition.
    /// </summary>
    public readonly struct OpeningMilestoneReached
    {
        public OpeningMilestoneReached(OpeningMilestone milestone, bool restored)
        {
            Milestone = milestone;
            Restored = restored;
        }

        /// <summary>The milestone the opening reached.</summary>
        public OpeningMilestone Milestone { get; }

        /// <summary>True when replayed from a loaded save at session start rather than reached just now.</summary>
        public bool Restored { get; }
    }
}
