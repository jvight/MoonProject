using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A friend's state for the UI (parts readout, repair prompt, log card): a snapshot, allocation-free.
    /// </summary>
    public readonly struct FriendStatus
    {
        /// <summary>A friend that needs parts only (no required items).</summary>
        public FriendStatus(FriendState state, int collected, int total, bool discovered, bool canRepair,
            Vector3 position)
            : this(state, collected, total, 0, 0, discovered, canRepair, position)
        {
        }

        public FriendStatus(FriendState state, int collected, int total, int itemsCollected, int itemsTotal,
            bool discovered, bool canRepair, Vector3 position)
        {
            State = state;
            Collected = collected;
            Total = total;
            ItemsCollected = itemsCollected;
            ItemsTotal = itemsTotal;
            Discovered = discovered;
            CanRepair = canRepair;
            Position = position;
        }

        public FriendState State { get; }

        /// <summary>Parts gathered.</summary>
        public int Collected { get; }

        /// <summary>Parts it needs.</summary>
        public int Total { get; }

        /// <summary>Required items held (Bell's cassette: her fourth lamp).</summary>
        public int ItemsCollected { get; }

        /// <summary>Required items it needs besides its parts (0 for most friends).</summary>
        public int ItemsTotal { get; }

        /// <summary>It has answered a ping (or been met).</summary>
        public bool Discovered { get; }

        /// <summary>Every part and item is gathered: 07 can repair it.</summary>
        public bool CanRepair { get; }

        /// <summary>Where it is now (its site while broken).</summary>
        public Vector3 Position { get; }
    }
}
