using System;
using MoonProject.Core;
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
        public const string TowerLevel = "ui.tower.level";
        public const string TowerHold = "ui.tower.hold";
        public const string TowerNeed = "ui.tower.need";
        public const string TowerPurchased = "ui.tower.purchased";
        public const string LogCaption = "ui.card.log_caption";
        public const string CrewLogCaption = "ui.card.crew_log";
        public const string LinerCaption = "ui.card.liner_caption";
        public const string TapeCount = "ui.card.tape_count";
        public const string PauseCassettes = "ui.pause.cassettes";

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
    }
}
