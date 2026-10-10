namespace MoonProject.Core.Save
{
    /// <summary>
    /// The player's settings (volumes, look, language, guidance) in their own file beside the save, so starting a new
    /// game (<see cref="ISaveService.PutAway"/>) never resets them. Registered by App; the UI owns the content.
    /// </summary>
    public interface ISettingsFile
    {
        /// <summary>The stored JSON (else the backup's); false when neither exists or both are unreadable.</summary>
        bool TryRead(out string json);

        /// <summary>Replaces the file crash-safely; false (logged) when the disk refuses.</summary>
        bool Write(string json);
    }
}
