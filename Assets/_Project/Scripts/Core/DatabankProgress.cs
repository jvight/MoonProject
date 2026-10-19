namespace MoonProject.Core
{
    /// <summary>
    /// How far a databank entry has come: <see cref="Found"/> of <see cref="Count"/> pieces of its scan set (1 of 1
    /// for an entry that unlocks whole).
    /// </summary>
    public readonly struct DatabankProgress
    {
        public DatabankProgress(int found, int count)
        {
            Found = found;
            Count = count;
        }

        public int Found { get; }

        public int Count { get; }

        /// <summary>Every piece is in: the model draws full and the history shows.</summary>
        public bool IsComplete => Count > 0 && Found >= Count;
    }
}
