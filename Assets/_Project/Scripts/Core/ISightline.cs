using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// "Which places can be seen from here" (level review §6), registered in the <see cref="GameContext"/> by the
    /// World domain: the deck lookout's "seen" sweep asks whether the ground hides a place's light from the eye.
    /// Answers come from the height function, never allocate, and are safe to call many times in one frame.
    /// </summary>
    public interface ISightline
    {
        /// <summary>
        /// True when the ground nowhere rises more than <paramref name="clearance"/> metres above the straight line
        /// from <paramref name="eye"/> to <paramref name="target"/>, sampled every <paramref name="step"/> metres. The
        /// place's own ground (within <paramref name="targetRadius"/> metres of the target) is not sampled. Raise the
        /// target to the height of the light that should be seen.
        /// </summary>
        bool IsClear(Vector3 eye, Vector3 target, float targetRadius, float step, float clearance);
    }
}
