using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A friend's progress: which parts and required items are gathered, whether it has answered the sonar, its state
    /// (Dormant → PartsGathering → Repairing → Awake) and whether it has had its first homecoming. Pure; saved through
    /// <see cref="FriendSaveData"/>. Nothing ever goes back.
    /// </summary>
    public sealed class FriendProgress
    {
        /// <summary>Most parts a friend can need (bits of the saved mask).</summary>
        public const int MaxParts = 5;

        /// <summary>Most required items (a cassette, a seed) a friend can need besides its parts.</summary>
        public const int MaxItems = 3;

        public FriendProgress(string id, int partCount)
            : this(id, partCount, 0)
        {
        }

        public FriendProgress(string id, int partCount, int itemCount)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A friend needs an id.", nameof(id));
            }

            if (partCount < 1 || partCount > MaxParts)
            {
                throw new ArgumentOutOfRangeException(nameof(partCount), partCount, $"1 to {MaxParts} parts.");
            }

            if (itemCount < 0 || itemCount > MaxItems)
            {
                throw new ArgumentOutOfRangeException(nameof(itemCount), itemCount, $"0 to {MaxItems} items.");
            }

            Id = id;
            PartCount = partCount;
            ItemCount = itemCount;
        }

        public string Id { get; }

        public int PartCount { get; }

        /// <summary>Required items besides the parts (Bell's cassette).</summary>
        public int ItemCount { get; }

        public FriendState State { get; private set; }

        /// <summary>Bit i set when part i is gathered.</summary>
        public int PartMask { get; private set; }

        /// <summary>Bit i set when required item i is held.</summary>
        public int ItemMask { get; private set; }

        /// <summary>True once it has answered a ping (or 07 has met it).</summary>
        public bool Discovered { get; private set; }

        /// <summary>True once it has greeted 07 coming home for the first time.</summary>
        public bool Welcomed { get; private set; }

        /// <summary>Parts gathered.</summary>
        public int Collected => Bits(PartMask, PartCount);

        /// <summary>Required items held.</summary>
        public int ItemsCollected => Bits(ItemMask, ItemCount);

        public bool AllPartsGathered => Collected == PartCount;

        /// <summary>Every part and every required item: nothing is missing for the repair.</summary>
        public bool AllGathered => AllPartsGathered && ItemsCollected == ItemCount;

        /// <summary>Still broken: it answers pings with its broken chirp.</summary>
        public bool AnswersSonar => State == FriendState.Dormant || State == FriendState.PartsGathering;

        /// <summary>Everything is gathered and the repair has not begun.</summary>
        public bool CanRepair => State == FriendState.PartsGathering && AllGathered;

        public bool IsCollected(int part)
        {
            return part >= 0 && part < PartCount && (PartMask & (1 << part)) != 0;
        }

        public bool IsItemCollected(int item)
        {
            return item >= 0 && item < ItemCount && (ItemMask & (1 << item)) != 0;
        }

        /// <summary>Gathers <paramref name="part"/>; false (nothing changes) when it was already gathered.</summary>
        public bool CollectPart(int part)
        {
            if (part < 0 || part >= PartCount)
            {
                throw new ArgumentOutOfRangeException(nameof(part), part, $"{Id} has {PartCount} parts.");
            }

            if (IsCollected(part) || !AnswersSonar)
            {
                return false;
            }

            PartMask |= 1 << part;
            State = FriendState.PartsGathering;
            return true;
        }

        /// <summary>Marks required <paramref name="item"/> held; false (nothing changes) when it already was.</summary>
        public bool CollectItem(int item)
        {
            if (item < 0 || item >= ItemCount)
            {
                throw new ArgumentOutOfRangeException(nameof(item), item, $"{Id} needs {ItemCount} items.");
            }

            if (IsItemCollected(item) || !AnswersSonar)
            {
                return false;
            }

            ItemMask |= 1 << item;
            State = FriendState.PartsGathering;
            return true;
        }

        public void MarkDiscovered()
        {
            Discovered = true;
        }

        /// <summary>
        /// Its greeting as 07 comes home: true the first time only (an awake friend), when the first homecoming is
        /// worth a ticker line.
        /// </summary>
        public bool Welcome()
        {
            if (Welcomed || State != FriendState.Awake)
            {
                return false;
            }

            Welcomed = true;
            return true;
        }

        public void BeginRepair()
        {
            if (!CanRepair)
            {
                throw new InvalidOperationException($"{Id} cannot be repaired while {State} " +
                                                    $"({Collected}/{PartCount} parts, " +
                                                    $"{ItemsCollected}/{ItemCount} items).");
            }

            State = FriendState.Repairing;
        }

        public void FinishRepair()
        {
            if (State != FriendState.Repairing)
            {
                throw new InvalidOperationException($"{Id} is not being repaired ({State}).");
            }

            State = FriendState.Awake;
        }

        public FriendSaveData Capture()
        {
            return new FriendSaveData
            {
                id = Id,
                state = (int)State,
                parts = PartMask,
                items = ItemMask,
                discovered = Discovered,
                welcomed = Welcomed,
            };
        }

        /// <summary>
        /// Applies saved progress. A repair caught mid-way comes back finished (it can only end one way), and the
        /// state always agrees with the parts and items.
        /// </summary>
        public void Restore(FriendSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            int allParts = (1 << PartCount) - 1;
            int allItems = (1 << ItemCount) - 1;
            var state = (FriendState)data.state;
            PartMask = data.parts & allParts;
            ItemMask = data.items & allItems;
            Discovered = data.discovered;
            if (state == FriendState.Repairing || state == FriendState.Awake)
            {
                PartMask = allParts;
                ItemMask = allItems;
                State = FriendState.Awake;
                Discovered = true;
                Welcomed = data.welcomed;
            }
            else
            {
                State = PartMask == 0 && ItemMask == 0 ? FriendState.Dormant : FriendState.PartsGathering;
                Welcomed = false;
            }
        }

        private static int Bits(int mask, int count)
        {
            int bits = 0;
            for (int i = 0; i < count; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    bits++;
                }
            }

            return bits;
        }
    }
}
