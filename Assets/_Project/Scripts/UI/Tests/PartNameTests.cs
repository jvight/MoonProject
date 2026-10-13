using NUnit.Framework;
using MoonProject.Core.Events;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Each part of 07's equipment is named as it settles onto 07 (docs/features/M3-11, docs/design/0.5.5 §3.5):
    /// crafted parts after the upgrade that brings them, a friend's gift and a part Kenji's port fits by their own title.
    /// </summary>
    public sealed class PartNameTests
    {
        [Test]
        public void CraftedParts_AreNamedAfterTheUpgradeThatBringsThem()
        {
            Assert.IsTrue(PartNames.TryTitle(RoverKitPiece.CapacitorDrums, out string coils));
            Assert.AreEqual(UiKeys.UpgradeName("rover.boost_coils"), coils);
            Assert.IsTrue(PartNames.TryTitle(RoverKitPiece.FocusLens, out string lens));
            Assert.AreEqual(UiKeys.UpgradeName("rover.focus_lens"), lens);
            Assert.IsTrue(PartNames.TryTitle(RoverKitPiece.SideBins, out string bins));
            Assert.AreEqual(UiKeys.UpgradeName("rover.side_bins"), bins);
        }

        [Test]
        public void Gifts_AndPortParts_HaveTheirOwnTitles()
        {
            Assert.IsTrue(PartNames.TryTitle(RoverKitPiece.SolarCell, out string cell));
            Assert.AreEqual("kit.solar_cell.name", cell);
            Assert.IsTrue(PartNames.TryTitle(RoverKitPiece.FreshPaint, out string paint));
            Assert.AreEqual("kit.fresh_paint.name", paint);
            Assert.IsTrue(PartNames.TryTitle(RoverKitPiece.TetherRing, out string ring));
            Assert.AreEqual("kit.tether_ring.name", ring);
            Assert.IsTrue(PartNames.TryTitle(RoverKitPiece.TapeDeck, out string deck));
            Assert.AreEqual("kit.tape_deck.name", deck);
        }

        [Test]
        public void EveryCraftedPart_HasItsUpgrade_AndNoGiftHasOne()
        {
            foreach (RoverKitPiece crafted in new[]
                     {
                         RoverKitPiece.HoverCoils, RoverKitPiece.LampBar, RoverKitPiece.CapacitorDrums,
                         RoverKitPiece.CargoRack, RoverKitPiece.BatterySpine, RoverKitPiece.FocusLens,
                         RoverKitPiece.SideBins,
                     })
            {
                Assert.IsTrue(PartNames.TryUpgradeId(crafted, out _), crafted.ToString());
            }

            Assert.IsFalse(PartNames.TryUpgradeId(RoverKitPiece.SolarCell, out _));
        }
    }
}
