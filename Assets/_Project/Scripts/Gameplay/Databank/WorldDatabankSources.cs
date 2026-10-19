using System;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>The databank's view of the live world: the scan field and the opening's milestones.</summary>
    internal sealed class WorldDatabankSources : IDatabankSources
    {
        private readonly ScanField _scans;
        private readonly Func<OpeningMilestone, bool> _reached;

        public WorldDatabankSources(ScanField scans, Func<OpeningMilestone, bool> reached)
        {
            _scans = scans ?? throw new ArgumentNullException(nameof(scans));
            _reached = reached ?? throw new ArgumentNullException(nameof(reached));
        }

        public bool IsStoryScanned(string storyId)
        {
            return _scans.IsStoryScanned(storyId);
        }

        public int ScannedInSet(string setId)
        {
            return _scans.ScannedInSet(setId);
        }

        public int SetSize(string setId)
        {
            return _scans.SetSize(setId);
        }

        public bool HasReached(OpeningMilestone milestone)
        {
            return _reached(milestone);
        }
    }
}
