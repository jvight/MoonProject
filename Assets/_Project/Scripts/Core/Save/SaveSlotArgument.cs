using System;
using System.Collections.Generic;

namespace MoonProject.Core.Save
{
    /// <summary>
    /// The <c>-saveSlot &lt;name&gt;</c> command-line override: playtests and smoke runs point the game at a throwaway
    /// slot so they never read or write the player's progress.
    /// </summary>
    public static class SaveSlotArgument
    {
        public const string Flag = "-saveSlot";

        /// <summary>
        /// True if <paramref name="commandLine"/> contains the flag (case-insensitive); <paramref name="slot"/> is the
        /// value that follows it, or an empty string when it is missing (which <see cref="SaveService.IsValidSlot"/>
        /// rejects). The last occurrence wins.
        /// </summary>
        public static bool TryRead(IReadOnlyList<string> commandLine, out string slot)
        {
            if (commandLine == null)
            {
                throw new ArgumentNullException(nameof(commandLine));
            }

            slot = null;
            for (int i = 0; i < commandLine.Count; i++)
            {
                if (string.Equals(commandLine[i], Flag, StringComparison.OrdinalIgnoreCase))
                {
                    slot = i + 1 < commandLine.Count ? commandLine[i + 1] : string.Empty;
                }
            }

            return slot != null;
        }
    }
}
