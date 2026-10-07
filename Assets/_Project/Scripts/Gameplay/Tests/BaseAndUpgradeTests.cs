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
        private TestAbilities _abilities;
        private UpgradeDefinition _tower;
        private UpgradeDefinition _hoverJump;

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
            _abilities = new TestAbilities();
            _tower = Create<UpgradeDefinition>();
            _tower.Populate("radio_tower", UpgradeStationKind.RadioTower, 60f, new[]
            {
                new UpgradeLevel(15, 110f, 1.25f), new UpgradeLevel(40, 170f, 1.5f), new UpgradeLevel(80, 260f, 1.8f),
            });
            _hoverJump = Create<UpgradeDefinition>();
            _hoverJump.Populate("rover.hover_jump", UpgradeStationKind.Workshop, 0f, new[]
            {
                new UpgradeLevel(150, RoverAbility.HoverJump),
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
            var service = Service(_tower);
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
            var service = Service(_tower);
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
            var service = Service(_tower);
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
            var service = Service(_tower);
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
            var service = Service(_tower);
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
        public void AbilityUpgrade_GrantsItsAbilityBeforeTheEvent_AndLeavesTheSignalAlone()
        {
            var service = Service(_tower, _hoverJump);
            bool grantedWhenAnnounced = false;
            _events.Subscribe<UpgradePurchased>(_ => grantedWhenAnnounced = _abilities.Has(RoverAbility.HoverJump));
            _wallet.Add(149);
            Assert.AreEqual(PurchaseResult.CannotAfford, service.Purchase("rover.hover_jump"));
            Assert.AreEqual(0, _abilities.Grants);

            _wallet.Add(1);
            _order.Clear();
            Assert.AreEqual(PurchaseResult.Purchased, service.Purchase("rover.hover_jump"));
            Assert.AreEqual(0, _wallet.Balance);
            Assert.AreEqual(1, service.LevelOf("rover.hover_jump"));
            Assert.IsTrue(_abilities.Has(RoverAbility.HoverJump));
            Assert.IsTrue(grantedWhenAnnounced, "listeners of the purchase already see the ability");
            CollectionAssert.AreEqual(new[] { nameof(CurrencyChanged), nameof(UpgradePurchased) }, _order,
                "not a radio upgrade: no signal change");
            Assert.AreEqual("rover.hover_jump", _purchases[0].UpgradeId);
            Assert.AreEqual(1, _purchases[0].Level);
            Assert.AreEqual(PurchaseResult.Maxed, service.Purchase("rover.hover_jump"));
            Assert.AreEqual(1f, service.LightBoost, 1e-5f);
        }

        [Test]
        public void TowerLevels_GrantNoAbility()
        {
            var service = Service(_tower, _hoverJump);
            _wallet.Add(135);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(PurchaseResult.Purchased, service.Purchase("radio_tower"));
            }

            Assert.AreEqual(0, _abilities.Grants);
        }

        [Test]
        public void Restore_RegrantsBoughtAbilitiesSilently()
        {
            var service = Service(_tower, _hoverJump);
            service.Restore(new UpgradesSaveData
            {
                upgrades = new[] { new UpgradeSaveData { id = "radio_tower", level = 1 } },
            });
            Assert.IsFalse(_abilities.Has(RoverAbility.HoverJump), "an older save without the workshop grants nothing");

            service.Restore(new UpgradesSaveData
            {
                upgrades = new[]
                {
                    new UpgradeSaveData { id = "radio_tower", level = 1 },
                    new UpgradeSaveData { id = "rover.hover_jump", level = 1 },
                },
            });
            Assert.IsTrue(_abilities.Has(RoverAbility.HoverJump));
            Assert.AreEqual(1, service.LevelOf("rover.hover_jump"));
            Assert.IsEmpty(_purchases, "a load is not a purchase");
            UpgradesSaveData saved = service.Capture();
            Assert.AreEqual("rover.hover_jump", saved.upgrades[1].id);
            Assert.AreEqual(1, saved.upgrades[1].level);
        }

        [Test]
        public void Shop_SellsEachUpgradeOnlyAtItsOwnStation()
        {
            var service = Service(_tower, _hoverJump);
            var tower = new TestStation { Definition = _tower };
            var bench = new TestStation { Definition = _hoverJump };
            var shop = new UpgradeShop(service, new IUpgradeStation[] { tower, bench }, new CountingSave());
            _wallet.Add(200);

            tower.Occupied = true;
            Assert.AreSame(_tower, shop.StationUpgrade);
            Assert.AreEqual(PurchaseResult.NotAtStation, shop.Purchase("rover.hover_jump"));
            Assert.IsFalse(_abilities.Has(RoverAbility.HoverJump));

            tower.Occupied = false;
            bench.Occupied = true;
            Assert.AreSame(_hoverJump, shop.StationUpgrade);
            Assert.AreEqual(UpgradeStationKind.Workshop, shop.StationUpgrade.Station, "the panel knows where it is");
            Assert.AreEqual(PurchaseResult.NotAtStation, shop.Purchase("radio_tower"));
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase("rover.hover_jump"));
            Assert.IsTrue(_abilities.Has(RoverAbility.HoverJump));
            Assert.AreEqual(50, _wallet.Balance);
        }

        [Test]
        public void Workshop_OffersItsFirstAbilityNotYetBought_ThenRestsOnTheLast()
        {
            var cradle = Create<UpgradeDefinition>();
            cradle.Populate("rover.cargo_cradle", UpgradeStationKind.Workshop, 0f, new[]
            {
                new UpgradeLevel(60, RoverAbility.CargoCradle),
            });
            var service = Service(_hoverJump, cradle);
            var sold = new[] { _hoverJump, cradle };
            Assert.AreEqual(0, Workshop.OfferIndex(sold, service));

            _wallet.Add(210);
            service.Purchase("rover.hover_jump");
            Assert.AreEqual(1, Workshop.OfferIndex(sold, service));
            service.Purchase("rover.cargo_cradle");
            Assert.AreEqual(1, Workshop.OfferIndex(sold, service), "everything bought: it rests on the last");
            Assert.IsTrue(_abilities.Has(RoverAbility.CargoCradle));
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
            Assert.Throws<ArgumentException>(() => Service(empty));
            Assert.IsNull(_hoverJump.Validate());
            Assert.AreEqual(UpgradeStationKind.Workshop, _hoverJump.Station);
            Assert.AreEqual(0f, _hoverJump.SignalRadiusAt(1), "not a radio upgrade");
            Assert.IsTrue(_hoverJump.Levels[0].GrantsAbility);
            Assert.AreEqual(RoverAbility.HoverJump, _hoverJump.Levels[0].Ability);
            Assert.IsFalse(_tower.Levels[0].GrantsAbility);
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
        public void Home_LightsTheGroundAsYouApproach_ButItsWindowsNeverDim()
        {
            var tuning = Create<BaseTuning>();
            Assert.AreEqual(1f, tuning.LampWarmth(tuning.FarnessAt(5f)), 1e-5f);
            Assert.AreEqual(tuning.FarLampWarmth, tuning.LampWarmth(tuning.FarnessAt(500f)), 1e-5f);
            Assert.AreEqual(tuning.WindowGlow, tuning.WindowGlowAt(tuning.FarnessAt(5f)), 1e-5f);
            Assert.AreEqual(tuning.WindowGlow * tuning.FarWindowBoost, tuning.WindowGlowAt(tuning.FarnessAt(500f)),
                1e-5f);
            Assert.GreaterOrEqual(tuning.FarWindowBoost, 1f, "home calls 07 back: a little brighter, never dimmer");
            float previousLamp = 2f;
            float previousWindow = 0f;
            for (float distance = 0f; distance < 400f; distance += 5f)
            {
                float farness = tuning.FarnessAt(distance);
                Assert.LessOrEqual(tuning.LampWarmth(farness), previousLamp + 1e-5f, "the lamps dim with distance");
                Assert.GreaterOrEqual(tuning.WindowGlowAt(farness), previousWindow - 1e-5f, "the windows never dim");
                previousLamp = tuning.LampWarmth(farness);
                previousWindow = tuning.WindowGlowAt(farness);
            }

            Assert.Greater(tuning.DepositGift, 0, "each memory brought home gives a scrap gift");
        }

        [Test]
        public void HomeHalo_IsInvisibleUpClose_AndShrinksOnScreenSlowerThanHome()
        {
            var tuning = Create<BaseTuning>();
            Assert.AreEqual(0f, tuning.HaloGlowAt(10f), "nothing over the base while 07 is there");
            Assert.AreEqual(0f, tuning.HaloGlowAt(tuning.HaloAppear), 1e-5f);
            Assert.AreEqual(tuning.HaloGlow, tuning.HaloGlowAt(tuning.HaloFull), 1e-5f);
            Assert.AreEqual(tuning.HaloGlow, tuning.HaloGlowAt(300f), 1e-5f, "full across the basin");
            Assert.Greater(tuning.HaloGlowAt(150f), 0.5f * tuning.HaloGlow, "it carries from 150 m");
            Assert.AreEqual(tuning.HaloRadius, tuning.HaloRadiusAt(tuning.HaloAppear), 1e-5f);
            float near = tuning.HaloRadiusAt(150f);
            float far = tuning.HaloRadiusAt(300f);
            Assert.Greater(far, near, "it grows with distance");
            Assert.Greater(far / 300f, 0.5f * near / 150f, "so on screen it shrinks slower than home does");
        }

        [Test]
        public void HomeHalo_FacesTheEye_ClearOfTheLander()
        {
            var tuning = Create<BaseTuning>();
            var lander = new GameObject("Lander").transform;
            _created.Add(lander.gameObject);
            lander.SetPositionAndRotation(new Vector3(10f, 2f, -5f), Quaternion.Euler(0f, 30f, 0f));
            Mesh quad = GlowMeshes.Quad();
            _created.Add(quad);
            var material = new Material(Shader.Find(GlowMaterials.ShaderName));
            _created.Add(material);
            var halo = new HomeHalo(lander, quad, material, tuning, lander);
            try
            {
                Vector3 centre = lander.TransformPoint(tuning.HaloCentre);
                Assert.Less(Vector3.Distance(centre, halo.Centre), 1e-4f);
                Vector3 away = new Vector3(0.6f, 0.1f, -0.8f).normalized;
                halo.ShowFrom(centre + away * 20f);
                Assert.AreEqual(0f, halo.Level, "hidden up close");
                Renderer sprite = lander.Find("HomeHalo").GetComponent<Renderer>();
                Assert.IsFalse(sprite.enabled, "and not drawn");

                halo.ShowFrom(centre + away * 300f);
                Assert.AreEqual(tuning.HaloGlowAt(300f), halo.Level, 1e-5f);
                Assert.IsTrue(sprite.enabled);
                Assert.AreEqual(tuning.HaloRadiusAt(300f), halo.Radius, 1e-4f);
                Assert.Less(Vector3.Distance(centre + away * halo.Radius, sprite.transform.position), 1e-3f,
                    "pulled toward the eye by its radius, so the lander never cuts it");
                Assert.Less(Vector3.Angle(away, sprite.transform.forward), 0.01f, "faces the eye");
                Assert.AreEqual(halo.Radius, sprite.transform.lossyScale.x, 1e-3f);

                halo.Boost = 2f;
                halo.ShowFrom(centre + away * 300f);
                Assert.AreEqual(2f * tuning.HaloGlowAt(300f), halo.Level, 1e-5f, "the tower's light boost");
            }
            finally
            {
                halo.Dispose();
            }
        }

        [Test]
        public void EmissionGlow_WritesALinearMultiplier_NotAGammaColour()
        {
            var host = new GameObject("Glow");
            _created.Add(host);
            var renderer = host.AddComponent<MeshRenderer>();
            var glow = new EmissionGlow(renderer);
            glow.Apply(0.5f);
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.AreEqual(new Vector4(0.5f, 0.5f, 0.5f, 1f), block.GetVector("_EmissionColor"),
                "SetVector keeps 0.5 as 0.5 (a gamma colour would light it at about 0.22)");
            Assert.AreEqual(0.5f, glow.Intensity, 1e-6f);
        }

        private UpgradeService Service(params UpgradeDefinition[] definitions)
        {
            return new UpgradeService(_events, _wallet, _abilities, definitions);
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

            public Vector3 PadCentre => Vector3.zero;

            public bool Sells(UpgradeDefinition definition)
            {
                return definition == Definition;
            }
        }

        private sealed class TestAbilities : IRoverAbilities
        {
            private readonly HashSet<RoverAbility> _granted = new HashSet<RoverAbility>();

            public int Grants { get; private set; }

            public bool Has(RoverAbility ability)
            {
                return _granted.Contains(ability);
            }

            public void Grant(RoverAbility ability)
            {
                Grants++;
                _granted.Add(ability);
            }
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
