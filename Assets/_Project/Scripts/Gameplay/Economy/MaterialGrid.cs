using System;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A self-packing grid of material slots (docs/features/M3-25 §2.2): it keeps one total per material and lays them
    /// out in material order, full stacks first, so the player never sorts. A material takes as many slots as its
    /// total needs at its stack size (<see cref="IMaterialCatalog.StackSize"/>). Allocation-free after construction.
    /// </summary>
    public sealed class MaterialGrid : IMaterialSlots
    {
        private readonly IMaterialCatalog _catalog;
        private readonly int[] _units = new int[SalvageMaterials.Count];

        public MaterialGrid(IMaterialCatalog catalog, int slotCount)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            SetSlotCount(slotCount);
        }

        public int SlotCount { get; private set; }

        public int UsedSlots
        {
            get
            {
                int used = 0;
                for (int m = 0; m < SalvageMaterials.Count; m++)
                {
                    used += SlotsFor(m, _units[m]);
                }

                return used;
            }
        }

        public bool IsFull
        {
            get
            {
                if (UsedSlots < SlotCount)
                {
                    return false;
                }

                for (int m = 0; m < SalvageMaterials.Count; m++)
                {
                    if (_units[m] > 0 && _units[m] % Stack(m) != 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Units of every material together.</summary>
        public int TotalUnits
        {
            get
            {
                int total = 0;
                for (int m = 0; m < SalvageMaterials.Count; m++)
                {
                    total += _units[m];
                }

                return total;
            }
        }

        public MaterialSlot GetSlot(int index)
        {
            if (index < 0 || index >= SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"The grid has {SlotCount} slots.");
            }

            int first = 0;
            for (int m = 0; m < SalvageMaterials.Count; m++)
            {
                int slots = SlotsFor(m, _units[m]);
                if (index < first + slots)
                {
                    int stack = Stack(m);
                    int before = (index - first) * stack;
                    var material = (SalvageMaterial)m;
                    return new MaterialSlot(material, Math.Min(stack, _units[m] - before), stack);
                }

                first += slots;
            }

            return default;
        }

        public int Units(SalvageMaterial material)
        {
            return _units[Index(material)];
        }

        public int FreeUnits(SalvageMaterial material)
        {
            int m = Index(material);
            int stack = Stack(m);
            int topUp = SlotsFor(m, _units[m]) * stack - _units[m];
            return topUp + Math.Max(0, SlotCount - UsedSlots) * stack;
        }

        /// <summary>Grows or shrinks the grid; shrinking below what it holds is a bug (nothing is ever dropped).</summary>
        public void SetSlotCount(int slotCount)
        {
            if (slotCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount), slotCount, "A grid has no negative slots.");
            }

            if (slotCount < UsedSlots)
            {
                throw new InvalidOperationException(
                    $"The grid holds {UsedSlots} slots of materials; it cannot shrink to {slotCount}.");
            }

            SlotCount = slotCount;
        }

        /// <summary>Puts in as much of <paramref name="units"/> as fits; returns how many went in.</summary>
        public int Add(SalvageMaterial material, int units)
        {
            if (units < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(units), units, "Adds a positive amount.");
            }

            int fits = Math.Min(units, FreeUnits(material));
            _units[Index(material)] += fits;
            return fits;
        }

        /// <summary>Takes out up to <paramref name="units"/>; returns how many came out.</summary>
        public int Remove(SalvageMaterial material, int units)
        {
            if (units < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(units), units, "Removes a positive amount.");
            }

            int m = Index(material);
            int taken = Math.Min(units, _units[m]);
            _units[m] -= taken;
            return taken;
        }

        /// <summary>Empties the grid (a load lays the saved totals in again).</summary>
        public void Clear()
        {
            Array.Clear(_units, 0, _units.Length);
        }

        private int SlotsFor(int material, int units)
        {
            return units <= 0 ? 0 : (units + Stack(material) - 1) / Stack(material);
        }

        private int Stack(int material)
        {
            return _catalog.StackSize((SalvageMaterial)material);
        }

        private static int Index(SalvageMaterial material)
        {
            int m = (int)material;
            if (m < 0 || m >= SalvageMaterials.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown material.");
            }

            return m;
        }
    }
}
