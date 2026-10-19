namespace MoonProject.Core.Events
{
    /// <summary>
    /// Entry <see cref="EntryId"/> joined 07's databank (a story scan, a set's first piece, a milestone): the CRT's
    /// event line says <c>Added to databank: …</c> once (docs/design/databank.md §3).
    /// </summary>
    public readonly struct DatabankEntryAdded
    {
        public DatabankEntryAdded(string entryId, DatabankCategory category)
        {
            EntryId = entryId ?? string.Empty;
            Category = category;
        }

        public string EntryId { get; }

        public DatabankCategory Category { get; }
    }

    /// <summary>
    /// Entry <see cref="EntryId"/> gained a piece of its scan set: <see cref="Found"/> of <see cref="Count"/>. Its model
    /// brightens and, when whole, draws full with its history.
    /// </summary>
    public readonly struct DatabankEntryProgressed
    {
        public DatabankEntryProgressed(string entryId, int found, int count)
        {
            EntryId = entryId ?? string.Empty;
            Found = found;
            Count = count;
        }

        public string EntryId { get; }

        public int Found { get; }

        public int Count { get; }
    }
}
