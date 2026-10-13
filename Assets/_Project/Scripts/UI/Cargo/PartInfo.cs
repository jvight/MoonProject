using System;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// What the 07 tab says of a part (docs/design/0.5.5-owner-feedback.md §3.5), pure logic, EditMode-tested: its name
    /// and the key it is used with, nothing else (the owner: no sentence about what 07 can do), and which part a socket
    /// is named after before one is in hand.
    /// </summary>
    internal static class PartInfo
    {
        /// <summary>The localization key of the part's name: the ability's word, else the part's own title.</summary>
        public static string NameKey(RoverKitPiece part)
        {
            if (AbilityTable.TryAbilityOf(part, out RoverAbility ability) &&
                AbilityTable.TryFind(ability, out AbilityRow row))
            {
                return row.VerbKey;
            }

            if (PartNames.TryTitle(part, out string title))
            {
                return title;
            }

            throw new ArgumentOutOfRangeException(nameof(part), part, "No name for this part.");
        }

        /// <summary>The key the part is used with; false for a part that has none (the rest just work).</summary>
        public static bool TryAction(RoverKitPiece part, out RoverAction action)
        {
            if (AbilityTable.TryAbilityOf(part, out RoverAbility ability) &&
                AbilityTable.TryFind(ability, out AbilityRow row) && row.HasAction)
            {
                action = row.Action;
                return true;
            }

            action = default;
            return false;
        }

        /// <summary>The part the socket is named after once it is known; false for a socket with no part in 0.5.</summary>
        public static bool TryDefaultPart(RoverSocket socket, out RoverKitPiece part)
        {
            switch (socket)
            {
                case RoverSocket.Cutter:
                    part = RoverKitPiece.FocusLens;
                    return true;
                case RoverSocket.Tether:
                    part = RoverKitPiece.TetherRing;
                    return true;
                case RoverSocket.Lamp:
                    part = RoverKitPiece.LampBar;
                    return true;
                case RoverSocket.Belly:
                    part = RoverKitPiece.HoverCoils;
                    return true;
                case RoverSocket.Flanks:
                    part = RoverKitPiece.CapacitorDrums;
                    return true;
                case RoverSocket.Panniers:
                    part = RoverKitPiece.SideBins;
                    return true;
                case RoverSocket.Back:
                    part = RoverKitPiece.BatterySpine;
                    return true;
                case RoverSocket.Hold:
                    part = RoverKitPiece.CargoRack;
                    return true;
                case RoverSocket.Wing:
                    part = RoverKitPiece.SolarCell;
                    return true;
                case RoverSocket.Deck:
                    part = RoverKitPiece.TapeDeck;
                    return true;
                default:
                    part = default;
                    return false;
            }
        }
    }
}
