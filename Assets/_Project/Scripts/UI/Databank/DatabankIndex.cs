using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The databank page's view of 07's archive (pure, EditMode-tested): the unlocked entries sorted into their
    /// categories, newest first, and which categories hold an unread card (docs/design/databank.md §1, §4). Rebuilt when an entry is added or the page opens, never per frame.
    /// </summary>
    internal sealed class DatabankIndex
    {
        /// <summary>The categories in the order the page lists them.</summary>
        public static readonly DatabankCategory[] Categories =
        {
            DatabankCategory.Machines, DatabankCategory.Structures, DatabankCategory.Places,
            DatabankCategory.Phenomena, DatabankCategory.Archive,
        };

        /// <summary>
        /// The marks of the era strip, P S B L N (Meridian machines count for their own era, unmarked).
        /// </summary>
        public static readonly DatabankEra[] StripEras =
        {
            DatabankEra.Probes, DatabankEra.Sorties, DatabankEra.BuildOut, DatabankEra.Lumen, DatabankEra.Now,
        };

        private readonly List<string>[] _entries = new List<string>[Categories.Length];

        public DatabankIndex()
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                _entries[i] = new List<string>();
            }
        }

        /// <summary>
        /// The entry ids of category <paramref name="category"/> (index into <see cref="Categories"/>).
        /// </summary>
        public IReadOnlyList<string> EntriesOf(int category) => _entries[category];

        /// <summary>The category <paramref name="entryId"/> is listed under, or -1.</summary>
        public int CategoryOf(string entryId)
        {
            for (int c = 0; c < _entries.Length; c++)
            {
                if (_entries[c].Contains(entryId))
                {
                    return c;
                }
            }

            return -1;
        }

        /// <summary>True while category <paramref name="category"/> holds a card the page has not shown.</summary>
        public bool HasUnread(int category, IDatabank databank)
        {
            List<string> entries = _entries[category];
            for (int i = 0; i < entries.Count; i++)
            {
                if (databank.IsUnread(entries[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sorts <paramref name="databank"/>'s unlocked entries; an id the catalogue does not know is a broken
        /// archive: it is reported and left out.
        /// </summary>
        public void Rebuild(IDatabank databank)
        {
            if (databank == null)
            {
                throw new ArgumentNullException(nameof(databank));
            }

            for (int c = 0; c < _entries.Length; c++)
            {
                _entries[c].Clear();
            }

            IReadOnlyList<string> unlocked = databank.UnlockedIds;
            for (int i = unlocked.Count - 1; i >= 0; i--)
            {
                if (!databank.TryGetEntry(unlocked[i], out DatabankEntryInfo entry))
                {
                    Debug.LogError($"DatabankIndex: '{unlocked[i]}' is unlocked but not in the catalogue.");
                    continue;
                }

                _entries[IndexOf(entry.Category)].Add(entry.Id);
            }
        }

        private static int IndexOf(DatabankCategory category)
        {
            for (int i = 0; i < Categories.Length; i++)
            {
                if (Categories[i] == category)
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(category), category, "a category the page does not list");
        }
    }
}
