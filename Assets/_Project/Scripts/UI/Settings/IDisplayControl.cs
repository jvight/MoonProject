using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The screen as the Display page drives it: what the monitor offers, and applying a choice at once.</summary>
    internal interface IDisplayControl
    {
        /// <summary>The monitor's resolutions, largest first, each size once.</summary>
        System.Collections.Generic.IReadOnlyList<Vector2Int> Resolutions { get; }

        /// <summary>The desktop's own resolution.</summary>
        Vector2Int Native { get; }

        void Apply(DisplayState state);
    }
}
