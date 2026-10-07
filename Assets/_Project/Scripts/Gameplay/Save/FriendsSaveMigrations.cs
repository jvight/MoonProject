using System;

namespace MoonProject.Gameplay
{
    /// <summary>Steps of the "gameplay.friends" section from one schema version to the next.</summary>
    public static class FriendsSaveMigrations
    {
        /// <summary>
        /// Version 1 to 2 added <see cref="FriendSaveData.items"/> and <see cref="FriendSaveData.welcomed"/>. A
        /// version 1 friend reads both as their defaults, which is exactly what it had: no required items held (no
        /// version 1 friend needed any) and no first homecoming announced yet. The JSON therefore carries over
        /// unchanged.
        /// </summary>
        public static string Migrate(string json, int fromVersion)
        {
            if (fromVersion == 1)
            {
                return json;
            }

            throw new InvalidOperationException(
                $"{GameplaySaveKeys.Friends} has no migration from version {fromVersion} to {fromVersion + 1}.");
        }
    }
}
