using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>
    /// Story showed the next line, and its object is at <see cref="Position"/>. Rover turns 07's head toward it and,
    /// if it is off screen, nudges the camera that way once (G5, docs/design/opening-v3-guidance-audit.md). Published
    /// once per target.
    /// </summary>
    public readonly struct NextActionShown
    {
        public NextActionShown(string targetId, Vector3 position)
        {
            TargetId = targetId;
            Position = position;
        }

        /// <summary>A stable id of the target (a step or anchor id): the nudge happens once per id.</summary>
        public string TargetId { get; }

        /// <summary>The object's world point.</summary>
        public Vector3 Position { get; }
    }
}
