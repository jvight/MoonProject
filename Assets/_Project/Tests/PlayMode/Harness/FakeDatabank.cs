using System;
using System.Collections.Generic;
using MoonProject.Core;

namespace MoonProject.Testing
{
    /// <summary>
    /// A scripted <see cref="IDatabank"/> for UI tests and captures until Gameplay's archive lands: entries are added
    /// unlocked with their set progress and stay unread until the page marks them read.
    /// </summary>
    public sealed class FakeDatabank : IDatabank
    {
        private readonly List<string> _unlocked = new List<string>();
        private readonly Dictionary<string, DatabankEntryInfo> _entries = new Dictionary<string, DatabankEntryInfo>();
        private readonly Dictionary<string, DatabankProgress> _progress = new Dictionary<string, DatabankProgress>();
        private readonly HashSet<string> _read = new HashSet<string>();

        public IReadOnlyList<string> UnlockedIds => _unlocked;

        /// <summary>
        /// Unlocks <paramref name="entry"/> at <paramref name="found"/> of <paramref name="count"/>, unread.
        /// </summary>
        public void Unlock(DatabankEntryInfo entry, int found = 1, int count = 1)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (!_entries.ContainsKey(entry.Id))
            {
                _unlocked.Add(entry.Id);
            }

            _entries[entry.Id] = entry;
            _progress[entry.Id] = new DatabankProgress(found, count);
        }

        public DatabankProgress Progress(string entryId)
        {
            return _progress.TryGetValue(entryId, out DatabankProgress progress) ? progress : default;
        }

        public bool IsUnread(string entryId)
        {
            return _entries.ContainsKey(entryId) && !_read.Contains(entryId);
        }

        public void MarkRead(string entryId)
        {
            _read.Add(entryId);
        }

        public bool TryGetEntry(string entryId, out DatabankEntryInfo entry)
        {
            return _entries.TryGetValue(entryId, out entry);
        }
    }
}
