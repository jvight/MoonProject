using System;

namespace MoonProject.UI
{
    /// <summary>
    /// Where the databank page's focus is (pure, EditMode-tested): one category open at a time, its entries listed one
    /// level below it (docs/design/databank.md §4). Up and down walk the open category's entries and step onto the
    /// next or previous category, which opens; a category's own row shows its newest card. The page remembers the
    /// last entry it showed.
    /// </summary>
    internal sealed class DatabankCursor
    {
        private readonly DatabankIndex _index;
        private string _remembered;

        public DatabankCursor(DatabankIndex index)
        {
            _index = index ?? throw new ArgumentNullException(nameof(index));
        }

        /// <summary>The open category (index into <see cref="DatabankIndex.Categories"/>).</summary>
        public int Category { get; private set; }

        /// <summary>The focused entry of the open category, or -1 while the category's own row has the focus.</summary>
        public int Entry { get; private set; } = -1;

        /// <summary>
        /// The card the page shows: the focused entry, else the open category's newest; null when empty.
        /// </summary>
        public string ShownId
        {
            get
            {
                var entries = _index.EntriesOf(Category);
                if (entries.Count == 0)
                {
                    return null;
                }

                return entries[Entry >= 0 && Entry < entries.Count ? Entry : 0];
            }
        }

        /// <summary>
        /// The page opened (or the archive changed): the focus returns to the last card shown, if it is there.
        /// </summary>
        public void Restore()
        {
            if (_remembered != null && Select(_remembered))
            {
                return;
            }

            Category = Math.Min(Category, DatabankIndex.Categories.Length - 1);
            Entry = Math.Min(Entry, _index.EntriesOf(Category).Count - 1);
        }

        /// <summary>Moves the focus to entry <paramref name="entryId"/>; false when it is not listed.</summary>
        public bool Select(string entryId)
        {
            int category = _index.CategoryOf(entryId);
            if (category < 0)
            {
                return false;
            }

            Category = category;
            Entry = IndexIn(category, entryId);
            Remember();
            return true;
        }

        /// <summary>Opens category <paramref name="category"/> on its own row.</summary>
        public void OpenCategory(int category)
        {
            if (category < 0 || category >= DatabankIndex.Categories.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(category));
            }

            Category = category;
            Entry = -1;
            Remember();
        }

        /// <summary>
        /// Focuses entry <paramref name="entry"/> of <paramref name="category"/> (-1: the category's own row).
        /// </summary>
        public void Focus(int category, int entry)
        {
            OpenCategory(category);
            Entry = Math.Max(-1, Math.Min(entry, _index.EntriesOf(category).Count - 1));
            Remember();
        }

        /// <summary>One step down: the next entry, else the next category's row (wrapping to the first).</summary>
        public void Down()
        {
            if (Entry < _index.EntriesOf(Category).Count - 1)
            {
                Entry++;
            }
            else
            {
                Category = (Category + 1) % DatabankIndex.Categories.Length;
                Entry = -1;
            }

            Remember();
        }

        /// <summary>
        /// One step up: the previous entry, else the category's row, else the previous category's last entry.
        /// </summary>
        public void Up()
        {
            if (Entry >= 0)
            {
                Entry--;
            }
            else
            {
                Category = (Category + DatabankIndex.Categories.Length - 1) % DatabankIndex.Categories.Length;
                Entry = _index.EntriesOf(Category).Count - 1;
            }

            Remember();
        }

        private void Remember()
        {
            string shown = ShownId;
            if (shown != null)
            {
                _remembered = shown;
            }
        }

        private int IndexIn(int category, string entryId)
        {
            var entries = _index.EntriesOf(category);
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i], entryId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
