using System;
using System.Text;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// Localization keys the UI reads from code (fixed UXML words carry their keys in GameUI.uxml). Builders of
    /// per-id keys allocate: call them when the content changes, never per frame.
    /// </summary>
    internal static class UiKeys
    {
        public const string PauseResume = "ui.pause.resume";
        public const string PauseSettings = "ui.pause.settings";
        public const string PauseQuit = "ui.pause.quit";
        public const string TitleContinue = "ui.title.continue";
        public const string TitleContinueNote = "ui.title.continue_note";
        public const string TitleKeyHint = "ui.title.key_hint";
        public const string DisplayUnlimited = "ui.display.unlimited";
        public const string CreditsTitle = "ui.credits.title";
        public const string CreditsVersion = "ui.credits.version";
        public const string TitleSettings = "ui.title.settings";
        public const string TitleQuit = "ui.title.quit";
        public const string LookUp = "ui.hint.look_up";
        public const string Drive = "ui.hint.drive";
        public const string HintHold = "hint.hold";
        public const string RowFull = "ui.bay.row_full";
        public const string HoldBoot = "ui.hold.boot";
        public const string ScanFragment = "scan.fragment";
        public const string ScanKnown = "scan.known";
        public const string HintShelves = "hint.shelves";
        public const string TransferToTrip = "ui.transfer.to_trip";
        public const string TransferToHome = "ui.transfer.to_home";

        /// <summary>Art's prefix on a component's model name (Part_TowerCoil), left out of its keys.</summary>
        private const string ComponentModelPrefix = "Part_";
        public const string RecipeRow = "ui.recipe.row";
        public const string CargoHold = "ui.cargo.hold";
        public const string CargoTabSeven = "ui.cargo.tab_seven";
        public const string CargoWaiting = "ui.cargo.waiting";
        public const string CargoSummary = "ui.cargo.summary";
        public const string CargoFitAtBay = "ui.cargo.fit_at_bay";
        public const string TillyCellJob = "pickup.tilly.cell.job";
        public const string BellValveJob = "pickup.bell.valve.job";
        public const string BuildStripTitle = "build.strip.title";
        public const string BuildCostAffordable = "build.cost.affordable";
        public const string BuildCostShort = "build.cost.short";
        public const string BuildReasonSteep = "build.reason.steep";
        public const string BuildReasonOverlaps = "build.reason.overlaps";
        public const string BuildReasonClose = "build.reason.close";
        public const string BuildReasonFar = "build.reason.far";
        public const string BuildReasonOutside = "build.reason.outside";
        public const string BuildReasonBlocked = "build.reason.blocked";
        public const string BuildHintRotate = "build.hint.rotate";
        public const string BuildHintSnap = "build.hint.snap";
        public const string BuildHintPlace = "build.hint.place";
        public const string BuildHintWeld = "build.hint.weld";
        public const string BuildHintDeconstruct = "build.hint.deconstruct";
        public const string BuildLocked = "build.locked";

        /// <summary>"ui.kit.wing.desc": the short (&lt;= 52 char) tooltip line for <paramref name="socket"/>.</summary>
        public static string KitDescription(RoverSocket socket)
        {
            switch (socket)
            {
                case RoverSocket.Cutter: return "ui.kit.cutter.desc";
                case RoverSocket.Tether: return "ui.kit.tether.desc";
                case RoverSocket.Lamp: return "ui.kit.lamp.desc";
                case RoverSocket.Belly: return "ui.kit.belly.desc";
                case RoverSocket.Flanks: return "ui.kit.flanks.desc";
                case RoverSocket.Panniers: return "ui.kit.panniers.desc";
                case RoverSocket.Back: return "ui.kit.back.desc";
                case RoverSocket.Hold: return "ui.kit.hold.desc";
                case RoverSocket.Wing: return "ui.kit.wing.desc";
                case RoverSocket.Deck: return "ui.kit.deck.desc";
                case RoverSocket.Wheels: return "ui.kit.wheels.desc";
                case RoverSocket.Antenna: return "ui.kit.antenna.desc";
                default:
                    throw new ArgumentOutOfRangeException(nameof(socket), socket, "This socket has no description key.");
            }
        }

        /// <summary>"hint.excavate": the one word of the prompt teaching <paramref name="kind"/>.</summary>
        public static string Hint(InteractionKind kind)
        {
            return "hint." + kind.ToString().ToLowerInvariant();
        }

        /// <summary>"material.wiring": a material's name (salvage, lunar or refined; docs/features/M3-27).</summary>
        public static string MaterialName(SalvageMaterial material)
        {
            switch (material)
            {
                case SalvageMaterial.Metal:
                    return "material.metal";
                case SalvageMaterial.Wiring:
                    return "material.wiring";
                case SalvageMaterial.Optics:
                    return "material.optics";
                case SalvageMaterial.CircuitChips:
                    return "material.circuit_chips";
                case SalvageMaterial.OrangeBeads:
                    return "material.orange_beads";
                case SalvageMaterial.Ilmenite:
                    return "material.ilmenite";
                case SalvageMaterial.WaterIce:
                    return "material.water_ice";
                case SalvageMaterial.RareEarths:
                    return "material.rare_earths";
                case SalvageMaterial.Glass:
                    return "material.glass";
                case SalvageMaterial.Alloy:
                    return "material.alloy";
                case SalvageMaterial.Coolant:
                    return "material.coolant";
                case SalvageMaterial.Crystal:
                    return "material.crystal";
                case SalvageMaterial.Superconductor:
                    return "material.superconductor";
                default:
                    throw new ArgumentOutOfRangeException(nameof(material), material, "This material has no name key.");
            }
        }

        /// <summary>
        /// "kit.solar_cell.name": the title of a friend's gift fitted on 07. False for a piece no friend gives (crafted
        /// kit is titled by its upgrade's <see cref="UpgradeName"/>).
        /// </summary>
        public static bool TryGetGiftName(RoverKitPiece piece, out string key)
        {
            switch (piece)
            {
                case RoverKitPiece.SolarCell:
                    key = "kit.solar_cell.name";
                    return true;
                case RoverKitPiece.FreshPaint:
                    key = "kit.fresh_paint.name";
                    return true;
                case RoverKitPiece.TapeDeck:
                    key = "kit.tape_deck.name";
                    return true;
                case RoverKitPiece.TetherRing:
                    key = "kit.tether_ring.name";
                    return true;
                default:
                    key = null;
                    return false;
            }
        }

        /// <summary>"ui.display.mode.borderless": how the window sits on the screen, in words.</summary>
        public static string DisplayModeName(DisplayMode mode)
        {
            switch (mode)
            {
                case DisplayMode.Exclusive:
                    return "ui.display.mode.exclusive";
                case DisplayMode.Windowed:
                    return "ui.display.mode.windowed";
                default:
                    return "ui.display.mode.borderless";
            }
        }

        public static string UpgradeName(string upgradeId)
        {
            return "upgrade." + upgradeId + ".name";
        }

        /// <summary>
        /// "upgrade.radio_tower.2.title": the name of level <paramref name="level"/> (1 = the first bought).
        /// </summary>
        public static string UpgradeTitle(string upgradeId, int level)
        {
            return "upgrade." + upgradeId + "." + level + ".title";
        }

        /// <summary>"pickup.part_towercoil": the name of pickup or story part <paramref name="partId"/>.</summary>
        public static string PickupName(string partId)
        {
            return "pickup." + partId.ToLowerInvariant();
        }

        /// <summary>
        /// "component.tower_coil.needed": the quiet line when the tower's next level still waits for component
        /// <paramref name="componentId"/>, Art's model name (Part_TowerCoil, Part_Transformer, Part_RelaySpare) in
        /// snake case without its prefix (docs/features/M3-21 §6). Built once per refused purchase, never per frame.
        /// </summary>
        public static string ComponentNeeded(string componentId)
        {
            string name = componentId.StartsWith(ComponentModelPrefix, StringComparison.Ordinal)
                ? componentId.Substring(ComponentModelPrefix.Length)
                : componentId;
            var key = new StringBuilder("component.", name.Length * 2 + 18);
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && i > 0)
                {
                    key.Append('_');
                }

                key.Append(char.ToLowerInvariant(name[i]));
            }

            return key.Append(".needed").ToString();
        }

        public static string StructureName(string structureId)
        {
            return StructureIds.NameKey(structureId);
        }

        public static string StructureDescription(string structureId)
        {
            return StructureIds.DescriptionKey(structureId);
        }

        public static string PlacementReasonKey(PlacementReason reason)
        {
            switch (reason)
            {
                case PlacementReason.TooSteep:
                    return BuildReasonSteep;
                case PlacementReason.Overlaps:
                    return BuildReasonOverlaps;
                case PlacementReason.TooCloseTo07:
                    return BuildReasonClose;
                case PlacementReason.TooFarFrom07:
                    return BuildReasonFar;
                case PlacementReason.OutsidePlayArea:
                    return BuildReasonOutside;
                case PlacementReason.Blocked:
                    return BuildReasonBlocked;
                default:
                    return null;
            }
        }
    }
}
