using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's hold (docs/features/M3-25 §2, <see cref="ICargoHold"/>): the body's own slots plus what the fitted kits add
    /// (<see cref="CargoTuning"/>). Only materials take space. Every change publishes <see cref="CargoChanged"/>.
    /// </summary>
    public sealed class CargoHold : ICargoHold
    {
        private readonly EventBus _events;
        private readonly MaterialGrid _grid;

        public CargoHold(EventBus events, IMaterialCatalog catalog, int slotCount)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _grid = new MaterialGrid(catalog, slotCount);
        }

        public int SlotCount => _grid.SlotCount;

        public int UsedSlots => _grid.UsedSlots;

        public bool IsFull => _grid.IsFull;

        public int WaitingBundles { get; private set; }

        public int TotalUnits => _grid.TotalUnits;

        public MaterialSlot GetSlot(int index)
        {
            return _grid.GetSlot(index);
        }

        public int Units(SalvageMaterial material)
        {
            return _grid.Units(material);
        }

        public int FreeUnits(SalvageMaterial material)
        {
            return _grid.FreeUnits(material);
        }

        /// <summary>A kit added (or a load restored) slots.</summary>
        public void SetSlotCount(int slotCount)
        {
            if (slotCount == _grid.SlotCount)
            {
                return;
            }

            _grid.SetSlotCount(slotCount);
            Announce();
        }

        /// <summary>Puts in what fits of <paramref name="units"/>; returns how many went in.</summary>
        public int Add(SalvageMaterial material, int units)
        {
            int fitted = _grid.Add(material, units);
            if (fitted > 0)
            {
                Announce();
            }

            return fitted;
        }

        /// <summary>Takes out up to <paramref name="units"/>; returns how many came out.</summary>
        public int Remove(SalvageMaterial material, int units)
        {
            int taken = _grid.Remove(material, units);
            if (taken > 0)
            {
                Announce();
            }

            return taken;
        }

        /// <summary>The bundles waiting in the world (the bundle field keeps the count).</summary>
        public void SetWaitingBundles(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "No negative bundles.");
            }

            WaitingBundles = count;
        }

        /// <summary>Empties the hold before a load lays the saved units in again.</summary>
        public void Clear()
        {
            _grid.Clear();
            Announce();
        }

        /// <summary>The per-material units, for the save.</summary>
        public int[] Capture()
        {
            var units = new int[SalvageMaterials.Count];
            for (int m = 0; m < units.Length; m++)
            {
                units[m] = _grid.Units((SalvageMaterial)m);
            }

            return units;
        }

        private void Announce()
        {
            _events.Publish(new CargoChanged(_grid.UsedSlots, _grid.SlotCount));
        }
    }
}
