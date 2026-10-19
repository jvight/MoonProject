using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Editor.Builders;
using MoonProject.Gameplay.Editor;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    public sealed class BuildingTests
    {
        /// <summary>Metres above the pad's origin a ray starts down onto its deck.</summary>
        private const float PadProbeHeight = 3f;

        /// <summary>Metres a pad's deck may rise at most: 07's wheels climb it.</summary>
        private const float PadDeckMostHeight = 0.5f;

        /// <summary>Metres the pad's totem stands at least.</summary>
        private const float PadTotemLeastHeight = 1f;

        private StructureCatalog _catalog;
        private BuildingTuning _tuning;
        private EventBus _events;
        private string _tempSaveDir;

        [SetUp]
        public void SetUp()
        {
            _catalog = BaseStructureRecipes.CreateCatalog();
            _tuning = ScriptableObject.CreateInstance<BuildingTuning>();
            _tuning.Populate(
                minPlacementDistance: 2.0f,
                maxPlacementDistance: 12.0f,
                snapDistance: 0.6f,
                snapAngleDegrees: 20f,
                weldRange: 6.0f,
                deconstructRefundShare: 0.75f);
            _events = new EventBus();
            _tempSaveDir = Path.Combine(Path.GetTempPath(), "moon-building-tests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (_catalog != null)
            {
                for (int i = 0; i < _catalog.Count; i++)
                {
                    StructureDefinition def = _catalog.StructureAt(i);
                    if (def != null)
                    {
                        Object.DestroyImmediate(def);
                    }
                }

                Object.DestroyImmediate(_catalog);
            }

            if (_tuning != null)
            {
                Object.DestroyImmediate(_tuning);
            }

            if (!string.IsNullOrEmpty(_tempSaveDir) && Directory.Exists(_tempSaveDir))
            {
                Directory.Delete(_tempSaveDir, true);
            }
        }

        [Test]
        public void CatalogAndDefinitions_ValidateAllTenStructures_AndReportArtPendingFlag()
        {
            Assert.AreEqual(StructureIds.Count, _catalog.Count);
            Assert.IsNull(_catalog.Validate(out int artPendingCount));
            Assert.AreEqual(0, artPendingCount, "All ten R1 structures have art prefabs and ArtPending == false.");

            for (int i = 0; i < StructureIds.All.Length; i++)
            {
                string id = StructureIds.All[i];
                StructureDefinition def = _catalog.Find(id);
                Assert.IsNotNull(def, "Missing StructureDefinition for " + id);
                Assert.IsFalse(def.ArtPending, "Expected ArtPending == false for " + id);
                Assert.IsNotNull(def.Prefab, "Expected runtime/art prefab for " + id);
                Assert.GreaterOrEqual(def.Ports.Count, 1, "Expected at least one Socket_Power port for " + id);
                Assert.Greater(def.Width, 0.2f, id);
                Assert.Greater(def.Height, 0.2f, id);
                Assert.Greater(def.Depth, 0.2f, id);
            }

            Assert.AreEqual(1, _catalog.Find(StructureIds.ChargingPad).Ports.Count);
            Assert.AreEqual(1, _catalog.Find(StructureIds.SolarArray).Ports.Count);
            Assert.AreEqual(2, _catalog.Find(StructureIds.BatteryBank).Ports.Count);
            Assert.AreEqual(3, _catalog.Find(StructureIds.PowerPylon).Ports.Count);
            Assert.AreEqual(1, _catalog.Find(StructureIds.AntennaMast).Ports.Count);

            var invalidNoPrefab = ScriptableObject.CreateInstance<StructureDefinition>();
            var dummyPrefab = new GameObject("DummyStructurePrefab");
            try
            {
                invalidNoPrefab.Populate(
                    "test_missing_art",
                    "structure.test.name",
                    new Vector3(2f, 1f, 2f),
                    15f,
                    0.25f,
                    0f,
                    50f,
                    0f,
                    0f,
                    new Recipe(2, 1, 0),
                    2f,
                    Array.Empty<StructurePort>(),
                    null,
                    false);

                string missingError = invalidNoPrefab.Validate(out bool pendingWhenFalse);
                Assert.IsFalse(pendingWhenFalse);
                StringAssert.Contains("ArtPending is false", missingError);

                invalidNoPrefab.Populate(
                    "test_with_prefab_and_pending",
                    "structure.test.name",
                    new Vector3(2f, 1f, 2f),
                    15f,
                    0.25f,
                    0f,
                    50f,
                    0f,
                    0f,
                    new Recipe(2, 1, 0),
                    2f,
                    Array.Empty<StructurePort>(),
                    dummyPrefab,
                    true);

                StringAssert.Contains("ArtPending is true", invalidNoPrefab.Validate(out _));

                invalidNoPrefab.Populate(
                    "test_complete_art",
                    "structure.test.name",
                    new Vector3(2f, 1f, 2f),
                    15f,
                    0.25f,
                    0f,
                    50f,
                    0f,
                    0f,
                    new Recipe(2, 1, 0),
                    2f,
                    Array.Empty<StructurePort>(),
                    dummyPrefab,
                    false);

                Assert.IsNull(invalidNoPrefab.Validate(out bool pendingWhenComplete));
                Assert.IsFalse(pendingWhenComplete);
            }
            finally
            {
                Object.DestroyImmediate(invalidNoPrefab);
                Object.DestroyImmediate(dummyPrefab);
            }
        }

        [Test]
        public void PlacementRules_ReturnsValid_OrFirstInvalidReasonForEveryRule()
        {
            StructureDefinition solar = _catalog.Find(StructureIds.SolarArray);
            StructureDefinition mast = _catalog.Find(StructureIds.AntennaMast);
            TestWorld flat = TestWorld.Flat();
            Vector3 roverPos = Vector3.zero;
            var placed = new List<PlacedStructure>();

            var validPose = new StructurePose(new Vector3(0f, 0f, 5f), 0f);
            Assert.AreEqual(
                PlacementReason.Valid,
                PlacementRules.Evaluate(solar, validPose, flat, placed, roverPos, _tuning));

            var outsidePose = new StructurePose(new Vector3(TestWorld.DrivableRadius + 5f, 0f, 0f), 0f);
            Assert.AreEqual(
                PlacementReason.OutsidePlayArea,
                PlacementRules.Evaluate(solar, outsidePose, flat, placed, new Vector3(TestWorld.DrivableRadius - 2f, 0f, 0f), _tuning));

            var tooClosePose = new StructurePose(new Vector3(0f, 0f, 1.0f), 0f);
            Assert.AreEqual(
                PlacementReason.TooCloseTo07,
                PlacementRules.Evaluate(solar, tooClosePose, flat, placed, roverPos, _tuning));

            var tooFarPose = new StructurePose(new Vector3(0f, 0f, 15.0f), 0f);
            Assert.AreEqual(
                PlacementReason.TooFarFrom07,
                PlacementRules.Evaluate(solar, tooFarPose, flat, placed, roverPos, _tuning));

            TestWorld basin = TestWorld.Basin();
            Vector3 slopeSpot = TestWorld.MoundCentre + new Vector3(5f, 0f, 0f);
            Vector3 roverNearSlope = slopeSpot + new Vector3(0f, 0f, -5f);
            Assert.AreEqual(
                PlacementReason.TooSteep,
                PlacementRules.Evaluate(mast, new StructurePose(slopeSpot, 0f), basin, placed, roverNearSlope,
                    _tuning));

            TestWorld stepWorld = TestWorld.WithStep();
            Assert.AreEqual(
                PlacementReason.TooSteep,
                PlacementRules.Evaluate(
                    mast,
                    new StructurePose(new Vector3(10f, 0f, 0f), 0f),
                    stepWorld,
                    placed,
                    new Vector3(5f, 0f, 0f),
                    _tuning));

            placed.Add(new PlacedStructure(1, solar, validPose));
            var overlappingPose = new StructurePose(new Vector3(0.5f, 0f, 5.5f), 35f);
            Assert.AreEqual(
                PlacementReason.Overlaps,
                PlacementRules.Evaluate(mast, overlappingPose, flat, placed, roverPos, _tuning));
        }

        [Test]
        public void SnapSolver_SnapsToPortOrEdgeWhenEnabled_AndKeepsFreePoseWhenDisabledOrOutOfRange()
        {
            var cardinalDef = ScriptableObject.CreateInstance<StructureDefinition>();
            var portlessDef = ScriptableObject.CreateInstance<StructureDefinition>();
            try
            {
                cardinalDef.Populate(
                    "cardinal_box",
                    "structure.cardinal_box.name",
                    new Vector3(1.6f, 1.4f, 1.2f),
                    15f,
                    0.25f,
                    0f,
                    10f,
                    0f,
                    0f,
                    new Recipe(2, 1, 0),
                    2f,
                    new[]
                    {
                        new StructurePort(new Vector3(0f, 0f, 0.85f), Vector3.forward),
                        new StructurePort(new Vector3(0f, 0f, -0.85f), Vector3.back),
                    },
                    null,
                    true);

                TestWorld flat = TestWorld.Flat();
                var anchorPose = new StructurePose(new Vector3(0f, 0f, 5f), 0f);
                var neighbours = new List<PlacedStructure>
                {
                    new PlacedStructure(1, cardinalDef, anchorPose),
                };

                float expectedPortCenterZ = 5f + (cardinalDef.Depth * 0.5f + cardinalDef.Clearance) * 2f;
                var nearFrontPort = new StructurePose(new Vector3(0.2f, 0f, expectedPortCenterZ + 0.25f), 8f);

                StructurePose snappedPortPose = SnapSolver.Solve(
                    cardinalDef,
                    nearFrontPort,
                    neighbours,
                    _tuning.SnapDistance,
                    _tuning.SnapAngleDegrees,
                    true,
                    out bool portSnapped);

                Assert.IsTrue(portSnapped);
                Assert.AreEqual(0f, snappedPortPose.Position.x, 1e-4f);
                Assert.AreEqual(expectedPortCenterZ, snappedPortPose.Position.z, 1e-4f);
                Assert.AreEqual(0f, snappedPortPose.Yaw, 1e-4f);
                Assert.AreEqual(
                    PlacementReason.Valid,
                    PlacementRules.Evaluate(cardinalDef, snappedPortPose, flat, neighbours, new Vector3(0f, 0f, 2f), _tuning));

                StructurePose unsnappedPose = SnapSolver.Solve(
                    cardinalDef,
                    nearFrontPort,
                    neighbours,
                    _tuning.SnapDistance,
                    _tuning.SnapAngleDegrees,
                    false,
                    out bool snappedWhenOff);
                Assert.IsFalse(snappedWhenOff);
                Assert.AreEqual(nearFrontPort, unsnappedPose);

                var farPose = new StructurePose(new Vector3(4f, 0f, 9f), 0f);
                StructurePose outOfRangePose = SnapSolver.Solve(
                    cardinalDef,
                    farPose,
                    neighbours,
                    _tuning.SnapDistance,
                    _tuning.SnapAngleDegrees,
                    true,
                    out bool snappedWhenFar);
                Assert.IsFalse(snappedWhenFar);
                Assert.AreEqual(farPose, outOfRangePose);

                portlessDef.Populate(
                    "portless_box",
                    "structure.portless_box.name",
                    new Vector3(2f, 1f, 2f),
                    15f,
                    0.25f,
                    0f,
                    10f,
                    0f,
                    0f,
                    new Recipe(2, 0, 0),
                    2f,
                    Array.Empty<StructurePort>(),
                    null,
                    true);

                float expectedEdgeX = (cardinalDef.Width * 0.5f + cardinalDef.Clearance) +
                                      (portlessDef.Width * 0.5f + portlessDef.Clearance);
                var nearRightEdge = new StructurePose(new Vector3(expectedEdgeX + 0.2f, 0f, 5.15f), 95f);
                StructurePose edgeSnapped = SnapSolver.Solve(
                    portlessDef,
                    nearRightEdge,
                    neighbours,
                    _tuning.SnapDistance,
                    _tuning.SnapAngleDegrees,
                    true,
                    out bool wasEdgeSnapped);

                Assert.IsTrue(wasEdgeSnapped);
                Assert.AreEqual(expectedEdgeX, edgeSnapped.Position.x, 1e-4f);
                Assert.AreEqual(5f, edgeSnapped.Position.z, 1e-4f);
                Assert.AreEqual(90f, edgeSnapped.Yaw, 1e-4f);
                Assert.AreEqual(
                    PlacementReason.Valid,
                    PlacementRules.Evaluate(portlessDef, edgeSnapped, flat, neighbours, Vector3.zero, _tuning));
            }
            finally
            {
                Object.DestroyImmediate(cardinalDef);
                Object.DestroyImmediate(portlessDef);
            }
        }

        [Test]
        public void PowerGrid_ConnectsAcrossPylonChain_AndRebuildsOnlyWhenNodesChange()
        {
            var grid = new PowerGrid();
            grid.AddNode(new PowerNode(1, new Vector3(0f, 0f, 0f), produceWatts: 200f, consumeWatts: 0f,
                storeWattHours: 0f, connectionRadius: 15f));
            grid.AddNode(new PowerNode(2, new Vector3(12f, 0f, 0f), produceWatts: 0f, consumeWatts: 10f,
                storeWattHours: 0f, connectionRadius: 15f));
            grid.AddNode(new PowerNode(3, new Vector3(24f, 0f, 0f), produceWatts: 0f, consumeWatts: 10f,
                storeWattHours: 0f, connectionRadius: 15f));
            grid.AddNode(new PowerNode(4, new Vector3(36f, 0f, 0f), produceWatts: 0f, consumeWatts: 100f,
                storeWattHours: 0f, connectionRadius: 0f));
            grid.AddNode(new PowerNode(5, new Vector3(80f, 0f, 0f), produceWatts: 0f, consumeWatts: 100f,
                storeWattHours: 0f, connectionRadius: 0f));

            Assert.AreEqual(0, grid.RebuildCount);
            grid.Step(1f, 1f);
            Assert.AreEqual(1, grid.RebuildCount);
            Assert.IsTrue(grid.AreConnected(1, 4));
            Assert.IsFalse(grid.AreConnected(1, 5));
            Assert.AreEqual(1f, grid.GetSupplyRatio(4), 1e-4f);
            Assert.AreEqual(0f, grid.GetSupplyRatio(5), 1e-4f);

            grid.Step(1f, 1f);
            grid.Step(1f, 0.5f);
            Assert.AreEqual(1, grid.RebuildCount, "Stepping the grid must not rebuild union-find connectivity.");

            grid.RemoveNode(2);
            grid.Step(1f, 1f);
            Assert.AreEqual(2, grid.RebuildCount, "Removing a pylon rebuilds connectivity once.");
            Assert.IsFalse(grid.AreConnected(1, 4));
            Assert.AreEqual(0f, grid.GetSupplyRatio(4), 1e-4f);
        }

        [Test]
        public void PowerGrid_ChargesStorageInSun_DischargesDuringEclipse_AndScalesShortPower()
        {
            var grid = new PowerGrid();
            grid.AddNode(new PowerNode(1, new Vector3(0f, 0f, 0f), produceWatts: 180f, consumeWatts: 0f,
                storeWattHours: 0f, connectionRadius: 15f));
            grid.AddNode(new PowerNode(2, new Vector3(5f, 0f, 0f), produceWatts: 0f, consumeWatts: 0f,
                storeWattHours: 200f, connectionRadius: 0f, storedWattHours: 0f));
            grid.AddNode(new PowerNode(3, new Vector3(8f, 0f, 0f), produceWatts: 0f, consumeWatts: 120f,
                storeWattHours: 0f, connectionRadius: 0f));

            grid.Step(PowerGrid.SecondsPerHour, sunlight01: 1f);
            Assert.AreEqual(1f, grid.GetSupplyRatio(3), 1e-4f);
            Assert.AreEqual(60f, grid.GetStoredWattHours(2), 1e-3f, "60W surplus over 1 hour stores 60Wh.");

            grid.Step(PowerGrid.SecondsPerHour * 0.25f, sunlight01: 0f);
            Assert.AreEqual(1f, grid.GetSupplyRatio(3), 1e-4f, "Battery covers 120W demand during eclipse.");
            Assert.AreEqual(30f, grid.GetStoredWattHours(2), 1e-3f, "120W for 0.25h drains 30Wh.");

            grid.Step(PowerGrid.SecondsPerHour * 0.5f, sunlight01: 0f);
            Assert.AreEqual(0f, grid.GetStoredWattHours(2), 1e-3f, "Battery drains to 0Wh.");
            Assert.AreEqual(0.5f, grid.GetSupplyRatio(3), 1e-4f, "30Wh available vs 60Wh needed yields 0.5 supply ratio.");

            grid.Step(60f, sunlight01: 0f);
            Assert.AreEqual(0f, grid.GetSupplyRatio(3), 1e-4f, "Eclipse with empty storage gives 0 supply ratio.");
        }

        [Test]
        public void PowerGrid_IsolatedBatteryBank_GetsNoSupplyConnectionWithoutSourceOrPylon()
        {
            StructureDefinition battery = _catalog.Find(StructureIds.BatteryBank);
            StructureDefinition mast = _catalog.Find(StructureIds.AntennaMast);
            StructureDefinition pylon = _catalog.Find(StructureIds.PowerPylon);

            var grid = new PowerGrid();
            grid.AddNode(new PowerNode(
                1,
                new Vector3(0f, 0f, 0f),
                battery.ProduceWatts,
                battery.ConsumeWatts,
                battery.StoreWattHours,
                battery.ConnectionRadius,
                storedWattHours: 240f));
            // The mast stands past the bank's own reach: only a source or a pylon can bring it into the bank's grid.
            grid.AddNode(new PowerNode(
                2,
                new Vector3(battery.ConnectionRadius + 2f, 0f, 0f),
                mast.ProduceWatts,
                mast.ConsumeWatts,
                mast.StoreWattHours,
                mast.ConnectionRadius));

            Assert.IsFalse(grid.AreConnected(1, 2), "A consumer past the bank's reach is not in its grid.");
            grid.Step(PowerGrid.SecondsPerHour * 0.5f, sunlight01: 0f);
            Assert.AreEqual(0f, grid.GetSupplyRatio(2), 1e-4f, "Consumer next to isolated battery receives 0 supply.");
            Assert.AreEqual(240f, grid.GetStoredWattHours(1), 1e-4f, "Isolated battery does not discharge.");

            grid.AddNode(new PowerNode(
                3,
                new Vector3(1.5f, 0f, 0f),
                pylon.ProduceWatts,
                pylon.ConsumeWatts,
                pylon.StoreWattHours,
                pylon.ConnectionRadius));

            Assert.IsTrue(grid.AreConnected(1, 2), "Pylon in reach connects the battery bank and consumer.");
            grid.Step(PowerGrid.SecondsPerHour * 0.5f, sunlight01: 0f);
            Assert.AreEqual(1f, grid.GetSupplyRatio(2), 1e-4f, "Connected battery supplies the mast via pylon.");
        }

        [Test]
        public void BuildJob_AdvancesWhileInRangeAndWelding_PausesWhen07Leaves_AndRefundsShare()
        {
            var cost = new Recipe(8, 4, 2);
            var job = new BuildJob(cost, buildSeconds: 4f);
            Vector3 structurePos = new Vector3(0f, 0f, 5f);

            Assert.IsFalse(job.IsDone);
            Assert.IsTrue(job.IsPaused);
            Assert.AreEqual(cost, job.Deconstruct(0.75f), "Unfinished frame at 0% returns the full build cost.");

            bool finished = job.Step(1.5f, structurePos, new Vector3(0f, 0f, 2f), weldRange: 5f, isWelding: true);
            Assert.IsFalse(finished);
            Assert.IsFalse(job.IsPaused);
            Assert.IsTrue(job.IsWeldingActive);
            Assert.AreEqual(0.375f, job.Progress01, 1e-4f);
            Assert.AreEqual(cost, job.Deconstruct(0.75f), "Partially welded frame still returns the full build cost.");

            finished = job.Step(10f, structurePos, new Vector3(0f, 0f, 20f), weldRange: 5f, isWelding: true);
            Assert.IsFalse(finished);
            Assert.IsTrue(job.IsPaused);
            Assert.IsFalse(job.IsWeldingActive);
            Assert.AreEqual(0.375f, job.Progress01, 1e-4f, "Leaving weld range pauses progress without loss.");

            finished = job.Step(5f, structurePos, new Vector3(0f, 0f, 2f), weldRange: 5f, isWelding: false);
            Assert.IsFalse(finished);
            Assert.IsTrue(job.IsPaused);
            Assert.AreEqual(0.375f, job.Progress01, 1e-4f, "Stopping the weld beam pauses progress.");

            finished = job.Step(2.5f, structurePos, new Vector3(0f, 0f, 2f), weldRange: 5f, isWelding: true);
            Assert.IsTrue(finished);
            Assert.IsTrue(job.IsDone);
            Assert.IsFalse(job.IsPaused);
            Assert.AreEqual(1f, job.Progress01, 1e-4f);

            Recipe refund = job.Deconstruct(0.75f);
            Assert.AreEqual(6, refund.Metal);
            Assert.AreEqual(3, refund.Wiring);
            Assert.AreEqual(1, refund.Optics);
        }

        [Test]
        public void PlacedBase_RequiresTuningAndEvents_AndRefundsFullCostForUnfinishedOrTunedShareForFinished()
        {
            Assert.Throws<ArgumentNullException>(() => new PlacedBase(null, _tuning, _events));
            Assert.Throws<ArgumentNullException>(() => new PlacedBase(_catalog, null, _events));
            Assert.Throws<ArgumentNullException>(() => new PlacedBase(_catalog, _tuning, null));

            var removedEvents = new List<StructureRemoved>();
            _events.Subscribe<StructureRemoved>(e => removedEvents.Add(e));

            var placedBase = new PlacedBase(_catalog, _tuning, _events);
            TestWorld flat = TestWorld.Flat();
            Vector3 roverPos = Vector3.zero;
            StructureDefinition mast = _catalog.Find(StructureIds.AntennaMast);

            Assert.IsTrue(placedBase.TryPlace(
                mast,
                new StructurePose(new Vector3(0f, 0f, 5f), 0f),
                flat,
                roverPos,
                out int unfinishedId,
                out _));
            placedBase.StepWeld(unfinishedId, mast.BuildSeconds * 0.4f, roverPos, isWelding: true);

            Assert.IsTrue(placedBase.TryDeconstruct(unfinishedId, out Recipe unfinishedRefund));
            Assert.AreEqual(mast.BuildCost, unfinishedRefund, "Removing an unfinished frame refunds full cost.");
            Assert.AreEqual(0, placedBase.Count);
            Assert.AreEqual(1, removedEvents.Count);

            Assert.IsTrue(placedBase.TryPlace(
                mast,
                new StructurePose(new Vector3(0f, 0f, 5f), 0f),
                flat,
                roverPos,
                out int finishedId,
                out _));
            Assert.IsTrue(placedBase.StepWeld(finishedId, mast.BuildSeconds, roverPos, isWelding: true));

            Assert.IsTrue(placedBase.TryDeconstruct(finishedId, out Recipe finishedRefund));
            Assert.AreEqual(
                BuildJob.CalculateRefund(mast.BuildCost, _tuning.DeconstructRefundShare),
                finishedRefund,
                "Removing a finished structure refunds the tuned share.");
            Assert.AreEqual(0, placedBase.Count);
            Assert.AreEqual(2, removedEvents.Count);
        }

        [Test]
        public void PlacedBase_SaveAndLoad_RoundTripsAndSkipsUnknownIdWithError()
        {
            var placedEvents = new List<StructurePlaced>();
            var builtEvents = new List<StructureBuilt>();
            var removedEvents = new List<StructureRemoved>();
            var powerEvents = new List<StructurePowerChanged>();

            _events.Subscribe<StructurePlaced>(e => placedEvents.Add(e));
            _events.Subscribe<StructureBuilt>(e => builtEvents.Add(e));
            _events.Subscribe<StructureRemoved>(e => removedEvents.Add(e));
            _events.Subscribe<StructurePowerChanged>(e => powerEvents.Add(e));

            var writerBase = new PlacedBase(_catalog, _tuning, _events);
            TestWorld flat = TestWorld.Flat();
            Vector3 roverPos = Vector3.zero;

            StructureDefinition solar = _catalog.Find(StructureIds.SolarArray);
            StructureDefinition battery = _catalog.Find(StructureIds.BatteryBank);
            StructureDefinition mast = _catalog.Find(StructureIds.AntennaMast);

            Assert.IsTrue(writerBase.TryPlace(
                solar,
                new StructurePose(new Vector3(0f, 0f, 5f), 90f),
                flat,
                roverPos,
                out int solarId,
                out PlacementReason solarReason));
            Assert.AreEqual(PlacementReason.Valid, solarReason);
            Assert.IsTrue(writerBase.StepWeld(solarId, solar.BuildSeconds, roverPos, isWelding: true));

            Assert.IsTrue(writerBase.TryPlace(
                battery,
                new StructurePose(new Vector3(4f, 0f, 5f), 180f),
                flat,
                roverPos,
                out int batteryId,
                out _,
                initialProgress01: 1f,
                initialStoredWh: 45f));

            Assert.IsTrue(writerBase.TryPlace(
                mast,
                new StructurePose(new Vector3(-3f, 0f, 4f), 270f),
                flat,
                roverPos,
                out int mastId,
                out _));
            writerBase.StepWeld(mastId, mast.BuildSeconds * 0.5f, roverPos, isWelding: true);

            writerBase.StepPower(PowerGrid.SecondsPerHour * 0.5f, sunlight01: 1f);
            Assert.AreEqual(135f, writerBase.PowerGrid.GetStoredWattHours(batteryId), 1e-3f);

            var saveWriter = new SaveService(_tempSaveDir, "building_slot", SaveService.OldestCompatibleContent);
            using (writerBase.RegisterSave(saveWriter))
            {
                Assert.AreEqual(SaveLoadResult.NoSave, saveWriter.Load());
                Assert.IsTrue(saveWriter.SaveNow());
            }

            var readerBase = new PlacedBase(_catalog, _tuning, _events);
            var saveReader = new SaveService(_tempSaveDir, "building_slot", SaveService.OldestCompatibleContent);
            using (readerBase.RegisterSave(saveReader))
            {
                Assert.AreEqual(SaveLoadResult.Loaded, saveReader.Load());
            }

            Assert.AreEqual(3, readerBase.Count);
            PlacedStructure loadedSolar = readerBase.StructureAt(0);
            PlacedStructure loadedBattery = readerBase.StructureAt(1);
            PlacedStructure loadedMast = readerBase.StructureAt(2);

            Assert.AreEqual(StructureIds.SolarArray, loadedSolar.Definition.Id);
            Assert.AreEqual(90f, loadedSolar.Pose.Yaw, 1e-4f);
            Assert.IsTrue(loadedSolar.IsBuilt);

            Assert.AreEqual(StructureIds.BatteryBank, loadedBattery.Definition.Id);
            Assert.AreEqual(180f, loadedBattery.Pose.Yaw, 1e-4f);
            Assert.AreEqual(135f, loadedBattery.StoredWattHours, 1e-3f);

            Assert.AreEqual(StructureIds.AntennaMast, loadedMast.Definition.Id);
            Assert.AreEqual(270f, loadedMast.Pose.Yaw, 1e-4f);
            Assert.AreEqual(0.5f, loadedMast.BuildProgress01, 1e-4f);

            readerBase.StepWeld(loadedMast.InstanceId, mast.BuildSeconds * 0.5f, roverPos, isWelding: true);
            readerBase.StepPower(1f, sunlight01: 1f);
            Assert.AreEqual(1, powerEvents.Count);
            Assert.AreEqual(1f, powerEvents[0].SupplyRatio, 1e-4f);

            Assert.IsTrue(readerBase.TryDeconstruct(loadedMast.InstanceId, out Recipe refund));
            Assert.AreEqual(BuildJob.CalculateRefund(mast.BuildCost, _tuning.DeconstructRefundShare), refund);
            Assert.AreEqual(1, removedEvents.Count);

            LogAssert.Expect(LogType.Error, new Regex("unknown_lunar_widget"));
            readerBase.Restore(new BuildingSaveData
            {
                structures = new[]
                {
                    new PlacedStructureSaveData
                    {
                        id = "unknown_lunar_widget",
                        position = new Vector3(1f, 0f, 5f),
                        yaw = 0f,
                        buildProgress = 1f,
                        storedCharge = 0f,
                    },
                    new PlacedStructureSaveData
                    {
                        id = StructureIds.PowerPylon,
                        position = new Vector3(2f, 0f, 6f),
                        yaw = 45f,
                        buildProgress = 1f,
                        storedCharge = 0f,
                    },
                },
            });

            Assert.AreEqual(1, readerBase.Count);
            Assert.AreEqual(StructureIds.PowerPylon, readerBase.StructureAt(0).Definition.Id);
        }

        [Test]
        public void KnownStructures_StartsEmpty_LearnsStructures_PublishesEvents_AndRoundTripsSaveData()
        {
            var learnedEvents = new List<StructureLearned>();
            _events.Subscribe<StructureLearned>(e => learnedEvents.Add(e));

            var known = new KnownStructures(_catalog, _events);
            Assert.AreEqual(0, known.Count, "A new game starts with zero learned structures.");
            Assert.IsFalse(known.IsKnown(StructureIds.ChargingPad));

            Assert.IsTrue(known.Learn(StructureIds.ChargingPad));
            Assert.IsFalse(known.Learn(StructureIds.ChargingPad), "Learning an already-known structure is idempotent.");
            Assert.IsTrue(known.IsKnown(StructureIds.ChargingPad));
            Assert.AreEqual(1, known.Count);
            Assert.AreEqual(1, learnedEvents.Count);
            Assert.AreEqual(StructureIds.ChargingPad, learnedEvents[0].StructureId);
            Assert.IsFalse(learnedEvents[0].Restored);

            Assert.IsTrue(known.Learn(StructureIds.SolarArray));
            string[] captured = known.Capture();
            CollectionAssert.AreEquivalent(new[] { StructureIds.ChargingPad, StructureIds.SolarArray }, captured);

            var restored = new KnownStructures(_catalog, _events);
            restored.Restore(captured);
            Assert.AreEqual(2, restored.Count);
            Assert.IsTrue(restored.IsKnown(StructureIds.ChargingPad));
            Assert.IsTrue(restored.IsKnown(StructureIds.SolarArray));
            Assert.AreEqual(4, learnedEvents.Count);
            Assert.IsTrue(learnedEvents[2].Restored);
            Assert.IsTrue(learnedEvents[3].Restored);
        }

        [Test]
        public void BuildModeController_TransitionsStates_FiltersKnownStructures_ChecksAndSpendsCost_WeldsAndDeconstructs()
        {
            var known = new KnownStructures(_catalog, _events);
            var placedBase = new PlacedBase(_catalog, _tuning, _events, known);
            MaterialCatalog matCatalog = CargoFixtures.Catalog();
            try
            {
                MaterialStock stock = CargoFixtures.Stock(_events, matCatalog);
                var controller = new BuildModeController(_catalog, known, placedBase, stock, _tuning);
                TestWorld flat = TestWorld.Flat();
                Vector3 roverPos = Vector3.zero;

                Assert.AreEqual(0, _tuning.PlaytestUnlockedIds.Count, "Default BuildingTuning has no playtest unlocks.");
                Assert.AreEqual(BuildModeState.Closed, controller.State);
                Assert.IsFalse(controller.TryOpenMenu(), "Cannot open build menu when 07 knows no structures.");
                Assert.AreEqual(BuildModeState.Closed, controller.State);

                // Configure PlaytestUnlockedIds on BuildingTuning and apply via PlacedBase.
                _tuning.SetPlaytestUnlockedIds(StructureIds.ChargingPad, StructureIds.PowerPylon);
                Assert.AreEqual(2, placedBase.ApplyPlaytestUnlocks(restored: true));
                Assert.AreEqual(0, placedBase.ApplyPlaytestUnlocks(restored: true), "Playtest unlock is idempotent.");
                Assert.IsTrue(controller.TryOpenMenu());
                Assert.AreEqual(BuildModeState.Menu, controller.State);
                Assert.AreEqual(2, controller.AvailableStructures.Count);
                Assert.AreEqual(StructureIds.ChargingPad, controller.SelectedStructure.Id);

                controller.MoveSelection(1);
                Assert.AreEqual(StructureIds.PowerPylon, controller.SelectedStructure.Id);
                controller.MoveSelection(-1);
                Assert.AreEqual(StructureIds.ChargingPad, controller.SelectedStructure.Id);

                Assert.IsTrue(controller.PickSelected());
                Assert.AreEqual(BuildModeState.Placing, controller.State);
                controller.RotateGhost(1f);
                Assert.AreEqual(BuildModeController.DefaultYawStepDegrees, controller.GhostYaw, 1e-4f);

                // Valid spot at (0, 0, 5), but stock is 0: cannot place yet.
                controller.UpdateCandidate(new Vector3(0f, 0f, 5f), roverPos, flat, propBlocked: false);
                Assert.AreEqual(PlacementReason.Valid, controller.CandidateReason);
                Assert.IsFalse(controller.CanAffordSelected);
                Assert.IsFalse(controller.TryConfirmPlacement(flat, roverPos, out _, out _));
                Assert.AreEqual(BuildModeState.Placing, controller.State);

                // Prop blocked check -> PlacementReason.Blocked.
                controller.UpdateCandidate(new Vector3(0f, 0f, 5f), roverPos, flat, propBlocked: true);
                Assert.AreEqual(PlacementReason.Blocked, controller.CandidateReason);

                // Give exact cost for ChargingPad (4 Metal, 2 Wiring) and place on valid terrain.
                Recipe padCost = controller.SelectedStructure.BuildCost;
                stock.Stow(padCost);
                controller.UpdateCandidate(new Vector3(0f, 0f, 5f), roverPos, flat, propBlocked: false);
                Assert.IsTrue(controller.CanPlaceNow);
                Assert.IsTrue(controller.TryConfirmPlacement(flat, roverPos, out int instanceId, out PlacementReason reason));
                Assert.AreEqual(PlacementReason.Valid, reason);
                Assert.AreEqual(BuildModeState.Welding, controller.State);
                Assert.AreEqual(instanceId, controller.ActiveWeldInstanceId);
                Assert.AreEqual(0, stock.Metal, "Full cost is spent immediately at placement.");
                Assert.AreEqual(0, stock.Wiring);

                // Weld halfway, then release weld input -> pauses and returns to Closed.
                float buildSeconds = controller.SelectedStructure.BuildSeconds;
                controller.StepWelding(buildSeconds * 0.5f, roverPos, weldHeld: true, out bool doneHalf, out bool cancelledHalf);
                Assert.IsFalse(doneHalf);
                Assert.IsFalse(cancelledHalf);
                Assert.AreEqual(0.5f, controller.ActiveWeldProgress01, 1e-4f);

                controller.StepWelding(0.1f, roverPos, weldHeld: false, out _, out bool cancelledOnRelease);
                Assert.IsTrue(cancelledOnRelease);
                Assert.AreEqual(BuildModeState.Closed, controller.State);

                // Deconstructing an unfinished structure refunds 100% of cost.
                Assert.IsTrue(controller.StepDeconstruct(2.0f, instanceId, roverPos, deconstructHeld: true, out int deconstructedUnfinished, out Recipe fullRefund));
                Assert.AreEqual(instanceId, deconstructedUnfinished);
                Assert.AreEqual(padCost, fullRefund);
                Assert.AreEqual(padCost.Metal, stock.Metal);
                Assert.AreEqual(padCost.Wiring, stock.Wiring);

                // Place again and weld to 100%, then deconstruct for tuned share (75%).
                Assert.IsTrue(controller.PickStructure(StructureIds.ChargingPad));
                controller.UpdateCandidate(new Vector3(0f, 0f, 5f), roverPos, flat, propBlocked: false);
                Assert.IsTrue(controller.TryConfirmPlacement(flat, roverPos, out int secondId, out _));
                controller.StepWelding(buildSeconds, roverPos, weldHeld: true, out bool doneFull, out _);
                Assert.IsTrue(doneFull);
                Assert.AreEqual(BuildModeState.Closed, controller.State);
                Assert.IsTrue(placedBase.TryGetStructure(secondId, out PlacedStructure builtStruct) && builtStruct.IsBuilt);

                Assert.IsTrue(controller.StepDeconstruct(2.0f, secondId, roverPos, deconstructHeld: true, out _, out Recipe shareRefund));
                Recipe expectedShare = BuildJob.CalculateRefund(padCost, _tuning.DeconstructRefundShare);
                Assert.AreEqual(expectedShare, shareRefund);
                Assert.AreEqual(expectedShare.Metal, stock.Metal);
                Assert.AreEqual(expectedShare.Wiring, stock.Wiring);
            }
            finally
            {
                Object.DestroyImmediate(matCatalog);
            }
        }

        [Test]
        public void StructureView_StageRevealThresholds_SwitchFromHologramToSolid_AndLightStandbyLampsWhenComplete()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Assert.IsNotNull(shader);
            var solidMat = new Material(shader);
            var validHolo = new Material(shader);
            var invalidHolo = new Material(shader);
            var root = new GameObject("TestStagedStructure");
            try
            {
                var stages = new Transform[4];
                var stageRenderers = new MeshRenderer[4];
                for (int i = 1; i <= 4; i++)
                {
                    var stage = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stage.name = "Stage_" + i;
                    stage.transform.SetParent(root.transform, false);
                    var mr = stage.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = solidMat;
                    stages[i - 1] = stage.transform;
                    stageRenderers[i - 1] = mr;
                }

                StructureView view = root.AddComponent<StructureView>();
                LogAssert.Expect(LogType.Error, new Regex("StructureView.*_stageRoots"));
                Assert.IsFalse(view.Initialize(validHolo, invalidHolo));

                view.Populate(
                    StructureIds.ChargingPad,
                    stages,
                    new Renderer[] { stageRenderers[3] },
                    Array.Empty<Collider>());
                Assert.IsTrue(view.Initialize(validHolo, invalidHolo));
                Assert.AreEqual(4, view.StageCount);

                // At 0% progress: stage 1 is hologram, stages 2..4 hidden, SolidStageCount == 0.
                view.SetBuildProgress(0f, false);
                Assert.AreEqual(0, view.SolidStageCount);
                Assert.IsFalse(view.LampsLit);
                Assert.IsTrue(stages[0].gameObject.activeSelf);
                Assert.IsFalse(stages[1].gameObject.activeSelf);

                // At 25% progress (1/4): stage 1 is solid, stage 2 is hologram, stages 3..4 hidden.
                view.SetBuildProgress(0.25f, false);
                Assert.AreEqual(1, view.SolidStageCount);
                Assert.AreSame(solidMat, stageRenderers[0].sharedMaterial);
                Assert.IsTrue(stages[1].gameObject.activeSelf);
                Assert.AreSame(validHolo, stageRenderers[1].sharedMaterial);
                Assert.IsFalse(stages[2].gameObject.activeSelf);

                // At 50% progress (2/4): stages 1..2 solid, stage 3 hologram, stage 4 hidden.
                view.SetBuildProgress(0.50f, false);
                Assert.AreEqual(2, view.SolidStageCount);
                Assert.AreSame(solidMat, stageRenderers[1].sharedMaterial);
                Assert.IsTrue(stages[2].gameObject.activeSelf);
                Assert.AreSame(validHolo, stageRenderers[2].sharedMaterial);
                Assert.IsFalse(stages[3].gameObject.activeSelf);

                // At 100% progress (isBuilt = true): all 4 stages solid and standby lamps lit.
                view.SetBuildProgress(1f, true);
                Assert.AreEqual(4, view.SolidStageCount);
                Assert.IsTrue(view.LampsLit);
                Assert.AreSame(solidMat, stageRenderers[3].sharedMaterial);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(solidMat);
                Object.DestroyImmediate(validHolo);
                Object.DestroyImmediate(invalidHolo);
            }
        }

        [Test]
        public void TheBuiltChargingPad_Is07sToDriveOnto_ItsDeckLowAndItsTotemSolid()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                GameplayAssetPaths.RuntimeStructurePrefab(StructureIds.ChargingPad));
            Assert.IsNotNull(prefab, "run Gameplay/Content");
            GameObject pad = Object.Instantiate(prefab);
            try
            {
                foreach (Transform part in pad.GetComponentsInChildren<Transform>(true))
                {
                    part.gameObject.SetActive(true);
                }

                Physics.SyncTransforms();
                float tallest = 0f;
                var parts = new List<string>();
                foreach (Collider collider in pad.GetComponentsInChildren<Collider>())
                {
                    tallest = Mathf.Max(tallest, collider.bounds.max.y);
                    parts.Add($"{collider.name} {collider.bounds.min.y:F2}..{collider.bounds.max.y:F2}");
                }

                Assert.IsTrue(Physics.Raycast(Vector3.up * PadProbeHeight, Vector3.down, out RaycastHit deck,
                    PadProbeHeight * 2f), "the deck is solid under 07's parking spot");
                Assert.Less(deck.point.y, PadDeckMostHeight,
                    $"07 parks on a low deck, not on {deck.collider.name} ({string.Join(", ", parts)})");
                Assert.Greater(tallest, PadTotemLeastHeight, "the totem still stands solid");
            }
            finally
            {
                Object.DestroyImmediate(pad);
            }
        }

        [Test]
        public void BaseStructureRecipes_ExtractsFootprintAndSocketPorts_AndWiresStageRootsOnStructureView()
        {
            var testRoot = new GameObject("TestArtPrefab");
            try
            {
                var meshChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
                meshChild.name = "Stage_1";
                meshChild.transform.SetParent(testRoot.transform, false);
                meshChild.transform.localScale = new Vector3(3.2f, 1.8f, 2.4f);
                meshChild.transform.localPosition = new Vector3(0f, 0.9f, 0f);

                var sock0 = new GameObject("Socket_Power_0");
                sock0.transform.SetParent(testRoot.transform, false);
                sock0.transform.localPosition = new Vector3(0f, 0.2f, -1.2f);

                var sock1 = new GameObject("Socket_Power_1");
                sock1.transform.SetParent(testRoot.transform, false);
                sock1.transform.localPosition = new Vector3(1.6f, 0.2f, 0f);

                BaseStructureRecipes.ExtractDimensionsAndPorts(
                    testRoot,
                    out Vector3 dimensions,
                    out Vector3 localCenter,
                    out StructurePort[] ports);

                Assert.AreEqual(3.2f, dimensions.x, 1e-3f);
                Assert.AreEqual(1.8f, dimensions.y, 1e-3f);
                Assert.AreEqual(2.4f, dimensions.z, 1e-3f);
                Assert.AreEqual(0.9f, localCenter.y, 1e-3f);
                Assert.AreEqual(2, ports.Length);
                Assert.AreEqual(new Vector3(0f, 0.2f, -1.2f), ports[0].LocalPosition);
                Assert.AreEqual(Vector3.back, ports[0].LocalDirection);
                Assert.AreEqual(new Vector3(1.6f, 0.2f, 0f), ports[1].LocalPosition);
                Assert.AreEqual(Vector3.right, ports[1].LocalDirection);

                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                Assert.IsNotNull(shader);
                var validHolo = new Material(shader);
                var invalidHolo = new Material(shader);
                try
                {
                    using (var scratch = new BuilderScratchScene())
                    {
                        string artPath = GameplayAssetPaths.R1StructurePrefab("ChargingPad");
                        var artPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(artPath);
                        Assert.IsNotNull(artPrefab, "Missing R1 structure art prefab at " + artPath);

                        GameObject assembled = BaseStructureRecipes.AssembleRuntimeHierarchy(
                            scratch,
                            StructureIds.ChargingPad,
                            "ChargingPad",
                            artPrefab);
                        Assert.IsTrue(assembled.TryGetComponent(out StructureView view));
                        Assert.AreEqual(StructureIds.ChargingPad, view.StructureId);
                        Assert.AreEqual(4, view.StageCount);
                        for (int i = 0; i < view.StageCount; i++)
                        {
                            Assert.IsNotNull(view.StageRoots[i]);
                            Assert.AreEqual("Stage_" + (i + 1), view.StageRoots[i].name);
                        }

                        Assert.IsTrue(view.Initialize(validHolo, invalidHolo));
                        Assert.AreEqual(4, view.StageSeams.Count);
                        var seamPoints = new Vector3[3];
                        for (int i = 0; i < view.StageCount; i++)
                        {
                            view.GetStageWeldSeam(i, seamPoints, out Vector3 normal);
                            Assert.AreEqual(1f, normal.sqrMagnitude, 1e-3f);
                            Assert.Greater(Vector3.Distance(seamPoints[0], seamPoints[2]), 0.1f);
                        }

                        view.SetBuildProgress(0f, false);
                        view.GetStageWeldSeam(0, seamPoints, out _);
                        Assert.AreEqual(view.transform.TransformPoint(seamPoints[0]), view.GetActiveWeldPoint());
                    }
                }
                finally
                {
                    Object.DestroyImmediate(validHolo);
                    Object.DestroyImmediate(invalidHolo);
                }
            }
            finally
            {
                Object.DestroyImmediate(testRoot);
            }
        }

        [Test]
        public void RuinScanning_TeachesStructureBlueprints_FiresBeatsAndToasts_RewardsKnownMetal_AndPersists()
        {
            var buildingTuningAsset = AssetDatabase.LoadAssetAtPath<BuildingTuning>(GameplayAssetPaths.BuildingTuning);
            Assert.IsNotNull(buildingTuningAsset);
            CollectionAssert.IsEmpty(
                buildingTuningAsset.StartingKnownIds,
                "A new game starts with 0 blueprints (opening-v2 §10): the lander's cargo bay teaches the pad.");
            CollectionAssert.IsEmpty(
                buildingTuningAsset.PlaytestUnlockedIds,
                "PlaytestUnlockedIds is kept empty in the shipped asset.");

            var scanTuningAsset = AssetDatabase.LoadAssetAtPath<ScanTuning>(GameplayAssetPaths.ScanTuning);
            Assert.IsNotNull(scanTuningAsset);
            var known = new KnownStructures(_catalog, _events);
            var placedBase = new PlacedBase(_catalog, buildingTuningAsset, _events, known);
            Assert.AreEqual(0, known.Count);
            Assert.IsFalse(known.IsKnown(StructureIds.SolarArray));

            var learnedEvents = new List<StructureLearned>();
            var toasts = new List<TickerLine>();
            var buildCues = new List<BuildCue>();
            var chirps = new List<RoverChirpRequested>();
            _events.Subscribe<StructureLearned>(learnedEvents.Add);
            _events.Subscribe<TickerLine>(toasts.Add);
            _events.Subscribe<BuildCue>(buildCues.Add);
            _events.Subscribe<RoverChirpRequested>(chirps.Add);

            MaterialCatalog matCatalog = CargoFixtures.Catalog();
            var ruinInstances = new List<GameObject>();
            try
            {
                MaterialStock stock = CargoFixtures.Stock(_events, matCatalog);
                var save = new SaveService(_tempSaveDir, "ruin_scans_slot", SaveService.OldestCompatibleContent);
                save.Load();
                var scans = new ScanField(scanTuningAsset, _events, save, stock, known);

                for (int i = 0; i < scanTuningAsset.Ruins.Count; i++)
                {
                    RuinScanSet set = scanTuningAsset.Ruins[i];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/_Project/Generated/Art/R1/Models/Ruins/" + set.RuinId + ".prefab");
                    Assert.IsNotNull(prefab, "Missing R1 ruin prefab for " + set.RuinId);
                    GameObject instance = Object.Instantiate(prefab);
                    instance.name = set.RuinId;
                    ruinInstances.Add(instance);
                    Assert.IsNull(scans.AddRuin(set, instance.transform));
                }

                // A set teaches only when whole (opening-v2 §1): the fallen array's 3 fragments teach SolarArray.
                for (int i = 0; i < 2; i++)
                {
                    ScanFragment part = scans.Find(ScanField.FragmentId("ruin.FallenSolarArray", i));
                    Assert.IsNotNull(part);
                    Assert.IsTrue(scans.TryGetSetStep(part, out int step, out int of), "an unlearned set piece counts");
                    Assert.AreEqual(i + 1, step, "the scan would make it this piece of the set");
                    Assert.AreEqual(3, of);
                    scans.CompleteTarget(part, 1f + i);
                    Assert.AreEqual(0, part.Owed, "new knowledge pays no Metal");
                }

                Assert.IsFalse(known.IsKnown(StructureIds.SolarArray), "two fragments of three teach nothing yet");
                Assert.AreEqual(0, learnedEvents.Count);
                scans.CompleteTarget(scans.Find(ScanField.FragmentId("ruin.FallenSolarArray", 2)), 3f);
                Assert.IsTrue(known.IsKnown(StructureIds.SolarArray));
                Assert.IsFalse(scans.TryGetSetStep(scans.Find(ScanField.FragmentId("ruin.FallenSolarArray", 2)),
                    out _, out _), "a learned set counts no more");
                Assert.AreEqual(1, learnedEvents.Count);
                Assert.AreEqual(StructureIds.SolarArray, learnedEvents[0].StructureId);
                Assert.AreEqual(1, toasts.Count);
                Assert.AreEqual("scan.complete", toasts[0].Key);
                Assert.AreEqual(StructureIds.NameKey(StructureIds.SolarArray), toasts[0].Argument);
                Assert.IsTrue(toasts[0].ArgumentIsKey);
                Assert.AreEqual(1, buildCues.Count);
                Assert.AreEqual(BuildCueKind.StageComplete, buildCues[0].Kind);
                Assert.AreEqual(1, chirps.Count);
                Assert.AreEqual(ChirpMood.Proud, chirps[0].Mood);

                // The habitat's 2 fragments teach BatteryBank.
                scans.CompleteTarget(scans.Find(ScanField.FragmentId("ruin.HabitatModules", 0)), 20f);
                Assert.IsFalse(known.IsKnown(StructureIds.BatteryBank));
                scans.CompleteTarget(scans.Find(ScanField.FragmentId("ruin.HabitatModules", 1)), 21f);
                Assert.IsTrue(known.IsKnown(StructureIds.BatteryBank));

                // A set whose structure is already known pays Metal instead and fires no duplicate unlock.
                known.Learn(StructureIds.AntennaMast);
                int learned = learnedEvents.Count;
                ScanFragment tower0 = scans.Find(ScanField.FragmentId("ruin.RadioTower", 0));
                ScanFragment tower1 = scans.Find(ScanField.FragmentId("ruin.RadioTower", 1));
                scans.CompleteTarget(tower0, 30f);
                scans.CompleteTarget(tower1, 31f);
                Assert.AreEqual(scanTuningAsset.KnownMetal, tower0.Owed);
                Assert.AreEqual(scanTuningAsset.KnownMetal, tower1.Owed);
                Assert.AreEqual(learned, learnedEvents.Count,
                    "Already-known blueprint does not fire a duplicate unlock.");

                // Round-trip save/load of both ScansSaveData and BuildingSaveData.
                ScansSaveData savedScans = scans.Capture();
                BuildingSaveData savedBuilding = placedBase.Capture();

                var loadedKnown = new KnownStructures(_catalog, _events);
                var loadedBase = new PlacedBase(_catalog, buildingTuningAsset, _events, loadedKnown);
                loadedBase.Restore(savedBuilding);
                Assert.IsTrue(loadedKnown.IsKnown(StructureIds.SolarArray),
                    "Learned blueprints are saved in BuildingSaveData.known and restored.");
                Assert.IsTrue(loadedKnown.IsKnown(StructureIds.BatteryBank));
                Assert.IsTrue(loadedKnown.IsKnown(StructureIds.AntennaMast));
                Assert.IsFalse(loadedKnown.IsKnown(StructureIds.ChargingPad), "the lander's cargo bay was not scanned");

                var loadedScans = new ScanField(scanTuningAsset, _events, save, stock, loadedKnown);
                for (int i = 0; i < scanTuningAsset.Ruins.Count; i++)
                {
                    loadedScans.AddRuin(scanTuningAsset.Ruins[i], ruinInstances[i].transform);
                }

                loadedScans.Restore(savedScans);
                Assert.IsTrue(loadedScans.Find(ScanField.FragmentId("ruin.FallenSolarArray", 0)).Scanned);
                Assert.AreEqual(scanTuningAsset.KnownMetal * 2, stock.Metal,
                    "Owed metal from the 2 known tower scans arrives on restore.");
            }
            finally
            {
                for (int i = 0; i < ruinInstances.Count; i++)
                {
                    if (ruinInstances[i] != null)
                    {
                        Object.DestroyImmediate(ruinInstances[i]);
                    }
                }

                Object.DestroyImmediate(matCatalog);
            }
        }

        [Test]
        public void BuildModeController_TapPlaceStaysInWeldingAtZeroProgress_AndClosingAtZeroRefundsWithoutOrphanFrame()
        {
            var known = new KnownStructures(_catalog, _events);
            var placedBase = new PlacedBase(_catalog, _tuning, _events, known);
            MaterialCatalog matCatalog = CargoFixtures.Catalog();
            try
            {
                MaterialStock stock = CargoFixtures.Stock(_events, matCatalog);
                var controller = new BuildModeController(_catalog, known, placedBase, stock, _tuning);
                TestWorld flat = TestWorld.Flat();
                Vector3 roverPos = Vector3.zero;

                known.Learn(StructureIds.ChargingPad);
                Assert.IsTrue(controller.PickStructure(StructureIds.ChargingPad));
                Recipe padCost = controller.SelectedStructure.BuildCost;
                stock.Stow(padCost);
                controller.UpdateCandidate(new Vector3(0f, 0f, 5f), roverPos, flat, propBlocked: false);
                Assert.IsTrue(controller.TryConfirmPlacement(flat, roverPos, out int firstId, out _));
                Assert.AreEqual(BuildModeState.Welding, controller.State);
                Assert.AreEqual(0, stock.Metal);

                // Releasing E on the next frame after tapping E to place (weldHeld = false at 0% progress)
                // keeps BuildModeController in Welding state waiting for Hold E, instead of canceling.
                Assert.IsTrue(controller.StepWelding(0.05f, roverPos, weldHeld: false, out bool completed, out bool cancelled));
                Assert.IsFalse(completed);
                Assert.IsFalse(cancelled, "Releasing E at 0% after tap-place must not cancel welding.");
                Assert.AreEqual(BuildModeState.Welding, controller.State);
                Assert.AreEqual(firstId, controller.ActiveWeldInstanceId);

                // Backing out (Close) before any weld progress removes the 0% frame and refunds 100% of cost.
                controller.Close();
                Assert.AreEqual(BuildModeState.Closed, controller.State);
                Assert.AreEqual(0, placedBase.Count, "Closing before welding must not leave an orphan 0% frame.");
                Assert.AreEqual(padCost.Metal, stock.Metal, "Closing at 0% refunds full Metal cost.");
                Assert.AreEqual(padCost.Wiring, stock.Wiring, "Closing at 0% refunds full Wiring cost.");
            }
            finally
            {
                Object.DestroyImmediate(matCatalog);
            }
        }

        [Test]
        public void BuildModeController_AFramePlacedBeyondWeldRange_StaysStandingUnwelded_AndHoldingEInRangeWeldsIt()
        {
            var known = new KnownStructures(_catalog, _events);
            var placedBase = new PlacedBase(_catalog, _tuning, _events, known);
            MaterialCatalog matCatalog = CargoFixtures.Catalog();
            try
            {
                MaterialStock stock = CargoFixtures.Stock(_events, matCatalog);
                var controller = new BuildModeController(_catalog, known, placedBase, stock, _tuning);
                TestWorld flat = TestWorld.Flat();
                Vector3 roverPos = Vector3.zero;

                known.Learn(StructureIds.ChargingPad);
                Assert.IsTrue(controller.PickStructure(StructureIds.ChargingPad));
                stock.Stow(controller.SelectedStructure.BuildCost);
                controller.UpdateCandidate(new Vector3(0f, 0f, 7f), roverPos, flat, propBlocked: false);
                Assert.IsTrue(controller.TryConfirmPlacement(flat, roverPos, out int id, out _));

                controller.StepWelding(0.05f, roverPos, weldHeld: true, out bool completed, out bool cancelled);
                Assert.IsFalse(completed);
                Assert.IsTrue(cancelled, "A frame beyond the weld range closes build mode.");
                Assert.AreEqual(BuildModeState.Closed, controller.State);
                Assert.AreEqual(1, placedBase.Count, "The frame stays placed: pressing E to place is never silent.");
                Assert.AreEqual(0, stock.Metal, "The frame stays paid for: nothing is refunded.");
                Assert.IsTrue(placedBase.TryGetStructure(id, out PlacedStructure frame));
                Assert.IsFalse(frame.IsBuilt);
                Assert.AreEqual(0f, frame.BuildProgress01, 1e-5f);

                Vector3 nearer = new Vector3(0f, 0f, 2f);
                Assert.IsTrue(controller.TryBeginResumeWeld(id, nearer), "07 five metres from the frame may resume it.");
                float buildSeconds = frame.Definition.BuildSeconds;
                controller.StepWelding(buildSeconds * 0.5f, nearer, weldHeld: true, out _, out bool cancelledNear);
                Assert.IsFalse(cancelledNear);
                Assert.AreEqual(0.5f, controller.ActiveWeldProgress01, 1e-4f, "Holding E in range welds the frame.");
            }
            finally
            {
                Object.DestroyImmediate(matCatalog);
            }
        }

        [Test]
        public void GhostAim_TheApronSnapNeverPutsTheFrameBeyondTheWeldReach()
        {
            Vector3 apron = new Vector3(3f, 0f, -2f);
            float reach = GhostAim.SnapReach(_tuning);
            Assert.Less(reach, _tuning.WeldRange, "The snap keeps a margin inside the weld range.");
            int snapped = 0;
            for (float distance = 0f; distance <= 14f; distance += 0.25f)
            {
                for (float yaw = 0f; yaw < 360f; yaw += 15f)
                {
                    Vector3 rover = apron + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * distance;
                    for (float heading = 0f; heading < 360f; heading += 15f)
                    {
                        Vector3 forward = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
                        Vector3 point = GhostAim.Point(rover, forward, apron, _tuning);
                        if (point == apron)
                        {
                            snapped++;
                            Assert.LessOrEqual(SurfaceRules.HorizontalDistance(rover, apron), reach + 1e-4f);
                        }
                    }
                }
            }

            Assert.Greater(snapped, 0, "The bay catches the ghost when 07 stands close and faces it.");
            Assert.AreEqual(apron + Vector3.forward * _tuning.GhostAheadMetres,
                GhostAim.Point(apron, Vector3.forward, null, _tuning), "Without an apron the ghost just rides ahead.");
        }

        [Test]
        public void ALiveBuild_IsPrintedInPlace_ItsLayerLineRising_AndItsGuideGoesOnceItStands()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Assert.IsNotNull(shader);
            var validHolo = new Material(shader);
            var invalidHolo = new Material(shader);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                GameplayAssetPaths.RuntimeStructurePrefab(StructureIds.ChargingPad));
            Assert.IsNotNull(prefab);
            GameObject pad = Object.Instantiate(prefab);
            try
            {
                Assert.IsTrue(pad.TryGetComponent(out StructureView view));
                Assert.IsTrue(view.Initialize(validHolo, invalidHolo));
                view.SetPlacedState(0f, instant: true);
                view.ConfigureLiveBuild(true, _tuning);
                view.SetPlacedState(0.10f, instant: false);
                float y10 = view.GetActiveWeldPoint().y;
                view.SetPlacedState(0.50f, instant: false);
                float y50 = view.GetActiveWeldPoint().y;
                view.SetPlacedState(0.90f, instant: false);
                float y90 = view.GetActiveWeldPoint().y;
                Assert.Greater(y50, y10, "the layer line the weld rides rises with the print (10% -> 50%)");
                Assert.Greater(y90, y50, "the layer line the weld rides rises with the print (50% -> 90%)");

                Transform guide = pad.transform.Find("PrintLayerGuide");
                Assert.IsNotNull(guide, "the print guide's bar rides the layer line");
                Assert.IsTrue(guide.gameObject.activeSelf);

                view.SetPlacedState(1f, instant: false);
                Assert.IsFalse(guide.gameObject.activeSelf, "the guide is gone once the structure stands");
                Assert.IsTrue(view.LampsLit);
            }
            finally
            {
                Object.DestroyImmediate(pad);
                Object.DestroyImmediate(validHolo);
                Object.DestroyImmediate(invalidHolo);
            }
        }
    }
}
