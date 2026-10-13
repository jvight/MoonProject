using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Cargo and storage on the real components (docs/features/M3-25, 0.5.5 §2): a piece cut with no room in the hold
    /// stays on its wreck, held, its halo amber, and the line says so once a trip; a save keeps it held; the dock
    /// unloads 07 onto home's shelves with no input; a press of E then takes the held piece; a crew crate's remainder
    /// waits in its mouth; and Tilly, once home, fetches a held piece inside the station's reach home.
    /// </summary>
    public sealed class CargoSessions : InputTestFixture
    {
        private const string TillyId = "tilly";
        private const float UnloadTimeout = 6f;
        private const int Stack = 4;

        /// <summary>Slots of one supply shelf (CargoTuning).</summary>
        private const int CargoFixtureSlots = 12;

        private readonly List<FeelBeat> _cargoBeats = new List<FeelBeat>();
        private readonly List<IDisposable> _listening = new List<IDisposable>();
        private InputActionAsset _controls;
        private GameplayFixture _fixture;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            foreach (IDisposable listening in _listening)
            {
                listening.Dispose();
            }

            _listening.Clear();
            _cargoBeats.Clear();
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator ANewGame_Carries07sSpares_AndALoadedSaveKeepsItsOwnHold()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot, spares: true);
            yield return null;
            MaterialStock stock = _fixture.Gameplay.Materials;
            Recipe spares = _fixture.CargoTuning.StarterItems;
            Assert.AreEqual(spares.Metal, stock.Hold.Units(SalvageMaterial.Metal), "07's spares, aboard");
            Assert.AreEqual(spares.Wiring, stock.Hold.Units(SalvageMaterial.Wiring));
            Assert.AreEqual(spares.Total, stock.Total);
            Assert.IsTrue(stock.TrySpend(new Recipe(spares.Metal, 0, 0)), "07 spends its metal");
            Assert.IsTrue(_fixture.Bootstrap.Context.Get<ISaveService>().SaveNow());

            _fixture.Dispose(true);
            yield return null;
            _fixture = GameplayFixture.Boot(_controls, slot, spares: true);
            yield return null;
            stock = _fixture.Gameplay.Materials;
            Assert.AreEqual(0, stock.Hold.Units(SalvageMaterial.Metal), "the save's hold, no spares again");
            Assert.AreEqual(spares.Wiring, stock.Hold.Units(SalvageMaterial.Wiring));
        }

        [UnityTest]
        public IEnumerator Docked_TheTransferTakesAStackAboard_AndUndockedItRefuses()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameContext context = _fixture.Bootstrap.Context;
            var dock = context.Get<IDockState>();
            var transfer = context.Get<IStackTransfer>();
            MaterialStock stock = _fixture.Gameplay.Materials;
            stock.Deliver(SalvageMaterial.Metal, Stack * 2);
            Assert.IsFalse(dock.IsDocked);
            Assert.IsFalse(transfer.TryMove(SalvageMaterial.Metal, false, out int moved), "away from the dock");
            Assert.AreEqual(0, moved);

            _fixture.Rover.Place(_fixture.Gameplay.Home.Dock.Position, 0f);
            yield return Waits.Until(() => dock.IsDocked, UnloadTimeout);
            Assert.IsTrue(dock.IsDocked, "07 rests on the dock");
            Assert.IsTrue(transfer.TryMove(SalvageMaterial.Metal, false, out moved));
            Assert.AreEqual(Stack, moved, "one stack aboard for the trip");
            Assert.AreEqual(Stack, stock.Hold.Units(SalvageMaterial.Metal));
            Assert.AreEqual(Stack, stock.Home.Units(SalvageMaterial.Metal));
        }

        [UnityTest]
        public IEnumerator AFullHold_HoldsTheCutPieceOnItsWreck_ASaveKeepsIt_AndAfterTheDockETakesIt()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            EventBus events = _fixture.Bootstrap.Context.Events;
            var overflowed = new List<CargoOverflowed>();
            Listen(events, overflowed);
            MaterialStock stock = gameplay.Materials;
            int room = stock.Hold.SlotCount * Stack;
            Assert.AreEqual(0, stock.Collect(SalvageMaterial.Metal, room - stock.Hold.TotalUnits), "the hold fills");
            Assert.IsTrue(stock.Hold.IsFull);
            SalvagePiece piece = Wiring(_fixture.FindSite("depot"));
            piece.Progress = 0.97f;
            yield return CutHeld(piece);
            yield return null;

            Assert.AreEqual(SalvagePieceState.Held, piece.State, "cut, but held: no room for it");
            Assert.IsTrue(piece.Body.gameObject.activeSelf, "it stays on its wreck, never on the ground");
            Assert.Greater(piece.Halo.Intensity, 0f, "its halo shell breathes");
            Assert.AreEqual(1, overflowed.Count, "the hatch bumps once");
            Assert.AreEqual(1, gameplay.Bundles.Count);
            Assert.AreEqual(1, stock.Hold.WaitingBundles);
            var chart = _fixture.Bootstrap.Context.Get<MoonProject.Gameplay.Map.IWaitingBundles>();
            Assert.AreEqual(1, chart.Count, "a pencil crate on the chart");
            Assert.Less(Vector3.Distance(chart.Position(0), piece.Body.TransformPoint(Vector3.zero)), 2f,
                "over the held piece");
            Assert.AreEqual(1, _fixture.Events.TickerLine.FindAll(line => line.Value.Key == CargoBundles.TickerFull)
                .Count, "the line once a trip");
            Assert.IsFalse(gameplay.Hints.TryGet(InteractionKind.PickUp, out _), "no room: no prompt");
            Assert.IsTrue(_fixture.Bootstrap.Context.Get<ISaveService>().SaveNow());
            _fixture.Dispose(true);
            yield return null;

            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            gameplay = _fixture.Gameplay;
            stock = gameplay.Materials;
            piece = Wiring(_fixture.FindSite("depot"));
            Assert.AreEqual(SalvagePieceState.Held, piece.State, "a save keeps it held on its wreck");
            Assert.AreEqual(1, gameplay.Bundles.Count);
            Assert.IsTrue(stock.Hold.IsFull);

            var unloaded = new List<CargoUnloaded>();
            Listen(_fixture.Bootstrap.Context.Events, unloaded);
            _fixture.Rover.Place(gameplay.Home.Dock.Position, 0f);
            yield return Waits.Until(() => unloaded.Count > 0, UnloadTimeout);
            Assert.AreEqual(0, stock.Hold.UsedSlots, "the dock unloads with no input");

            int before = _fixture.Events.MaterialSalvaged.Count;
            yield return Beside(piece);
            yield return PickupPress.Take(this, gameplay, 2f, "the held piece");
            yield return Waits.Until(() => piece.State == SalvagePieceState.Taken, 6f);
            Assert.AreEqual(SalvagePieceState.Taken, piece.State, "with room, it folds and flies in");
            Assert.Greater(_fixture.Events.MaterialSalvaged.Count, before);
            Assert.AreEqual(piece.Units, stock.Hold.Units(SalvageMaterial.Wiring));
            Assert.AreEqual(0, gameplay.Bundles.Count);
        }

        [UnityTest]
        public IEnumerator ACrewCrate_OpenedWithAFullHold_HoldsItsStackInItsMouth_AndETakesIt()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            MaterialStock stock = gameplay.Materials;
            int room = stock.Hold.SlotCount * Stack;
            stock.Collect(SalvageMaterial.Optics, room - stock.Hold.TotalUnits);
            Assert.IsTrue(stock.Hold.IsFull);
            CrewCrate crate = OpenKestrelCrate(gameplay);
            yield return null;
            yield return CutCrate(crate);
            Assert.IsTrue(crate.Opened);
            Assert.AreEqual(GameplayFixture.CrateUnits, gameplay.Bundles.UnitsAt(0), "its stack waits in its mouth");
            Assert.Less(Vector3.Distance(gameplay.Bundles.RestPosition(0), crate.Root.position), 1.5f, "in the crate");
            CratesSaveData saved = gameplay.Crates.Capture();
            CollectionAssert.AreEqual(new[] { GameplayFixture.CrateUnits }, saved.held,
                "the crate saves what it holds");

            stock.Hold.Remove(SalvageMaterial.Optics, room);
            yield return null;
            yield return PickupPress.Take(this, gameplay, 2f, "the crate's stack");
            yield return Waits.Until(() => gameplay.Bundles.Count == 0, 6f);
            Assert.AreEqual(GameplayFixture.CrateUnits, stock.Hold.Units(SalvageMaterial.Metal), "taken into the hold");
            CollectionAssert.AreEqual(new[] { 0 }, gameplay.Crates.Capture().held);
        }

        [UnityTest]
        public IEnumerator TillyAtHome_FetchesAHeldPieceInReach_Home()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            EventBus events = _fixture.Bootstrap.Context.Events;
            var fetched = new List<BundleFetched>();
            Listen(events, fetched);
            MaterialStock stock = gameplay.Materials;
            stock.Collect(SalvageMaterial.Metal, stock.Hold.SlotCount * Stack - stock.Hold.TotalUnits);
            SalvagePiece piece = Wiring(_fixture.FindSite("depot"));
            piece.Progress = 0.97f;
            yield return CutHeld(piece);
            Assert.AreEqual(SalvagePieceState.Held, piece.State);
            _fixture.Rover.Place(gameplay.Home.Dock.Position + Vector3.right * 20f, 0f);
            gameplay.Friends.Restore(new FriendsSaveData
            {
                friends = new[] { new FriendSaveData { id = TillyId, state = (int)FriendState.Awake, welcomed = true } },
            });

            const float budget = 40f;
            yield return Waits.Until(() => fetched.Count > 0, budget);
            Assert.AreEqual(1, fetched.Count, "Tilly unclips it and brings it home");
            Assert.AreEqual(piece.Units, fetched[0].Units);
            Assert.AreEqual(piece.Units, stock.Home.Units(SalvageMaterial.Wiring), "on the shelves");
            Assert.AreEqual(SalvagePieceState.Taken, piece.State);
            Assert.AreEqual(0, gameplay.Bundles.Count);
        }

        [UnityTest]
        public IEnumerator TheShelves_ShowTheStash_AndAHoldOfEBuildsTheNextOne()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            EventBus events = _fixture.Bootstrap.Context.Events;
            var built = new List<ShelfBuilt>();
            var buildBeats = new List<FeelBeat>();
            Listen(events, built);
            _listening.Add(events.Subscribe<FeelBeat>(beat =>
            {
                if (beat.Kind == FeelKind.Build)
                {
                    buildBeats.Add(beat);
                }
            }));

            MaterialStock stock = gameplay.Materials;
            stock.Deliver(SalvageMaterial.Metal, 6);
            yield return null;
            Transform[] racks = gameplay.Home.ShelfRacks;
            Assert.AreEqual(3, Crates(racks[0], "Slot_00"), "a full slot of metal: three crates");
            Assert.AreEqual(2, Crates(racks[0], "Slot_01"), "half a slot: two");
            Assert.AreEqual(0, Crates(racks[0], "Slot_02"));
            Assert.IsFalse(racks[2].gameObject.activeSelf, "shelf 3 waits on its socket");

            HomeShelves shelves = gameplay.Shelves;
            _fixture.Rover.Place(racks[2].position + Vector3.forward * 1.5f, 180f);
            yield return null;
            Assert.AreEqual(2, shelves.Candidate);
            Assert.IsFalse(gameplay.Hints.TryGet(InteractionKind.Build, out _), "home has room: no Build prompt");
            stock.Deliver(SalvageMaterial.Metal, 2 * CargoFixtureSlots * Stack - 6);
            yield return null;
            Assert.IsTrue(gameplay.Hints.TryGet(InteractionKind.Build, out InteractionHint hint), "home is full");
            Assert.IsTrue(hint.Ready, "home holds the shelf's metal");
            Press(Keyboard.current.eKey);
            yield return Waits.Until(() => built.Count > 0, _fixture.CargoTuning.BuildHoldSeconds + 1f);
            Release(Keyboard.current.eKey);
            yield return new WaitForSeconds(_fixture.CargoTuning.UnfoldSeconds + 0.2f);
            Assert.AreEqual(2, built[0].SocketIndex);
            Assert.IsTrue(racks[2].gameObject.activeSelf, "the third shelf stands");
            Assert.AreEqual(3 * CargoFixtureSlots, stock.Home.SlotCount);
            Assert.AreEqual(2 * CargoFixtureSlots * Stack - 3, stock.Home.Units(SalvageMaterial.Metal),
                "it took three metal");
            CollectionAssert.AreEqual(new[] { FeelPhase.Impact, FeelPhase.Reward },
                buildBeats.ConvertAll(beat => beat.Phase), "a shelf is only Impact and Reward (the tower Anticipates)");
        }

        [UnityTest]
        public IEnumerator ACrewCrate_OpensWithOneCut_ItsStackGoesToTheHold()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            EventBus events = _fixture.Bootstrap.Context.Events;
            var opened = new List<CrewCrateOpened>();
            Listen(events, opened);
            gameplay.Salvage.Restore(new SalvageSaveData
            {
                sites = new[]
                {
                    new SalvageSiteSaveData
                    {
                        id = WorldAnchorIds.SitePrefix + "kestrel", discovered = true, taken = new[] { 0, 1, 2 },
                    },
                },
            });
            yield return null;
            CrewCrate crate = _fixture.FindSite("kestrel").Crate;
            Vector3 front = crate.CutPoint.position + crate.CutPoint.forward * 3f;
            _fixture.Rover.Place(new Vector3(front.x, 0f, front.z), 0f);
            yield return null;
            yield return null;
            Assert.AreSame(crate, gameplay.Crates.Candidate);
            Assert.IsTrue(gameplay.Hints.TryGet(InteractionKind.Salvage, out InteractionHint hint), "a cut prompt");
            Assert.Greater(crate.Seal.Intensity, 0f, "the seal glows like a seam");

            Press(Keyboard.current.eKey);
            yield return Waits.Until(() => opened.Count > 0, 4f);
            Release(Keyboard.current.eKey);
            Assert.AreEqual(1, opened.Count, "one cut opens it");
            Assert.AreEqual("site.kestrel", opened[0].SiteId);
            Assert.AreEqual("kenji", opened[0].CrewId, "his packing note");
            Assert.AreEqual(GameplayFixture.CrateUnits, gameplay.Materials.Hold.Units(SalvageMaterial.Metal));
            yield return new WaitForSeconds(_fixture.CargoTuning.LidSeconds + 0.1f);
            Assert.Greater(crate.Lid.localPosition.y, 0.5f, "the lid lifted");
            Assert.AreEqual(0f, crate.Seal.Intensity, 1e-4f, "the seal is dark");
            Assert.IsNull(gameplay.Crates.Candidate, "an empty frame now");
            CollectionAssert.AreEqual(new[] { "site.kestrel" }, gameplay.Crates.Capture().opened);
        }

        private static SalvagePiece Wiring(SalvageSite site)
        {
            foreach (SalvagePiece piece in site.Pieces)
            {
                if (piece.Material == SalvageMaterial.Wiring)
                {
                    return piece;
                }
            }

            Assert.Fail($"{site.Id} has no Wiring piece");
            return null;
        }

        /// <summary>07 parks in front of the piece's cut, facing it, and holds E until it is cut through.</summary>
        private IEnumerator CutHeld(SalvagePiece piece)
        {
            yield return Beside(piece);
            Press(Keyboard.current.eKey);
            float timeout = _fixture.Gameplay.Cutter.Plan(3, false).TotalSeconds + 3f;
            yield return Waits.Until(() => piece.State != SalvagePieceState.Attached, timeout);
            Release(Keyboard.current.eKey);
        }

        private IEnumerator Beside(SalvagePiece piece)
        {
            Vector3 cut = piece.CutPosition;
            Vector3 away = piece.CutNormal;
            away.y = 0f;
            away.Normalize();
            Vector3 park = cut + away * 4.5f;
            _fixture.Rover.Place(new Vector3(park.x, 0f, park.z), Mathf.Atan2(-away.x, -away.z) * Mathf.Rad2Deg);
            _fixture.Rover.Aim(cut + away * 9f + Vector3.up * 3.5f, cut);
            yield return null;
            yield return null;
        }

        private CrewCrate OpenKestrelCrate(GameplaySystem gameplay)
        {
            gameplay.Salvage.Restore(new SalvageSaveData
            {
                sites = new[]
                {
                    new SalvageSiteSaveData
                    {
                        id = WorldAnchorIds.SitePrefix + "kestrel", discovered = true, taken = new[] { 0, 1, 2 },
                    },
                },
            });
            CrewCrate crate = _fixture.FindSite("kestrel").Crate;
            Vector3 front = crate.CutPoint.position + crate.CutPoint.forward * 3f;
            _fixture.Rover.Place(new Vector3(front.x, 0f, front.z), 0f);
            return crate;
        }

        private IEnumerator CutCrate(CrewCrate crate)
        {
            yield return null;
            Press(Keyboard.current.eKey);
            yield return Waits.Until(() => crate.Opened, 4f);
            Release(Keyboard.current.eKey);
        }

        private static int Crates(Transform rack, string slot)
        {
            int crates = 0;
            for (int seat = 0; seat < HomeShelves.Seats; seat++)
            {
                crates += rack.Find(slot + "/Stack_" + seat).childCount;
            }

            return crates;
        }

        private void Listen<T>(EventBus events, List<T> into) where T : struct
        {
            _listening.Add(events.Subscribe<T>(into.Add));
        }
    }
}
