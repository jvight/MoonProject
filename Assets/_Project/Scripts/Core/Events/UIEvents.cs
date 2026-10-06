namespace MoonProject.Core.Events
{
    /// <summary>
    /// The pause menu opened (<see cref="Paused"/> true: game time eases to a stop and the rover controls are off) or
    /// closed again. The radio keeps playing on unscaled time; listeners may soften anything that would otherwise
    /// hang frozen mid-sound.
    /// </summary>
    public readonly struct PauseChanged
    {
        public PauseChanged(bool paused)
        {
            Paused = paused;
        }

        public bool Paused { get; }
    }

    /// <summary>
    /// The player chose another language: every string read through <see cref="ILocalization"/> should be read
    /// again.
    /// </summary>
    public readonly struct LanguageChanged
    {
        public LanguageChanged(string language)
        {
            Language = language;
        }

        /// <summary>Code of the new language, e.g. "vi".</summary>
        public string Language { get; }
    }

    /// <summary>What a <see cref="UiCue"/> marks, so Audio can give every UI touch its own soft sound.</summary>
    public enum UiCueKind
    {
        /// <summary>The pause menu opened.</summary>
        MenuOpen = 0,

        /// <summary>The pause menu closed (resumed).</summary>
        MenuClose = 1,

        /// <summary>Keyboard or gamepad focus moved to another menu item.</summary>
        FocusMove = 2,

        /// <summary>A menu item was pressed (a button, a toggle, the language selector).</summary>
        Confirm = 3,

        /// <summary>Stepped back one level (a submenu or the quit question closed, a memory card dismissed).</summary>
        Back = 4,

        /// <summary>A memory card began to appear.</summary>
        CardShown = 5,

        /// <summary>The tower's hold-to-confirm ring started filling.</summary>
        HoldFill = 6,

        /// <summary>The ring filled and the upgrade was bought.</summary>
        HoldComplete = 7,

        /// <summary>A context prompt began to appear.</summary>
        PromptShown = 8,

        /// <summary>A settings slider moved one step.</summary>
        SliderStep = 9,

        /// <summary>
        /// The hold ended before the ring filled: the button was let go, or the offer disappeared (once per press).
        /// </summary>
        HoldRelease = 10,
    }

    /// <summary>
    /// A UI moment worth a sound. Published by the UI at the moment it happens; carries no position (2D).
    /// </summary>
    public readonly struct UiCue
    {
        public UiCue(UiCueKind kind)
        {
            Kind = kind;
        }

        public UiCueKind Kind { get; }
    }
}
