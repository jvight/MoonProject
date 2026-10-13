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
    /// Cargo and storage on the real components (docs/features/M3-25): a bundle that does not fit waits beside 07
    /// and the line says so once a trip; the dock unloads 07 onto home's shelves with no input; a press of E takes the
    /// waiting bundle once there is room; and Tilly, once home, fetches a bundle waiting inside the station's reach.
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
        public IEnumerator AFullHold_LeavesTheBundleWaiting_TheDockUnloads_AndEThenTakesIt()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            EventBus events = _fixture.Bootstrap.Context.Events;
            var unloaded = new List<CargoUnloaded>();
            var taken = new List<CargoPickupTaken>();
            Listen(events, unloaded);
            Listen(events, taken);
            _listening.Add(events.Subscribe<FeelBeat>(beat =>
            {
                if (beat.Kind == FeelKind.Cargo)
                {
                    _cargoBeats.Add(beat);
                }
            }));

            MaterialStock stock = gameplay.Materials;
            int room = stock.Hold.SlotCount * Stack;
            Assert.AreEqual(0, stock.Collect(SalvageMaterial.Metal, room), "the hold fills");
            Assert.IsTrue(stock.Hold.IsFull);
            Vector3 socket = _fixture.Rover.Position + Vector3.up;
            Assert.AreEqual(3, stock.Collect(SalvageMaterial.Optics, 3));
            events.Publish(new CargoOverflowed(SalvageMaterial.Optics, 3, socket));
            events.Publish(new CargoOverflowed(SalvageMaterial.Wiring, 2, socket));
            yield return new WaitForSeconds(_fixture.CargoTuning.BundleSettleSeconds + 0.3f);
            CargoBundles bundles = gameplay.Bundles;
            Assert.AreEqual(2, bundles.Count, "both wait beside 07");
            Assert.IsTrue(bundles.IsWaiting(0));
            Assert.AreEqual(2, stock.Hold.WaitingBundles);
            var chart = _fixture.Bootstrap.Context.Get<MoonProject.Gameplay.Map.IWaitingBundles>();
            Assert.AreEqual(2, chart.Count, "a pencil crate on the chart for each");
            Assert.AreEqual(bundles.RestPosition(1), chart.Position(1));
            Assert.AreEqual(1, _fixture.Events.TickerLine.FindAll(line => line.Value.Key == CargoBundles.TickerFull)
                .Count, "the line once a trip");
            Assert.IsFalse(gameplay.Hints.TryGet(InteractionKind.PickUp, out _), "no room: no prompt");
            Vector3 waiting = bundles.RestPosition(0);

            ChargingDock dock = gameplay.Home.Dock;
            _fixture.Rover.Place(dock.Position, 0f);
            yield return Waits.Until(() => unloaded.Count > 0, UnloadTimeout);
            Assert.AreEqual(1, unloaded.Count, "the dock unloads with no input");
            Assert.AreEqual(room, unloaded[0].UnitsMoved);
            Assert.IsFalse(unloaded[0].HomeFull);
            Assert.AreEqual(0, stock.Hold.UsedSlots);
            Assert.AreEqual(room, stock.Home.Units(SalvageMaterial.Metal));
            Assert.AreEqual(FeelPhase.Anticipate, _cargoBeats[0].Phase, "the hatch opens");
            Assert.AreEqual(FeelPhase.Reward, _cargoBeats[_cargoBeats.Count - 1].Phase, "the last crate");
            Assert.AreEqual(stock.Hold.SlotCount, _cargoBeats.FindAll(beat => beat.Phase == FeelPhase.Act).Count,
                "one crate per slot");

            _fixture.Rover.Place(new Vector3(waiting.x, 0f, waiting.z) + Vector3.back * 1.5f, 0f);
            yield return PickupPress.Take(this, gameplay, 2f, "the waiting bundle");
            yield return Waits.Until(() => taken.Count > 0, 4f);
            Assert.AreEqual(1, taken.Count);
            Assert.AreEqual(1, bundles.Count, "one still waits");
            Assert.Greater(stock.Hold.TotalUnits, 0, "taken into the hold");
        }

        [UnityTest]
        public IEnumerator TillyAtHome_FetchesABundleWaitingInReach_ToTheShelves()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            EventBus events = _fixture.Bootstrap.Context.Events;
            var fetched = new List<BundleFetched>();
            Listen(events, fetched);
            Vector3 near = _fixture.Rover.Position + Vector3.forward * 12f;
            events.Publish(new CargoOverflowed(SalvageMaterial.Wiring, 3, near));
            yield return new WaitForSeconds(_fixture.CargoTuning.BundleSettleSeconds + 0.3f);
            Assert.AreEqual(1, gameplay.Bundles.Count);
            gameplay.Friends.Restore(new FriendsSaveData
            {
                friends = new[] { new FriendSaveData { id = TillyId, state = (int)FriendState.Awake, welcomed = true } },
            });

            const float budget = 40f;
            yield return Waits.Until(() => fetched.Count > 0, budget);
            Assert.AreEqual(1, fetched.Count, "Tilly brings it home");
            Assert.AreEqual(3, fetched[0].Units);
            Assert.AreEqual(3, gameplay.Materials.Home.Units(SalvageMaterial.Wiring), "on the shelves");
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
