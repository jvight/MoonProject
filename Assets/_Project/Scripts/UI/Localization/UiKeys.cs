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
