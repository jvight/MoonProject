using System;
using System.Text;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>One thing 07 can do: its key (if it has one) and the words of its unlock card.</summary>
    internal readonly struct AbilityRow
    {
        public AbilityRow(RoverAbility ability, bool hasAction, RoverAction action, string verbKey)
        {
            Ability = ability;
            HasAction = hasAction;
            Action = action;
            VerbKey = verbKey;
        }

        public RoverAbility Ability { get; }

        /// <summary>True when the ability has a key of its own (the rest just work).</summary>
        public bool HasAction { get; }

        public RoverAction Action { get; }

        /// <summary>"ui.can.tether.verb": the part's name, on the unlock card.</summary>
        public string VerbKey { get; }
    }

    /// <summary>
    /// Which ability a fitted piece of kit gave, and the words and key of that ability's unlock card. Pure logic,
    /// EditMode-tested; the words live in the localization tables.
    /// </summary>
    internal static class AbilityTable
    {
        private static readonly AbilityRow[] Table =
        {
            Row(RoverAbility.Tether, RoverAction.Tether),
            Row(RoverAbility.HoverJump, RoverAction.Jump),
            Row(RoverAbility.CargoCradle, RoverAction.Tether),
            Row(RoverAbility.WarmHeadlamp),
            Row(RoverAbility.BoostCoils),
            Row(RoverAbility.CellPack),
            Row(RoverAbility.WideSonar),
            Row(RoverAbility.MagneticTreads),
            Row(RoverAbility.SideBins),
            Row(RoverAbility.FocusLens),
        };

        /// <summary>The ability a fitted piece of kit gives, if it gives one.</summary>
        public static bool TryAbilityOf(RoverKitPiece piece, out RoverAbility ability)
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
                case RoverKitPiece.TetherRing:
                    ability = RoverAbility.Tether;
                    return true;
                case RoverKitPiece.FocusLens:
                    ability = RoverAbility.FocusLens;
                    return true;
                case RoverKitPiece.SideBins:
                    ability = RoverAbility.SideBins;
                    return true;
                default:
                    ability = default;
                    return false;
            }
        }

        /// <summary>The row of <paramref name="ability"/>; false for an ability without an unlock card.</summary>
        public static bool TryFind(RoverAbility ability, out AbilityRow row)
        {
            for (int i = 0; i < Table.Length; i++)
            {
                if (Table[i].Ability == ability)
                {
                    row = Table[i];
                    return true;
                }
            }

            row = default;
            return false;
        }

        private static AbilityRow Row(RoverAbility ability, RoverAction? action = null)
        {
            string snake = Snake(ability.ToString());
            return new AbilityRow(ability, action.HasValue, action ?? default, "ui.can." + snake + ".verb");
        }

        private static string Snake(string name)
        {
            var text = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && i > 0)
                {
                    text.Append('_');
                }

                text.Append(char.ToLowerInvariant(name[i]));
            }

            return text.ToString();
        }
    }
}
