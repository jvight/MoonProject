using System.Collections.Generic;

namespace MoonProject.Core
{
    /// <summary>
    /// 07's scan archive (docs/design/databank.md §5), registered in the <see cref="GameContext"/> by Gameplay and
    /// read by the UI's databank page. Read-only apart from <see cref="MarkRead"/>; reads never allocate.
    /// </summary>
    public interface IDatabank
    {
        /// <summary>The unlocked entry ids, oldest first.</summary>
        IReadOnlyList<string> UnlockedIds { get; }

        /// <summary>How far entry <paramref name="entryId"/> has come (0 of 0 when it is not unlocked).</summary>
        DatabankProgress Progress(string entryId);

        /// <summary>True while entry <paramref name="entryId"/> is unlocked and has not been read on the page.</summary>
        bool IsUnread(string entryId);

        /// <summary>The page showed entry <paramref name="entryId"/>: it is read from now on (saved).</summary>
        void MarkRead(string entryId);

        /// <summary>The catalogue's fields for <paramref name="entryId"/>; false for an unknown id.</summary>
        bool TryGetEntry(string entryId, out DatabankEntryInfo entry);
    }
}
