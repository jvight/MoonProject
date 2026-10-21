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
    /// The opening v2 on the real gameplay components behind the fake rover (docs/design/opening-v2.md §2–§4): a new
    /// game at half charge, every objective line in order as the world moves on, the lander cargo bay's two scans
    /// teaching the pad (and a ruin's set teaching only when whole), the first charge on a built pad drawing the
    /// lander's reserve until it gives out, the faulted cutter sputtering at the fallen array and the spare igniter
    /// taken from the rack beside it and fitted where 07 stands, the first dusk begun by the dock after a visit to the
    /// seismic station (R5–R6), the milestones FirstCharge, PowerOn, NightReady and
    /// SignalWest, the supply crates' yields, a load that shows its step at once and replays its milestones as
    /// Restored, and the Build key's chip standing on 07 (07 looking toward the apron) while the apron is out of view.
    /// </summary>
    public sealed class OpeningSessions : InputTestFixture
    {
        private const float PressTimeout = 1f;
        private const float Park = 4f;
        private const float SealStandOff = 1.6f;

        // The view camera's spot over 07, how far it looks, and how near 07 a chip on 07 stands.
        private const float CameraRise = 3f;
        private const float CameraReach = 10f;
        private const float OnRoverReach = 1f;

        private readonly List<OpeningMilestoneReached> _milestones = new List<OpeningMilestoneReached>();
        private readonly List<LanderReserveChanged> _reserve = new List<LanderReserveChanged>();
        private readonly List<StoryPartCollected> _parts = new List<StoryPartCollected>();
        private readonly List<RoverSystemChanged> _systems = new List<RoverSystemChanged>();
        private readonly List<CutterRefused> _refused = new List<CutterRefused>();
        private readonly List<OpeningDuskBegan> _dusk = new List<OpeningDuskBegan>();
        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Keyboard _keyboard;
        private System.IDisposable _milestoneToken;
        private System.IDisposable _reserveToken;
        private readonly List<System.IDisposable> _kitTokens = new List<System.IDisposable>();

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _milestoneToken?.Dispose();
            _reserveToken?.Dispose();
            DisposeKitTokens();
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator TheOpening_FromTheWakeToTheWestSignal_OneLineAtATime_AndALoadKeepsIt()
        {
            string slot = BootstrapHarness.NewTestSlot();
            Boot(slot);
            yield return null;
            yield return null;
            OpeningField opening = _fixture.Gameplay.Opening;
            KnownStructures known = _fixture.Gameplay.KnownStructures;
            Assert.AreEqual(0.5f, _fixture.Gameplay.Battery.Model.Charge01, 0.01f, "a new game wakes half empty");
            Assert.IsFalse(known.IsKnown(StructureIds.ChargingPad), "no blueprint is known from the start");
            Assert.AreEqual(OpeningKeys.DriveToLander, opening.Flow.Current);

            yield return PlaceAt(_fixture.World.Anchor("place.lander").Position + new Vector3(0f, 0f, -10f));
            Assert.AreEqual(OpeningKeys.ScanLander, opening.Flow.Current, "at the lander");

            yield return ScanSet("ruin." + ScanTuning.LanderCargoId, 2);
            Assert.IsTrue(known.IsKnown(StructureIds.ChargingPad), "the cargo bay's two fragments teach the pad");
            Assert.AreEqual(OpeningKeys.CollectParts, opening.Flow.Current,
                "the cargo crate gives nothing while its fragments are scanned, nor until 07 takes it: the line says " +
                "to collect the parts");

            RoverStartKit kit = _fixture.Gameplay.StartKit;
            Assert.AreEqual(RoverSystemState.Fault, kit.StateOf(RoverSystem.Cutter), "no igniter: the cutter is FAULT");
            yield return PlaceAt(_fixture.Gameplay.Containers.Find("supply.lander.open").CutPoint.position);
            CargoHold hold = _fixture.Gameplay.Materials.Hold;
            Assert.AreEqual(0, hold.Units(SalvageMaterial.Metal), "in reach, the open crate only offers its parts");
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.PickUp, out _),
                "the open crate offers Pick up");
            yield return TapE();
            Assert.IsFalse(kit.CarriesIgniter, "B4: the charger kit holds no igniter (it waits by the fallen array)");
            Assert.AreEqual(4, hold.Units(SalvageMaterial.Metal), "the charger kit's Metal 4, into an empty hold");
            Assert.AreEqual(2, hold.Units(SalvageMaterial.Wiring), "and its Wiring 2: the pad's whole cost");
            Assert.AreEqual(OpeningKeys.BuildPad, opening.Flow.Current);

            Vector3 apron = _fixture.World.Anchor("place.apron").Position;
            Build(StructureIds.ChargingPad, apron);
            yield return null;
            Assert.AreEqual(OpeningKeys.Charge, opening.Flow.Current);

            yield return PlaceAt(apron);
            BatteryModel battery = _fixture.Gameplay.Battery.Model;
            yield return Waits.Until(() => battery.IsCharging, 5f);
            Assert.AreEqual(ChargerKind.Pad, battery.Charger, "the built pad charges, fed by the lander's port");
            yield return Waits.Until(() => _fixture.Gameplay.PadLock.Raised, 2f);
            Assert.IsTrue(_fixture.Gameplay.PadLock.Raised, "the pad's bars rise round 07 (its dock lock)");
            yield return null;
            Assert.IsTrue(Reached(OpeningMilestone.FirstCharge), "B6: the eclipse may end");
            Assert.IsFalse(opening.Port.Spent, "the reserve is being drawn");
            Assert.AreEqual(string.Empty, opening.Flow.Current, "charging: the lander's 0% speaks next");
            yield return Waits.Until(() => !battery.IsCharging, 60f);
            Assert.AreEqual(1f, battery.Charge01, 0.01f,
                $"full (reserve {opening.Port.ReserveCells:F2}, cells {battery.Cells}, " +
                $"rover {_fixture.Rover.Position}, reserve events {_reserve.Count})");
            Assert.IsTrue(opening.Port.Spent, "the lander's old cells gave out after one charge");
            Assert.AreEqual(0f, _reserve[_reserve.Count - 1].Reserve01, "LANDER RESERVE · 0%");
            yield return null;
            Assert.AreEqual(OpeningKeys.FindSolar, opening.Flow.Current);

            yield return ScanSet("ruin.FallenSolarArray", 3);
            Assert.IsTrue(known.IsKnown(StructureIds.SolarArray));
            Assert.AreEqual(OpeningKeys.CutOptics, opening.Flow.Current, "the array needs an optic");

            yield return FaceTheSeal(_fixture.Gameplay.Containers.Find("supply.array.junction"));
            yield return TapE();
            Assert.AreEqual(RoverSystemState.Fault, kit.StateOf(RoverSystem.Cutter), "no igniter: the cut is refused");
            Assert.IsTrue(kit.Sputtered, "B7: the first cut attempt sputters (CUTTER · NO IGNITER)");
            Assert.AreEqual(OpeningKeys.TakeIgniter, opening.Flow.Current, "stalled: the line points at the rack");
            yield return PlaceAt(_fixture.Gameplay.Containers.Find("supply.array.spares").CutPoint.position);
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.PickUp, out _),
                "the open rack offers Pick up");
            yield return TapE();
            Assert.IsTrue(kit.CarriesIgniter, "a held E takes the spare igniter from the rack");
            Assert.IsTrue(_parts.Exists(part => part.PartId == kit.IgniterPartId), "with its pickup note");
            Assert.AreEqual(OpeningKeys.FitIgniter, opening.Flow.Current, "a field swap: fitted where 07 stands");
            Assert.IsTrue(kit.TryFitIgniter());
            Assert.AreEqual(RoverSystemState.Ok, kit.StateOf(RoverSystem.Cutter), "the cutter works");
            Assert.IsFalse(kit.CarriesIgniter, "the igniter left the hold for the cutter");
            Assert.IsTrue(_systems.Exists(changed => changed.System == RoverSystem.Cutter &&
                                                     changed.State == RoverSystemState.Ok && !changed.Restored),
                "the pilot light and the done tone");
            yield return null;
            Assert.AreEqual(OpeningKeys.CutOptics, opening.Flow.Current, "now the cutter cuts");
            Collect(0, 0, 1);
            yield return null;
            Assert.AreEqual(OpeningKeys.BuildSolar, opening.Flow.Current);

            Build(StructureIds.SolarArray, apron + new Vector3(8f, 0f, 0f));
            yield return Waits.Until(() => Reached(OpeningMilestone.PowerOn), 3f);
            Assert.IsTrue(Reached(OpeningMilestone.PowerOn), "B8: a connected solar array powers the pad");
            Assert.AreEqual(2, _fixture.Gameplay.Recovery.LitLamps(RecoveryTarget.Power),
                "the Power row: the pad and solar lit");
            Assert.AreEqual(OpeningKeys.VisitSite, opening.Flow.Current, "R5, the first free choice: the sites");

            yield return PlaceAt(GameplayFixture.SeismicStation);
            Assert.IsTrue(opening.Dusk.Visited, "R5: 07 visited the 1971 seismic station");
            Assert.IsFalse(opening.DuskBegun, "the afternoon holds until 07 docks");
            yield return null;
            Assert.AreEqual(OpeningKeys.DockForDusk, opening.Flow.Current, "a site visited: the dock ends the day");
            yield return PlaceAt(apron);
            yield return Waits.Until(() => opening.DuskBegun, 5f);
            Assert.IsTrue(opening.DuskBegun, "R6: the dock after the visit begins the first dusk");
            Assert.AreEqual(1, _dusk.Count, "published once");
            Assert.IsFalse(_dusk[0].Restored);
            Assert.AreEqual(OpeningKeys.ScanModule, opening.Flow.Current, "night is coming: the power module");

            Build(StructureIds.BatteryBank, apron + new Vector3(-6f, 0f, 0f));
            yield return Waits.Until(() => Reached(OpeningMilestone.NightReady), 3f);
            Assert.IsTrue(Reached(OpeningMilestone.NightReady));
            Assert.AreEqual(OpeningKeys.AwaitNight, opening.Flow.Current, "night-ready: the base waits");
            _fixture.World.Phase = SkyPhase.Earthlit;
            yield return null;
            yield return null;
            Assert.AreEqual(OpeningKeys.ScanTower, opening.Flow.Current, "after nightfall");

            known.Learn(StructureIds.AntennaMast);
            yield return null;
            Assert.AreEqual(OpeningKeys.BuildMast, opening.Flow.Current);
            Build(StructureIds.AntennaMast, apron + new Vector3(0f, 0f, 8f));
            yield return null;
            yield return null;
            Assert.IsTrue(Reached(OpeningMilestone.SignalWest), "B11: the west signal");
            Assert.AreEqual(OpeningKeys.Transmit, opening.Flow.Current, "the mast stands: transmit from it");
            Assert.AreEqual(4, _milestones.Count, "each milestone once");
            Assert.IsTrue(_fixture.Bootstrap.Context.Get<ISaveService>().SaveNow());
            _fixture.Dispose(true);
            yield return null;

            Boot(slot);
            yield return null;
            yield return null;
            Assert.AreEqual(4, _milestones.Count, "a load replays every reached milestone");
            Assert.IsTrue(_milestones.TrueForAll(reached => reached.Restored), "as Restored");
            Assert.AreEqual(1, _dusk.Count, "a load states the begun dusk once");
            Assert.IsTrue(_dusk[0].Restored, "as Restored, so the sky snaps to it");
            Assert.IsTrue(_fixture.Gameplay.Opening.Port.Spent, "the reserve stays spent");
            Assert.IsTrue(_fixture.Gameplay.StartKit.IgniterFitted, "the cutter keeps its igniter");
            Assert.IsTrue(_systems.Exists(changed => changed.System == RoverSystem.Cutter &&
                                                     changed.State == RoverSystemState.Ok && changed.Restored),
                "the readout starts from the save");
            Assert.AreEqual(OpeningKeys.Transmit, _fixture.Gameplay.Opening.Flow.Current,
                "a load stands where the save stood: the call still waits for its answer");
        }

        [UnityTest]
        public IEnumerator ARuinSet_TeachesOnlyWhenWhole_AndASupplyCrateFillsTheHold()
        {
            Boot(null);
            yield return null;
            KnownStructures known = _fixture.Gameplay.KnownStructures;
            ScanField scans = _fixture.Gameplay.Scans;
            yield return Scan(scans.Find("ruin.FallenSolarArray.0"));
            Assert.IsFalse(known.IsKnown(StructureIds.SolarArray), "one fragment of three teaches nothing yet");
            Assert.AreEqual(1, _fixture.Events.FragmentScanned.Count, "but the note says fragment 1 of 3");
            Assert.AreEqual(StructureIds.SolarArray, _fixture.Events.FragmentScanned[0].Value.BlueprintId,
                "named by the structure it teaches");
            Assert.AreEqual(0, _fixture.Events.FragmentScanned[0].Value.Index);
            Assert.AreEqual(3, _fixture.Events.FragmentScanned[0].Value.Count);

            var stories = new List<StoryScanned>();
            using (_fixture.Bootstrap.Context.Events.Subscribe<StoryScanned>(stories.Add))
            {
                int fragments = _fixture.Events.FragmentScanned.Count;
                int metal = _fixture.Gameplay.Materials.Hold.Units(SalvageMaterial.Metal);
                yield return Scan(scans.Find(StoryScan.IdPrefix + "airlock"));
                Assert.AreEqual(1, stories.Count, "B7: the habitat's airlock panel reads out on the CRT");
                Assert.AreEqual("story.airlock_panel", stories[0].TextKey);
                Assert.AreEqual(fragments, _fixture.Events.FragmentScanned.Count, "a story is no blueprint fragment");
                yield return new WaitForSeconds(_fixture.ScanTuning.DissolveSeconds + 0.2f);
                Assert.AreEqual(metal, _fixture.Gameplay.Materials.Hold.Units(SalvageMaterial.Metal),
                    "and pays nothing");
            }

            SupplyCaches caches = _fixture.Gameplay.Opening.Caches;
            Assert.AreEqual(9, caches.Count, "the fixed sources, the spares rack and 06's field crate; the module is towed (opening-v3 R6)");
            SealedContainer crate = _fixture.Gameplay.Containers.Find("supply.array.junction");
            Assert.IsNotNull(crate, "the fallen array's junction box is a sealed container the cutter opens");
            _fixture.Gameplay.Containers.StandOpen("supply.array.junction");
            _fixture.Bootstrap.Context.Events.Publish(new ContainerOpened("supply.array.junction", crate.Kind,
                crate.CutPoint.position));
            CargoHold hold = _fixture.Gameplay.Materials.Hold;
            Assert.AreEqual(2, hold.Units(SalvageMaterial.Wiring), "the junction box's Wiring 2");
            Assert.AreEqual(0, hold.Units(SalvageMaterial.Metal), "and nothing the solar array does not use");
        }

        [UnityTest]
        public IEnumerator TheCutter_RefusesWithItsFault_UntilTheIgniterIsFitted()
        {
            Boot(null);
            yield return null;
            SealedContainer crate = _fixture.Gameplay.Containers.Find("supply.array.junction");
            yield return FaceTheSeal(crate);
            for (int press = 0; press < 2; press++)
            {
                Press(_keyboard.eKey);
                yield return new WaitForSeconds(0.5f);
                Release(_keyboard.eKey);
                yield return null;
                yield return null;
            }

            Assert.AreEqual(2, _refused.Count, "each press is refused once, with the fault tone");
            Assert.AreEqual(0f, crate.Progress, "and nothing is cut");
            Assert.IsFalse(_fixture.Gameplay.Hints.TryGet(InteractionKind.Salvage, out InteractionHint hint) &&
                           hint.Ready, "the cut hint shows as not ready");
        }

        [UnityTest]
        public IEnumerator TheBuildKey_StandsOn07_WhileTheApronIsBehindTheCamera_And07LooksThere_AskingOnce()
        {
            Boot(BootstrapHarness.NewTestSlot());
            yield return null;
            yield return null;
            var asks = new List<RoverChirpRequested>();
            System.IDisposable askToken = _fixture.Bootstrap.Context.Events.Subscribe<RoverChirpRequested>(chirp =>
            {
                if (chirp.Mood == ChirpMood.Ask)
                {
                    asks.Add(chirp);
                }
            });
            try
            {
                yield return PlaceAt(_fixture.World.Anchor("place.lander").Position + new Vector3(0f, 0f, -10f));
                yield return ScanSet("ruin." + ScanTuning.LanderCargoId, 2);
                yield return PlaceAt(_fixture.Gameplay.Containers.Find("supply.lander.open").CutPoint.position);
                yield return TapE();
                Assert.AreEqual(OpeningKeys.BuildPad, _fixture.Gameplay.Opening.Flow.Current, "the pad's cost is in");

                Vector3 apron = _fixture.Gameplay.Building.ApronCentre.Value;
                Vector3 eye = _fixture.Rover.Position + Vector3.up * CameraRise;
                Vector3 away = new Vector3(eye.x - apron.x, 0f, eye.z - apron.z).normalized;
                _fixture.Rover.Aim(eye, eye + away * CameraReach);
                yield return null;
                yield return null;
                InteractionHints hints = (InteractionHints)_fixture.Gameplay.Hints;
                Assert.AreEqual(InteractionKind.OpenBuild, hints.Primary.Kind, "the Build key is the thing to teach");
                Assert.IsTrue(hints.TryGet(InteractionKind.OpenBuild, out InteractionHint behind));
                Assert.IsTrue(behind.OnRover, "the apron is behind the camera: the chip stands on 07");
                Assert.Less(Vector3.Distance(behind.Position, _fixture.Rover.Position), OnRoverReach,
                    "on 07, which is always in view");
                Assert.AreEqual(apron, behind.Target, "and still points at the apron");
                Assert.IsTrue(_fixture.Rover.TryGetGaze(_fixture.Gameplay.FirstUsePointer, out Vector3 gaze, out _),
                    "07 turns its head toward the apron");
                Assert.AreEqual(apron, gaze);
                Assert.AreEqual(1, asks.Count, "with one short ask");

                _fixture.Rover.Aim(eye, apron);
                yield return null;
                yield return null;
                Assert.IsTrue(hints.TryGet(InteractionKind.OpenBuild, out InteractionHint ahead));
                Assert.IsFalse(ahead.OnRover, "in view, the chip stands at the apron itself");
                Assert.AreEqual(apron, ahead.Position);
                Assert.IsFalse(_fixture.Rover.TryGetGaze(_fixture.Gameplay.FirstUsePointer, out _, out _),
                    "07 stops pointing");

                _fixture.Rover.Aim(eye, eye + away * CameraReach);
                yield return null;
                Assert.AreEqual(1, asks.Count, "07 asks once per kind of key, not every time it points");
            }
            finally
            {
                askToken.Dispose();
            }
        }

        private void Boot(string slot)
        {
            _milestoneToken?.Dispose();
            _reserveToken?.Dispose();
            DisposeKitTokens();
            _milestones.Clear();
            _reserve.Clear();
            _parts.Clear();
            _systems.Clear();
            _refused.Clear();
            _dusk.Clear();
            _fixture = GameplayFixture.Boot(_controls, slot, startKit: true);
            EventBus events = _fixture.Bootstrap.Context.Events;
            _milestoneToken = events.Subscribe<OpeningMilestoneReached>(_milestones.Add);
            _reserveToken = events.Subscribe<LanderReserveChanged>(_reserve.Add);
            _kitTokens.Add(events.Subscribe<StoryPartCollected>(_parts.Add));
            _kitTokens.Add(events.Subscribe<RoverSystemChanged>(_systems.Add));
            _kitTokens.Add(events.Subscribe<CutterRefused>(_refused.Add));
            _kitTokens.Add(events.Subscribe<OpeningDuskBegan>(_dusk.Add));
        }

        private void DisposeKitTokens()
        {
            foreach (System.IDisposable token in _kitTokens)
            {
                token.Dispose();
            }

            _kitTokens.Clear();
        }

        private bool Reached(OpeningMilestone milestone)
        {
            return _milestones.Exists(reached => reached.Milestone == milestone);
        }

        private void Collect(int metal, int wiring, int optics)
        {
            Collect(SalvageMaterial.Metal, metal);
            Collect(SalvageMaterial.Wiring, wiring);
            Collect(SalvageMaterial.Optics, optics);
        }

        private void Collect(SalvageMaterial material, int units)
        {
            if (units > 0)
            {
                _fixture.Gameplay.Materials.Collect(material, units);
            }
        }

        private void Build(string structureId, Vector3 at)
        {
            PlacedBase placed = _fixture.Gameplay.PlacedBase;
            placed.PlaceUnchecked(_fixture.Gameplay.Building.Catalog.Find(structureId), new StructurePose(at, 0f), 1f);
        }

        /// <summary>07 stands before <paramref name="crate"/>'s seal, facing it, the crate its cut candidate.</summary>
        private IEnumerator FaceTheSeal(SealedContainer crate)
        {
            Vector3 front = crate.CutPoint.position + crate.Root.forward * SealStandOff;
            float facing = Quaternion.LookRotation(-crate.Root.forward).eulerAngles.y;
            _fixture.Rover.Place(new Vector3(front.x, 0f, front.z), facing);
            yield return null;
            yield return null;
            Assert.AreEqual(crate, _fixture.Gameplay.Containers.Candidate, $"07 faces the seal of {crate.Id}");
        }

        /// <summary>A short press of E: an open crate gives what it holds, a seal is tried once.</summary>
        private IEnumerator TapE()
        {
            Press(_keyboard.eKey);
            yield return null;
            yield return null;
            Release(_keyboard.eKey);
            yield return null;
        }

        private IEnumerator PlaceAt(Vector3 at)
        {
            _fixture.Rover.Place(new Vector3(at.x, 0f, at.z), 0f);
            yield return null;
            yield return null;
        }

        private IEnumerator ScanSet(string blueprintId, int count)
        {
            ScanField scans = _fixture.Gameplay.Scans;
            for (int i = 0; i < count; i++)
            {
                yield return Scan(scans.Find(blueprintId + "." + i));
            }
        }

        private IEnumerator Scan(ScanFragment fragment)
        {
            Assert.IsNotNull(fragment);
            Vector3 point = fragment.Point;
            Vector3 park = point + Vector3.back * Park;
            _fixture.Rover.Place(new Vector3(park.x, 0f, park.z), 0f);
            _fixture.Rover.Aim(point + Vector3.back * (Park + 4f) + Vector3.up * 3f, point);
            yield return null;
            yield return null;
            Press(_keyboard.eKey);
            yield return Waits.Until(() => fragment.Scanned,
                _fixture.ScanTuning.Seconds(fragment.Size) + PressTimeout);
            Release(_keyboard.eKey);
            yield return null;
            ScanField scans = _fixture.Gameplay.Scans;
            Assert.IsTrue(fragment.Scanned, $"{fragment.FragmentId} at {fragment.Point}: candidate " +
                $"{scans.Candidate?.FragmentId ?? "none"}, scanning {scans.Scanning?.FragmentId ?? "none"}, primary " +
                $"hint {_fixture.Gameplay.Hints.Primary.Kind}");
        }
    }
}
