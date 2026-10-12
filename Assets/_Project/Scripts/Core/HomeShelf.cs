namespace MoonProject.Core
{
    /// <summary>
    /// One supply shelf at home (docs/features/M3-25 §3.1): its tier and the run of <see cref="IHomeStorage"/> slots
    /// it holds. A free socket is not built and holds no slots.
    /// </summary>
    public readonly struct HomeShelf
    {
        public HomeShelf(int tier, int firstSlot, int slotCount, bool built, bool powered)
        {
            Tier = tier;
            FirstSlot = firstSlot;
            SlotCount = slotCount;
            Built = built;
            Powered = powered;
        }

        /// <summary>0 for the open shelves; higher tiers from 0.6.</summary>
        public int Tier { get; }

        /// <summary>Index of its first slot in <see cref="IHomeStorage"/>.</summary>
        public int FirstSlot { get; }

        public int SlotCount { get; }

        /// <summary>False for a free socket that waits to be built.</summary>
        public bool Built { get; }

        /// <summary>True once the tower powers home: its rack lamp lights.</summary>
        public bool Powered { get; }
    }
}
