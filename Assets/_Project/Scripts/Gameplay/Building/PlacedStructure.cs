using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Snapshot of a structure placed in the base: its runtime instance id, <see cref="StructureDefinition"/>, world
    /// <see cref="StructurePose"/>, weld progress (0..1),stored battery energy (Wh), and current power supply ratio.
    /// </summary>
    public readonly struct PlacedStructure
    {
        public PlacedStructure(
            int instanceId,
            StructureDefinition definition,
            StructurePose pose,
            float buildProgress01 = 1f,
            float storedWattHours = 0f,
            float supplyRatio = 0f)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            InstanceId = instanceId;
            Definition = definition;
            Pose = pose;
            BuildProgress01 = Mathf.Clamp01(buildProgress01);
            StoredWattHours = Mathf.Clamp(storedWattHours, 0f, definition.StoreWattHours);
            SupplyRatio = Mathf.Clamp01(supplyRatio);
        }

        public int InstanceId { get; }

        public StructureDefinition Definition { get; }

        public StructurePose Pose { get; }

        /// <summary>Normalized weld progress in [0, 1]. Reaching 1 brings the structure into service.</summary>
        public float BuildProgress01 { get; }

        /// <summary>Stored electrical energy in Watt-hours (for battery banks).</summary>
        public float StoredWattHours { get; }

        /// <summary>Current power supply ratio in [0, 1] for consumer structures.</summary>
        public float SupplyRatio { get; }

        public bool IsBuilt => BuildProgress01 >= 1f;
    }
}
