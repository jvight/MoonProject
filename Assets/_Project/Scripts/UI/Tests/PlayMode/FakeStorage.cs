using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>A grid of slots a test sets by hand (07's hold or home's shelves).</summary>
    public class FakeSlots : IMaterialSlots
    {
        private readonly List<MaterialSlot> _slots = new List<MaterialSlot>();

        public int SlotCount => _slots.Count;

        public int UsedSlots
        {
            get
            {
                int used = 0;
                foreach (MaterialSlot slot in _slots)
                {
                    used += slot.IsEmpty ? 0 : 1;
                }

                return used;
            }
        }

        public bool IsFull => UsedSlots == SlotCount;

        public MaterialSlot GetSlot(int index)
        {
            return _slots[index];
        }

        public int Units(SalvageMaterial material)
        {
            int units = 0;
            foreach (MaterialSlot slot in _slots)
            {
                units += !slot.IsEmpty && slot.Material == material ? slot.Units : 0;
            }

            return units;
        }

        public int FreeUnits(SalvageMaterial material)
        {
            return 0;
        }

        /// <summary>Empties the first slot holding <paramref name="material"/> and returns what it held.</summary>
        public bool TryTake(SalvageMaterial material, out MaterialSlot taken)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].Material == material)
                {
                    taken = _slots[i];
                    _slots[i] = new MaterialSlot(material, 0, taken.Capacity);
                    return true;
                }
            }

            taken = default;
            return false;
        }

        /// <summary>Puts <paramref name="slot"/> into the first empty slot; false when none is.</summary>
        public bool TryPut(MaterialSlot slot)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    _slots[i] = slot;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Replaces every slot: <paramref name="empty"/> empty slots after the filled ones.</summary>
        public void Set(int empty, params MaterialSlot[] filled)
        {
            _slots.Clear();
            _slots.AddRange(filled);
            for (int i = 0; i < empty; i++)
            {
                _slots.Add(new MaterialSlot(SalvageMaterial.Metal, 0, 4));
            }
        }
    }

    /// <summary>07's hold, set by hand.</summary>
    public sealed class FakeHold : FakeSlots, ICargoHold
    {
        public int WaitingBundles { get; set; }
    }

    /// <summary>Home's shelves, set by hand.</summary>
    public sealed class FakeHome : FakeSlots, IHomeStorage
    {
        public int ShelfCount => 0;

        public HomeShelf GetShelf(int index)
        {
            throw new System.ArgumentOutOfRangeException(nameof(index));
        }

        public int OverflowUnits(SalvageMaterial material)
        {
            return 0;
        }
    }

    /// <summary>07's equipment, set by hand: what is fitted, known and waiting, and whether 07 is at the station.</summary>
    public sealed class FakeEquipment : IRoverEquipment
    {
        private readonly Dictionary<RoverSocket, RoverKitPiece> _fitted = new Dictionary<RoverSocket, RoverKitPiece>();
        private readonly HashSet<RoverSocket> _known = new HashSet<RoverSocket>();
        private readonly List<RoverKitPiece> _row = new List<RoverKitPiece>();

        /// <summary>True while 07 rests at the part's station (the bay or the port).</summary>
        public bool AtStation { get; set; }

        /// <summary>Parts fitted through <see cref="TryFit"/>.</summary>
        public int Fits { get; private set; }

        /// <summary>Parts taken off through <see cref="TryUnequip"/>.</summary>
        public int Removals { get; private set; }

        public int RowCapacity => 4;

        public int RowCount => _row.Count;

        public RoverSocket SocketOf(RoverKitPiece part)
        {
            switch (part)
            {
                case RoverKitPiece.HoverCoils: return RoverSocket.Belly;
                case RoverKitPiece.LampBar: return RoverSocket.Lamp;
                case RoverKitPiece.CapacitorDrums: return RoverSocket.Flanks;
                case RoverKitPiece.CargoRack: return RoverSocket.Hold;
                case RoverKitPiece.SolarCell: return RoverSocket.Wing;
                case RoverKitPiece.BatterySpine: return RoverSocket.Back;
                case RoverKitPiece.TapeDeck: return RoverSocket.Deck;
                case RoverKitPiece.TetherRing: return RoverSocket.Tether;
                case RoverKitPiece.FocusLens: return RoverSocket.Cutter;
                case RoverKitPiece.SideBins: return RoverSocket.Panniers;
                default: return RoverSocket.Wing;
            }
        }

        public bool TryGetFitted(RoverSocket socket, out RoverKitPiece part)
        {
            return _fitted.TryGetValue(socket, out part);
        }

        public bool IsKnown(RoverSocket socket)
        {
            return _fitted.ContainsKey(socket) || _known.Contains(socket);
        }

        public RoverKitPiece RowPart(int index)
        {
            return _row[index];
        }

        public bool CanFit(RoverKitPiece part)
        {
            return AtStation;
        }

        public bool TryFit(int rowIndex)
        {
            if (!AtStation || rowIndex >= _row.Count)
            {
                return false;
            }

            RoverKitPiece part = _row[rowIndex];
            _row.RemoveAt(rowIndex);
            _fitted[SocketOf(part)] = part;
            Fits++;
            return true;
        }

        public bool TryUnequip(RoverSocket socket)
        {
            if (!AtStation || !_fitted.TryGetValue(socket, out RoverKitPiece part) || _row.Count >= RowCapacity)
            {
                return false;
            }

            _fitted.Remove(socket);
            _row.Add(part);
            Removals++;
            return true;
        }

        /// <summary>A part was crafted: it waits in the row and its socket is known.</summary>
        public void Craft(RoverKitPiece part)
        {
            _row.Add(part);
            _known.Add(SocketOf(part));
        }

        /// <summary>A part is already on 07.</summary>
        public void Fit(RoverKitPiece part)
        {
            _fitted[SocketOf(part)] = part;
        }
    }

    /// <summary>Whether 07 rests on the dock, set by a test.</summary>
    public sealed class FakeDock : IDockState
    {
        public bool IsDocked { get; set; }
    }

    /// <summary>Moves a whole slot between two fakes while docked, counting the moves.</summary>
    public sealed class FakeTransfer : IStackTransfer
    {
        private readonly FakeSlots _hold;
        private readonly FakeSlots _home;
        private readonly FakeDock _dock;

        public FakeTransfer(FakeSlots hold, FakeSlots home, FakeDock dock)
        {
            _hold = hold;
            _home = home;
            _dock = dock;
        }

        /// <summary>How many moves went through.</summary>
        public int Moves { get; private set; }

        public bool TryMove(SalvageMaterial material, bool toHome, out int moved)
        {
            moved = 0;
            FakeSlots from = toHome ? _hold : _home;
            FakeSlots to = toHome ? _home : _hold;
            if (!_dock.IsDocked || !from.TryTake(material, out MaterialSlot taken))
            {
                return false;
            }

            if (!to.TryPut(taken))
            {
                from.TryPut(taken);
                return false;
            }

            moved = taken.Units;
            Moves++;
            return true;
        }
    }

    /// <summary>Every material described with the names the tables hold.</summary>
    public sealed class FakeCatalog : IMaterialCatalog
    {
        private const int SalvageStack = 4;
        private const int LunarStack = 6;

        public bool Contains(SalvageMaterial material)
        {
            return true;
        }

        public MaterialFamily FamilyOf(SalvageMaterial material)
        {
            return (int)material < 3 ? MaterialFamily.Salvage
                : (int)material < 8 ? MaterialFamily.Lunar : MaterialFamily.Refined;
        }

        public int StackSize(SalvageMaterial material)
        {
            return FamilyOf(material) == MaterialFamily.Salvage ? SalvageStack : LunarStack;
        }

        public string NameKey(SalvageMaterial material)
        {
            return UiKeys.MaterialName(material);
        }

        public string JobKey(SalvageMaterial material)
        {
            return UiKeys.MaterialName(material) + ".job";
        }

        public string SourceKey(SalvageMaterial material)
        {
            return UiKeys.MaterialName(material) + ".source";
        }
    }
}
