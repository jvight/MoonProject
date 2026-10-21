using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How wide the range ring's line is in the world so it is a few pixels on screen, whatever its distance (pure,
    /// EditMode-tested). The edge lies hundreds of metres away: a fixed world width would be a thick stripe edge-on.
    /// The line holds its widest out to a distance and thins with it, never under a floor.
    /// </summary>
    public static class RangeRingWidth
    {
        private const float MinDistance = 0.1f;

        /// <summary>The line's width on screen (px) at <paramref name="distance"/> metres.</summary>
        public static float Pixels(float distance, float maxPixels, float minPixels, float fullDistance)
        {
            float wanted = maxPixels * fullDistance / Mathf.Max(MinDistance, distance);
            return Mathf.Clamp(wanted, minPixels, maxPixels);
        }

        /// <summary>The line's width in the world (m) that shows as <see cref="Pixels"/> at that distance.</summary>
        public static float World(float distance, float focalPixels, float maxPixels, float minPixels,
            float fullDistance)
        {
            return Pixels(distance, maxPixels, minPixels, fullDistance) * Mathf.Max(MinDistance, distance) /
                   Mathf.Max(1f, focalPixels);
        }

        /// <summary>The camera's focal length in pixels.</summary>
        public static float FocalPixels(float pixelHeight, float fieldOfViewDegrees)
        {
            return pixelHeight / (2f * Mathf.Tan(fieldOfViewDegrees * Mathf.Deg2Rad * 0.5f));
        }
    }
}
