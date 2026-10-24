using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How wide the range ring's line is in the world so it is a few pixels on screen, whatever its distance, and how its
    /// brightness falls off across its width (pure, EditMode-tested). The edge lies hundreds of metres away: a fixed
    /// world width would be a thick stripe edge-on. The line holds its widest out to a distance and thins with it, never
    /// under a floor, while its cross-section eases smoothly from a warm centre to zero at both edges.
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

        /// <summary>
        /// The cross-width glow falloff (0..1) at <paramref name="v01"/> across the ribbon (0 and 1 at the edges, 0.5 at
        /// the centre), matching the <c>SoftGlow</c> shader's smoothstep edge mask and core falloff so the line is a soft
        /// gradient across its width, never a hard-edged stripe.
        /// </summary>
        public static float AcrossFalloff(float v01, float edgeFalloff, float corePower, float coreMix)
        {
            float clamped = Mathf.Clamp01(v01);
            float fade = Mathf.Max(0.0001f, edgeFalloff);
            float inMask = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(clamped / fade));
            float outMask = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - clamped) / fade));
            float across = inMask * outMask;
            float center = 1f - Mathf.Clamp01(Mathf.Abs(clamped * 2f - 1f));
            float core = Mathf.Lerp(1f, Mathf.Pow(center, Mathf.Max(0.2f, corePower)), Mathf.Clamp01(coreMix));
            return Mathf.Clamp01(across * core);
        }
    }
}
