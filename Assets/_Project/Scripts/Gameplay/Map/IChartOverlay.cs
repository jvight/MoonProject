using UnityEngine;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// What the lab's survey chart draws beyond its places (docs/features/M3-21 §2, M3-20): each charger's circle (the
    /// dock and every lit mast, as far as 07 comfortably drives out and back on a full battery) and the Link's line,
    /// from home through each lit mast of the chain toward the Peak. Registered in the GameContext by the map system.
    /// Allocation-free: safe to poll every frame.
    /// </summary>
    public interface IChartOverlay
    {
        /// <summary>Chargers to circle: the dock first, then each lit mast.</summary>
        int ChargerCount { get; }

        /// <summary>Where charger <paramref name="index"/> stands (world, on the surface).</summary>
        Vector3 ChargerPosition(int index);

        /// <summary>A charger's circle, in metres: 07's comfortable range with the cells it has now.</summary>
        float ChargerRadius { get; }

        /// <summary>Points of the Link's line: home first, then each lit chain mast nearest home first.</summary>
        int LinkPointCount { get; }

        /// <summary>The Link line's point <paramref name="index"/> (world, on the surface).</summary>
        Vector3 LinkPoint(int index);

        /// <summary>Where the Link's line points past its last lit mast: the Peak.</summary>
        Vector3 Peak { get; }
    }
}
