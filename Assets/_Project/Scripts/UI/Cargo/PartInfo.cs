using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// What <see cref="FaceInventoryView"/> says about each socket, kit piece, material, and story part: a localized
    /// name plus one short description line (at most ~52 characters, zero slot codes, zero key hints).
    /// </summary>
    internal static class PartInfo
    {
        public static RoverKitPiece DefaultPartFor(RoverSocket socket)
        {
            switch (socket)
            {
                case RoverSocket.Cutter: return RoverKitPiece.FocusLens;
                case RoverSocket.Tether: return RoverKitPiece.TetherRing;
                case RoverSocket.Lamp: return RoverKitPiece.LampBar;
                case RoverSocket.Belly: return RoverKitPiece.HoverCoils;
                case RoverSocket.Flanks: return RoverKitPiece.CapacitorDrums;
                case RoverSocket.Panniers: return RoverKitPiece.SideBins;
                case RoverSocket.Back: return RoverKitPiece.BatterySpine;
                case RoverSocket.Hold: return RoverKitPiece.CargoRack;
                case RoverSocket.Wing: return RoverKitPiece.SolarCell;
                case RoverSocket.Deck: return RoverKitPiece.TapeDeck;
                default: return RoverKitPiece.SolarCell;
            }
        }

        public static string SocketName(RoverSocket socket, ILocalization localization)
        {
            if (socket == RoverSocket.Wheels)
            {
                return localization.Get("ui.can.magnetic_treads.verb");
            }

            if (socket == RoverSocket.Antenna)
            {
                return localization.Get("ui.can.wide_sonar.verb");
            }

            return PartName(DefaultPartFor(socket), localization);
        }

        public static string SocketDescription(RoverSocket socket, ILocalization localization)
        {
            return localization.Get(UiKeys.KitDescription(socket));
        }

        public static string PartName(RoverKitPiece part, ILocalization localization)
        {
            return PartNames.TryTitle(part, out string key) ? localization.Get(key) : string.Empty;
        }

        public static string PartDescription(RoverKitPiece part, ILocalization localization)
        {
            switch (part)
            {
                case RoverKitPiece.FocusLens: return SocketDescription(RoverSocket.Cutter, localization);
                case RoverKitPiece.TetherRing: return SocketDescription(RoverSocket.Tether, localization);
                case RoverKitPiece.LampBar: return SocketDescription(RoverSocket.Lamp, localization);
                case RoverKitPiece.HoverCoils: return SocketDescription(RoverSocket.Belly, localization);
                case RoverKitPiece.CapacitorDrums: return SocketDescription(RoverSocket.Flanks, localization);
                case RoverKitPiece.SideBins: return SocketDescription(RoverSocket.Panniers, localization);
                case RoverKitPiece.BatterySpine: return SocketDescription(RoverSocket.Back, localization);
                case RoverKitPiece.CargoRack: return SocketDescription(RoverSocket.Hold, localization);
                case RoverKitPiece.SolarCell: return SocketDescription(RoverSocket.Wing, localization);
                case RoverKitPiece.TapeDeck: return SocketDescription(RoverSocket.Deck, localization);
                default: return string.Empty;
            }
        }

        public static string MaterialName(SalvageMaterial material, ILocalization localization)
        {
            return localization.Get(UiKeys.MaterialName(material));
        }

        public static string MaterialDescription(SalvageMaterial material, ILocalization localization)
        {
            return localization.Get(UiKeys.MaterialName(material) + ".job");
        }

        public static string StoryPartName(string partId, ILocalization localization)
        {
            return localization.Get(UiKeys.PickupName(partId));
        }

        public static string StoryPartDescription(string partId, ILocalization localization)
        {
            if (string.IsNullOrEmpty(partId))
            {
                return string.Empty;
            }

            if (partId.IndexOf("Tilly", StringComparison.OrdinalIgnoreCase) >= 0 ||
                string.Equals(partId, "cell", StringComparison.OrdinalIgnoreCase))
            {
                return localization.Get(UiKeys.TillyCellJob);
            }

            if (partId.IndexOf("Bell", StringComparison.OrdinalIgnoreCase) >= 0 ||
                string.Equals(partId, "valve", StringComparison.OrdinalIgnoreCase))
            {
                return localization.Get(UiKeys.BellValveJob);
            }

            return localization.Get(UiKeys.ComponentNeeded(partId));
        }
    }
}
