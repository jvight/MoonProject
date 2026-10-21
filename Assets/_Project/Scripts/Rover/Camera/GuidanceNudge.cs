using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The rules of the next-action nudge (G5, pure and EditMode-tested): a target is nudged toward at most once, and
    /// only when it is far enough from 07, outside the chase view's horizontal field, and beyond the nudge slack so a
    /// target already in view never leaves it.
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
        /// behind the camera or beyond <paramref name="margin"/> of a horizontal edge of the view.
        /// </summary>
        public static bool OffScreen(Vector3 viewport, float margin)
        {
            return viewport.z <= 0f || viewport.x < margin || viewport.x > 1f - margin;
        }

        /// <summary>
        /// True when <paramref name="viewport"/> is currently inside the camera's viewport (ahead of the camera and
        /// within [0, 1] horizontally and vertically).
        /// </summary>
        public static bool InView(Vector3 viewport)
        {
            return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        }

        /// <summary>
        /// True when the camera should yaw-nudge toward <paramref name="target"/>: the target is at least
        /// <paramref name="minDistance"/> from <paramref name="rover"/> horizontally, not already in view or within the
        /// horizontal field (<see cref="OffScreen"/>), and its bearing from <paramref name="lookingBearing"/> lies
        /// strictly between <paramref name="slack"/> and <paramref name="maxBearing"/> so the camera only turns toward
        /// targets outside the view.
        /// </summary>
        public static bool ShouldNudge(Vector3 rover, Vector3 target, float lookingBearing, Vector3 viewport,
            float margin, float minDistance, float slack, float maxBearing)
        {
            float dx = target.x - rover.x;
            float dz = target.z - rover.z;
            if (dx * dx + dz * dz < minDistance * minDistance)
            {
                return false;
            }

            if (InView(viewport) || !OffScreen(viewport, margin))
            {
                return false;
            }

            float bearing = Mathf.Abs(Mathf.DeltaAngle(lookingBearing, GazePan.BearingTo(rover, target)));
            return bearing > slack && bearing <= maxBearing;
        }
    }
}
