using NUnit.Framework;
using MoonProject.Core.Events;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Each piece of kit is named as it settles onto 07 (docs/features/M3-11, M3-14): crafted kit after the upgrade the
    /// event says brought it, whatever order things were bought in; a friend's gift by its own title.
    /// </summary>
    public sealed class KitNameTests
    {
        [Test]
        public void CraftedKit_IsNamedAfterTheUpgradeThatBroughtIt()
        {
            var drums = new RoverKitFitted(RoverKitPiece.CapacitorDrums, false, "rover.boost_coils");
            Assert.IsTrue(KitNames.TryTitle(drums, out string coils));
            Assert.AreEqual(UiKeys.UpgradeName("rover.boost_coils"), coils);
            var lampBar = new RoverKitFitted(RoverKitPiece.LampBar, false, "rover.warm_headlamp");
            Assert.IsTrue(KitNames.TryTitle(lampBar, out string headlamp));
            Assert.AreEqual(UiKeys.UpgradeName("rover.warm_headlamp"), headlamp, "by identity, not by purchase order");
        }

        [Test]
        public void ACraftedPieceWithoutItsUpgrade_IsAFault_NotAGuess()
        {
            var rack = new RoverKitFitted(RoverKitPiece.CargoRack, false, string.Empty);
            Assert.IsFalse(KitNames.TryTitle(rack, out string key));
            Assert.IsNull(key);
        }

        [Test]
        public void Gifts_HaveTheirOwnTitles()
        {
            var solarCell = new RoverKitFitted(RoverKitPiece.SolarCell, true, string.Empty);
            Assert.IsTrue(KitNames.TryTitle(solarCell, out string cell));
            Assert.AreEqual("kit.solar_cell.name", cell);
            var freshPaint = new RoverKitFitted(RoverKitPiece.FreshPaint, true, string.Empty);
            Assert.IsTrue(KitNames.TryTitle(freshPaint, out string paint));
            Assert.AreEqual("kit.fresh_paint.name", paint);
            Assert.IsFalse(KitNames.TryTitle(new RoverKitFitted(RoverKitPiece.HoverCoils, true, string.Empty), out _),
                "no friend gives the hover coils");
        }
    }
}
