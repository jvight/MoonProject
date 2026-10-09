using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>A place in the world where upgrades are sold (the radio tower's pad, Kenji's Rover Bay).</summary>
    public interface IUpgradeStation
    {
        /// <summary>What it offers now (its first upgrade not yet fully bought; its last when all are).</summary>
        UpgradeDefinition Definition { get; }

        /// <summary>True while 07 is parked there.</summary>
        bool Occupied { get; }

        /// <summary>Centre of its pad on the ground.</summary>
        Vector3 PadCentre { get; }

        bool Sells(UpgradeDefinition definition);

        /// <summary>How many upgrades it sells, bought ones included.</summary>
        int UpgradeCount { get; }

        /// <summary>Its upgrade <paramref name="index"/>, in its own order.</summary>
        UpgradeDefinition UpgradeAt(int index);
    }
}
