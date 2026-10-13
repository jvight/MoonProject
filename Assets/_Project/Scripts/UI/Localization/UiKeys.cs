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
        public const string CardCaption = "ui.card.caption";
        public const string CardClose = "ui.card.close";
        public const string BayFitted = "ui.bay.fitted";
        public const string LogCaption = "ui.card.log_caption";
        public const string CrewLogCaption = "ui.card.crew_log";
        public const string LinerCaption = "ui.card.liner_caption";
        public const string TapeCount = "ui.card.tape_count";
        public const string PauseCassettes = "ui.pause.cassettes";
        public const string PauseRelays = "ui.pause.relays";
        public const string PauseResume = "ui.pause.resume";
        public const string PauseSettings = "ui.pause.settings";
        public const string PauseQuit = "ui.pause.quit";
        public const string TitleContinue = "ui.title.continue";
        public const string DisplayUnlimited = "ui.display.unlimited";
        public const string CreditsTitle = "ui.credits.title";
        public const string CreditsVersion = "ui.credits.version";
        public const string TitleSettings = "ui.title.settings";
        public const string TitleQuit = "ui.title.quit";
        public const string JourneyPutAway = "ui.journey.put_away";
        public const string LookUp = "ui.hint.look_up";
        public const string Drive = "ui.hint.drive";
        public const string HintHold = "hint.hold";
        public const string HoldBoot = "ui.hold.boot";
        public const string BlueprintFound = "ui.blueprint.found";
        public const string BlueprintDelivered = "ui.blueprint.delivered";

        /// <summary>Art's prefix on a component's model name (Part_TowerCoil), left out of its keys.</summary>
        private const string ComponentModelPrefix = "Part_";
        public const string RecipeRow = "ui.recipe.row";
        public const string RewardTitle = "ui.reward.title";
        public const string FriendParts = "ui.friend.parts";
        public const string CargoWaiting = "ui.cargo.waiting";
        public const string CargoSummary = "ui.cargo.summary";
        public const string CrateCaption = "ui.crate.caption";
        public const string CanDecoded = "ui.can.decoded";
        public const string StationOffline = "ui.station.offline";

        /// <summary>"hint.excavate": the one word of the prompt teaching <paramref name="kind"/>.</summary>
        public static string Hint(InteractionKind kind)
        {
            return "hint." + kind.ToString().ToLowerInvariant();
        }

        public static string RelicName(string relicId)
        {
            return "relic." + relicId + ".name";
        }

        public static string RelicMemory(string relicId)
        {
            return "relic." + relicId + ".memory";
        }

        public static string FriendName(string friendId)
        {
            return "friend." + friendId + ".name";
        }

        /// <summary>"friend.tilly.repair_log": the crew log a friend remembers when it wakes.</summary>
        public static string FriendRepairLog(string friendId)
        {
            return "friend." + friendId + ".repair_log";
        }

        /// <summary>"log.ro_1": a crew log's text.</summary>
        public static string CrewLog(string logId)
        {
            return "log." + logId;
        }

        public static string CassetteTitle(string cassetteId)
        {
            return "cassette." + cassetteId + ".title";
        }

        /// <summary>"cassette.after_dark_1.note": Ro's liner note for a tape.</summary>
        public static string CassetteNote(string cassetteId)
        {
            return "cassette." + cassetteId + ".note";
        }

        /// <summary>"radio.channel.tape_deck": the name of a station on Bell's dial.</summary>
        public static string RadioChannelName(RadioChannel channel)
        {
            switch (channel)
            {
                case RadioChannel.LumenAfterDark:
                    return "radio.channel.lumen_after_dark";
                case RadioChannel.TapeDeck:
                    return "radio.channel.tape_deck";
                case RadioChannel.QuietHours:
                    return "radio.channel.quiet_hours";
                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, "This station has no name key.");
            }
        }

        /// <summary>"ui.station.workshop": the name of the place an upgrade is sold.</summary>
        /// <summary>"ui.reward.light_on": the words of one line of the tower's reward card.</summary>
        public static string RewardLine(RewardLineKind kind)
        {
            switch (kind)
            {
                case RewardLineKind.LightOn:
                    return "ui.reward.light_on";
                case RewardLineKind.LightUp:
                    return "ui.reward.light_up";
                case RewardLineKind.RadioOn:
                    return "ui.reward.radio_on";
                case RewardLineKind.Reach:
                    return "ui.reward.reach";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "This reward line has no key.");
            }
        }

        public static string StationName(UpgradeStationKind station)
        {
            switch (station)
            {
                case UpgradeStationKind.RadioTower:
                    return "ui.station.radio_tower";
                case UpgradeStationKind.Workshop:
                    return "ui.station.workshop";
                default:
                    throw new ArgumentOutOfRangeException(nameof(station), station, "This station has no name key.");
            }
        }

        /// <summary>
        /// The quiet line a station says once its work on a purchase shows: the Rover Bay's "ui.bay.fitted" as it sets
        /// the piece on 07. The radio tower says nothing (the world shows the stitch, then the reward card).
        /// </summary>
        public static bool TryStationLine(UpgradeStationKind station, out string key)
        {
            key = station == UpgradeStationKind.Workshop ? BayFitted : null;
            return key != null;
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

        /// <summary>"site.kestrel.name" for the site anchored as "site.kestrel".</summary>
        public static string SiteName(string siteId)
        {
            return siteId + ".name";
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

        /// <summary>"upgrade.radio_tower.2.effect": what level <paramref name="level"/> does, in plain words.</summary>
        public static string UpgradeEffect(string upgradeId, int level)
        {
            return "upgrade." + upgradeId + "." + level + ".effect";
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

        /// <summary>
        /// The ticker's quiet line as a recovery target's row completes (M3-21 §2); Power has none, its reward card
        /// says it.
        /// </summary>
        public static string RecoveryComplete(RecoveryTarget target)
        {
            switch (target)
            {
                case RecoveryTarget.Power:
                    throw new ArgumentOutOfRangeException(nameof(target), target, "Power has no ticker line.");
                case RecoveryTarget.Link: return "ticker.recovery.link";
                case RecoveryTarget.Dish: return "ticker.recovery.dish";
                default: return "ticker.recovery.answer";
            }
        }
    }
}
