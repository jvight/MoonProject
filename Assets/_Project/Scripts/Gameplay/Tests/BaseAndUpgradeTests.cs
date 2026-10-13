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
        private const string HoverJumpKit = "rover.hover_jump";
        private const string CargoCradleKit = "rover.cargo_cradle";
        private const string BoostCoilsKit = "rover.boost_coils";

        private static readonly string[] Kits = { HoverJumpKit, CargoCradleKit, BoostCoilsKit };

        private readonly List<Object> _created = new List<Object>();
        private EventBus _events;
        private List<string> _order;
        private List<float> _radii;
        private List<UpgradePurchased> _purchases;
        private MaterialStock _stock;
        private TestAbilities _abilities;
        private RoverEquipment _equipment;
        private UpgradeDefinition _tower;
        private UpgradeDefinition _hoverJump;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _order = new List<string>();
            _radii = new List<float>();
            _purchases = new List<UpgradePurchased>();
            _events.Subscribe<MaterialsChanged>(_ => _order.Add(nameof(MaterialsChanged)));
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
            MaterialCatalog catalog = CargoFixtures.Catalog();
            _created.Add(catalog);
            _stock = CargoFixtures.Stock(_events, catalog);
            _abilities = new TestAbilities();
            _equipment = new RoverEquipment(_events, _abilities, EquipmentTable.Basin(), 4, station => true,
                part => 0, metal => { });
            _tower = Create<UpgradeDefinition>();
            _tower.Populate("radio_tower", UpgradeStationKind.RadioTower, 60f, new[]
            {
                new UpgradeLevel(new Recipe(0, 1, 1), 110f, 1.25f), new UpgradeLevel(new Recipe(0, 2, 1), 170f, 1.5f),
                new UpgradeLevel(new Recipe(0, 2, 2), 260f, 1.8f),
            });
            _hoverJump = Create<UpgradeDefinition>();
            _hoverJump.Populate("rover.hover_jump", UpgradeStationKind.Workshop, 0f, new[]
            {
                new UpgradeLevel(new Recipe(4, 2, 1), RoverAbility.HoverJump),
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
        public void Purchase_SpendsTheRecipe_RaisesTheLevel_AndWidensTheSignal()
        {
            var service = Service(_tower);
            Assert.AreEqual(PurchaseResult.CannotAfford, service.Purchase("radio_tower"));
            Assert.AreEqual(0, service.LevelOf("radio_tower"));
            Assert.IsEmpty(_order, "nothing happens without the materials");

            Give(0, 2, 2);
            _order.Clear();
            Assert.AreEqual(PurchaseResult.Purchased, service.Purchase("radio_tower"));
            Assert.AreEqual(1, service.LevelOf("radio_tower"));
            Assert.AreEqual(1, _stock.Wiring);
            Assert.AreEqual(1, _stock.Optics);
            CollectionAssert.AreEqual(new[]
            {
                nameof(MaterialsChanged), nameof(UpgradePurchased), nameof(SignalRadiusChanged),
            }, _order);
            Assert.AreEqual(1, _purchases[0].Level);
            Assert.AreEqual(110f, _radii[0]);
            Assert.AreEqual(1.25f, service.LightBoost, 1e-5f);
        }

        [Test]
        public void Purchase_StopsAtTheLastLevel_AndRejectsUnknownIds()
        {
            var service = Service(_tower);
            Give(0, 5, 4);
            Assert.AreEqual(PurchaseResult.Purchased, service.Purchase("radio_tower"));
            Assert.AreEqual(PurchaseResult.Purchased, service.Purchase("radio_tower"));
            Assert.AreEqual(PurchaseResult.Purchased, service.Purchase("radio_tower"));
            Assert.AreEqual(0, _stock.Total, "the three levels take 5 wiring and 4 optics in all");
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
            Give(0, 1, 0);
            Assert.IsTrue(service.TryGetOffer("radio_tower", out UpgradeOffer offer));
            Assert.AreEqual(0, offer.CurrentLevel);
            Assert.AreEqual(3, offer.MaxLevel);
            Assert.AreEqual(1, offer.NextCost.Wiring);
            Assert.AreEqual(1, offer.NextCost.Optics);
            Assert.AreSame(_tower.Levels[0], offer.Next);
            Assert.IsFalse(offer.CanAfford, "one optics short");
            Give(0, 0, 1);
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
            var shop = new UpgradeShop(service, new IUpgradeStation[] { station }, save, new Board(), NoParts(),
                _ => false);
            Give(0, 3, 2);
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
        public void AKitPurchase_GrantsNothingItself_ThePartItBringsCarriesTheAbility()
        {
            var service = Service(_tower, _hoverJump);
            Give(4, 2, 0);
            Assert.AreEqual(PurchaseResult.CannotAfford, service.Purchase("rover.hover_jump"));
            Assert.AreEqual(0, _abilities.Grants);

            Give(0, 0, 1);
            _order.Clear();
            Assert.AreEqual(PurchaseResult.Purchased, service.Purchase("rover.hover_jump"));
            Assert.AreEqual(0, _stock.Total);
            Assert.AreEqual(1, service.LevelOf("rover.hover_jump"));
            Assert.IsFalse(_abilities.Has(RoverAbility.HoverJump), "the bay crafts the part; fitting it grants");
            Assert.IsTrue(service.TryGetPart("rover.hover_jump", 1, out RoverKitPiece part));
            Assert.AreEqual(RoverKitPiece.HoverCoils, part);
            Assert.IsTrue(service.TryGetRecipeOf(RoverKitPiece.HoverCoils, out Recipe recipe));
            Assert.AreEqual(4, recipe.Metal, "what the bay would recycle it from");
            CollectionAssert.AreEqual(new[] { nameof(MaterialsChanged), nameof(UpgradePurchased) }, _order,
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
            Give(0, 5, 4);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(PurchaseResult.Purchased, service.Purchase("radio_tower"));
            }

            Assert.AreEqual(0, _abilities.Grants);
        }

        [Test]
        public void Restore_LaysAnOlderSavesBoughtPartsInFitted_Silently()
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
            Assert.IsTrue(_abilities.Has(RoverAbility.HoverJump), "the part loads fitted and brings its ability");
            Assert.IsTrue(_equipment.IsFitted(RoverKitPiece.HoverCoils));
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
            var shop = new UpgradeShop(service, new IUpgradeStation[] { tower, bench }, new CountingSave(),
                new Board(), NoParts(), _ => false);
            Give(10, 10, 10);

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
            Assert.AreEqual(6, _stock.Metal, "Hover-Jump took its 4 metal");
        }

        [Test]
        public void Shop_RefusesAKit_WhileThePartsRowIsFull_AndSpendsNothing()
        {
            var service = Service(_hoverJump);
            var bench = new TestStation { Definition = _hoverJump, Occupied = true };
            bool rowFull = true;
            var shop = new UpgradeShop(service, new IUpgradeStation[] { bench }, new CountingSave(), new Board(),
                NoParts(), id => rowFull);
            Give(10, 10, 10);
            Assert.AreEqual(PurchaseResult.RowFull, shop.Purchase("rover.hover_jump"), "fit a part first");
            Assert.AreEqual(30, _stock.Total);
            rowFull = false;
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase("rover.hover_jump"));
        }

        [Test]
        public void Shop_ListsEverythingTheParkedStationSells_InItsOrder()
        {
            UpgradeDefinition headlamp = Create<UpgradeDefinition>();
            headlamp.Populate("rover.warm_headlamp", UpgradeStationKind.Workshop, 0f,
                new[] { new UpgradeLevel(new Recipe(1, 0, 2), RoverAbility.WarmHeadlamp) });
            var service = Service(_tower, _hoverJump, headlamp);
            var tower = new TestStation { Definition = _tower };
            var bench = new TestStation { Definition = _hoverJump, Catalog = new[] { _hoverJump, headlamp } };
            var shop = new UpgradeShop(service, new IUpgradeStation[] { tower, bench }, new CountingSave(),
                new Board(), NoParts(), _ => false);
            Assert.AreEqual(0, shop.StationUpgradeCount, "not parked: nothing listed");
            Assert.Throws<InvalidOperationException>(() => shop.StationUpgradeAt(0));

            tower.Occupied = true;
            Assert.AreEqual(1, shop.StationUpgradeCount, "the tower sells its one upgrade");
            Assert.AreSame(_tower, shop.StationUpgradeAt(0));

            tower.Occupied = false;
            bench.Occupied = true;
            Assert.AreEqual(2, shop.StationUpgradeCount);
            Assert.AreSame(_hoverJump, shop.StationUpgradeAt(0));
            Assert.AreSame(headlamp, shop.StationUpgradeAt(1), "in the bench's own order");

            Give(10, 10, 10);
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase("rover.warm_headlamp"),
                "any piece the bench sells can be crafted, not only the first");
            Assert.AreEqual(2, shop.StationUpgradeCount, "bought ones stay listed; the UI leaves out the maxed");
        }

        [Test]
        public void Workshop_OffersItsFirstAbilityNotYetBought_ThenRestsOnTheLast()
        {
            var cradle = Create<UpgradeDefinition>();
            cradle.Populate("rover.cargo_cradle", UpgradeStationKind.Workshop, 0f, new[]
            {
                new UpgradeLevel(new Recipe(2, 1, 1), RoverAbility.CargoCradle),
            });
            var service = Service(_hoverJump, cradle);
            var sold = new[] { _hoverJump, cradle };
            Assert.AreEqual(0, Workshop.OfferIndex(sold, service, new Board()));

            Give(6, 3, 2);
            service.Purchase("rover.hover_jump");
            Assert.AreEqual(1, Workshop.OfferIndex(sold, service, new Board()));
            service.Purchase("rover.cargo_cradle");
            Assert.AreEqual(1, Workshop.OfferIndex(sold, service, new Board()),
                "everything bought: it rests on the last");
        }

        [Test]
        public void Workshop_OffersOnlyPinnedKits_AndTheShopBuildsNoKitWithoutItsBlueprint()
        {
            var cradle = Create<UpgradeDefinition>();
            cradle.Populate("rover.cargo_cradle", UpgradeStationKind.Workshop, 0f, new[]
            {
                new UpgradeLevel(new Recipe(2, 1, 1), RoverAbility.CargoCradle),
            });
            var service = Service(_hoverJump, cradle);
            var sold = new[] { _hoverJump, cradle };
            var board = new Board("rover.cargo_cradle");
            var station = new TestStation { Definition = cradle, Occupied = true };
            var shop = new UpgradeShop(service, new IUpgradeStation[] { station }, new CountingSave(), board, NoParts(),
                _ => false);
            Give(6, 3, 2);

            Assert.AreEqual(PurchaseResult.NotPinned, shop.Purchase("rover.cargo_cradle"));
            Assert.IsFalse(_abilities.Has(RoverAbility.CargoCradle));
            service.Purchase("rover.hover_jump");
            Assert.AreEqual(0, Workshop.OfferIndex(sold, service, board), "nothing new pinned: it rests on Hover-Jump");
            board.Pin("rover.cargo_cradle");
            Assert.AreEqual(1, Workshop.OfferIndex(sold, service, board), "the board grows, so does the bay's offer");
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase("rover.cargo_cradle"));
        }

        [Test]
        public void Blueprints_AreLearnedOnceTheirSetIsScanned_ThenThePlotterDrawsThem_InTheKitsOrder()
        {
            var completed = new List<BlueprintCompleted>();
            _events.Subscribe<BlueprintCompleted>(completed.Add);
            var board = new Blueprints(_events, Kits);
            Assert.IsFalse(board.IsPinned(HoverJumpKit), "no sheet is pinned from the start (M3-21)");
            Assert.IsFalse(board.IsLearned(CargoCradleKit));
            Assert.IsTrue(board.IsPinned("radio_tower"), "the tower is no kit of the board");
            Assert.IsNull(board.NextToDraw());

            board.Learn(BoostCoilsKit);
            board.Learn(CargoCradleKit);
            board.Learn(CargoCradleKit);
            Assert.AreEqual(2, completed.Count, "each blueprint completes once");
            Assert.AreEqual(BoostCoilsKit, completed[0].BlueprintId);
            Assert.IsTrue(board.IsPinned(CargoCradleKit), "learned: the bay can build it");
            Assert.IsTrue(board.IsLearned(BoostCoilsKit));
            Assert.IsFalse(board.IsDrawn(CargoCradleKit), "the plotter has not drawn it yet");
            Assert.AreEqual(CargoCradleKit, board.NextToDraw(), "drawn in the kits' order");
            board.MarkDrawn(CargoCradleKit);
            Assert.AreEqual(BoostCoilsKit, board.NextToDraw());
            Assert.Throws<InvalidOperationException>(() => board.MarkDrawn(HoverJumpKit), "not learned");
            Assert.Throws<ArgumentException>(() => board.Learn("radio_tower"), "the tower is no kit");
            CollectionAssert.AreEqual(new[] { CargoCradleKit, BoostCoilsKit }, board.Capture().found);
            CollectionAssert.AreEqual(new[] { CargoCradleKit }, board.Capture().drawn);
            CollectionAssert.IsEmpty(board.Capture().carried, "no tube rides anywhere any more");
            CollectionAssert.IsEmpty(board.Capture().reading);

            board.Restore(new BlueprintsSaveData
            {
                found = new[] { HoverJumpKit, "rover.unknown" }, drawn = new[] { HoverJumpKit, BoostCoilsKit },
            });
            Assert.AreEqual(2, completed.Count, "a load is silent");
            Assert.IsTrue(board.IsDrawn(HoverJumpKit));
            Assert.IsFalse(board.IsLearned(CargoCradleKit), "a load replaces the board");
            Assert.IsFalse(board.IsDrawn(BoostCoilsKit), "a sheet is drawn only of a learned blueprint");
            Assert.Throws<ArgumentException>(() => new Blueprints(_events, new[] { HoverJumpKit, HoverJumpKit }),
                "a kit listed twice");
        }

        [Test]
        public void Blueprints_FromASaveBeforeScanning_ADecodedTubeIsLearned_AndKeepsItsDrawnSheet()
        {
            var board = new Blueprints(_events, Kits);
            board.Restore(new BlueprintsSaveData
            {
                found = new[] { CargoCradleKit, HoverJumpKit }, drawn = new[] { CargoCradleKit },
            });
            Assert.IsTrue(board.IsLearned(CargoCradleKit), "decoded and drawn: learned");
            Assert.IsTrue(board.IsDrawn(CargoCradleKit), "its sheet still hangs");
            Assert.IsTrue(board.IsLearned(HoverJumpKit), "decoded, waiting to be drawn: learned");
            Assert.AreEqual(HoverJumpKit, board.NextToDraw(), "the plotter draws it once the bay is awake");
        }

        [Test]
        public void Blueprints_FromASaveBeforeScanning_ATubeRidingOn07OrInTheReaderIsLearned()
        {
            var board = new Blueprints(_events, Kits);
            board.Restore(new BlueprintsSaveData
            {
                carried = new[] { CargoCradleKit }, reading = new[] { BoostCoilsKit },
            });
            Assert.IsTrue(board.IsLearned(CargoCradleKit), "found, riding home on 07: learned");
            Assert.IsTrue(board.IsLearned(BoostCoilsKit), "taken by the reader: learned");
            Assert.IsFalse(board.IsDrawn(CargoCradleKit));
            CollectionAssert.AreEqual(new[] { CargoCradleKit, BoostCoilsKit }, board.Capture().found,
                "saved as learned from now on");
            CollectionAssert.IsEmpty(board.Capture().carried);
        }

        [Test]
        public void Blueprints_FromASaveBeforeScanning_ATubeStillWaitingIsGone_ItsSetWaitsToBeScanned()
        {
            var completed = new List<BlueprintCompleted>();
            _events.Subscribe<BlueprintCompleted>(completed.Add);
            var board = new Blueprints(_events, Kits);
            board.Restore(new BlueprintsSaveData { found = new[] { CargoCradleKit } });
            Assert.IsFalse(board.IsLearned(HoverJumpKit), "its tube waited at the lip: nothing of it is known");
            Assert.IsFalse(board.IsPinned(HoverJumpKit));
            board.Learn(HoverJumpKit);
            Assert.AreEqual(1, completed.Count, "scanning its set learns it as in a new game");
        }

        [Test]
        public void TowerParts_ArePickedUp_RideOn07_AndAreFedToThePort_ThenTheirLevelCanBeBuilt()
        {
            var found = new List<ComponentFound>();
            var delivered = new List<ComponentDelivered>();
            _events.Subscribe<ComponentFound>(found.Add);
            _events.Subscribe<ComponentDelivered>(delivered.Add);
            var parts = new TowerParts(_events, "radio_tower", new[]
            {
                new TowerPart("Part_Transformer", 2, TowerPartSource.SiteHeart, "site.garage", Vector2.up),
                new TowerPart("Part_TowerCoil", 1, TowerPartSource.SiteHeart, "site.depot", Vector2.zero),
            });
            Assert.AreEqual("Part_TowerCoil", parts.PartAt(0).PartId, "kept in level order");
            Assert.IsTrue(parts.NeedsPart("radio_tower", 1));
            Assert.IsTrue(parts.NeedsPart("radio_tower", 2));
            Assert.IsFalse(parts.NeedsPart("radio_tower", 3), "a level with no part needs none");
            Assert.IsFalse(parts.NeedsPart("rover.hover_jump", 1), "only the tower's levels need parts");
            Assert.AreEqual(-1, parts.FirstCarried());

            parts.PickUp(1, Vector3.one);
            parts.PickUp(1, Vector3.one);
            parts.PickUp(0, Vector3.zero);
            Assert.AreEqual(2, found.Count, "each part is picked up once");
            Assert.AreEqual("Part_Transformer", found[0].PartId);
            Assert.AreEqual(2, found[0].TowerLevel);
            Assert.IsFalse(found[0].Restored);
            Assert.AreEqual(0, parts.FirstCarried(), "the lowest level is fed first");
            Assert.IsTrue(parts.NeedsPart("radio_tower", 1), "carried is not delivered");

            parts.Deliver(0, Vector3.up);
            parts.Deliver(0, Vector3.up);
            Assert.AreEqual(1, delivered.Count);
            Assert.AreEqual("Part_TowerCoil", delivered[0].PartId);
            Assert.AreEqual(Vector3.up, delivered[0].Position);
            Assert.IsFalse(parts.NeedsPart("radio_tower", 1), "fed to the port: L1 can be built");
            Assert.AreEqual(1, parts.FirstCarried());

            TowerPartsSaveData saved = parts.Capture();
            CollectionAssert.AreEqual(new[] { "Part_Transformer" }, saved.carried);
            CollectionAssert.AreEqual(new[] { "Part_TowerCoil" }, saved.delivered);
            parts.Restore(new TowerPartsSaveData());
            Assert.AreEqual(TowerPartState.Waiting, parts.StateOf(0), "a load replaces every part's state");
            parts.Restore(saved);
            Assert.AreEqual(TowerPartState.Delivered, parts.StateOf(0));
            Assert.AreEqual(TowerPartState.Carried, parts.StateOf(1));
            Assert.AreEqual(2, found.Count, "a load is silent");
            parts.Announce();
            Assert.AreEqual(3, found.Count, "until the game announces the parts still on 07");
            Assert.IsTrue(found[2].Restored);
            Assert.AreEqual("Part_Transformer", found[2].PartId);
        }

        [Test]
        public void TowerParts_RejectBrokenContent()
        {
            Assert.Throws<ArgumentException>(() => new TowerParts(_events, "radio_tower", new[]
            {
                new TowerPart("Part_TowerCoil", 1, TowerPartSource.SiteHeart, "site.depot", Vector2.zero),
                new TowerPart("Part_Transformer", 1, TowerPartSource.SiteHeart, "site.garage", Vector2.zero),
            }), "two parts for one level");
            Assert.Throws<ArgumentException>(() => new TowerParts(_events, "radio_tower", new[]
            {
                new TowerPart("Part_TowerCoil", 1, TowerPartSource.SiteHeart, string.Empty, Vector2.zero),
            }), "a heart part names no site");
            Assert.Throws<ArgumentException>(() => new TowerParts(_events, string.Empty, Array.Empty<TowerPart>()));
        }

        [Test]
        public void Shop_BuildsATowerLevelOnlyOnceItsComponentIsFedToThePort()
        {
            var service = Service(_tower);
            var station = new TestStation { Definition = _tower, Occupied = true };
            var parts = new TowerParts(_events, "radio_tower", new[]
            {
                new TowerPart("Part_TowerCoil", 1, TowerPartSource.SiteHeart, "site.depot", Vector2.zero),
            });
            var shop = new UpgradeShop(service, new IUpgradeStation[] { station }, new CountingSave(), new Board(),
                parts, _ => false);
            Give(10, 10, 10);
            Assert.AreEqual(PurchaseResult.NeedsPart, shop.Purchase("radio_tower"), "materials alone do not do it");
            Assert.AreEqual(0, shop.LevelOf("radio_tower"));

            parts.PickUp(0, Vector3.zero);
            Assert.AreEqual(PurchaseResult.NeedsPart, shop.Purchase("radio_tower"), "on 07's back is not in the port");
            parts.Deliver(0, Vector3.zero);
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase("radio_tower"));
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase("radio_tower"), "L2 has no part here");
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

        private void Give(int metal, int wiring, int optics)
        {
            if (metal > 0)
            {
                _stock.Deliver(SalvageMaterial.Metal, metal);
            }

            if (wiring > 0)
            {
                _stock.Deliver(SalvageMaterial.Wiring, wiring);
            }

            if (optics > 0)
            {
                _stock.Deliver(SalvageMaterial.Optics, optics);
            }
        }

        private UpgradeService Service(params UpgradeDefinition[] definitions)
        {
            return new UpgradeService(_events, _stock, _abilities, _equipment, EquipmentTable.Basin(), definitions);
        }

        private T Create<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _created.Add(asset);
            return asset;
        }

        private TowerParts NoParts()
        {
            return new TowerParts(_events, "radio_tower", Array.Empty<TowerPart>());
        }

        private sealed class TestStation : IUpgradeStation
        {
            public UpgradeDefinition Definition { get; set; }

            /// <summary>Everything it sells, in order; just <see cref="Definition"/> when not set.</summary>
            public UpgradeDefinition[] Catalog { get; set; }

            public bool Occupied { get; set; }

            public Vector3 PadCentre => Vector3.zero;

            public int UpgradeCount => Catalog?.Length ?? (Definition != null ? 1 : 0);

            public bool Sells(UpgradeDefinition definition)
            {
                return Catalog != null ? Array.IndexOf(Catalog, definition) >= 0 : definition == Definition;
            }

            public UpgradeDefinition UpgradeAt(int index)
            {
                return Catalog != null ? Catalog[index] : Definition;
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

            public void Revoke(RoverAbility ability)
            {
                _granted.Remove(ability);
            }
        }

        /// <summary>A blueprint board with every kit pinned except <c>unpinned</c>.</summary>
        private sealed class Board : IBlueprintBoard
        {
            private readonly HashSet<string> _unpinned;

            public Board(params string[] unpinned)
            {
                _unpinned = new HashSet<string>(unpinned);
            }

            public bool IsPinned(string upgradeId)
            {
                return !_unpinned.Contains(upgradeId);
            }

            public void Pin(string upgradeId)
            {
                _unpinned.Remove(upgradeId);
            }
        }

        private sealed class CountingSave : ISaveService
        {
            public int Saves { get; private set; }

            public string FilePath => "memory";

            public bool IsLoaded => true;

            public SaveLoadResult LoadResult => SaveLoadResult.NoSave;

            public event Action Saved
            {
                add { }
                remove { }
            }

            public IDisposable Register(ISaveSection section)
            {
                throw new NotSupportedException("The shop never registers sections.");
            }

            public bool SaveNow()
            {
                Saves++;
                return true;
            }

            public string PutAway()
            {
                throw new NotSupportedException("The shop never starts a new game.");
            }
        }
    }
}
