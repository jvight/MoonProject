using System;
using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>One databank entry's unlock rule.</summary>
    [Serializable]
    public struct DatabankUnlock
    {
        [Tooltip("How the entry unlocks.")]
        [SerializeField] private DatabankUnlockKind _kind;

        [Tooltip("StoryScan: the story id (without the 'story.' prefix). FragmentOf / SetComplete: the scan set id, " +
                 "as in 'ruin.<id>.<n>' (comma-separated for several sets). Seeded and Milestone: empty.")]
        [SerializeField] private string _target;

        [Tooltip("Milestone: the opening milestone that unlocks the entry.")]
        [SerializeField] private OpeningMilestone _milestone;

        public DatabankUnlock(DatabankUnlockKind kind, string target, OpeningMilestone milestone = default)
        {
            _kind = kind;
            _target = target ?? string.Empty;
            _milestone = milestone;
        }

        public DatabankUnlockKind Kind => _kind;

        public string Target => _target ?? string.Empty;

        public OpeningMilestone Milestone => _milestone;

        public static DatabankUnlock Seeded => new DatabankUnlock(DatabankUnlockKind.Seeded, string.Empty);

        public static DatabankUnlock StoryScan(string storyId)
        {
            return new DatabankUnlock(DatabankUnlockKind.StoryScan, storyId);
        }

        public static DatabankUnlock FragmentOf(string setIds)
        {
            return new DatabankUnlock(DatabankUnlockKind.FragmentOf, setIds);
        }

        public static DatabankUnlock SetComplete(string setId)
        {
            return new DatabankUnlock(DatabankUnlockKind.SetComplete, setId);
        }

        public static DatabankUnlock OnMilestone(OpeningMilestone milestone)
        {
            return new DatabankUnlock(DatabankUnlockKind.Milestone, string.Empty, milestone);
        }

        /// <summary>True when this rule names <paramref name="setId"/> (its target may list several, comma-separated).</summary>
        public bool NamesSet(string setId)
        {
            string target = Target;
            int start = 0;
            while (start <= target.Length)
            {
                int end = target.IndexOf(',', start);
                if (end < 0)
                {
                    end = target.Length;
                }

                if (end - start == setId.Length && string.CompareOrdinal(target, start, setId, 0, setId.Length) == 0)
                {
                    return true;
                }

                start = end + 1;
            }

            return false;
        }

        /// <summary>True when the target lists more than one set.</summary>
        public bool NamesSeveralSets => Target.IndexOf(',') >= 0;
    }
}
