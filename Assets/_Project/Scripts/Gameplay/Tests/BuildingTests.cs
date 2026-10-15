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
            Assert.AreEqual(StructureIds.Count, artPendingCount);

            Assert.IsNotNull(_catalog.Find(StructureIds.ChargingPad));
            Assert.IsNotNull(_catalog.Find(StructureIds.SolarArray));
            Assert.IsNotNull(_catalog.Find(StructureIds.BatteryBank));
            Assert.IsNotNull(_catalog.Find(StructureIds.PowerPylon));
            Assert.IsNotNull(_catalog.Find(StructureIds.Smelter));
            Assert.IsNotNull(_catalog.Find(StructureIds.Fabricator));
            Assert.IsNotNull(_catalog.Find(StructureIds.StorageRack));
            Assert.IsNotNull(_catalog.Find(StructureIds.UpgradeBay));
            Assert.IsNotNull(_catalog.Find(StructureIds.AntennaMast));
            Assert.IsNotNull(_catalog.Find(StructureIds.VehiclePad));

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
            StructureDefinition solar = _catalog.Find(StructureIds.SolarArray);
            TestWorld flat = TestWorld.Flat();
            var anchorPose = new StructurePose(new Vector3(0f, 0f, 5f), 0f);
            var neighbours = new List<PlacedStructure>
            {
                new PlacedStructure(1, solar, anchorPose),
            };

            float expectedPortCenterZ = 5f + (solar.Depth * 0.5f + solar.Clearance) * 2f;
            var nearFrontPort = new StructurePose(new Vector3(0.2f, 0f, expectedPortCenterZ + 0.25f), 8f);

            StructurePose snappedPortPose = SnapSolver.Solve(
                solar,
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
                PlacementRules.Evaluate(solar, snappedPortPose, flat, neighbours, new Vector3(0f, 0f, 2f), _tuning));

            StructurePose unsnappedPose = SnapSolver.Solve(
                solar,
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
                solar,
                farPose,
                neighbours,
                _tuning.SnapDistance,
                _tuning.SnapAngleDegrees,
                true,
                out bool snappedWhenFar);
            Assert.IsFalse(snappedWhenFar);
            Assert.AreEqual(farPose, outOfRangePose);

            var portlessDef = ScriptableObject.CreateInstance<StructureDefinition>();
            try
            {
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

                float expectedEdgeX = (solar.Width * 0.5f + solar.Clearance) +
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
        public void BuildJob_AdvancesWhileInRangeAndWelding_PausesWhen07Leaves_AndRefundsShare()
        {
            var cost = new Recipe(8, 4, 2);
            var job = new BuildJob(cost, buildSeconds: 4f);
            Vector3 structurePos = new Vector3(0f, 0f, 5f);

            Assert.IsFalse(job.IsDone);
            Assert.IsTrue(job.IsPaused);

            bool finished = job.Step(1.5f, structurePos, new Vector3(0f, 0f, 2f), weldRange: 5f, isWelding: true);
            Assert.IsFalse(finished);
            Assert.IsFalse(job.IsPaused);
            Assert.IsTrue(job.IsWeldingActive);
            Assert.AreEqual(0.375f, job.Progress01, 1e-4f);

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
        public void PlacedBase_SaveAndLoad_RoundTripsAndSkipsUnknownIdWithError()
        {
            var events = new EventBus();
            var placedEvents = new List<StructurePlaced>();
            var builtEvents = new List<StructureBuilt>();
            var removedEvents = new List<StructureRemoved>();
            var powerEvents = new List<StructurePowerChanged>();

            events.Subscribe<StructurePlaced>(e => placedEvents.Add(e));
            events.Subscribe<StructureBuilt>(e => builtEvents.Add(e));
            events.Subscribe<StructureRemoved>(e => removedEvents.Add(e));
            events.Subscribe<StructurePowerChanged>(e => powerEvents.Add(e));

            var writerBase = new PlacedBase(_catalog, _tuning, events);
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

            var readerBase = new PlacedBase(_catalog, _tuning, events);
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
    }
}
