using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Which ability brings each piece of 07's visible kit and which friend gives each gift (VISION ruling 11), and
    /// the upgrade ids that fit kit on 07.
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
                default:
                    ability = RoverAbility.HoverJump;
                    return false;
            }
        }

        /// <summary>True for an upgrade id that fits kit on 07.</summary>
        public static bool IsRoverUpgrade(string upgradeId)
        {
            return upgradeId != null && upgradeId.StartsWith(UpgradePrefix, System.StringComparison.Ordinal);
        }
    }
}
