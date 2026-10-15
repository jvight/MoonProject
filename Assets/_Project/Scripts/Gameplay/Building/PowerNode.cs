using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One electrical node in <see cref="PowerGrid"/>: world position, production (W at full sun), consumption (W),
    /// storage capacity and current charge (Wh), reach radius (m), and resulting consumer supply ratio (0..1).
    /// </summary>
    public struct PowerNode
    {
        public PowerNode(
            int id,
            Vector3 position,
            float produceWatts,
            float consumeWatts,
            float storeWattHours,
            float connectionRadius,
            float storedWattHours = 0f)
        {
            Id = id;
            Position = position;
            ProduceWatts = Mathf.Max(0f, produceWatts);
            ConsumeWatts = Mathf.Max(0f, consumeWatts);
            StoreWattHours = Mathf.Max(0f, storeWattHours);
            ConnectionRadius = Mathf.Max(0f, connectionRadius);
            StoredWattHours = Mathf.Clamp(storedWattHours, 0f, StoreWattHours);
            SupplyRatio = 0f;
        }

        public int Id { get; }

        public Vector3 Position { get; }

        public float ProduceWatts { get; }

        public float ConsumeWatts { get; }

        public float StoreWattHours { get; }

        public float ConnectionRadius { get; }

        public float StoredWattHours { get; internal set; }

        /// <summary>Share of demanded power supplied on the last grid step (0..1; 0 for non-consumers).</summary>
        public float SupplyRatio { get; internal set; }
    }
}
