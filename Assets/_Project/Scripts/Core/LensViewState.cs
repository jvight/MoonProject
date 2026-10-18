using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// How 07's lid darkens the picture this frame (docs/design/cutscenes.md §1). Rover fills one each frame of the
    /// opening's fades and hands it to <see cref="ILensView"/>; World turns it into the lid mask and the focus.
    /// </summary>
    public readonly struct LensViewState
    {
        public LensViewState(float lid01, float focusDistance)
        {
            Lid01 = lid01;
            FocusDistance = focusDistance;
        }

        /// <summary>How open 07's lid is over the picture: 0 shut (black), 1 fully open.</summary>
        public float Lid01 { get; }

        /// <summary>Distance in metres the lens is focused at: kept on 07, so the picture stays sharp on it.</summary>
        public float FocusDistance { get; }
    }
}
