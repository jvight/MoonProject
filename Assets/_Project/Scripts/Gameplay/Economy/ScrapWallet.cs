using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The scrap balance. Every change publishes <see cref="CurrencyChanged"/>; a restore from a save publishes a
    /// delta of 0 (a refresh, not a gain).
    /// </summary>
    public sealed class ScrapWallet : IScrapWallet
    {
        private readonly EventBus _events;

        public ScrapWallet(EventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public int Balance { get; private set; }

        public bool CanAfford(int cost)
        {
            return cost >= 0 && Balance >= cost;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Scrap gains are positive.");
            }

            Balance = checked(Balance + amount);
            _events.Publish(new CurrencyChanged(Balance, amount));
        }

        /// <summary>Spends <paramref name="cost"/> if the balance covers it; nothing changes otherwise.</summary>
        public bool TrySpend(int cost)
        {
            if (cost <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), cost, "Costs are positive.");
            }

            if (Balance < cost)
            {
                return false;
            }

            Balance -= cost;
            _events.Publish(new CurrencyChanged(Balance, -cost));
            return true;
        }

        public WalletSaveData Capture()
        {
            return new WalletSaveData { balance = Balance };
        }

        public void Restore(WalletSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.balance < 0)
            {
                throw new FormatException($"Saved scrap balance {data.balance} is negative.");
            }

            Balance = data.balance;
            _events.Publish(new CurrencyChanged(Balance, 0));
        }
    }
}
