using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// Where 07 can charge next and what it costs to get there (docs/design/first-hour-qa.md #8), registered in the
    /// <see cref="GameContext"/> by Gameplay, which knows the dock and the lit relays. UI marks the battery dial with
    /// <see cref="ChargeToReach"/> and names the charger when it gets tight; the home meter shows only when it matters.
    /// Poll it each frame; it never allocates.
    /// </summary>
    public interface IChargerReach
    {
        /// <summary>True when at least one charger 07 can use is lit (the dock always counts once it has power).</summary>
        bool HasCharger { get; }

        /// <summary>The nearest usable charger's position (meaningful only while <see cref="HasCharger"/>).</summary>
        Vector3 NearestPosition { get; }

        /// <summary>The nearest charger's name key (e.g. the dock's, or a relay's hop name), cached, never built.</summary>
        string NearestNameKey { get; }

        /// <summary>Straight-line metres from 07 to the nearest charger.</summary>
        float DistanceToNearest { get; }

        /// <summary>
        /// The battery fraction 07 needs to reach the nearest charger, 0..1, with the same margin the Low warning uses
        /// (docs/features/M3-20 §7).
        /// </summary>
        float ChargeToReach { get; }
    }
}
