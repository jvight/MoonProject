namespace MoonProject.Core
{
    /// <summary>
    /// A self-packing grid of material slots (07's hold, home's shelves; docs/features/M3-25 §2, §3): one material per
    /// slot, part-filled slots topped up first. Read-only and allocation-free: poll it, or listen for its events.
    /// </summary>
    public interface IMaterialSlots
    {
        /// <summary>Slots there are now (kits and shelves add more).</summary>
        int SlotCount { get; }

        /// <summary>Slots holding at least one unit.</summary>
        int UsedSlots { get; }

        /// <summary>True when no unit of any material fits anywhere.</summary>
        bool IsFull { get; }

        /// <summary>The slot at <paramref name="index"/> (0 .. <see cref="SlotCount"/> - 1).</summary>
        MaterialSlot GetSlot(int index);

        /// <summary>Units of <paramref name="material"/> held, across every slot.</summary>
        int Units(SalvageMaterial material);

        /// <summary>
        /// Units of <paramref name="material"/> that still fit: room in its part-filled slots plus whole empty slots.
        /// </summary>
        int FreeUnits(SalvageMaterial material);
    }
}
