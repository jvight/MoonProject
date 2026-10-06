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
}
