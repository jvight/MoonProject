using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// Which name goes with each part of 07's equipment as it settles onto 07 (docs/design/0.5.5-owner-feedback.md
    /// §3.5), pure logic, EditMode-tested. A crafted part is named after the upgrade that brought it
    /// ("upgrade.&lt;id&gt;.name"); a friend's gift and a part Kenji's garage port fits have their own title
    /// ("kit.&lt;piece&gt;.name").
    /// </summary>
    internal static class PartNames
    {
        /// <summary>The upgrade whose craft brings <paramref name="part"/>; false for a gift or a found part.</summary>
        public static bool TryUpgradeId(RoverKitPiece part, out string upgradeId)
        {
            switch (part)
            {
                case RoverKitPiece.HoverCoils:
                    upgradeId = "rover.hover_jump";
                    return true;
                case RoverKitPiece.LampBar:
                    upgradeId = "rover.warm_headlamp";
                    return true;
                case RoverKitPiece.CapacitorDrums:
                    upgradeId = "rover.boost_coils";
                    return true;
                case RoverKitPiece.CargoRack:
                    upgradeId = "rover.cargo_cradle";
                    return true;
                case RoverKitPiece.BatterySpine:
                    upgradeId = "rover.cell_pack";
                    return true;
                case RoverKitPiece.FocusLens:
                    upgradeId = "rover.focus_lens";
                    return true;
                case RoverKitPiece.SideBins:
                    upgradeId = "rover.side_bins";
                    return true;
                default:
                    upgradeId = null;
                    return false;
            }
        }

        /// <summary>
        /// The title key for the part that just settled. False when nothing names it (a wiring fault for the caller
        /// to report).
        /// </summary>
        public static bool TryTitle(RoverKitPiece part, out string key)
        {
            if (TryUpgradeId(part, out string upgradeId))
            {
                key = UiKeys.UpgradeName(upgradeId);
                return true;
            }

            return UiKeys.TryGetGiftName(part, out key);
        }
    }
}
