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
    /// lander's reserve until it gives out, the milestones FirstCharge, PowerOn, NightReady and SignalWest, the supply
    /// crates' yields, and a load that shows its step at once and replays its milestones as Restored.
    /// </summary>
    public sealed class OpeningSessions : InputTestFixture
    {
        private const float PressTimeout = 1f;
        private const float Park = 4f;

        private readonly List<OpeningMilestoneReached> _milestones = new List<OpeningMilestoneReached>();
        private readonly List<LanderReserveChanged> _reserve = new List<LanderReserveChanged>();
        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Keyboard _keyboard;
        private System.IDisposable _milestoneToken;
        private System.IDisposable _reserveToken;

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
            Assert.AreEqual(OpeningKeys.CutCrates, opening.Flow.Current, "the hold does not cover the pad yet");

            Collect(4, 2, 0);
            yield return null;
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
            yield return Waits.Until(() => !battery.IsCharging, 60f);
            Assert.AreEqual(1f, battery.Charge01, 0.01f, $"full (reserve {opening.Port.ReserveCells:F2}, cells {battery.Cells}, " +
                $"rover {_fixture.Rover.Position}, reserve events {_reserve.Count})");
            Assert.IsTrue(opening.Port.Spent, "the lander's old cells gave out after one charge");
            Assert.AreEqual(0f, _reserve[_reserve.Count - 1].Reserve01, "LANDER RESERVE · 0%");
            yield return null;
            Assert.AreEqual(OpeningKeys.FindSolar, opening.Flow.Current);

            yield return ScanSet("ruin.FallenSolarArray", 3);
            Assert.IsTrue(known.IsKnown(StructureIds.SolarArray));
            Assert.AreEqual(OpeningKeys.CutOptics, opening.Flow.Current, "the array needs an optic");
            Collect(0, 0, 1);
            yield return null;
            Assert.AreEqual(OpeningKeys.BuildSolar, opening.Flow.Current);

            Build(StructureIds.SolarArray, apron + new Vector3(8f, 0f, 0f));
            yield return Waits.Until(() => Reached(OpeningMilestone.PowerOn), 3f);
            Assert.IsTrue(Reached(OpeningMilestone.PowerOn), "B8: a connected solar array powers the pad");
            Assert.AreEqual(OpeningKeys.BuildBatteryBank, opening.Flow.Current, "night is coming");
            Assert.AreEqual(2, _fixture.Gameplay.Recovery.LitLamps(RecoveryTarget.Power),
                "the Power row: the pad and solar lit");

            Build(StructureIds.BatteryBank, apron + new Vector3(-6f, 0f, 0f));
            yield return Waits.Until(() => Reached(OpeningMilestone.NightReady), 3f);
            Assert.IsTrue(Reached(OpeningMilestone.NightReady));
            Assert.AreEqual(string.Empty, opening.Flow.Current, "the first nightfall says nothing");
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
            Assert.AreEqual(string.Empty, opening.Flow.Current, "the pillar in the west is the message");
            Assert.AreEqual(4, _milestones.Count, "each milestone once");
            Assert.IsTrue(_fixture.Bootstrap.Context.Get<ISaveService>().SaveNow());
            _fixture.Dispose(true);
            yield return null;

            Boot(slot);
            yield return null;
            yield return null;
            Assert.AreEqual(4, _milestones.Count, "a load replays every reached milestone");
            Assert.IsTrue(_milestones.TrueForAll(reached => reached.Restored), "as Restored");
            Assert.IsTrue(_fixture.Gameplay.Opening.Port.Spent, "the reserve stays spent");
            Assert.AreEqual(string.Empty, _fixture.Gameplay.Opening.Flow.Current, "the opening stays over");
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

            SupplyCaches caches = _fixture.Gameplay.Opening.Caches;
            Assert.AreEqual(11, caches.Count, "the opening's crates (opening-v2 §4)");
            SealedContainer crate = _fixture.Gameplay.Containers.Find("supply.wake");
            Assert.IsNotNull(crate, "the wake crate is a sealed container the cutter opens");
            _fixture.Gameplay.Containers.StandOpen("supply.wake");
            _fixture.Bootstrap.Context.Events.Publish(new ContainerOpened("supply.wake", crate.Kind,
                crate.CutPoint.position));
            CargoHold hold = _fixture.Gameplay.Materials.Hold;
            Assert.AreEqual(3, hold.Units(SalvageMaterial.Metal), "the wake crate's Metal 3");
            Assert.AreEqual(1, hold.Units(SalvageMaterial.Wiring), "and its Wiring 1");
        }

        private void Boot(string slot)
        {
            _milestoneToken?.Dispose();
            _reserveToken?.Dispose();
            _milestones.Clear();
            _reserve.Clear();
            _fixture = GameplayFixture.Boot(_controls, slot);
            _milestoneToken = _fixture.Bootstrap.Context.Events.Subscribe<OpeningMilestoneReached>(_milestones.Add);
            _reserveToken = _fixture.Bootstrap.Context.Events.Subscribe<LanderReserveChanged>(_reserve.Add);
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
            Assert.IsTrue(fragment.Scanned, fragment.FragmentId);
        }
    }
}
