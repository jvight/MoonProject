using System;
using System.Collections.Generic;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// Which name goes with each piece of kit as it settles onto 07 (docs/features/M3-11), pure logic, EditMode-tested.
    /// A crafted piece is named after the upgrade that brought it ("upgrade.&lt;id&gt;.name"): every bought level that
    /// grants an ability fits one piece, in the order they were bought, so the names wait in line for their pieces. A
    /// friend's gift has its own title ("kit.&lt;gift&gt;.name").
    /// </summary>
    internal sealed class KitNames
    {
        private readonly Queue<string> _crafted = new Queue<string>();

        /// <summary>Pieces bought and not yet fitted.</summary>
        public int Pending => _crafted.Count;

        /// <summary>
        /// Notes a purchase: when level <paramref name="level"/> (1-based) of <paramref name="upgrade"/> grants an
        /// ability, a piece of kit is on its way to 07.
        /// </summary>
        public void Purchased(UpgradeDefinition upgrade, int level)
        {
            if (upgrade == null)
            {
                throw new ArgumentNullException(nameof(upgrade));
            }

            if (level < 1 || level > upgrade.MaxLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level,
                    $"'{upgrade.Id}' has levels 1 to {upgrade.MaxLevel}.");
            }

            if (upgrade.Levels[level - 1].GrantsAbility)
            {
                _crafted.Enqueue(UiKeys.UpgradeName(upgrade.Id));
            }
        }

        /// <summary>
        /// The title key for the piece that just settled. False when nothing explains it: a crafted piece with no
        /// purchase noted, or a gift no friend gives (a wiring fault for the caller to report).
        /// </summary>
        public bool TryTitle(RoverKitFitted fitted, out string key)
        {
            if (fitted.Gift)
            {
                return UiKeys.TryGetGiftName(fitted.Piece, out key);
            }

            if (_crafted.Count > 0)
            {
                key = _crafted.Dequeue();
                return true;
            }

            key = null;
            return false;
        }
    }
}
