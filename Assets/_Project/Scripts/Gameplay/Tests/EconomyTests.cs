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
        private List<CurrencyChanged> _changes;
        private ScrapWallet _wallet;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _changes = new List<CurrencyChanged>();
            _events.Subscribe<CurrencyChanged>(change => _changes.Add(change));
            _wallet = new ScrapWallet(_events);
        }

        [Test]
        public void Wallet_AddAndSpend_PublishTotalsAndDeltas()
        {
            _wallet.Add(3);
            _wallet.Add(2);
            Assert.IsTrue(_wallet.TrySpend(4));

            Assert.AreEqual(1, _wallet.Balance);
            Assert.AreEqual(3, _changes.Count);
            Assert.AreEqual(5, _changes[1].Total);
            Assert.AreEqual(2, _changes[1].Delta);
            Assert.AreEqual(1, _changes[2].Total);
            Assert.AreEqual(-4, _changes[2].Delta);
        }

        [Test]
        public void Wallet_CannotSpendMoreThanItHolds_AndNothingChanges()
        {
            _wallet.Add(3);
            Assert.IsFalse(_wallet.CanAfford(4));
            Assert.IsFalse(_wallet.TrySpend(4));
            Assert.AreEqual(3, _wallet.Balance);
            Assert.AreEqual(1, _changes.Count);
        }

        [Test]
        public void Wallet_Restore_IsARefreshWithZeroDelta()
        {
            _wallet.Add(5);
            _wallet.Restore(new WalletSaveData { balance = 42 });
            Assert.AreEqual(42, _wallet.Balance);
            Assert.AreEqual(42, _changes[1].Total);
            Assert.AreEqual(0, _changes[1].Delta);
            Assert.AreEqual(42, _wallet.Capture().balance);
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

            var scrap = new ScrapSaveData { layoutSignature = 77, collected = new[] { 3, 9 } };
            ScrapSaveData scrapCopy = JsonUtility.FromJson<ScrapSaveData>(JsonUtility.ToJson(scrap));
            Assert.AreEqual(77, scrapCopy.layoutSignature);
            CollectionAssert.AreEqual(new[] { 3, 9 }, scrapCopy.collected);

            var upgrades = new UpgradesSaveData
            {
                upgrades = new[] { new UpgradeSaveData { id = "radio_tower", level = 2 } },
            };
            UpgradesSaveData upgradesCopy = JsonUtility.FromJson<UpgradesSaveData>(JsonUtility.ToJson(upgrades));
            Assert.AreEqual(2, upgradesCopy.upgrades[0].level);
        }
    }
}
