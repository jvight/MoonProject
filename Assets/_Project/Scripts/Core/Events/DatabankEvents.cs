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

    /// <summary>
    /// 07 understands <see cref="Maker"/> better (docs/design/maker-understanding.md §4): it now holds
    /// <see cref="Points"/> of that maker's completed entries, which is understanding <see cref="Level"/>; the next
    /// level needs <see cref="NextThreshold"/> (0 past the last). <see cref="AdaptedId"/> names the adapted blueprint
    /// this gain unlocked ("adapted.ssr3_lamp"), empty when it unlocked none; <see cref="First"/> marks the maker's
    /// first announced gain (its standards are logged). Held through the opening until the west signal, then
    /// announced once.
    /// </summary>
    public readonly struct UnderstandingGained
    {
        public UnderstandingGained(DatabankMaker maker, int points, int level, int nextThreshold, string adaptedId,
            bool first)
        {
            Maker = maker;
            Points = points;
            Level = level;
            NextThreshold = nextThreshold;
            AdaptedId = adaptedId ?? string.Empty;
            First = first;
        }

        public DatabankMaker Maker { get; }

        public int Points { get; }

        public int Level { get; }

        public int NextThreshold { get; }

        public string AdaptedId { get; }

        public bool First { get; }
    }
}
