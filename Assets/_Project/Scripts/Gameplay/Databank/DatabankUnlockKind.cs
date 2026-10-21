namespace MoonProject.Gameplay
{
    /// <summary>How a databank entry unlocks (docs/design/databank.md §2, §3).</summary>
    public enum DatabankUnlockKind
    {
        /// <summary>It exists from the first open (07, the Kessler–Arai card, the Earth eclipse).</summary>
        Seeded = 0,

        /// <summary>A story scan's readout (<c>StoryScanned</c>) unlocks it whole.</summary>
        StoryScan = 1,

        /// <summary>
        /// The first fragment of a scan set unlocks it; a single-set entry then counts the set's fragments
        /// (<c>SCAN 1/3</c>) until the last draws it full. With several set ids any one fragment unlocks it whole.
        /// </summary>
        FragmentOf = 2,

        /// <summary>The set's last fragment unlocks it whole.</summary>
        SetComplete = 3,

        /// <summary>An opening milestone (<c>OpeningMilestoneReached</c>) unlocks it whole.</summary>
        Milestone = 4,

        /// <summary>
        /// 07 reads it as it wakes (the trickle post it slept on, opening v3 5.2): the New Game self-test's end
        /// (<c>BootCheckFinished</c>) unlocks it whole, silently, so the wake keeps its one line; a loaded game has
        /// woken.
        /// </summary>
        Wake = 5,
    }
}
