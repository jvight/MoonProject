using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>
    /// 07 placed a structure frame at <see cref="Position"/> with heading <see cref="Yaw"/> (degrees), or a save
    /// restored it (<see cref="Restored"/> = true: show it at once with no placement cue).
    /// </summary>
    public readonly struct StructurePlaced
    {
        public StructurePlaced(int instanceId, string structureId, Vector3 position, float yaw, bool restored = false)
        {
            InstanceId = instanceId;
            StructureId = structureId;
            Position = position;
            Yaw = yaw;
            Restored = restored;
        }

        public int InstanceId { get; }

        public string StructureId { get; }

        public Vector3 Position { get; }

        public float Yaw { get; }

        public bool Restored { get; }
    }

    /// <summary>
    /// 07 finished welding a placed structure into service, or a save restored it already built
    /// (<see cref="Restored"/> = true: show it built at once with no weld-completion cue).
    /// </summary>
    public readonly struct StructureBuilt
    {
        public StructureBuilt(int instanceId, string structureId, Vector3 position, float yaw, bool restored = false)
        {
            InstanceId = instanceId;
            StructureId = structureId;
            Position = position;
            Yaw = yaw;
            Restored = restored;
        }

        public int InstanceId { get; }

        public string StructureId { get; }

        public Vector3 Position { get; }

        public float Yaw { get; }

        public bool Restored { get; }
    }

    /// <summary>07 deconstructed and removed a structure from the base.</summary>
    public readonly struct StructureRemoved
    {
        public StructureRemoved(int instanceId, string structureId, Vector3 position)
        {
            InstanceId = instanceId;
            StructureId = structureId;
            Position = position;
        }

        public int InstanceId { get; }

        public string StructureId { get; }

        public Vector3 Position { get; }
    }

    /// <summary>
    /// A consumer structure's power supply ratio changed (0..1: 1 = full speed/brightness, 0 = unpowered, between =
    /// slowed machine and dimmed lamp). Published only for consumers when their ratio changes.
    /// </summary>
    public readonly struct StructurePowerChanged
    {
        public StructurePowerChanged(int instanceId, string structureId, Vector3 position, float supplyRatio,
            bool restored = false)
        {
            InstanceId = instanceId;
            StructureId = structureId;
            Position = position;
            SupplyRatio = supplyRatio;
            Restored = restored;
        }

        public int InstanceId { get; }

        public string StructureId { get; }

        public Vector3 Position { get; }

        /// <summary>Share of demanded power currently supplied, clamped to 0..1.</summary>
        public float SupplyRatio { get; }

        public bool Restored { get; }
    }

    /// <summary>
    /// 07 learned how to build a structure (<see cref="StructureId"/>), or a save restored it as known
    /// (<see cref="Restored"/> = true).
    /// </summary>
    public readonly struct StructureLearned
    {
        public StructureLearned(string structureId, bool restored = false)
        {
            StructureId = structureId;
            Restored = restored;
        }

        public string StructureId { get; }

        public bool Restored { get; }
    }

    /// <summary>The transient build-mode sound beats that do not correspond to structure lifecycle mutations.</summary>
    public enum BuildCueKind : byte
    {
        Ghost = 0,
        Snap = 1,
        Invalid = 2,
        StageComplete = 3,
    }

    /// <summary>
    /// Published by <c>BuildField</c> when the placement ghost appears (<see cref="BuildCueKind.Ghost"/>), snaps to a
    /// neighbour port or edge (<see cref="BuildCueKind.Snap"/>), becomes or is refused as invalid
    /// (<see cref="BuildCueKind.Invalid"/>), or finishes an intermediate weld stage
    /// (<see cref="BuildCueKind.StageComplete"/>).
    /// </summary>
    public readonly struct BuildCue
    {
        public BuildCue(BuildCueKind kind, Vector3 position)
        {
            Kind = kind;
            Position = position;
        }

        public BuildCueKind Kind { get; }

        public Vector3 Position { get; }
    }

    /// <summary>
    /// Published by <c>BuildField</c> when 07's welding beam starts, moves, or stops welding a placed structure so the
    /// spatial weld loop (<c>build_weld_loop</c>) fades in at <see cref="Position"/> and fades out when done.
    /// </summary>
    public readonly struct BuildWeldStateChanged
    {
        public BuildWeldStateChanged(bool welding, Vector3 position, float progress01 = 0f)
        {
            Welding = welding;
            Position = position;
            Progress01 = Mathf.Clamp01(progress01);
        }

        public bool Welding { get; }

        public Vector3 Position { get; }

        public float Progress01 { get; }
    }
}
