using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Pure runtime coordinator for 07's player-built base (0.5.7). Owns the placed structures, their <see cref="BuildJob"/>
    /// states, and the <see cref="PowerGrid"/>; publishes <see cref="StructurePlaced"/>, <see cref="StructureBuilt"/>,
    /// <see cref="StructureRemoved"/>, and <see cref="StructurePowerChanged"/> events; and captures/restores versioned
    /// save data (<see cref="GameplaySaveKeys.Building"/>).
    /// </summary>
    public sealed class PlacedBase
    {
        private const float SupplyChangeEpsilon = 1e-4f;
        private const float DefaultMinDistance = 2.0f;
        private const float DefaultMaxDistance = 12.0f;
        private const float DefaultSnapDistance = 0.6f;
        private const float DefaultSnapAngle = 20f;
        private const float DefaultWeldRange = 6.0f;
        private const float DefaultRefundShare = 0.75f;

        private readonly StructureCatalog _catalog;
        private readonly BuildingTuning _tuning;
        private readonly EventBus _events;
        private readonly PowerGrid _powerGrid = new PowerGrid();
        private readonly List<PlacedStructure> _structures = new List<PlacedStructure>();
        private readonly List<BuildJob> _jobs = new List<BuildJob>();
        private readonly List<bool> _powerPublished = new List<bool>();
        private int _nextInstanceId = 1;

        public PlacedBase(StructureCatalog catalog, BuildingTuning tuning = null, EventBus events = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _tuning = tuning;
            _events = events;
        }

        public IReadOnlyList<PlacedStructure> Structures => _structures;

        public int Count => _structures.Count;

        public PowerGrid PowerGrid => _powerGrid;

        public PlacedStructure StructureAt(int index)
        {
            return _structures[index];
        }

        public bool TryGetStructure(int instanceId, out PlacedStructure structure)
        {
            int index = IndexOf(instanceId);
            if (index >= 0)
            {
                structure = _structures[index];
                return true;
            }

            structure = default;
            return false;
        }

        public bool TryGetBuildJob(int instanceId, out BuildJob job)
        {
            int index = IndexOf(instanceId);
            if (index >= 0)
            {
                job = _jobs[index];
                return true;
            }

            job = null;
            return false;
        }

        /// <summary>Solves soft snapping for <paramref name="definition"/> against currently placed structures.</summary>
        public StructurePose SolveSnap(StructureDefinition definition, StructurePose freePose, bool snapEnabled = true)
        {
            float snapDist = _tuning != null ? _tuning.SnapDistance : DefaultSnapDistance;
            float snapAngle = _tuning != null ? _tuning.SnapAngleDegrees : DefaultSnapAngle;
            return SnapSolver.Solve(definition, freePose, _structures, snapDist, snapAngle, snapEnabled);
        }

        /// <summary>Evaluates whether <paramref name="definition"/> may be placed at <paramref name="pose"/>.</summary>
        public PlacementReason EvaluatePlacement(
            StructureDefinition definition,
            StructurePose pose,
            ITerrainQuery terrain,
            Vector3 roverPosition)
        {
            float minDist = _tuning != null ? _tuning.MinPlacementDistance : DefaultMinDistance;
            float maxDist = _tuning != null ? _tuning.MaxPlacementDistance : DefaultMaxDistance;
            return PlacementRules.Evaluate(definition, pose, terrain, _structures, roverPosition, minDist, maxDist);
        }

        /// <summary>
        /// Validates placement with <see cref="PlacementRules"/> and, when valid, places the structure and publishes
        /// <see cref="StructurePlaced"/> (plus <see cref="StructureBuilt"/> when <paramref name="initialProgress01"/>
        /// is already 1).
        /// </summary>
        public bool TryPlace(
            StructureDefinition definition,
            StructurePose pose,
            ITerrainQuery terrain,
            Vector3 roverPosition,
            out int instanceId,
            out PlacementReason reason,
            float initialProgress01 = 0f,
            float initialStoredWh = 0f)
        {
            reason = EvaluatePlacement(definition, pose, terrain, roverPosition);
            if (reason != PlacementReason.Valid)
            {
                instanceId = 0;
                return false;
            }

            instanceId = PlaceUnchecked(definition, pose, initialProgress01, initialStoredWh, false);
            return true;
        }

        /// <summary>
        /// Places a structure directly without terrain validation (used by save restore and tests).
        /// </summary>
        public int PlaceUnchecked(
            StructureDefinition definition,
            StructurePose pose,
            float initialProgress01 = 0f,
            float initialStoredWh = 0f,
            bool restored = false)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            int instanceId = _nextInstanceId++;
            var job = new BuildJob(definition.BuildCost, definition.BuildSeconds, initialProgress01);
            float clampedStored = Mathf.Clamp(initialStoredWh, 0f, definition.StoreWattHours);
            var placed = new PlacedStructure(
                instanceId,
                definition,
                pose,
                job.Progress01,
                clampedStored,
                0f);

            _structures.Add(placed);
            _jobs.Add(job);
            _powerPublished.Add(false);

            _events?.Publish(new StructurePlaced(instanceId, definition.Id, pose.Position, pose.Yaw, restored));

            if (job.IsDone)
            {
                AddPowerNodeFor(placed);
                _events?.Publish(new StructureBuilt(instanceId, definition.Id, pose.Position, pose.Yaw, restored));
            }

            return instanceId;
        }

        /// <summary>
        /// Steps welding on structure <paramref name="instanceId"/>. When welding reaches 100%, registers the structure
        /// in <see cref="PowerGrid"/> and publishes <see cref="StructureBuilt"/>.
        /// </summary>
        public bool StepWeld(int instanceId, float deltaTime, Vector3 roverPosition, bool isWelding)
        {
            int index = IndexOf(instanceId);
            if (index < 0)
            {
                return false;
            }

            PlacedStructure current = _structures[index];
            BuildJob job = _jobs[index];
            if (job.IsDone)
            {
                return false;
            }

            float weldRange = _tuning != null ? _tuning.WeldRange : DefaultWeldRange;
            bool completedNow = job.Step(deltaTime, current.Pose.Position, roverPosition, weldRange, isWelding);

            var updated = new PlacedStructure(
                current.InstanceId,
                current.Definition,
                current.Pose,
                job.Progress01,
                current.StoredWattHours,
                current.SupplyRatio);
            _structures[index] = updated;

            if (completedNow)
            {
                AddPowerNodeFor(updated);
                _events?.Publish(new StructureBuilt(
                    updated.InstanceId,
                    updated.Definition.Id,
                    updated.Pose.Position,
                    updated.Pose.Yaw,
                    false));
            }

            return completedNow;
        }

        /// <summary>
        /// Steps the <see cref="PowerGrid"/> over <paramref name="deltaTime"/> seconds at <paramref name="sunlight01"/>
        /// and publishes <see cref="StructurePowerChanged"/> for any built consumer whose supply ratio changed. Zero GC
        /// when no event listeners allocate.
        /// </summary>
        public void StepPower(float deltaTime, float sunlight01, bool restored = false)
        {
            _powerGrid.Step(deltaTime, sunlight01);

            int count = _structures.Count;
            for (int i = 0; i < count; i++)
            {
                PlacedStructure current = _structures[i];
                if (!current.IsBuilt)
                {
                    continue;
                }

                if (!_powerGrid.TryGetNode(current.InstanceId, out PowerNode node))
                {
                    continue;
                }

                float previousRatio = current.SupplyRatio;
                bool hadPublished = _powerPublished[i];
                var updated = new PlacedStructure(
                    current.InstanceId,
                    current.Definition,
                    current.Pose,
                    current.BuildProgress01,
                    node.StoredWattHours,
                    node.SupplyRatio);
                _structures[i] = updated;

                if (current.Definition.IsPowerConsumer &&
                    (!hadPublished || Mathf.Abs(node.SupplyRatio - previousRatio) > SupplyChangeEpsilon))
                {
                    _powerPublished[i] = true;
                    _events?.Publish(new StructurePowerChanged(
                        current.InstanceId,
                        current.Definition.Id,
                        current.Pose.Position,
                        node.SupplyRatio,
                        restored));
                }
            }
        }

        /// <summary>
        /// Deconstructs and removes structure <paramref name="instanceId"/>, returning the tuned share of its build
        /// cost and publishing <see cref="StructureRemoved"/>.
        /// </summary>
        public bool TryDeconstruct(int instanceId, out Recipe refund)
        {
            int index = IndexOf(instanceId);
            if (index < 0)
            {
                refund = default;
                return false;
            }

            PlacedStructure removed = _structures[index];
            BuildJob job = _jobs[index];
            float refundShare = _tuning != null ? _tuning.DeconstructRefundShare : DefaultRefundShare;
            refund = job.Deconstruct(refundShare);

            _powerGrid.RemoveNode(instanceId);
            _structures.RemoveAt(index);
            _jobs.RemoveAt(index);
            _powerPublished.RemoveAt(index);

            _events?.Publish(new StructureRemoved(instanceId, removed.Definition.Id, removed.Pose.Position));
            return true;
        }

        /// <summary>Registers this base's save section (<see cref="GameplaySaveKeys.Building"/>) with <paramref name="saveService"/>.</summary>
        public IDisposable RegisterSave(ISaveService saveService)
        {
            if (saveService == null)
            {
                throw new ArgumentNullException(nameof(saveService));
            }

            return saveService.Register(new SaveSection<BuildingSaveData>(
                GameplaySaveKeys.Building,
                GameplaySaveKeys.BuildingVersion,
                Capture,
                Restore));
        }

        /// <summary>Captures all placed structures into a serializable <see cref="BuildingSaveData"/> DTO.</summary>
        public BuildingSaveData Capture()
        {
            int count = _structures.Count;
            if (count == 0)
            {
                return new BuildingSaveData();
            }

            var entries = new PlacedStructureSaveData[count];
            for (int i = 0; i < count; i++)
            {
                PlacedStructure item = _structures[i];
                entries[i] = new PlacedStructureSaveData
                {
                    id = item.Definition.Id,
                    position = item.Pose.Position,
                    yaw = item.Pose.Yaw,
                    buildProgress = item.BuildProgress01,
                    storedCharge = item.StoredWattHours,
                };
            }

            return new BuildingSaveData { structures = entries };
        }

        /// <summary>
        /// Restores placed structures from <paramref name="data"/>. An entry whose structure id is not in
        /// <see cref="StructureCatalog"/> logs an error via <see cref="Debug.LogError(object)"/> and is skipped.
        /// </summary>
        public void Restore(BuildingSaveData data)
        {
            _structures.Clear();
            _jobs.Clear();
            _powerPublished.Clear();
            _powerGrid.Clear();
            _nextInstanceId = 1;

            if (data == null || data.structures == null)
            {
                return;
            }

            for (int i = 0; i < data.structures.Length; i++)
            {
                PlacedStructureSaveData entry = data.structures[i];
                string id = entry != null ? entry.id : null;
                if (string.IsNullOrEmpty(id) || !_catalog.TryGet(id, out StructureDefinition definition))
                {
                    Debug.LogError($"{nameof(PlacedBase)}: unknown structure id '{id}' at save index {i}; skipping entry.");
                    continue;
                }

                var pose = new StructurePose(entry.position, entry.yaw);
                PlaceUnchecked(definition, pose, entry.buildProgress, entry.storedCharge, true);
            }
        }

        private void AddPowerNodeFor(PlacedStructure placed)
        {
            StructureDefinition def = placed.Definition;
            _powerGrid.AddNode(new PowerNode(
                placed.InstanceId,
                placed.Pose.Position,
                def.ProduceWatts,
                def.ConsumeWatts,
                def.StoreWattHours,
                def.ConnectionRadius,
                placed.StoredWattHours));
        }

        private int IndexOf(int instanceId)
        {
            int count = _structures.Count;
            for (int i = 0; i < count; i++)
            {
                if (_structures[i].InstanceId == instanceId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
