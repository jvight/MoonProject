using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Home's open supply shelves (docs/features/M3-25 §3, <see cref="IHomeStorage"/>): the built-in shelves, then the
    /// free sockets a build turns into shelves, their slots in shelf order. Units a migrated save brought beyond the
    /// shelves' room wait in overflow crates beside them: counted, spent first, never deleted. Every change publishes
    /// <see cref="HomeStorageChanged"/>.
    /// </summary>
    public sealed class HomeStorage : IHomeStorage
    {
        private readonly EventBus _events;
        private readonly MaterialGrid _grid;
        private readonly HomeShelf[] _shelves;
        private readonly int[] _overflow = new int[SalvageMaterials.Count];

        /// <param name="shelfSlots">Slots of every shelf, the built-in ones first.</param>
        /// <param name="builtIn">How many of them come with the base.</param>
        public HomeStorage(EventBus events, IMaterialCatalog catalog, int[] shelfSlots, int builtIn)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            if (shelfSlots == null || builtIn < 0 || builtIn > shelfSlots.Length)
            {
                throw new ArgumentException("Home needs its shelves' slots and how many are built in.",
                    nameof(shelfSlots));
            }

            _shelves = new HomeShelf[shelfSlots.Length];
            int first = 0;
            for (int i = 0; i < shelfSlots.Length; i++)
            {
                _shelves[i] = new HomeShelf(0, first, shelfSlots[i], i < builtIn, false);
                first += shelfSlots[i];
            }

            BuiltIn = builtIn;
            _grid = new MaterialGrid(catalog, BuiltSlots());
        }

        /// <summary>Shelves that come with the base (never a socket to build).</summary>
        public int BuiltIn { get; }

        public int SlotCount => _grid.SlotCount;

        public int UsedSlots => _grid.UsedSlots;

        public bool IsFull => _grid.IsFull;

        public int ShelfCount => _shelves.Length;

        /// <summary>Shelves built so far, the built-in ones included.</summary>
        public int BuiltCount
        {
            get
            {
                int built = 0;
                for (int i = 0; i < _shelves.Length; i++)
                {
                    built += _shelves[i].Built ? 1 : 0;
                }

                return built;
            }
        }

        public MaterialSlot GetSlot(int index)
        {
            return _grid.GetSlot(index);
        }

        public HomeShelf GetShelf(int index)
        {
            return _shelves[index];
        }

        /// <summary>Units on the shelves and in overflow crates.</summary>
        public int Units(SalvageMaterial material)
        {
            return _grid.Units(material) + _overflow[(int)material];
        }

        public int FreeUnits(SalvageMaterial material)
        {
            return _grid.FreeUnits(material);
        }

        public int OverflowUnits(SalvageMaterial material)
        {
            return _overflow[(int)material];
        }

        /// <summary>The next free socket to build, or -1 once every socket holds a shelf.</summary>
        public int NextSocket
        {
            get
            {
                for (int i = 0; i < _shelves.Length; i++)
                {
                    if (!_shelves[i].Built)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        /// <summary>Puts what fits of <paramref name="units"/> on the shelves; returns how many went on.</summary>
        public int Add(SalvageMaterial material, int units)
        {
            int fitted = _grid.Add(material, units);
            if (fitted > 0)
            {
                Announce();
            }

            return fitted;
        }

        /// <summary>
        /// Takes up to <paramref name="units"/>, from the overflow crates first (they leave no room until spent), then
        /// the shelves; returns how many came out.
        /// </summary>
        public int Remove(SalvageMaterial material, int units)
        {
            if (units < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(units), units, "Removes a positive amount.");
            }

            int m = (int)material;
            int fromOverflow = Math.Min(units, _overflow[m]);
            _overflow[m] -= fromOverflow;
            int taken = fromOverflow + _grid.Remove(material, units - fromOverflow);
            if (taken > 0)
            {
                Announce();
            }

            return taken;
        }

        /// <summary>A shelf goes up on <paramref name="socket"/>: its slots join the grid.</summary>
        public void Build(int socket)
        {
            HomeShelf shelf = _shelves[socket];
            if (shelf.Built)
            {
                throw new InvalidOperationException($"Shelf {socket} is already built.");
            }

            _shelves[socket] = new HomeShelf(shelf.Tier, shelf.FirstSlot, shelf.SlotCount, true, shelf.Powered);
            _grid.SetSlotCount(BuiltSlots());
            _events.Publish(new ShelfBuilt(socket, shelf.Tier));
            Announce();
        }

        /// <summary>The tower powers home (or no longer): every shelf's rack lamp follows.</summary>
        public void SetPowered(bool powered)
        {
            for (int i = 0; i < _shelves.Length; i++)
            {
                HomeShelf shelf = _shelves[i];
                _shelves[i] = new HomeShelf(shelf.Tier, shelf.FirstSlot, shelf.SlotCount, shelf.Built, powered);
            }
        }

        /// <summary>
        /// Lays a save in: <paramref name="built"/> shelves standing (the built-in ones at least), then each
        /// material's units on the shelves, and whatever does not fit into overflow crates. Nothing is dropped.
        /// </summary>
        public void Restore(int built, int[] units, int[] overflow)
        {
            if (units == null || overflow == null)
            {
                throw new ArgumentNullException(units == null ? nameof(units) : nameof(overflow));
            }

            for (int i = 0; i < _shelves.Length; i++)
            {
                HomeShelf shelf = _shelves[i];
                _shelves[i] = new HomeShelf(shelf.Tier, shelf.FirstSlot, shelf.SlotCount, i < Math.Max(built, BuiltIn),
                    shelf.Powered);
            }

            _grid.Clear();
            _grid.SetSlotCount(BuiltSlots());
            Array.Clear(_overflow, 0, _overflow.Length);
            for (int m = 0; m < SalvageMaterials.Count; m++)
            {
                int saved = (m < units.Length ? units[m] : 0) + (m < overflow.Length ? overflow[m] : 0);
                if (saved < 0)
                {
                    throw new FormatException($"Home's saved {(SalvageMaterial)m} is negative ({saved}).");
                }

                _overflow[m] = saved > 0 ? saved - _grid.Add((SalvageMaterial)m, saved) : 0;
            }

            Announce();
        }

        /// <summary>Puts <paramref name="units"/> straight into overflow crates (a stock that already overflowed).</summary>
        public void AddOverflow(SalvageMaterial material, int units)
        {
            if (units <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(units), units, "Adds a positive amount.");
            }

            _overflow[(int)material] += units;
            Announce();
        }

        /// <summary>The shelves' units per material (overflow apart), for the save.</summary>
        public int[] CaptureShelves()
        {
            var units = new int[SalvageMaterials.Count];
            for (int m = 0; m < units.Length; m++)
            {
                units[m] = _grid.Units((SalvageMaterial)m);
            }

            return units;
        }

        /// <summary>The overflow crates' units per material, for the save.</summary>
        public int[] CaptureOverflow()
        {
            return (int[])_overflow.Clone();
        }

        private int BuiltSlots()
        {
            int slots = 0;
            for (int i = 0; i < _shelves.Length; i++)
            {
                slots += _shelves[i].Built ? _shelves[i].SlotCount : 0;
            }

            return slots;
        }

        private void Announce()
        {
            _events.Publish(new HomeStorageChanged(_grid.UsedSlots, _grid.SlotCount));
        }
    }
}
