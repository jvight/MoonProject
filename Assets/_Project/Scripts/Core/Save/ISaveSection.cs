namespace MoonProject.Core.Save
{
    /// <summary>
    /// One domain's slice of the save file. Most domains use <see cref="SaveSection{T}"/> instead of implementing this.
    /// </summary>
    public interface ISaveSection
    {
        /// <summary>Stable unique key such as "gameplay.scrap". Never rename it: it identifies the data in old saves.</summary>
        string Key { get; }

        /// <summary>
        /// Schema version of the data, starting at 1. Bump it whenever the data shape changes and teach
        /// <see cref="Migrate"/> the step from the previous version.
        /// </summary>
        int Version { get; }

        /// <summary>The current state as JSON. Called on every save; must not change any state.</summary>
        string Capture();

        /// <summary>Applies saved JSON that is already at <see cref="Version"/>. Called at most once per session.</summary>
        void Restore(string json);

        /// <summary>Converts JSON written at <paramref name="fromVersion"/> to <paramref name="fromVersion"/> + 1.</summary>
        string Migrate(string json, int fromVersion);
    }
}
