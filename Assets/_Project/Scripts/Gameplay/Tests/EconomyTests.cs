using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    public sealed class EconomyTests
    {
        private EventBus _events;
        private List<MaterialsChanged> _changes;
        private MaterialCatalog _catalog;
        private MaterialStock _stock;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _changes = new List<MaterialsChanged>();
            _events.Subscribe<MaterialsChanged>(change => _changes.Add(change));
            _catalog = CargoFixtures.Catalog();
            _stock = CargoFixtures.Stock(_events, _catalog);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void Stock_CollectsSalvage_AndSpendsWholeRecipes_PublishingTotals()
        {
            _stock.Collect(SalvageMaterial.Metal, 4);
            _stock.Collect(SalvageMaterial.Wiring, 3);
            _stock.Collect(SalvageMaterial.Optics, 2);
            Assert.IsTrue(_stock.Has(new Recipe(4, 2, 1)));
            Assert.IsTrue(_stock.TrySpend(new Recipe(4, 2, 1)));

            Assert.AreEqual(0, _stock.Metal);
            Assert.AreEqual(1, _stock.Wiring);
            Assert.AreEqual(1, _stock.Optics);
            Assert.AreEqual(2, _stock.Total);
            Assert.AreEqual(1, _stock.Of(SalvageMaterial.Optics));
            Assert.AreEqual(4, _changes.Count);
            Assert.AreEqual(4, _changes[2].Metal);
            Assert.AreEqual(2, _changes[2].Optics);
            Assert.AreEqual(0, _changes[3].Metal);
            Assert.AreEqual(1, _changes[3].Wiring);
        }

        [Test]
        public void Stock_NeverSpendsPartOfARecipe()
        {
            _stock.Collect(SalvageMaterial.Metal, 9);
            _stock.Collect(SalvageMaterial.Wiring, 9);
            Assert.IsFalse(_stock.Has(new Recipe(1, 1, 1)), "one optics short");
            Assert.IsFalse(_stock.TrySpend(new Recipe(1, 1, 1)));
            Assert.AreEqual(18, _stock.Total, "nothing taken");
            Assert.AreEqual(2, _changes.Count);
            Assert.Throws<ArgumentException>(() => _stock.TrySpend(new Recipe(0, 0, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => _stock.Collect(SalvageMaterial.Optics, 0));
        }

        [Test]
        public void Stock_Restore_RoundTripsAndAnnounces()
        {
            _stock.Collect(SalvageMaterial.Metal, 5);
            var saved = new MaterialsSaveData
            {
                hold = new[] { 2, 1, 0 }, holdSlots = 8,
            };
            _stock.Restore(saved);
            Assert.AreEqual(3, _stock.Total, "the save replaces what was aboard");
            Assert.AreEqual(2, _changes[_changes.Count - 1].Metal);
            Assert.AreEqual(1, _changes[_changes.Count - 1].Wiring);
            MaterialsSaveData captured = _stock.Capture();
            Assert.AreEqual(2, captured.hold[(int)SalvageMaterial.Metal]);
            Assert.AreEqual(SalvageMaterials.Count, captured.hold.Length, "every material, new ones at 0");
            Assert.Throws<FormatException>(() => _stock.Restore(new MaterialsSaveData { hold = new[] { 0, -1 } }));
        }

        [Test]
        public void Recipe_AddsUp()
        {
            var recipe = new Recipe(4, 2, 1);
            Assert.AreEqual(7, recipe.Total);
            Assert.AreEqual(2, recipe.Of(SalvageMaterial.Wiring));
            Recipe sum = recipe.Plus(new Recipe(1, 1, 1));
            Assert.AreEqual(5, sum.Metal);
            Assert.AreEqual(2, sum.Optics);
            Assert.IsTrue(new Recipe(0, 0, 0).IsFree);
            Assert.Throws<ArgumentOutOfRangeException>(() => new Recipe(-1, 0, 0));
        }

        [Test]
        public void Combo_ClimbsWithinTheWindow_AndRestartsAfterAPause()
        {
            var combo = new ComboCounter(2f);
            Assert.AreEqual(0, combo.Register(10f));
            Assert.AreEqual(1, combo.Register(10.5f));
            Assert.AreEqual(2, combo.Register(12.4f), "1.9 s later is still the same chain");
            Assert.AreEqual(0, combo.Register(14.5f), "2.1 s of silence starts a new chain");
            Assert.AreEqual(1, combo.Register(14.6f));
        }

        [Test]
        public void Cadence_SpacesPickupsAtLeastTheMinimumInterval()
        {
            var cadence = new PickupCadence(0.11f);
            Assert.IsTrue(cadence.TryClaim(1f));
            Assert.IsFalse(cadence.TryClaim(1.05f));
            Assert.IsTrue(cadence.TryClaim(1.11f));
        }

    }
}
