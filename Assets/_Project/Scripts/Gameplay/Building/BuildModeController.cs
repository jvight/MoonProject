using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Pure state machine for 07's build mode (0.5.7 slice 6, BUILD-MODE): <see cref="BuildModeState.Closed"/>,
    /// <see cref="BuildModeState.Menu"/>, <see cref="BuildModeState.Placing"/>, and
    /// <see cref="BuildModeState.Welding"/>, plus hold-to-deconstruct progress. Driven by plain input values with zero
    /// per-frame allocations.
    /// </summary>
    public sealed class BuildModeController
    {
        public const float DefaultYawStepDegrees = 15f;
        public const float DefaultDeconstructHoldSeconds = 1.1f;

        /// <summary>Seconds a released, part-filled take-down takes to rewind to nothing.</summary>
        public const float DefaultDeconstructRewindSeconds = 0.6f;

        private readonly StructureCatalog _catalog;
        private readonly KnownStructures _known;
        private readonly PlacedBase _placedBase;
        private readonly MaterialStock _stock;
        private readonly BuildingTuning _tuning;
        private readonly float _yawStepDegrees;
        private readonly float _deconstructHoldSeconds;
        private readonly float _deconstructRewindSeconds;
        private readonly List<StructureDefinition> _available = new List<StructureDefinition>(StructureIds.Count);

        public BuildModeController(
            StructureCatalog catalog,
            KnownStructures known,
            PlacedBase placedBase,
            MaterialStock stock,
            BuildingTuning tuning,
            float yawStepDegrees = DefaultYawStepDegrees,
            float deconstructHoldSeconds = DefaultDeconstructHoldSeconds,
            float deconstructRewindSeconds = DefaultDeconstructRewindSeconds)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _known = known ?? throw new ArgumentNullException(nameof(known));
            _placedBase = placedBase ?? throw new ArgumentNullException(nameof(placedBase));
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _yawStepDegrees = Mathf.Max(1f, yawStepDegrees);
            _deconstructHoldSeconds = Mathf.Max(0.1f, deconstructHoldSeconds);
            _deconstructRewindSeconds = Mathf.Max(0.05f, deconstructRewindSeconds);
        }

        public BuildModeState State { get; private set; } = BuildModeState.Closed;

        public bool IsOpen => State != BuildModeState.Closed;

        /// <summary>Known structures that have a real runtime prefab (<c>!ArtPending</c>).</summary>
        public IReadOnlyList<StructureDefinition> AvailableStructures => _available;

        public int SelectedIndex { get; private set; }

        public StructureDefinition SelectedStructure { get; private set; }

        public float GhostYaw { get; private set; }

        public StructurePose CandidatePose { get; private set; }

        public bool CandidateSnapped { get; private set; }

        public PlacementReason CandidateReason { get; private set; }

        public bool HasCandidatePoint { get; private set; }

        public bool CanAffordSelected =>
            SelectedStructure != null && _stock.Has(SelectedStructure.BuildCost);

        public bool CanPlaceNow =>
            State == BuildModeState.Placing &&
            SelectedStructure != null &&
            HasCandidatePoint &&
            CandidateReason == PlacementReason.Valid &&
            CanAffordSelected;

        public int ActiveWeldInstanceId { get; private set; }

        public float ActiveWeldProgress01 { get; private set; }

        public int DeconstructTargetId { get; private set; }

        public float DeconstructProgress01 { get; private set; }

        /// <summary>
        /// Rebuilds <see cref="AvailableStructures"/> from <see cref="KnownStructures"/> and
        /// <see cref="StructureCatalog"/> (excluding <see cref="StructureDefinition.ArtPending"/> entries).
        /// </summary>
        public int RefreshAvailable()
        {
            _available.Clear();
            int count = _catalog.Count;
            for (int i = 0; i < count; i++)
            {
                StructureDefinition def = _catalog.StructureAt(i);
                if (def != null && !def.ArtPending && _known.IsKnown(def.Id))
                {
                    _available.Add(def);
                }
            }

            if (_available.Count == 0)
            {
                SelectedIndex = 0;
                if (State == BuildModeState.Menu || State == BuildModeState.Placing)
                {
                    SelectedStructure = null;
                }

                return 0;
            }

            if (SelectedStructure != null)
            {
                int existingIndex = IndexOfAvailable(SelectedStructure.Id);
                if (existingIndex >= 0)
                {
                    SelectedIndex = existingIndex;
                    SelectedStructure = _available[SelectedIndex];
                    return _available.Count;
                }
            }

            SelectedIndex = Mathf.Clamp(SelectedIndex, 0, _available.Count - 1);
            SelectedStructure = _available[SelectedIndex];
            return _available.Count;
        }

        /// <summary>
        /// Opens the build menu if 07 knows at least one structure with a ready prefab; otherwise does nothing and
        /// returns false.
        /// </summary>
        public bool TryOpenMenu()
        {
            if (RefreshAvailable() == 0)
            {
                return false;
            }

            State = BuildModeState.Menu;
            ActiveWeldInstanceId = 0;
            ActiveWeldProgress01 = 0f;
            HasCandidatePoint = false;
            CandidateReason = PlacementReason.Valid;
            return true;
        }

        /// <summary>
        /// Closes build mode from any state. If an unwelded (0%) structure is currently active in
        /// <see cref="BuildModeState.Welding"/>, it is removed and its build cost refunded so no orphan 0% frame is left.
        /// </summary>
        public void Close()
        {
            if (ActiveWeldInstanceId > 0 &&
                _placedBase.TryGetStructure(ActiveWeldInstanceId, out PlacedStructure active) &&
                !active.IsBuilt &&
                active.BuildProgress01 <= 0f &&
                _placedBase.TryDeconstruct(ActiveWeldInstanceId, out Recipe refund))
            {
                Refund(refund);
            }

            State = BuildModeState.Closed;
            ActiveWeldInstanceId = 0;
            ActiveWeldProgress01 = 0f;
            HasCandidatePoint = false;
            CandidateSnapped = false;
            CandidateReason = PlacementReason.Valid;
        }

        /// <summary>Toggles build mode between <see cref="BuildModeState.Closed"/> and <see cref="BuildModeState.Menu"/>.</summary>
        public bool ToggleBuildMode()
        {
            if (State == BuildModeState.Closed)
            {
                return TryOpenMenu();
            }

            Close();
            return false;
        }

        /// <summary>Moves selection across <see cref="AvailableStructures"/> by <paramref name="delta"/> steps.</summary>
        public void MoveSelection(int delta)
        {
            int count = _available.Count;
            if (count == 0 || delta == 0)
            {
                return;
            }

            int next = (SelectedIndex + (delta % count) + count) % count;
            SelectedIndex = next;
            SelectedStructure = _available[next];
        }

        /// <summary>Selects the available structure at <paramref name="index"/> without leaving <see cref="BuildModeState.Menu"/>.</summary>
        public bool SelectIndex(int index)
        {
            if (index < 0 || index >= _available.Count)
            {
                return false;
            }

            SelectedIndex = index;
            SelectedStructure = _available[index];
            return true;
        }

        /// <summary>Confirms the currently highlighted menu item and enters <see cref="BuildModeState.Placing"/>.</summary>
        public bool PickSelected()
        {
            if (State != BuildModeState.Menu && State != BuildModeState.Placing)
            {
                if (!TryOpenMenu())
                {
                    return false;
                }
            }

            if (_available.Count == 0)
            {
                return false;
            }

            SelectedIndex = Mathf.Clamp(SelectedIndex, 0, _available.Count - 1);
            SelectedStructure = _available[SelectedIndex];
            State = BuildModeState.Placing;
            HasCandidatePoint = false;
            CandidateSnapped = false;
            CandidateReason = PlacementReason.Valid;
            return true;
        }

        /// <summary>Selects the available structure at <paramref name="index"/> and enters <see cref="BuildModeState.Placing"/>.</summary>
        public bool PickStructure(int index)
        {
            if (!SelectIndex(index))
            {
                return false;
            }

            State = BuildModeState.Placing;
            HasCandidatePoint = false;
            CandidateSnapped = false;
            CandidateReason = PlacementReason.Valid;
            return true;
        }

        /// <summary>Selects a known structure by <paramref name="structureId"/> and enters <see cref="BuildModeState.Placing"/>.</summary>
        public bool PickStructure(string structureId)
        {
            RefreshAvailable();
            int index = IndexOfAvailable(structureId);
            return index >= 0 && PickStructure(index);
        }

        /// <summary>Rotates the ghost yaw in <see cref="BuildModeState.Placing"/> by <paramref name="winchInput"/> steps.</summary>
        public void RotateGhost(float winchInput)
        {
            if (State != BuildModeState.Placing || Mathf.Abs(winchInput) <= 1e-4f)
            {
                return;
            }

            float direction = Mathf.Sign(winchInput);
            GhostYaw = StructurePose.NormalizeYaw(GhostYaw + direction * _yawStepDegrees);
        }

        /// <summary>Sets the ghost yaw directly (degrees, normalized to [0, 360)).</summary>
        public void SetGhostYaw(float yawDegrees)
        {
            GhostYaw = StructurePose.NormalizeYaw(yawDegrees);
        }

        /// <summary>
        /// Updates the placement candidate pose, soft-snap result, and <see cref="PlacementReason"/> for the current
        /// aim point on the terrain.
        /// </summary>
        public void UpdateCandidate(
            Vector3 groundPoint,
            Vector3 roverPosition,
            ITerrainQuery terrain,
            bool propBlocked,
            bool snapEnabled = true)
        {
            if (State != BuildModeState.Placing || SelectedStructure == null || terrain == null)
            {
                HasCandidatePoint = false;
                CandidateSnapped = false;
                CandidateReason = PlacementReason.OutsidePlayArea;
                return;
            }

            HasCandidatePoint = true;
            var freePose = new StructurePose(groundPoint, GhostYaw);
            StructurePose snappedPose = SnapSolver.Solve(
                SelectedStructure,
                freePose,
                _placedBase.Structures,
                _tuning.SnapDistance,
                _tuning.SnapAngleDegrees,
                snapEnabled,
                out bool snapped);

            float groundY = terrain.SampleHeight(snappedPose.Position.x, snappedPose.Position.z);
            snappedPose = new StructurePose(
                new Vector3(snappedPose.Position.x, groundY, snappedPose.Position.z),
                snappedPose.Yaw);

            PlacementReason reason = _placedBase.EvaluatePlacement(
                SelectedStructure,
                snappedPose,
                terrain,
                roverPosition);
            if (reason == PlacementReason.Valid && propBlocked)
            {
                reason = PlacementReason.Blocked;
            }

            CandidatePose = snappedPose;
            CandidateSnapped = snapped;
            CandidateReason = reason;
        }

        /// <summary>
        /// Marks the candidate aim point as missing (e.g. camera ray missed the terrain) with the given fallback reason.
        /// </summary>
        public void ClearCandidate(PlacementReason fallbackReason = PlacementReason.TooFarFrom07)
        {
            HasCandidatePoint = false;
            CandidateSnapped = false;
            CandidateReason = fallbackReason;
        }

        /// <summary>
        /// Attempts to confirm placement in <see cref="BuildModeState.Placing"/>: verifies validity and material stock,
        /// spends <see cref="StructureDefinition.BuildCost"/>, places the stage-0 frame in <see cref="PlacedBase"/>,
        /// and transitions to <see cref="BuildModeState.Welding"/>.
        /// </summary>
        public bool TryConfirmPlacement(
            ITerrainQuery terrain,
            Vector3 roverPosition,
            out int placedInstanceId,
            out PlacementReason reason)
        {
            placedInstanceId = 0;
            if (State != BuildModeState.Placing || SelectedStructure == null || !HasCandidatePoint)
            {
                reason = CandidateReason;
                return false;
            }

            if (CandidateReason != PlacementReason.Valid)
            {
                reason = CandidateReason;
                return false;
            }

            if (!_stock.Has(SelectedStructure.BuildCost))
            {
                reason = PlacementReason.Valid;
                return false;
            }

            if (!_placedBase.TryPlace(
                    SelectedStructure,
                    CandidatePose,
                    terrain,
                    roverPosition,
                    out placedInstanceId,
                    out reason,
                    initialProgress01: 0f))
            {
                return false;
            }

            _stock.TrySpend(SelectedStructure.BuildCost);
            State = BuildModeState.Welding;
            ActiveWeldInstanceId = placedInstanceId;
            ActiveWeldProgress01 = 0f;
            return true;
        }

        /// <summary>
        /// A take-down the key was let go of (or lost) runs back as it came: the piece builds itself up again over
        /// <see cref="DefaultDeconstructRewindSeconds"/>, and the target is let go once nothing is left of the hold.
        /// </summary>
        private void RewindDeconstruct(float deltaTime)
        {
            if (DeconstructTargetId > 0 && DeconstructProgress01 > 0f &&
                _placedBase.TryGetStructure(DeconstructTargetId, out _))
            {
                DeconstructProgress01 = Mathf.Max(0f, DeconstructProgress01 - deltaTime / _deconstructRewindSeconds);
                if (DeconstructProgress01 > 0f)
                {
                    return;
                }
            }

            DeconstructTargetId = 0;
            DeconstructProgress01 = 0f;
        }

        /// <summary>
        /// Enters <see cref="BuildModeState.Welding"/> on an existing unfinished structure within
        /// <see cref="BuildingTuning.WeldRange"/> so 07 can resume welding it.
        /// </summary>
        public bool TryBeginResumeWeld(int instanceId, Vector3 roverPosition)
        {
            if (!_placedBase.TryGetStructure(instanceId, out PlacedStructure placed) || placed.IsBuilt)
            {
                return false;
            }

            float distance = SurfaceRules.HorizontalDistance(placed.Pose.Position, roverPosition);
            if (distance > _tuning.WeldRange)
            {
                return false;
            }

            State = BuildModeState.Welding;
            ActiveWeldInstanceId = instanceId;
            SelectedStructure = placed.Definition;
            ActiveWeldProgress01 = placed.BuildProgress01;
            return true;
        }

        /// <summary>
        /// Steps active welding in <see cref="BuildModeState.Welding"/>.
        /// Intended input contract (ENV-01):
        /// - Tap <c>E</c> (<c>Excavate</c>) in <see cref="BuildModeState.Placing"/> places the structure at 0% and enters
        ///   <see cref="BuildModeState.Welding"/>, waiting for the player to hold <c>E</c> to weld (or holding <c>E</c>
        ///   continuously from placement welds straight through).
        /// - Releasing <c>E</c> before any weld progress is made (<c>BuildProgress01 == 0</c>) keeps build mode in
        ///   <see cref="BuildModeState.Welding"/> while in range so tapping <c>E</c> to place never cancels on the next
        ///   frame or leaves an orphan 0% frame.
        /// - Releasing <c>E</c> after welding has progressed (<c>BuildProgress01 &gt; 0</c>) pauses welding on the
        ///   partially welded structure and returns build mode to <see cref="BuildModeState.Closed"/>
        ///   (<paramref name="cancelledNow"/> = true).
        /// - Leaving <see cref="BuildingTuning.WeldRange"/> (or placing a frame beyond it) closes build mode but leaves
        ///   the frame standing, unwelded or partly welded, so pressing E to place never silently does nothing; holding
        ///   <c>E</c> within range resumes it.
        /// - When progress reaches 100%, <paramref name="completedNow"/> is true and build mode closes cleanly.
        /// </summary>
        public bool StepWelding(
            float deltaTime,
            Vector3 roverPosition,
            bool weldHeld,
            out bool completedNow,
            out bool cancelledNow)
        {
            completedNow = false;
            cancelledNow = false;

            if (State != BuildModeState.Welding || ActiveWeldInstanceId <= 0)
            {
                return false;
            }

            if (!_placedBase.TryGetStructure(ActiveWeldInstanceId, out PlacedStructure current) || current.IsBuilt)
            {
                ActiveWeldInstanceId = 0;
                Close();
                return false;
            }

            float distance = SurfaceRules.HorizontalDistance(current.Pose.Position, roverPosition);
            bool inRange = distance <= _tuning.WeldRange;
            if (!inRange)
            {
                _placedBase.StepWeld(ActiveWeldInstanceId, 0f, roverPosition, false);
                cancelledNow = true;
                ActiveWeldInstanceId = 0;
                Close();
                return false;
            }

            if (!weldHeld)
            {
                _placedBase.StepWeld(ActiveWeldInstanceId, 0f, roverPosition, false);
                if (current.BuildProgress01 <= 0f)
                {
                    return true;
                }

                cancelledNow = true;
                Close();
                return false;
            }

            completedNow = _placedBase.StepWeld(ActiveWeldInstanceId, deltaTime, roverPosition, true);
            if (_placedBase.TryGetStructure(ActiveWeldInstanceId, out PlacedStructure updated))
            {
                ActiveWeldProgress01 = updated.BuildProgress01;
            }

            if (completedNow)
            {
                ActiveWeldProgress01 = 1f;
                ActiveWeldInstanceId = 0;
                Close();
            }

            return true;
        }

        /// <summary>
        /// Steps hold-to-deconstruct progress when 07 holds <c>Deconstruct</c> while looking at a placed structure
        /// within <see cref="BuildingTuning.WeldRange"/> and 07's hold has room for its whole cost. When the hold
        /// completes, removes the structure from <see cref="PlacedBase"/> and refunds materials into 07's hold.
        /// </summary>
        public bool StepDeconstruct(
            float deltaTime,
            int targetInstanceId,
            Vector3 roverPosition,
            bool deconstructHeld,
            out int deconstructedId,
            out Recipe refund)
        {
            deconstructedId = 0;
            refund = default;

            if (!deconstructHeld || targetInstanceId <= 0 ||
                !_placedBase.TryGetStructure(targetInstanceId, out PlacedStructure target))
            {
                RewindDeconstruct(deltaTime);
                return false;
            }

            float distance = SurfaceRules.HorizontalDistance(target.Pose.Position, roverPosition);
            if (distance > _tuning.WeldRange || !HasRoomFor(target.Definition.BuildCost))
            {
                RewindDeconstruct(deltaTime);
                return false;
            }

            if (DeconstructTargetId != targetInstanceId)
            {
                DeconstructTargetId = targetInstanceId;
                DeconstructProgress01 = 0f;
            }

            if (deltaTime <= 0f)
            {
                return false;
            }

            DeconstructProgress01 = Mathf.Clamp01(DeconstructProgress01 + deltaTime / _deconstructHoldSeconds);
            if (DeconstructProgress01 < 1f)
            {
                return false;
            }

            if (!_placedBase.TryDeconstruct(targetInstanceId, out refund))
            {
                DeconstructTargetId = 0;
                DeconstructProgress01 = 0f;
                return false;
            }

            Refund(refund);
            if (ActiveWeldInstanceId == targetInstanceId)
            {
                Close();
            }

            deconstructedId = targetInstanceId;
            DeconstructTargetId = 0;
            DeconstructProgress01 = 0f;
            return true;
        }

        private void Refund(Recipe refund)
        {
            RefundMaterial(SalvageMaterial.Metal, refund.Metal);
            RefundMaterial(SalvageMaterial.Wiring, refund.Wiring);
            RefundMaterial(SalvageMaterial.Optics, refund.Optics);
        }

        private void RefundMaterial(SalvageMaterial material, int amount)
        {
            if (amount > 0)
            {
                _stock.Collect(material, amount);
            }
        }

        /// <summary>07's hold has room for all of <paramref name="cost"/>: a refund never finds it full.</summary>
        private bool HasRoomFor(Recipe cost)
        {
            return _stock.Room(SalvageMaterial.Metal) >= cost.Metal &&
                   _stock.Room(SalvageMaterial.Wiring) >= cost.Wiring &&
                   _stock.Room(SalvageMaterial.Optics) >= cost.Optics;
        }

        private int IndexOfAvailable(string structureId)
        {
            if (string.IsNullOrEmpty(structureId))
            {
                return -1;
            }

            int count = _available.Count;
            for (int i = 0; i < count; i++)
            {
                if (string.Equals(_available[i].Id, structureId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
