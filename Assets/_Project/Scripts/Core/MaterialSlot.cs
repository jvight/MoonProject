namespace MoonProject.Core
{
    /// <summary>One slot of a material grid: one material, up to <see cref="Capacity"/> units (M3-25 §2).</summary>
    public readonly struct MaterialSlot
    {
        public MaterialSlot(SalvageMaterial material, int units, int capacity)
        {
            Material = material;
            Units = units;
            Capacity = capacity;
        }

        /// <summary>What the slot holds; meaningless while <see cref="IsEmpty"/>.</summary>
        public SalvageMaterial Material { get; }

        public int Units { get; }

        /// <summary>The material's stack size: the most this slot can hold.</summary>
        public int Capacity { get; }

        public bool IsEmpty => Units == 0;
    }
}
