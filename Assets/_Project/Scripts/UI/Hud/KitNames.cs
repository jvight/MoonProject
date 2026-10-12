using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// Which name goes with each piece of kit as it settles onto 07 (docs/features/M3-11, M3-14), pure logic,
    /// EditMode-tested. A crafted piece is named after the upgrade that brought it, which the event carries
    /// ("upgrade.&lt;id&gt;.name"); a friend's gift and a piece Kenji's garage port fits have their own title
    /// ("kit.&lt;piece&gt;.name").
    /// </summary>
    internal static class KitNames
    {
        /// <summary>
        /// The title key for the piece that just settled. False when nothing names it: a crafted piece without its
        /// upgrade, or a gift no friend gives (a wiring fault for the caller to report).
        /// </summary>
        public static bool TryTitle(RoverKitFitted fitted, out string key)
        {
            if (string.IsNullOrEmpty(fitted.UpgradeId))
            {
                return UiKeys.TryGetGiftName(fitted.Piece, out key);
            }

            key = UiKeys.UpgradeName(fitted.UpgradeId);
            return true;
        }
    }
}
