using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// What 07's eye does to the picture while the camera looks through it (docs/design/cutscenes.md §1). Rover fills
    /// one each frame of the opening and hands it to <see cref="ILensView"/>; World turns it into the lid mask, the
    /// focus, the flicker, the warm tint and the dust speck.
    /// </summary>
    public readonly struct LensViewState
    {
        public LensViewState(float lid01, float focusDistance, float flicker01, float warmth01, Vector2 speckOffset)
        {
            Lid01 = lid01;
            FocusDistance = focusDistance;
            Flicker01 = flicker01;
            Warmth01 = warmth01;
            SpeckOffset = speckOffset;
        }

        /// <summary>How open 07's brow-shutter is: 0 shut (black), 0.7 its half-lidded rest, 1 fully open.</summary>
        public float Lid01 { get; }

        /// <summary>Distance in metres the lens is focused at; very near blurs the whole world.</summary>
        public float FocusDistance { get; }

        /// <summary>
        /// How deep the old electronics' dip is right now, 0..1 of the lens view's largest dip (at most 30% exposure).
        /// The roll band travels while it is above zero.
        /// </summary>
        public float Flicker01 { get; }

        /// <summary>How much of the brief warm tint is lifted, 0..1.</summary>
        public float Warmth01 { get; }

        /// <summary>
        /// Where the dust speck sits on the glass, in viewport units from the frame's centre (±0.5 at the edges). It is
        /// sharp only while the lens focuses very near; beyond the edges it is gone.
        /// </summary>
        public Vector2 SpeckOffset { get; }

    }
}
