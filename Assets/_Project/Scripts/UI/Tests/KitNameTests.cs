using System;
using NUnit.Framework;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Each piece of kit is named as it settles onto 07 (docs/features/M3-11): crafted kit after the upgrade that
    /// brought it, in the order bought; a friend's gift by its own title.
    /// </summary>
    public sealed class KitNameTests
    {
        private TestUpgrades _upgrades;

        [SetUp]
        public void SetUp()
        {
            _upgrades = new TestUpgrades();
        }

        [TearDown]
        public void TearDown()
        {
            _upgrades.Dispose();
        }

        [Test]
        public void CraftedKit_IsNamedAfterItsUpgrade_InTheOrderBought()
        {
            var names = new KitNames();
            names.Purchased(_upgrades.Kit("rover.warm_headlamp", new Recipe(1, 2, 2)), 1);
            names.Purchased(_upgrades.Kit("rover.boost_coils", new Recipe(4, 3, 1)), 1);
            Assert.AreEqual(2, names.Pending);

            Assert.IsTrue(names.TryTitle(new RoverKitFitted(RoverKitPiece.LampBar, false), out string first));
            Assert.AreEqual(UiKeys.UpgradeName("rover.warm_headlamp"), first);
            Assert.IsTrue(names.TryTitle(new RoverKitFitted(RoverKitPiece.CapacitorDrums, false), out string second));
            Assert.AreEqual(UiKeys.UpgradeName("rover.boost_coils"), second);
            Assert.IsFalse(names.TryTitle(new RoverKitFitted(RoverKitPiece.CargoRack, false), out _),
                "a crafted piece nobody bought is a wiring fault, not a guess");
        }

        [Test]
        public void ALevelGrantingNoAbility_BringsNoKit()
        {
            var names = new KitNames();
            UpgradeDefinition tower = _upgrades.Tower("radio_tower", new Recipe(2, 1, 0), new Recipe(4, 2, 1));
            names.Purchased(tower, 1);
            names.Purchased(tower, 2);
            Assert.AreEqual(0, names.Pending, "the radio tower's signal is not kit on 07");
            Assert.Throws<ArgumentOutOfRangeException>(() => names.Purchased(tower, 3));
        }

        [Test]
        public void Gifts_HaveTheirOwnTitles()
        {
            var names = new KitNames();
            Assert.IsTrue(names.TryTitle(new RoverKitFitted(RoverKitPiece.SolarCell, true), out string cell));
            Assert.AreEqual("kit.solar_cell.name", cell);
            Assert.IsTrue(names.TryTitle(new RoverKitFitted(RoverKitPiece.FreshPaint, true), out string paint));
            Assert.AreEqual("kit.fresh_paint.name", paint);
            Assert.IsFalse(names.TryTitle(new RoverKitFitted(RoverKitPiece.HoverCoils, true), out _),
                "no friend gives the hover coils");
        }
    }
}
