using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Gameplay.Editor;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    public sealed class BuildingTests
    {
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
            Assert.AreEqual(2, _catalog.Find(StructureIds.Smelter).Ports.Count);
            Assert.AreEqual(2, _catalog.Find(StructureIds.Fabricator).Ports.Count);
            Assert.AreEqual(1, _catalog.Find(StructureIds.StorageRack).Ports.Count);
            Assert.AreEqual(1, _catalog.Find(StructureIds.UpgradeBay).Ports.Count);
            Assert.AreEqual(1, _catalog.Find(StructureIds.AntennaMast).Ports.Count);
            Assert.AreEqual(1, _catalog.Find(StructureIds.VehiclePad).Ports.Count);

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
            StructureDefinition smelter = _catalog.Find(StructureIds.Smelter);
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
                PlacementRules.Evaluate(smelter, new StructurePose(slopeSpot, 0f), basin, placed, roverNearSlope, _tuning));

            TestWorld stepWorld = TestWorld.WithStep();
            Assert.AreEqual(
                PlacementReason.TooSteep,
                PlacementRules.Evaluate(
                    smelter,
                    new StructurePose(new Vector3(10f, 0f, 0f), 0f),
                    stepWorld,
                    placed,
                    new Vector3(5f, 0f, 0f),
                    _tuning));

            placed.Add(new PlacedStructure(1, solar, validPose));
            var overlappingPose = new StructurePose(new Vector3(0.5f, 0f, 5.5f), 35f);
            Assert.AreEqual(
                PlacementReason.Overlaps,
                PlacementRules.Evaluate(smelter, overlappingPose, flat, placed, roverPos, _tuning));
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
            StructureDefinition smelter = _catalog.Find(StructureIds.Smelter);
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
            grid.AddNode(new PowerNode(
                2,
                new Vector3(3f, 0f, 0f),
                smelter.ProduceWatts,
                smelter.ConsumeWatts,
                smelter.StoreWattHours,
                smelter.ConnectionRadius));

            Assert.IsFalse(grid.AreConnected(1, 2), "Battery bank without a source or pylon does not form a grid.");
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
            Assert.AreEqual(1f, grid.GetSupplyRatio(2), 1e-4f, "Connected battery supplies the smelter via pylon.");
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
            StructureDefinition fabricator = _catalog.Find(StructureIds.Fabricator);

            Assert.IsTrue(placedBase.TryPlace(
                fabricator,
                new StructurePose(new Vector3(0f, 0f, 5f), 0f),
                flat,
                roverPos,
                out int unfinishedId,
                out _));
            placedBase.StepWeld(unfinishedId, fabricator.BuildSeconds * 0.4f, roverPos, isWelding: true);

            Assert.IsTrue(placedBase.TryDeconstruct(unfinishedId, out Recipe unfinishedRefund));
            Assert.AreEqual(fabricator.BuildCost, unfinishedRefund, "Removing an unfinished frame refunds full cost.");
            Assert.AreEqual(0, placedBase.Count);
            Assert.AreEqual(1, removedEvents.Count);

            Assert.IsTrue(placedBase.TryPlace(
                fabricator,
                new StructurePose(new Vector3(0f, 0f, 5f), 0f),
                flat,
                roverPos,
                out int finishedId,
                out _));
            Assert.IsTrue(placedBase.StepWeld(finishedId, fabricator.BuildSeconds, roverPos, isWelding: true));

            Assert.IsTrue(placedBase.TryDeconstruct(finishedId, out Recipe finishedRefund));
            Assert.AreEqual(
                BuildJob.CalculateRefund(fabricator.BuildCost, _tuning.DeconstructRefundShare),
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
            StructureDefinition smelter = _catalog.Find(StructureIds.Smelter);

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
                smelter,
                new StructurePose(new Vector3(-3f, 0f, 4f), 270f),
                flat,
                roverPos,
                out int smelterId,
                out _));
            writerBase.StepWeld(smelterId, smelter.BuildSeconds * 0.5f, roverPos, isWelding: true);

            writerBase.StepPower(PowerGrid.SecondsPerHour * 0.5f, sunlight01: 1f);
            Assert.AreEqual(135f, writerBase.PowerGrid.GetStoredWattHours(batteryId), 1e-3f);

            var saveWriter = new SaveService(_tempSaveDir, "building_slot", "0.5.7");
            using (writerBase.RegisterSave(saveWriter))
            {
                Assert.AreEqual(SaveLoadResult.NoSave, saveWriter.Load());
                Assert.IsTrue(saveWriter.SaveNow());
            }

            var readerBase = new PlacedBase(_catalog, _tuning, _events);
            var saveReader = new SaveService(_tempSaveDir, "building_slot", "0.5.7");
            using (readerBase.RegisterSave(saveReader))
            {
                Assert.AreEqual(SaveLoadResult.Loaded, saveReader.Load());
            }

            Assert.AreEqual(3, readerBase.Count);
            PlacedStructure loadedSolar = readerBase.StructureAt(0);
            PlacedStructure loadedBattery = readerBase.StructureAt(1);
            PlacedStructure loadedSmelter = readerBase.StructureAt(2);

            Assert.AreEqual(StructureIds.SolarArray, loadedSolar.Definition.Id);
            Assert.AreEqual(90f, loadedSolar.Pose.Yaw, 1e-4f);
            Assert.IsTrue(loadedSolar.IsBuilt);

            Assert.AreEqual(StructureIds.BatteryBank, loadedBattery.Definition.Id);
            Assert.AreEqual(180f, loadedBattery.Pose.Yaw, 1e-4f);
            Assert.AreEqual(135f, loadedBattery.StoredWattHours, 1e-3f);

            Assert.AreEqual(StructureIds.Smelter, loadedSmelter.Definition.Id);
            Assert.AreEqual(270f, loadedSmelter.Pose.Yaw, 1e-4f);
            Assert.AreEqual(0.5f, loadedSmelter.BuildProgress01, 1e-4f);

            readerBase.StepWeld(loadedSmelter.InstanceId, smelter.BuildSeconds * 0.5f, roverPos, isWelding: true);
            readerBase.StepPower(1f, sunlight01: 1f);
            Assert.AreEqual(1, powerEvents.Count);
            Assert.AreEqual(1f, powerEvents[0].SupplyRatio, 1e-4f);

            Assert.IsTrue(readerBase.TryDeconstruct(loadedSmelter.InstanceId, out Recipe refund));
            Assert.AreEqual(BuildJob.CalculateRefund(smelter.BuildCost, _tuning.DeconstructRefundShare), refund);
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
                for (int i = 1; i <= 4; i++)
                {
                    var stage = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stage.name = "Stage_" + i;
                    stage.transform.SetParent(root.transform, false);
                    stage.GetComponent<MeshRenderer>().sharedMaterial = solidMat;
                }

                StructureView view = root.AddComponent<StructureView>();
                view.Initialize(validHolo, invalidHolo);
                Assert.AreEqual(4, view.StageCount);

                // At 0% progress: stage 1 is hologram, stages 2..4 hidden, SolidStageCount == 0.
                view.SetBuildProgress(0f, false);
                Assert.AreEqual(0, view.SolidStageCount);
                Assert.IsFalse(view.LampsLit);
                Assert.IsTrue(root.transform.Find("Stage_1").gameObject.activeSelf);
                Assert.IsFalse(root.transform.Find("Stage_2").gameObject.activeSelf);

                // At 25% progress (1/4): stage 1 is solid, stage 2 is hologram, stages 3..4 hidden.
                view.SetBuildProgress(0.25f, false);
                Assert.AreEqual(1, view.SolidStageCount);
                Assert.AreSame(solidMat, root.transform.Find("Stage_1").GetComponent<MeshRenderer>().sharedMaterial);
                Assert.IsTrue(root.transform.Find("Stage_2").gameObject.activeSelf);
                Assert.AreSame(validHolo, root.transform.Find("Stage_2").GetComponent<MeshRenderer>().sharedMaterial);
                Assert.IsFalse(root.transform.Find("Stage_3").gameObject.activeSelf);

                // At 50% progress (2/4): stages 1..2 solid, stage 3 hologram, stage 4 hidden.
                view.SetBuildProgress(0.50f, false);
                Assert.AreEqual(2, view.SolidStageCount);
                Assert.AreSame(solidMat, root.transform.Find("Stage_2").GetComponent<MeshRenderer>().sharedMaterial);
                Assert.IsTrue(root.transform.Find("Stage_3").gameObject.activeSelf);
                Assert.AreSame(validHolo, root.transform.Find("Stage_3").GetComponent<MeshRenderer>().sharedMaterial);
                Assert.IsFalse(root.transform.Find("Stage_4").gameObject.activeSelf);

                // At 100% progress (isBuilt = true): all 4 stages solid and standby lamps lit.
                view.SetBuildProgress(1f, true);
                Assert.AreEqual(4, view.SolidStageCount);
                Assert.IsTrue(view.LampsLit);
                Assert.AreSame(solidMat, root.transform.Find("Stage_4").GetComponent<MeshRenderer>().sharedMaterial);
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
        public void BaseStructureRecipes_ExtractsFootprintAndSocketPortsFromPrefabHierarchy()
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
            }
            finally
            {
                Object.DestroyImmediate(testRoot);
            }
        }
    }
}
