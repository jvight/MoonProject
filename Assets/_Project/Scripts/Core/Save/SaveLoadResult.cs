namespace MoonProject.Core.Save
{
    /// <summary>Outcome of <see cref="SaveService.Load"/>.</summary>
    public enum SaveLoadResult
    {
        /// <summary>Load has not run yet.</summary>
        NotLoaded = 0,

        /// <summary>No save file exists: a new game.</summary>
        NoSave = 1,

        /// <summary>Loaded from the save file.</summary>
        Loaded = 2,

        /// <summary>The save file was unreadable and was moved aside; progress came from the backup.</summary>
        RecoveredFromBackup = 3,

        /// <summary>Save and backup were both unreadable and moved aside; the game starts fresh.</summary>
        Unreadable = 4,

        /// <summary>The file was written by a newer build; it is left untouched and saving is disabled.</summary>
        NewerFormat = 5,
    }
}
