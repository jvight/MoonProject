using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>How the range ring shows itself.</summary>
    public enum RangeRingMode
    {
        /// <summary>A faint ring for a moment (the first charge, R3): the edge of the world 07 can reach.</summary>
        Faint = 0,

        /// <summary>The full ring, shown once at the release (R9): what 07 can and cannot reach, out and back.</summary>
        Full = 1,
    }

    /// <summary>
    /// The range ring (docs/design/opening-v3-purpose.md R3 and R9, plan slice 4.2), registered in the
    /// <see cref="GameContext"/> by Gameplay: the edge of what 07 can reach and still come back from, as a closed
    /// polygon on the ground round 07. It is counted in energy (the battery's charge, the ground's slope, the pads
    /// that can recharge 07), never in plain metres. UI may draw it on 07's face; Gameplay draws it on the ground.
    /// </summary>
    public interface IRangeRing
    {
        /// <summary>The ring is on show now.</summary>
        bool IsShown { get; }

        /// <summary>How it is shown while <see cref="IsShown"/>.</summary>
        RangeRingMode Mode { get; }

        /// <summary>How visible it is now, 0 (gone) to 1 (the full strength of its mode).</summary>
        float Visibility01 { get; }

        /// <summary>Points of the ring's polygon (the last joins the first).</summary>
        int Count { get; }

        /// <summary>Ground position of polygon point <paramref name="index"/>.</summary>
        Vector3 Point(int index);
    }
}
