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
    /// 07's hold (docs/features/M3-25): packing into stacks, stock laid in by hand until a save lays its own, a
    /// partial fit that hands back the rest, and the material catalog.
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
        public void WhatIsStowed_GoesInto07sHold_UntilASaveLaysItsOwnHoldIn()
        {
            var totals = new List<MaterialsChanged>();
            _events.Subscribe<MaterialsChanged>(totals.Add);
            _stock.Stow(new Recipe(2, 1, 0));
            Assert.AreEqual(2, _stock.Hold.Units(SalvageMaterial.Metal));
            Assert.AreEqual(1, _stock.Hold.Units(SalvageMaterial.Wiring));
            Assert.AreEqual(2, totals[totals.Count - 1].Metal, "the totals count them");

            _stock.Restore(new MaterialsSaveData { holdSlots = CargoFixtures.HoldSlots });
            Assert.AreEqual(0, _stock.Total, "a loaded save keeps its own hold");
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
