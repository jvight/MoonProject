using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    public sealed class BaseAndUpgradeTests
    {
        private readonly List<Object> _created = new List<Object>();
        private EventBus _events;
        private List<string> _order;
        private List<float> _radii;
        private List<UpgradePurchased> _purchases;
        private ScrapWallet _wallet;
        private UpgradeDefinition _tower;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _order = new List<string>();
            _radii = new List<float>();
            _purchases = new List<UpgradePurchased>();
            _events.Subscribe<CurrencyChanged>(_ => _order.Add(nameof(CurrencyChanged)));
            _events.Subscribe<UpgradePurchased>(evt =>
            {
                _order.Add(nameof(UpgradePurchased));
                _purchases.Add(evt);
            });
            _events.Subscribe<SignalRadiusChanged>(evt =>
            {
                _order.Add(nameof(SignalRadiusChanged));
                _radii.Add(evt.Radius);
            });
            _wallet = new ScrapWallet(_events);
            _tower = Create<UpgradeDefinition>();
            _tower.Populate("radio_tower", 60f, new[]
            {
                new UpgradeLevel(15, 110f, 1.25f), new UpgradeLevel(40, 170f, 1.5f), new UpgradeLevel(80, 260f, 1.8f),
            });
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void Purchase_SpendsScrap_RaisesTheLevel_AndWidensTheSignal()
        {
            var service = new UpgradeService(_events, _wallet, new[] { _tower });
            Assert.AreEqual(PurchaseResult.CannotAfford, service.Purchase("radio_tower"));
            Assert.AreEqual(0, service.LevelOf("radio_tower"));
            Assert.IsEmpty(_order, "nothing happens without the scrap");

            _wallet.Add(20);
            _order.Clear();
            Assert.AreEqual(PurchaseResult.Purchased, service.Purchase("radio_tower"));
            Assert.AreEqual(1, service.LevelOf("radio_tower"));
            Assert.AreEqual(5, _wallet.Balance);
            CollectionAssert.AreEqual(new[]
            {
                nameof(CurrencyChanged), nameof(UpgradePurchased), nameof(SignalRadiusChanged),
            }, _order);
            Assert.AreEqual(1, _purchases[0].Level);
            Assert.AreEqual(110f, _radii[0]);
            Assert.AreEqual(1.25f, service.LightBoost, 1e-5f);
        }

        [Test]
        public void Purchase_StopsAtTheLastLevel_AndRejectsUnknownIds()
        {
            var service = new UpgradeService(_events, _wallet, new[] { _tower });
            _wallet.Add(135);
            Assert.AreEqual(PurchaseResult.Purchased, service.Purchase("radio_tower"));
            Assert.AreEqual(PurchaseResult.Purchased, service.Purchase("radio_tower"));
            Assert.AreEqual(PurchaseResult.Purchased, service.Purchase("radio_tower"));
            Assert.AreEqual(0, _wallet.Balance, "the three levels cost 135 in all");
            Assert.AreEqual(PurchaseResult.Maxed, service.Purchase("radio_tower"));
            Assert.AreEqual(PurchaseResult.Unknown, service.Purchase("hover_jump"));
            Assert.IsTrue(service.TryGetOffer("radio_tower", out UpgradeOffer offer));
            Assert.IsTrue(offer.IsMaxed);
            Assert.IsNull(offer.Next);
            CollectionAssert.AreEqual(new[] { 110f, 170f, 260f }, _radii);
        }

        [Test]
        public void Offer_ShowsTheNextLevel_AndWhetherItIsAffordable()
        {
            var service = new UpgradeService(_events, _wallet, new[] { _tower });
            _wallet.Add(14);
            Assert.IsTrue(service.TryGetOffer("radio_tower", out UpgradeOffer offer));
            Assert.AreEqual(0, offer.CurrentLevel);
            Assert.AreEqual(3, offer.MaxLevel);
            Assert.AreEqual(15, offer.NextCost);
            Assert.AreSame(_tower.Levels[0], offer.Next);
            Assert.IsFalse(offer.CanAfford);
            _wallet.Add(1);
            Assert.IsTrue(service.TryGetOffer("radio_tower", out offer));
            Assert.IsTrue(offer.CanAfford);
            Assert.IsFalse(service.TryGetOffer("nope", out _));
        }

        [Test]
        public void Restore_AppliesLevelsSilently_AndAnnouncesTheSignal()
        {
            var service = new UpgradeService(_events, _wallet, new[] { _tower });
            service.PublishSignals();
            Assert.AreEqual(60f, _radii[0], "before any purchase the old mast reaches 60 m");
            service.Restore(new UpgradesSaveData
            {
                upgrades = new[]
                {
                    new UpgradeSaveData { id = "radio_tower", level = 7 }, new UpgradeSaveData { id = "gone" },
                },
            });
            Assert.AreEqual(3, service.LevelOf("radio_tower"), "clamped to the last level");
            Assert.IsEmpty(_purchases, "a load is not a purchase");
            Assert.AreEqual(260f, _radii[_radii.Count - 1]);
            UpgradesSaveData saved = service.Capture();
            Assert.AreEqual("radio_tower", saved.upgrades[0].id);
            Assert.AreEqual(3, saved.upgrades[0].level);
        }

        [Test]
        public void Shop_SellsTheTowerOnlyOnItsPad_AndSavesEachPurchase()
        {
            var service = new UpgradeService(_events, _wallet, new[] { _tower });
            var station = new TestStation { Definition = _tower };
            var save = new CountingSave();
            var shop = new UpgradeShop(service, new IUpgradeStation[] { station }, save);
            _wallet.Add(60);
            Assert.IsFalse(shop.IsAtStation);
            Assert.IsNull(shop.StationUpgrade);
            Assert.AreEqual(PurchaseResult.NotAtStation, shop.Purchase("radio_tower"));
            Assert.AreEqual(0, save.Saves);

            station.Occupied = true;
            Assert.IsTrue(shop.IsAtStation);
            Assert.AreSame(_tower, shop.StationUpgrade);
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase("radio_tower"));
            Assert.AreEqual(1, shop.LevelOf("radio_tower"));
            Assert.AreEqual(1, save.Saves, "every purchase is a checkpoint");
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase("radio_tower"));
            Assert.AreEqual(PurchaseResult.CannotAfford, shop.Purchase("radio_tower"));
            Assert.AreEqual(2, save.Saves);
        }

        [Test]
        public void Definition_ReportsEffectsPerLevel_AndValidates()
        {
            Assert.AreEqual(60f, _tower.SignalRadiusAt(0));
            Assert.AreEqual(170f, _tower.SignalRadiusAt(2));
            Assert.AreEqual(1f, _tower.LightBoostAt(0));
            Assert.AreEqual(1.8f, _tower.LightBoostAt(3));
            Assert.IsNull(_tower.Validate());
            var empty = Create<UpgradeDefinition>();
            Assert.IsNotNull(empty.Validate());
            Assert.Throws<ArgumentException>(() => new UpgradeService(_events, _wallet, new[] { empty }));
        }

        [Test]
        public void Slots_TheNearestFreeOneIsTaken()
        {
            var slots = new[] { new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f) };
            var taken = new[] { false, true, false };
            Assert.AreEqual(2, ShelfSlots.Nearest(new Vector3(0.2f, 0f, 1f), slots, taken));
            Assert.AreEqual(0, ShelfSlots.Nearest(new Vector3(-3f, 0f, 1f), slots, taken));
            Assert.AreEqual(-1, ShelfSlots.Nearest(Vector3.zero, slots, new[] { true, true, true }));
            Assert.IsTrue(ShelfSlots.InZone(new Vector3(5f, 1f, 0f), Vector3.zero, 7f, 6f));
            Assert.IsFalse(ShelfSlots.InZone(new Vector3(8f, 1f, 0f), Vector3.zero, 7f, 6f));
            Assert.IsFalse(ShelfSlots.InZone(new Vector3(1f, 9f, 0f), Vector3.zero, 7f, 6f));
        }

        [Test]
        public void TowerSwap_FlaresThenGrowsTheNextStageWithASoftSettle()
        {
            Assert.AreEqual(0, TowerStageSwap.StageFor(0, 3), "level 0 is the dark old mast (first stage)");
            Assert.AreEqual(0, TowerStageSwap.StageFor(1, 3));
            Assert.AreEqual(1, TowerStageSwap.StageFor(2, 3));
            Assert.AreEqual(2, TowerStageSwap.StageFor(3, 3));
            Assert.AreEqual(0f, TowerStageSwap.Flare(0f, 0.7f, 1.3f), 1e-5f);
            Assert.AreEqual(1f, TowerStageSwap.Flare(0.7f, 0.7f, 1.3f), 1e-5f);
            Assert.AreEqual(0f, TowerStageSwap.Flare(2f, 0.7f, 1.3f), 1e-5f);
            Assert.IsFalse(TowerStageSwap.Swapped(0.6f, 0.7f));
            Assert.AreEqual(0.85f, TowerStageSwap.Grow(0.7f, 0.7f, 1.3f, 0.85f, 1.2f), 1e-5f);
            Assert.AreEqual(1f, TowerStageSwap.Grow(2f, 0.7f, 1.3f, 0.85f, 1.2f), 1e-5f);
            float peak = 0f;
            for (int i = 0; i <= 100; i++)
            {
                peak = Mathf.Max(peak, TowerStageSwap.Grow(0.7f + 1.3f * i / 100f, 0.7f, 1.3f, 0.85f, 1.2f));
            }

            Assert.Greater(peak, 1f, "settles with a small overshoot");
            Assert.Less(peak, 1.05f);
            Assert.AreEqual(0.92f, TowerStageSwap.Sink(0.7f, 0.7f, 0.92f), 1e-5f);
        }

        [Test]
        public void Home_WarmsAsYouApproach()
        {
            var tuning = Create<BaseTuning>();
            Assert.AreEqual(1f, tuning.WarmthAt(5f), 1e-5f);
            Assert.AreEqual(tuning.FarWarmth, tuning.WarmthAt(500f), 1e-5f);
            float previous = 2f;
            for (float distance = 0f; distance < 120f; distance += 5f)
            {
                float warmth = tuning.WarmthAt(distance);
                Assert.LessOrEqual(warmth, previous + 1e-5f);
                previous = warmth;
            }

            Assert.Greater(tuning.DepositGift, 0, "each memory brought home gives a scrap gift");
        }

        private T Create<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _created.Add(asset);
            return asset;
        }

        private sealed class TestStation : IUpgradeStation
        {
            public UpgradeDefinition Definition { get; set; }

            public bool Occupied { get; set; }
        }

        private sealed class CountingSave : ISaveService
        {
            public int Saves { get; private set; }

            public string FilePath => "memory";

            public bool IsLoaded => true;

            public SaveLoadResult LoadResult => SaveLoadResult.NoSave;

            public IDisposable Register(ISaveSection section)
            {
                throw new NotSupportedException("The shop never registers sections.");
            }

            public bool SaveNow()
            {
                Saves++;
                return true;
            }
        }
    }
}
