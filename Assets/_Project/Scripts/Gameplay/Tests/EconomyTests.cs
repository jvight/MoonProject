using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    public sealed class EconomyTests
    {
        private EventBus _events;
        private List<MaterialsChanged> _changes;
        private MaterialStock _stock;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _changes = new List<MaterialsChanged>();
            _events.Subscribe<MaterialsChanged>(change => _changes.Add(change));
            _stock = new MaterialStock(_events);
        }

        [Test]
        public void Stock_AddsSalvage_AndSpendsWholeRecipes_PublishingTotals()
        {
            _stock.Add(SalvageMaterial.Metal, 4);
            _stock.Add(SalvageMaterial.Wiring, 3);
            _stock.Add(SalvageMaterial.Optics, 2);
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
            _stock.Add(SalvageMaterial.Metal, 9);
            _stock.Add(SalvageMaterial.Wiring, 9);
            Assert.IsFalse(_stock.Has(new Recipe(1, 1, 1)), "one optics short");
            Assert.IsFalse(_stock.TrySpend(new Recipe(1, 1, 1)));
            Assert.AreEqual(18, _stock.Total, "nothing taken");
            Assert.AreEqual(2, _changes.Count);
            Assert.Throws<ArgumentException>(() => _stock.TrySpend(new Recipe(0, 0, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => _stock.Add(SalvageMaterial.Optics, 0));
        }

        [Test]
        public void Stock_Restore_RoundTripsAndAnnounces()
        {
            _stock.Add(SalvageMaterial.Metal, 5);
            _stock.Restore(new MaterialsSaveData { metal = 7, wiring = 3, optics = 2 });
            Assert.AreEqual(12, _stock.Total);
            Assert.AreEqual(7, _changes[1].Metal);
            Assert.AreEqual(3, _changes[1].Wiring);
            MaterialsSaveData saved = _stock.Capture();
            Assert.AreEqual(7, saved.metal);
            Assert.AreEqual(2, saved.optics);
            Assert.Throws<FormatException>(() => _stock.Restore(new MaterialsSaveData { wiring = -1 }));
        }

        [Test]
        public void OldScrap_BecomesAFairMaterialStock()
        {
            string json = MaterialsSaveMigrations.Migrate(JsonUtility.ToJson(new WalletSaveData { balance = 150 }),
                1);
            MaterialsSaveData stock = JsonUtility.FromJson<MaterialsSaveData>(json);
            Assert.AreEqual(8, stock.metal + stock.wiring + stock.optics, "150 scrap at 20 per unit, rounded");
            Assert.AreEqual(4, stock.metal, "dealt metal, wiring, metal, wiring, metal, wiring, optics, metal");
            Assert.AreEqual(3, stock.wiring);
            Assert.AreEqual(1, stock.optics);
            MaterialsSaveData none = MaterialsSaveMigrations.Convert(0);
            Assert.AreEqual(0, none.metal + none.wiring + none.optics);
            MaterialsSaveData all = MaterialsSaveMigrations.Convert(705);
            Assert.AreEqual(35, all.metal + all.wiring + all.optics, "the whole old economy buys about every recipe");
            Assert.Throws<InvalidOperationException>(() => MaterialsSaveMigrations.Migrate("{}", 2));
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

        [Test]
        public void SaveData_RoundTripsThroughJson()
        {
            var relics = new RelicsSaveData
            {
                relics = new[]
                {
                    new RelicSaveData
                    {
                        id = "teapot", state = (int)RelicState.Surfacing, progress = 0.4f, discovered = true,
                        position = new Vector3(1f, 2f, 3f), rotation = Quaternion.Euler(0f, 30f, 0f), slot = -1,
                    },
                },
            };
            RelicsSaveData copy = JsonUtility.FromJson<RelicsSaveData>(JsonUtility.ToJson(relics));
            Assert.AreEqual("teapot", copy.relics[0].id);
            Assert.AreEqual((int)RelicState.Surfacing, copy.relics[0].state);
            Assert.AreEqual(0.4f, copy.relics[0].progress, 1e-6f);
            Assert.IsTrue(copy.relics[0].discovered);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), copy.relics[0].position);

            var upgrades = new UpgradesSaveData
            {
                upgrades = new[] { new UpgradeSaveData { id = "radio_tower", level = 2 } },
            };
            UpgradesSaveData upgradesCopy = JsonUtility.FromJson<UpgradesSaveData>(JsonUtility.ToJson(upgrades));
            Assert.AreEqual(2, upgradesCopy.upgrades[0].level);
        }
    }
}
