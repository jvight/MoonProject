using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Which ability brings each piece of 07's visible kit, which friend gives each gift and which pieces Kenji's garage
    /// port fits (VISION ruling 11), and the upgrade ids that fit kit on 07.
    /// </summary>
    public static class RoverKitPieces
    {
        /// <summary>Upgrades bought for 07 itself (Kenji's bench) have ids starting with this.</summary>
        public const string UpgradePrefix = "rover.";

        /// <summary>Friend ids (Gameplay's friend definitions) and their gifts.</summary>
        public const string TillyId = "tilly";
        public const string BellId = "bell";

        /// <summary>The ability that brings <paramref name="piece"/>; false for a friend's gift.</summary>
        public static bool TryGetAbility(RoverKitPiece piece, out RoverAbility ability)
        {
            switch (piece)
            {
                case RoverKitPiece.HoverCoils:
                    ability = RoverAbility.HoverJump;
                    return true;
                case RoverKitPiece.LampBar:
                    ability = RoverAbility.WarmHeadlamp;
                    return true;
                case RoverKitPiece.CapacitorDrums:
                    ability = RoverAbility.BoostCoils;
                    return true;
                case RoverKitPiece.CargoRack:
                    ability = RoverAbility.CargoCradle;
                    return true;
                case RoverKitPiece.BatterySpine:
                    ability = RoverAbility.CellPack;
                    return true;
                case RoverKitPiece.FocusLens:
                    ability = RoverAbility.FocusLens;
                    return true;
                case RoverKitPiece.SideBins:
                    ability = RoverAbility.SideBins;
                    return true;
                default:
                    ability = RoverAbility.HoverJump;
                    return false;
            }
        }

        /// <summary>
        /// True for a piece Kenji's garage port fits (first-hour v2 §3): the tether ring and the tape deck. Neither is a
        /// friend's gift nor bay kit; each grows in place in its own install moment.
        /// </summary>
        public static bool IsPortPiece(RoverKitPiece piece)
        {
            return piece == RoverKitPiece.TetherRing || piece == RoverKitPiece.TapeDeck;
        }

        /// <summary>True for an upgrade id that fits kit on 07.</summary>
        public static bool IsRoverUpgrade(string upgradeId)
        {
            return upgradeId != null && upgradeId.StartsWith(UpgradePrefix, System.StringComparison.Ordinal);
        }
    }
}
