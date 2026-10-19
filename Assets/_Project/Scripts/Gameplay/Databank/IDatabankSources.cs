using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What the databank reads from the world's scans and the opening to unlock entries a load did not save (an older
    /// save): which story scans are read, how far each scan set has come, which milestones are reached.
    /// </summary>
    internal interface IDatabankSources
    {
        /// <summary>True when the story scan <paramref name="storyId"/> (without 'story.') is read.</summary>
        bool IsStoryScanned(string storyId);

        /// <summary>How many fragments of scan set <paramref name="setId"/> are scanned.</summary>
        int ScannedInSet(string setId);

        /// <summary>How many fragments scan set <paramref name="setId"/> has (0 for an unknown set).</summary>
        int SetSize(string setId);

        bool HasReached(OpeningMilestone milestone);
    }
}
