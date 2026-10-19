using System;
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
        public const string BlueprintLearned = "ui.unlock.blueprint";
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
        public const string VerbScan = "ui.verb.scan";
        public const string VerbCut = "ui.verb.cut";
        public const string VerbBuild = "ui.verb.build";
        public const string VerbFit = "ui.verb.fit";
        public const string FaceNoLine = "ui.face.no_line";
        public const string HoldBoot = "ui.hold.boot";
        public const string ScanFragment = "scan.fragment";
        public const string ScanKnown = "scan.known";
        public const string RecipeRow = "ui.recipe.row";
        public const string CargoHold = "ui.cargo.hold";
        public const string FaceTitle = "ui.face.title";
        public const string CargoMoreSlots = "ui.cargo.more_slots";
        public const string CargoPage = "ui.cargo.page";
        public const string CargoEmptyName = "ui.cargo.empty_name";
        public const string CargoEmptyDesc = "ui.cargo.empty_desc";
        public const string FaceClose = "ui.face.close";
        public const string FaceFit = "ui.face.fit";
        public const string FaceBuild = "ui.face.build";
        public const string CargoDragNotDocked = "ui.cargo.drag.not_docked";
        public const string CargoDragToCutter = "ui.cargo.drag.to_cutter";
        public const string CargoDragNowhere = "ui.cargo.drag.nowhere";
        public const string CargoIgniterFitHint = "ui.cargo.igniter.fit_hint";
        public const string KeyDragOrE = "glyph.combo.drag_e";
        public const string KeyMouseOrE = "glyph.combo.mouse_e";
        public const string KeyBOrEscape = "glyph.combo.b_esc";
        public const string KeyPadYOrB = "glyph.combo.pad_y_b";
        public const string KeyB = "glyph.keyboard.b";
        public const string KeyQ = "glyph.keyboard.q";
        public const string KeyF = "glyph.keyboard.f";
        public const string KeyShoulders = "glyph.gamepad.shoulders";
        public const string KeyDrive = "glyph.keyboard.wasd";
        public const string PadDrive = "glyph.gamepad.leftstick";
        public const string MouseLook = "glyph.mouse.move";
        public const string PadLook = "glyph.gamepad.rightstick";
        public const string KeyTab = "glyph.keyboard.tab";
        public const string PadView = "glyph.gamepad.select";
        public const string ControlsLook = "ui.controls.look";
        public const string ControlsFace = "ui.controls.face";
        public const string CargoWaiting = "ui.cargo.waiting";
        public const string CargoSummary = "ui.cargo.summary";
        public const string BuildStripTitle = "build.strip.title";
        public const string BuildCostAffordable = "build.cost.affordable";
        public const string BuildCostShort = "build.cost.short";
        public const string BuildReasonSteep = "build.reason.steep";
        public const string BuildReasonOverlaps = "build.reason.overlaps";
        public const string BuildReasonClose = "build.reason.close";
        public const string BuildReasonFar = "build.reason.far";
        public const string BuildReasonOutside = "build.reason.outside";
        public const string BuildReasonBlocked = "build.reason.blocked";
        public const string BuildHintPick = "build.hint.pick";
        public const string BuildHintLeave = "build.hint.leave";
        public const string BuildStepPlace = "build.step.place";
        public const string BuildStepWeld = "build.step.weld";
        public const string BuildHintWeld = "build.hint.weld";
        public const string BuildHintDeconstruct = "build.hint.deconstruct";
        public const string BuildLocked = "build.locked";
        public const string BuildEnergyCard = "build.energy.card";

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

        /// <summary>"pickup.part.igniter": the name of story part <paramref name="partId"/>.</summary>
        public static string PickupName(string partId)
        {
            return "pickup." + partId.ToLowerInvariant();
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
