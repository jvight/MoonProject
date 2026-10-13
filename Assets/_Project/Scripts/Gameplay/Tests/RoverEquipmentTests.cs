using System.Collections.Generic;
using NUnit.Framework;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// 07's equipment (docs/design/0.5.5-owner-feedback.md §3.5): crafted parts wait in the row, fit by hand only at
    /// their station, swap with the old part recycled into Metal, come off back into the row with their ability paused,
    /// and load from a save (or from a 0.5.4 save's owned kit) fitted, publishing nothing.
    /// </summary>
    public sealed class RoverEquipmentTests
    {
        private const int Row = 4;

        private readonly List<PartCrafted> _crafted = new List<PartCrafted>();
        private readonly List<PartFitted> _fitted = new List<PartFitted>();
        private readonly List<PartRemoved> _removed = new List<PartRemoved>();
        private EventBus _events;
        private Abilities _abilities;
        private bool _atBay;
        private bool _atPort;
        private int _delivered;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _abilities = new Abilities();
            _crafted.Clear();
            _fitted.Clear();
            _removed.Clear();
            _atBay = false;
            _atPort = false;
            _delivered = 0;
            _events.Subscribe<PartCrafted>(_crafted.Add);
            _events.Subscribe<PartFitted>(_fitted.Add);
            _events.Subscribe<PartRemoved>(_removed.Add);
        }

        [Test]
        public void ACraftedPart_WaitsInTheRow_UntilItIsFittedAtTheBay()
        {
            RoverEquipment equipment = Basin();
            Assert.IsFalse(equipment.IsKnown(RoverSocket.Belly), "an unknown socket has no name");
            Assert.IsTrue(equipment.Craft(RoverKitPiece.HoverCoils));
            Assert.AreEqual(1, equipment.RowCount);
            Assert.AreEqual(RoverKitPiece.HoverCoils, equipment.RowPart(0));
            Assert.AreEqual(1, _crafted.Count);
            Assert.IsTrue(equipment.IsKnown(RoverSocket.Belly), "crafted: the socket is named");
            Assert.IsFalse(_abilities.Has(RoverAbility.HoverJump), "no ability until it is fitted");

            Assert.IsFalse(equipment.CanFit(RoverKitPiece.HoverCoils), "away from the bay");
            Assert.IsFalse(equipment.TryFit(0), "dragging does nothing away from the bay");
            Assert.AreEqual(0, _fitted.Count);

            _atBay = true;
            Assert.IsTrue(equipment.TryFit(0));
            Assert.AreEqual(0, equipment.RowCount);
            Assert.IsTrue(equipment.TryGetFitted(RoverSocket.Belly, out RoverKitPiece part));
            Assert.AreEqual(RoverKitPiece.HoverCoils, part);
            Assert.IsTrue(_abilities.Has(RoverAbility.HoverJump), "fitted: the ability follows the part");
            Assert.AreEqual(1, _fitted.Count);
            Assert.AreEqual(RoverSocket.Belly, _fitted[0].Socket);
            Assert.IsFalse(_fitted[0].Gift);
            Assert.IsFalse(_fitted[0].Replaced);
        }

        [Test]
        public void TheRowHoldsFour_ThenTheBayWaitsForAFit()
        {
            RoverEquipment equipment = Basin();
            Assert.IsTrue(equipment.Craft(RoverKitPiece.HoverCoils));
            Assert.IsTrue(equipment.Craft(RoverKitPiece.LampBar));
            Assert.IsTrue(equipment.Craft(RoverKitPiece.CapacitorDrums));
            Assert.IsTrue(equipment.Craft(RoverKitPiece.CargoRack));
            Assert.IsTrue(equipment.RowFull);
            Assert.IsFalse(equipment.Craft(RoverKitPiece.BatterySpine), "fit a part first");
            Assert.AreEqual(Row, _crafted.Count);

            _atBay = true;
            Assert.IsTrue(equipment.TryFit(1), "the lamp bar, from the middle of the row");
            Assert.AreEqual(RoverKitPiece.CapacitorDrums, equipment.RowPart(1), "the row closes up");
            Assert.IsTrue(equipment.Craft(RoverKitPiece.BatterySpine));
        }

        [Test]
        public void KenjisPortAndTillysGift_FitThemselves_TheGiftSoftly()
        {
            RoverEquipment equipment = Basin();
            equipment.FitItself(RoverKitPiece.TetherRing);
            equipment.FitItself(RoverKitPiece.SolarCell);
            Assert.IsTrue(_abilities.Has(RoverAbility.Tether));
            Assert.AreEqual(2, _fitted.Count);
            Assert.IsFalse(_fitted[0].Gift, "the port's arm fits the ring");
            Assert.IsTrue(_fitted[1].Gift, "Tilly's cell settles as a gift");
            Assert.IsTrue(equipment.IsFitted(RoverKitPiece.SolarCell));
            Assert.IsFalse(equipment.CanFit(RoverKitPiece.SolarCell), "a gift never sits in the row");
            Assert.Throws<System.ArgumentException>(() => equipment.Craft(RoverKitPiece.TetherRing));
            Assert.Throws<System.ArgumentException>(() => equipment.FitItself(RoverKitPiece.HoverCoils));
        }

        [Test]
        public void Unequip_AtItsStationOnly_PausesTheAbility_AndRefittingRestoresIt()
        {
            RoverEquipment equipment = Basin();
            equipment.Craft(RoverKitPiece.CargoRack);
            _atBay = true;
            equipment.TryFit(0);
            _atBay = false;
            Assert.IsFalse(equipment.TryUnequip(RoverSocket.Hold), "away from the bay nothing comes off");

            _atBay = true;
            Assert.IsTrue(equipment.TryUnequip(RoverSocket.Hold));
            Assert.IsFalse(_abilities.Has(RoverAbility.CargoCradle), "the ability pauses");
            Assert.AreEqual(RoverKitPiece.CargoRack, equipment.RowPart(0), "back in the row, nothing lost");
            Assert.AreEqual(1, _removed.Count);
            Assert.IsFalse(equipment.TryUnequip(RoverSocket.Hold), "the socket is empty now");

            equipment.FitItself(RoverKitPiece.TetherRing);
            Assert.IsFalse(equipment.TryUnequip(RoverSocket.Tether), "the ring comes off at the port, not the bay");
            _atPort = true;
            Assert.IsTrue(equipment.TryUnequip(RoverSocket.Tether));

            Assert.IsTrue(equipment.TryFit(0));
            Assert.IsTrue(_abilities.Has(RoverAbility.CargoCradle), "refitted: the ability is back");
        }

        [Test]
        public void ABetterTier_SwapsIntoTheSameSocket_AndTheOldPartIsRecycledIntoMetal()
        {
            var table = new EquipmentTable(new[]
            {
                new EquipmentPart(RoverKitPiece.HoverCoils, RoverSocket.Belly, PartStation.Bay, RoverAbility.HoverJump),
                new EquipmentPart(RoverKitPiece.CapacitorDrums, RoverSocket.Belly, PartStation.Bay,
                    RoverAbility.BoostCoils),
            });
            var equipment = new RoverEquipment(_events, _abilities, table, Row, AtStation, piece => 3,
                metal => _delivered += metal);
            _atBay = true;
            equipment.Craft(RoverKitPiece.HoverCoils);
            equipment.TryFit(0);
            equipment.Craft(RoverKitPiece.CapacitorDrums);
            Assert.IsTrue(equipment.TryFit(0));

            PartFitted swap = _fitted[1];
            Assert.IsTrue(swap.Replaced);
            Assert.AreEqual(RoverKitPiece.HoverCoils, swap.ReplacedPart);
            Assert.AreEqual(3, swap.RecycledMetal);
            Assert.AreEqual(3, _delivered, "the bay's Metal is never lost");
            Assert.AreEqual(0, equipment.RowCount, "no obsolete part piles up");
            Assert.IsFalse(_abilities.Has(RoverAbility.HoverJump));
            Assert.IsTrue(_abilities.Has(RoverAbility.BoostCoils));
        }

        [Test]
        public void ASave_LaysItsPartsBackIn_AndPublishesNothing()
        {
            RoverEquipment equipment = Basin();
            _atBay = true;
            equipment.Craft(RoverKitPiece.LampBar);
            equipment.TryFit(0);
            equipment.Craft(RoverKitPiece.BatterySpine);
            equipment.FitItself(RoverKitPiece.SolarCell);
            EquipmentSaveData saved = equipment.Capture();

            var abilities = new Abilities();
            var events = new EventBus();
            int published = 0;
            events.Subscribe<PartFitted>(_ => published++);
            var loaded = new RoverEquipment(events, abilities, EquipmentTable.Basin(), Row, AtStation, piece => 0,
                metal => { });
            loaded.Restore(saved);
            Assert.AreEqual(0, published, "a load publishes nothing");
            Assert.IsTrue(loaded.IsFitted(RoverKitPiece.LampBar));
            Assert.IsTrue(loaded.IsFitted(RoverKitPiece.SolarCell));
            Assert.IsTrue(abilities.Has(RoverAbility.WarmHeadlamp));
            Assert.AreEqual(1, loaded.RowCount);
            Assert.AreEqual(RoverKitPiece.BatterySpine, loaded.RowPart(0));
            Assert.IsFalse(abilities.Has(RoverAbility.CellPack), "a part in the row gives nothing");
        }

        [Test]
        public void A054Save_LoadsEveryOwnedPartFitted()
        {
            RoverEquipment equipment = Basin();
            equipment.RestoreOwned(RoverKitPiece.TetherRing);
            equipment.RestoreOwned(RoverKitPiece.CapacitorDrums);
            Assert.AreEqual(0, _fitted.Count, "silently");
            Assert.IsTrue(equipment.IsFitted(RoverKitPiece.TetherRing));
            Assert.IsTrue(_abilities.Has(RoverAbility.BoostCoils));
            Assert.AreEqual(0, equipment.RowCount, "the row starts empty");
        }

        [Test]
        public void TheSavesOwnSection_WinsOverItsOlderKit_InEitherLoadOrder()
        {
            var saved = new EquipmentSaveData { row = new[] { (int)RoverKitPiece.HoverCoils } };

            RoverEquipment kitFirst = Basin();
            kitFirst.RestoreOwned(RoverKitPiece.HoverCoils);
            kitFirst.Restore(saved);
            Assert.IsFalse(kitFirst.IsFitted(RoverKitPiece.HoverCoils), "the section says it waits in the row");
            Assert.IsFalse(_abilities.Has(RoverAbility.HoverJump));
            Assert.AreEqual(1, kitFirst.RowCount);

            RoverEquipment sectionFirst = Basin();
            sectionFirst.Restore(saved);
            sectionFirst.RestoreOwned(RoverKitPiece.HoverCoils);
            Assert.IsFalse(sectionFirst.IsFitted(RoverKitPiece.HoverCoils), "the older kit is ignored");
            Assert.AreEqual(1, sectionFirst.RowCount);
        }

        [Test]
        public void ABoughtPartALoadFoundNowhere_GoesBackIntoTheRow_Once()
        {
            RoverEquipment equipment = Basin();
            equipment.Restore(new EquipmentSaveData { fitted = new[] { (int)RoverKitPiece.LampBar } });
            equipment.ReturnUnplaced(RoverKitPiece.LampBar);
            equipment.ReturnUnplaced(RoverKitPiece.CargoRack);
            equipment.ReturnUnplaced(RoverKitPiece.CargoRack);
            Assert.AreEqual(1, equipment.RowCount, "the fitted lamp stays; the rack returns once");
            Assert.AreEqual(RoverKitPiece.CargoRack, equipment.RowPart(0));
            Assert.AreEqual(0, _crafted.Count, "silently");
        }

        [Test]
        public void TheBasinTable_PutsEachPartInItsSection35Socket()
        {
            EquipmentTable table = EquipmentTable.Basin();
            Assert.AreEqual(RoverSocket.Cutter, table.Find(RoverKitPiece.FocusLens).Socket);
            Assert.AreEqual(RoverSocket.Panniers, table.Find(RoverKitPiece.SideBins).Socket);
            Assert.AreEqual(RoverSocket.Deck, table.Find(RoverKitPiece.TapeDeck).Socket);
            Assert.AreEqual(PartStation.Port, table.Find(RoverKitPiece.TetherRing).Station);
            Assert.AreEqual(PartStation.Gift, table.Find(RoverKitPiece.SolarCell).Station);
            Assert.IsFalse(table.TryFind(RoverKitPiece.FreshPaint, out _), "Bell's paint is not a socket part");
            Assert.IsTrue(table.TryFindByAbility(RoverAbility.CellPack, out EquipmentPart spine));
            Assert.AreEqual(RoverKitPiece.BatterySpine, spine.Piece);
        }

        private RoverEquipment Basin()
        {
            return new RoverEquipment(_events, _abilities, EquipmentTable.Basin(), Row, AtStation, piece => 0,
                metal => _delivered += metal);
        }

        private bool AtStation(PartStation station)
        {
            return station == PartStation.Bay ? _atBay : _atPort;
        }

        private sealed class Abilities : IRoverAbilities
        {
            private readonly HashSet<RoverAbility> _has = new HashSet<RoverAbility>();

            public bool Has(RoverAbility ability)
            {
                return _has.Contains(ability);
            }

            public void Grant(RoverAbility ability)
            {
                _has.Add(ability);
            }

            public void Revoke(RoverAbility ability)
            {
                _has.Remove(ability);
            }
        }
    }
}
