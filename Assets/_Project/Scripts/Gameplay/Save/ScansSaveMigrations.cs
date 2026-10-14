using System;

namespace MoonProject.Gameplay
{
    /// <summary>Steps of the "gameplay.scans" section from one schema version to the next.</summary>
    public static class ScansSaveMigrations
    {
        /// <summary>
        /// Version 1 to 2 added the places already reached (D2): a version 1 save reached none, so its JSON reads as
        /// version 2 unchanged and every place with fragments left says so once more.
        /// </summary>
        public static string Migrate(string json, int fromVersion)
        {
            if (fromVersion == 1)
            {
                return json;
            }

            throw new InvalidOperationException(
                $"{GameplaySaveKeys.Scans} has no migration from version {fromVersion}.");
        }
    }
}
