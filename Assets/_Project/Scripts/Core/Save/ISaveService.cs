using System;

namespace MoonProject.Core.Save
{
    /// <summary>
    /// Persistent progress, registered in the <see cref="GameContext"/> by GameBootstrap. Systems register their
    /// <see cref="ISaveSection"/> in Initialize; the bootstrap loads once every system is initialised (so each section
    /// is restored before any Start), and saves when the game quits or goes to the background. Gameplay calls
    /// <see cref="SaveNow"/> at checkpoints (relic deposited, upgrade bought). Nothing runs per frame.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>Absolute path of the save file.</summary>
        string FilePath { get; }

        /// <summary>True once the save file has been read (successfully or not).</summary>
        bool IsLoaded { get; }

        /// <summary>Where the loaded progress came from.</summary>
        SaveLoadResult LoadResult { get; }

        /// <summary>
        /// Adds <paramref name="section"/>; if the file is already loaded, its saved data is restored immediately.
        /// Dispose the token when the owner goes away: the section's state is captured one last time and kept for
        /// the next save. Throws on a duplicate key or an invalid section (wiring bug).
        /// </summary>
        IDisposable Register(ISaveSection section);

        /// <summary>Writes every section to disk atomically. Returns false (and logs why) if nothing was written.</summary>
        bool SaveNow();
    }
}
