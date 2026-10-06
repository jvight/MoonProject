using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Where and how 07 is helped out when stuck. Candidate spots lie on rings around it, nearest ring first; on each
    /// ring the search starts straight behind 07 (the way it came, usually open ground) and sweeps alternately left and
    /// right round to the front. The move itself is a continuous arc: up, across, gently down, never a teleport.
    /// </summary>
    public static class RecoveryPlanner
    {
        /// <summary>Ground-plane offset (x, z) of candidate <paramref name="index"/> for a rover facing
        /// <paramref name="heading"/> degrees.</summary>
        public static Vector2 CandidateOffset(RecoverySettings settings, float heading, int index)
        {
            int directions = settings.SearchDirections;
            int ring = index / directions;
            int slot = index % directions;
            int sweep = (slot + 1) / 2 * (slot % 2 == 0 ? -1 : 1);
            float bearing = (heading + 180f + sweep * 360f / directions) * Mathf.Deg2Rad;
            float radius = settings.SearchMinRadius + ring * settings.SearchRingStep;
            return new Vector2(Mathf.Sin(bearing), Mathf.Cos(bearing)) * radius;
        }

        /// <summary>
        /// Point on the lift arc at progress <paramref name="t"/> (0..1): the horizontal glide eases in and out while
        /// the height rises and falls on a smooth bump, so 07 leaves and arrives without a jolt.
        /// </summary>
        public static Vector3 LiftPosition(Vector3 from, Vector3 to, float lift, float t)
        {
            t = Mathf.Clamp01(t);
            float glide = t * t * t * (t * (6f * t - 15f) + 10f);
            Vector3 position = Vector3.LerpUnclamped(from, to, glide);
            position.y += lift * Mathf.Sin(Mathf.PI * t) * Mathf.Sin(Mathf.PI * t);
            return position;
        }
    }
}
