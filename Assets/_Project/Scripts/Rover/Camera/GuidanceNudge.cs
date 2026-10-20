using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The rules of the next-action nudge (G5, pure and EditMode-tested): a target is nudged toward at most once, and
    /// only when it is off screen.
    /// </summary>
    public sealed class GuidanceNudge
    {
        private readonly HashSet<string> _seen = new HashSet<string>();

        /// <summary>True the first time <paramref name="targetId"/> is asked about, false every time after.</summary>
        public bool TryTake(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                throw new ArgumentException("A next action has a target id.", nameof(targetId));
            }

            return _seen.Add(targetId);
        }

        /// <summary>
        /// True when a point at <paramref name="viewport"/> (a camera's viewport coordinates, z the distance ahead) is
        /// behind the camera or beyond <paramref name="margin"/> of an edge of the view.
        /// </summary>
        public static bool OffScreen(Vector3 viewport, float margin)
        {
            return viewport.z <= 0f || viewport.x < margin || viewport.x > 1f - margin || viewport.y < margin ||
                   viewport.y > 1f - margin;
        }
    }
}
