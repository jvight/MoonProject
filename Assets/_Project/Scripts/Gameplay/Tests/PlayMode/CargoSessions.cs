using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
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

        private void Listen<T>(EventBus events, List<T> into) where T : struct
        {
            _listening.Add(events.Subscribe<T>(into.Add));
        }
    }
}
