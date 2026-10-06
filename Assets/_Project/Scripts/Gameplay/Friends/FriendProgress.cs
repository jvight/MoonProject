using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A friend's progress: which parts are gathered, whether it has answered the sonar, and its state (Dormant →
    /// PartsGathering → Repairing → Awake). Pure; saved through <see cref="FriendSaveData"/>. Nothing ever goes back.
    /// </summary>
    public sealed class FriendProgress
    {
        /// <summary>Most parts a friend can need (bits of the saved mask).</summary>
        public const int MaxParts = 5;

        public FriendProgress(string id, int partCount)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A friend needs an id.", nameof(id));
            }

            if (partCount < 1 || partCount > MaxParts)
            {
                throw new ArgumentOutOfRangeException(nameof(partCount), partCount, $"1 to {MaxParts} parts.");
            }

            Id = id;
            PartCount = partCount;
        }

        public string Id { get; }

        public int PartCount { get; }

        public FriendState State { get; private set; }

        /// <summary>Bit i set when part i is gathered.</summary>
        public int PartMask { get; private set; }

        /// <summary>True once it has answered a ping (or 07 has met it).</summary>
        public bool Discovered { get; private set; }

        public int Collected
        {
            get
            {
                int count = 0;
                for (int i = 0; i < PartCount; i++)
                {
                    if (IsCollected(i))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool AllPartsGathered => Collected == PartCount;

        /// <summary>Still broken: it answers pings with its broken chirp.</summary>
        public bool AnswersSonar => State == FriendState.Dormant || State == FriendState.PartsGathering;

        /// <summary>Every part is gathered and the repair has not begun.</summary>
        public bool CanRepair => State == FriendState.PartsGathering && AllPartsGathered;

        public bool IsCollected(int part)
        {
            return part >= 0 && part < PartCount && (PartMask & (1 << part)) != 0;
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

        public void MarkDiscovered()
        {
            Discovered = true;
        }

        public void BeginRepair()
        {
            if (!CanRepair)
            {
                throw new InvalidOperationException($"{Id} cannot be repaired while {State} " +
                                                    $"({Collected}/{PartCount} parts).");
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
            return new FriendSaveData { id = Id, state = (int)State, parts = PartMask, discovered = Discovered };
        }

        /// <summary>
        /// Applies saved progress. A repair caught mid-way comes back finished (it can only end one way), and the
        /// state always agrees with the parts.
        /// </summary>
        public void Restore(FriendSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            int all = (1 << PartCount) - 1;
            var state = (FriendState)data.state;
            PartMask = data.parts & all;
            Discovered = data.discovered;
            if (state == FriendState.Repairing || state == FriendState.Awake)
            {
                PartMask = all;
                State = FriendState.Awake;
                Discovered = true;
            }
            else
            {
                State = PartMask == 0 ? FriendState.Dormant : FriendState.PartsGathering;
            }
        }
    }
}
