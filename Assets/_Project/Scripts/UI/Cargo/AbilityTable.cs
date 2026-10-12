using System;
using System.Text;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>One thing 07 can do: its key (if it has one), its words and the kit that brings it.</summary>
    internal readonly struct AbilityRow
    {
        public AbilityRow(RoverAbility ability, bool hasAction, RoverAction action, bool hasNode, TechNodeId node,
            string verbKey, string lineKey)
        {
            Ability = ability;
            HasAction = hasAction;
            Action = action;
            HasNode = hasNode;
            Node = node;
            VerbKey = verbKey;
            LineKey = lineKey;
        }

        public RoverAbility Ability { get; }

        /// <summary>True when the ability has a key of its own (the rest just work).</summary>
        public bool HasAction { get; }

        public RoverAction Action { get; }

        /// <summary>True when a kit's blueprint can be decoded for it (it then shows as a pencil sheet).</summary>
        public bool HasNode { get; }

        public TechNodeId Node { get; }

        /// <summary>"ui.can.tether.verb": the one word on the "07 can" page and on the unlock card.</summary>
        public string VerbKey { get; }

        /// <summary>"ui.can.tether.line": the one plain line of what it does.</summary>
        public string LineKey { get; }
    }

    /// <summary>
    /// What the "07 can" page lists (docs/design/first-hour-qa.md R1), in the order the player meets it, and which
    /// piece of kit an unlock card is for. Pure logic, EditMode-tested; the words live in the localization tables.
    /// </summary>
    internal static class AbilityTable
    {
        private static readonly AbilityRow[] Table =
        {
            Row(RoverAbility.Tether, RoverAction.Tether),
            Row(RoverAbility.HoverJump, RoverAction.Jump, TechNodeId.HoverJump),
            Row(RoverAbility.CargoCradle, RoverAction.Tether, TechNodeId.CargoCradle),
            Row(RoverAbility.WarmHeadlamp, null, TechNodeId.WarmHeadlamp),
            Row(RoverAbility.BoostCoils, null, TechNodeId.CapacitorDrums),
            Row(RoverAbility.CellPack, null, TechNodeId.CellPackII),
            Row(RoverAbility.WideSonar),
            Row(RoverAbility.MagneticTreads),
            Row(RoverAbility.SideBins, null, TechNodeId.SideBins),
            Row(RoverAbility.FocusLens, null, TechNodeId.FocusLens),
        };

        /// <summary>How many abilities the page can list.</summary>
        public static int Count => Table.Length;

        public static AbilityRow At(int index)
        {
            return Table[index];
        }

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
                default:
                    ability = default;
                    return false;
            }
        }

        /// <summary>The row of <paramref name="ability"/>; false for an ability the page does not list.</summary>
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

        private static AbilityRow Row(RoverAbility ability, RoverAction? action = null, TechNodeId? node = null)
        {
            string snake = Snake(ability.ToString());
            return new AbilityRow(ability, action.HasValue, action ?? default, node.HasValue, node ?? default,
                "ui.can." + snake + ".verb", "ui.can." + snake + ".line");
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
