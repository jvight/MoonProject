using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// 07's hold and home's shelves (docs/features/M3-25): packing into stacks, a partial fit, the unload, spending
    /// home first, building a shelf, and old saves (all home, overflow crates, new materials at 0). Nothing is lost.
    /// </summary>
    public sealed class CargoTests
    {
        private EventBus _events;
        private MaterialCatalog _catalog;
        private MaterialStock _stock;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _catalog = CargoFixtures.Catalog();
            _stock = CargoFixtures.Stock(_events, _catalog);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void Bundles_TopUpTheirStacksFirst_ThenOpenSlots()
        {
            var changes = new List<CargoChanged>();
            _events.Subscribe<CargoChanged>(changes.Add);
            _stock.Collect(SalvageMaterial.Metal, 3);
            _stock.Collect(SalvageMaterial.Wiring, 2);
            _stock.Collect(SalvageMaterial.Metal, 3);
            CargoHold hold = _stock.Hold;
            Assert.AreEqual(3, hold.UsedSlots, "metal 4 + 2, wiring 2");
            Assert.AreEqual(new MaterialSlot(SalvageMaterial.Metal, 4, 4), hold.GetSlot(0));
            Assert.AreEqual(new MaterialSlot(SalvageMaterial.Metal, 2, 4), hold.GetSlot(1));
            Assert.AreEqual(new MaterialSlot(SalvageMaterial.Wiring, 2, 4), hold.GetSlot(2));
            Assert.IsTrue(hold.GetSlot(3).IsEmpty);
            Assert.AreEqual(2 + 5 * 4, hold.FreeUnits(SalvageMaterial.Metal), "the open stack, then 5 empty slots");
            Assert.AreEqual(3, changes.Count);
            Assert.AreEqual(3, changes[2].UsedSlots);
            Assert.AreEqual(CargoFixtures.HoldSlots, changes[2].SlotCount);
        }

        [Test]
        public void ANewGamesSpares_GoInto07sHold_UntilASaveLaysItsOwnHoldIn()
        {
            var totals = new List<MaterialsChanged>();
            _events.Subscribe<MaterialsChanged>(totals.Add);
            _stock.Stow(new Recipe(2, 1, 0));
            Assert.AreEqual(2, _stock.Hold.Units(SalvageMaterial.Metal));
            Assert.AreEqual(1, _stock.Hold.Units(SalvageMaterial.Wiring));
            Assert.AreEqual(0, _stock.Home.Units(SalvageMaterial.Metal), "aboard, not on the shelves");
            Assert.AreEqual(2, totals[totals.Count - 1].Metal, "the totals count them");

            _stock.Restore(new MaterialsSaveData { holdSlots = CargoFixtures.HoldSlots,
                shelvesBuilt = CargoFixtures.BuiltInShelves });
            Assert.AreEqual(0, _stock.Total, "a loaded save keeps its own hold: no spares added");
        }

        [Test]
        public void AFullHold_TakesWhatFits_AndHandsBackTheRest()
        {
            Assert.AreEqual(0, _stock.Collect(SalvageMaterial.Metal, 7 * 4));
            Assert.AreEqual(0, _stock.Collect(SalvageMaterial.Optics, 3));
            Assert.IsFalse(_stock.Hold.IsFull, "one optics still fits");
            Assert.AreEqual(2, _stock.Collect(SalvageMaterial.Optics, 3), "a partial fit: two wait in the world");
            Assert.IsTrue(_stock.Hold.IsFull);
            Assert.AreEqual(3, _stock.Collect(SalvageMaterial.Wiring, 3), "nothing fits");
            Assert.AreEqual(32, _stock.Total, "what waits is not counted until it is taken");
        }

        [Test]
        public void TheDock_MovesTheHoldHome_UpToHomesRoom()
        {
            _stock.Collect(SalvageMaterial.Metal, 6);
            _stock.Collect(SalvageMaterial.Optics, 2);
            Assert.AreEqual(6, _stock.MoveHome(SalvageMaterial.Metal, 6));
            Assert.AreEqual(2, _stock.MoveHome(SalvageMaterial.Optics, 99), "only what 07 has");
            Assert.AreEqual(0, _stock.Hold.UsedSlots);
            Assert.AreEqual(3, _stock.Home.UsedSlots);
            Assert.AreEqual(8, _stock.Total, "moving changes no total");

            _stock.Deliver(SalvageMaterial.Wiring, 2 * CargoFixtures.ShelfSlots * 4);
            _stock.Collect(SalvageMaterial.Wiring, 4);
            Assert.AreEqual(0, _stock.MoveHome(SalvageMaterial.Wiring, 4), "home is full: it stays in 07");
            Assert.AreEqual(4, _stock.Hold.Units(SalvageMaterial.Wiring));
        }

        [Test]
        public void Recipes_ReadHomeAnd07_AndSpendHomeFirst()
        {
            _stock.Deliver(SalvageMaterial.Metal, 6);
            _stock.Collect(SalvageMaterial.Metal, 3);
            Assert.IsTrue(_stock.Has(new Recipe(9, 0, 0)), "home 6 + 07 3");
            Assert.IsTrue(_stock.TrySpend(new Recipe(7, 0, 0)));
            Assert.AreEqual(0, _stock.Home.Units(SalvageMaterial.Metal), "home spent first");
            Assert.AreEqual(2, _stock.Hold.Units(SalvageMaterial.Metal), "then 07");
        }

        [Test]
        public void AShelf_OnAFreeSocket_AddsItsSlots()
        {
            var built = new List<ShelfBuilt>();
            _events.Subscribe<ShelfBuilt>(built.Add);
            HomeStorage home = _stock.Home;
            Assert.AreEqual(2 * CargoFixtures.ShelfSlots, home.SlotCount);
            Assert.AreEqual(CargoFixtures.BuiltInShelves, home.NextSocket);
            home.Build(home.NextSocket);
            Assert.AreEqual(3 * CargoFixtures.ShelfSlots, home.SlotCount);
            Assert.AreEqual(3, home.NextSocket);
            Assert.AreEqual(2, built[0].SocketIndex);
            Assert.IsTrue(home.GetShelf(2).Built);
            Assert.AreEqual(2 * CargoFixtures.ShelfSlots, home.GetShelf(2).FirstSlot);
            Assert.Throws<InvalidOperationException>(() => home.Build(2));
        }

        [Test]
        public void AVersion2Save_GoesAllHome_AndWhatTheShelvesCannotHoldWaitsInCrates()
        {
            string json = MaterialsSaveMigrations.Migrate(
                JsonUtility.ToJson(new MaterialsSaveDataV2 { metal = 100, wiring = 9, optics = 2 }), 2);
            _stock.Restore(JsonUtility.FromJson<MaterialsSaveData>(json));
            Assert.AreEqual(0, _stock.Hold.UsedSlots, "07's hold starts empty");
            Assert.AreEqual(111, _stock.Total, "nothing lost");
            Assert.AreEqual(9, _stock.Home.Units(SalvageMaterial.Wiring), "on the shelves or in crates");
            int crated = _stock.Home.OverflowUnits(SalvageMaterial.Metal) +
                         _stock.Home.OverflowUnits(SalvageMaterial.Wiring) +
                         _stock.Home.OverflowUnits(SalvageMaterial.Optics);
            Assert.AreEqual(111 - 2 * CargoFixtures.ShelfSlots * 4, crated, "overflow crates beside the shelves");

            int crates = _stock.Home.OverflowUnits(SalvageMaterial.Metal);
            Assert.IsTrue(_stock.TrySpend(new Recipe(1, 0, 0)));
            Assert.AreEqual(crates - 1, _stock.Home.OverflowUnits(SalvageMaterial.Metal), "crates are spent first");
            Assert.AreEqual(SalvageMaterials.Count, _stock.Capture().home.Length, "new materials saved at 0");
        }

        [Test]
        public void ASaveWithASmallerHold_SendsTheRestHome_NeverDropsIt()
        {
            var save = new MaterialsSaveData { hold = new[] { 40 }, holdSlots = 4 };
            _stock.Restore(save);
            Assert.AreEqual(32, _stock.Hold.Units(SalvageMaterial.Metal), "the hold keeps its 8 slots");
            Assert.AreEqual(8, _stock.Home.Units(SalvageMaterial.Metal), "the rest goes home");
        }

        [Test]
        public void ACratesNote_IsKeyedByItsSite()
        {
            Assert.AreEqual("crate.depot.note", CrewCrateKeys.Note("site.depot"));
            Assert.AreEqual("crate.lander.note", CrewCrateKeys.Note("lander"));
            Assert.Throws<ArgumentException>(() => CrewCrateKeys.Note(string.Empty));
        }

        [Test]
        public void TheCatalog_KnowsEachMaterialsStackAndKeys()
        {
            Assert.AreEqual(Enum.GetValues(typeof(SalvageMaterial)).Length, SalvageMaterials.Count);
            Assert.AreEqual(4, _catalog.StackSize(SalvageMaterial.Optics));
            Assert.AreEqual(6, _catalog.StackSize(SalvageMaterial.OrangeBeads));
            Assert.AreEqual(MaterialFamily.Lunar, _catalog.FamilyOf(SalvageMaterial.OrangeBeads));
            Assert.IsFalse(_catalog.IsHarvestable(SalvageMaterial.OrangeBeads), "seen, not gathered, in 0.5");
            Assert.AreEqual("material.metal", _catalog.NameKey(SalvageMaterial.Metal));
            Assert.AreEqual("material.orange_beads.job", _catalog.JobKey(SalvageMaterial.OrangeBeads));
            Assert.IsFalse(_catalog.Contains(SalvageMaterial.Alloy), "not in this version");
            Assert.Throws<ArgumentOutOfRangeException>(() => _catalog.StackSize(SalvageMaterial.Alloy));
            Assert.IsNull(_catalog.Validate());
        }
    }
}
