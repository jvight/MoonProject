using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// How wide the tow rope is drawn in the world so it reads as a rope on screen at any distance (pure,
    /// EditMode-tested): its real width up close, and far away a share of the screen's height that never falls under a
    /// few pixels at 1440p, so the load is seen to be towed, not following by magic. The share is of the screen, not of
    /// a pixel count, so it holds at any window size.
    /// </summary>
    public static class RopeWidth
    {
        /// <summary>The screen height (px) the tuned pixel widths are measured at.</summary>
        public const float ReferenceHeight = 1440f;

        private const float MinDistance = 0.1f;

        /// <summary>The share of the screen's height a rope of <paramref name="pixels"/> at 1440p covers.</summary>
        public static float ScreenShare(float pixels)
        {
            return pixels / ReferenceHeight;
        }

        /// <summary>
        /// The rope's width in metres at <paramref name="distance"/> from a lens of <paramref name="fieldOfView"/>
        /// degrees: <paramref name="minMetres"/> up close, <paramref name="pixels"/> at 1440p when that is more.
        /// </summary>
        public static float World(float distance, float fieldOfView, float pixels, float minMetres)
        {
            float visibleHeight = 2f * Mathf.Max(MinDistance, distance) * Mathf.Tan(fieldOfView * Mathf.Deg2Rad * 0.5f);
            return Mathf.Max(minMetres, visibleHeight * ScreenShare(pixels));
        }
    }
}
