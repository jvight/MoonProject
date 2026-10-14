using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's hold (docs/features/M3-25 §2, <see cref="ICargoHold"/>): the body's own slots plus what the fitted kits add
    /// (<see cref="CargoTuning"/>). Material stacks and carried tower story parts share the slots (one slot per story
    /// part). Every change publishes <see cref="CargoChanged"/>.
    /// </summary>
    public sealed class CargoHold : ICargoHold
    {
        private readonly EventBus _events;
        private readonly MaterialGrid _grid;
        private readonly List<string> _parts = new List<string>(4);
        private int _slotCount;

        public CargoHold(EventBus events, IMaterialCatalog catalog, int slotCount)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _grid = new MaterialGrid(catalog, slotCount);
            _slotCount = slotCount;
        }

        public int SlotCount => _slotCount;

        /// <summary>Slots currently holding material stacks (excluding story parts).</summary>
        public int MaterialSlots => _grid.UsedSlots;

        public int UsedSlots => _grid.UsedSlots + _parts.Count;

        public bool IsFull => _grid.IsFull;

        /// <summary>True when at least one hold slot is completely empty (usable by a story part).</summary>
        public bool HasFreeSlot => UsedSlots < _slotCount;

        public int WaitingBundles { get; private set; }

        public int TotalUnits => _grid.TotalUnits;

        public MaterialSlot GetSlot(int index)
        {
            if (index < 0 || index >= _slotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Out of the hold.");
            }

            return index < _grid.SlotCount ? _grid.GetSlot(index) : default;
        }

        public string GetPart(int index)
        {
            if (index < 0 || index >= _slotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Out of the hold.");
            }

            int part = index - _grid.UsedSlots;
            return part >= 0 && part < _parts.Count ? _parts[part] : string.Empty;
        }

        public bool HasPart(string partId)
        {
            return !string.IsNullOrEmpty(partId) && _parts.Contains(partId);
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
            if (slotCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount), slotCount, "Slot count is non-negative.");
            }

            if (slotCount == _slotCount)
            {
                return;
            }

            int materialSlots = slotCount - _parts.Count;
            if (materialSlots < _grid.UsedSlots)
            {
                throw new InvalidOperationException(
                    $"Cannot shrink the grid to {slotCount} slots while {UsedSlots} are in use.");
            }

            _slotCount = slotCount;
            _grid.SetSlotCount(materialSlots);
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

        /// <summary>Puts story part <paramref name="partId"/> into a free hold slot; false when the hold is full.</summary>
        public bool TryAddPart(string partId)
        {
            if (string.IsNullOrEmpty(partId))
            {
                throw new ArgumentException("A story part needs its id.", nameof(partId));
            }

            if (_parts.Contains(partId))
            {
                return true;
            }

            if (UsedSlots >= _slotCount)
            {
                return false;
            }

            _parts.Add(partId);
            _grid.SetSlotCount(_slotCount - _parts.Count);
            Announce();
            return true;
        }

        /// <summary>Takes story part <paramref name="partId"/> out of the hold; false when it was not there.</summary>
        public bool RemovePart(string partId)
        {
            if (string.IsNullOrEmpty(partId) || !_parts.Remove(partId))
            {
                return false;
            }

            _grid.SetSlotCount(_slotCount - _parts.Count);
            Announce();
            return true;
        }

        /// <summary>Empties any carried story parts before a load lays the saved ones in again.</summary>
        public void ClearParts()
        {
            if (_parts.Count == 0)
            {
                return;
            }

            _parts.Clear();
            _grid.SetSlotCount(_slotCount);
            Announce();
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

        /// <summary>Empties the hold's material stacks before a load lays the saved units in again.</summary>
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
            _events.Publish(new CargoChanged(UsedSlots, _slotCount));
        }
    }
}
