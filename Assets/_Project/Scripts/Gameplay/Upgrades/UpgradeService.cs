using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Upgrade levels and their effects. A purchase spends scrap through the wallet, raises the level and publishes
    /// <see cref="UpgradePurchased"/>; a radio upgrade also publishes <see cref="SignalRadiusChanged"/>, and a level
    /// that unlocks a rover ability grants it through <see cref="IRoverAbilities"/> (before the event, so listeners
    /// already see it). Restoring a save applies levels silently (no purchase event), re-grants their abilities and
    /// republishes the signal radius.
    /// </summary>
    public sealed class UpgradeService
    {
        private readonly EventBus _events;
        private readonly ScrapWallet _wallet;
        private readonly IRoverAbilities _abilities;
        private readonly UpgradeDefinition[] _definitions;
        private readonly int[] _levels;

        public UpgradeService(EventBus events, ScrapWallet wallet, IRoverAbilities abilities,
            IReadOnlyList<UpgradeDefinition> definitions)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _definitions = new UpgradeDefinition[definitions.Count];
            for (int i = 0; i < _definitions.Length; i++)
            {
                string problem = definitions[i] == null ? "is empty" : definitions[i].Validate();
                if (problem != null)
                {
                    throw new ArgumentException($"Upgrade {i} {problem}.", nameof(definitions));
                }

                _definitions[i] = definitions[i];
            }

            _levels = new int[_definitions.Length];
        }

        public IReadOnlyList<UpgradeDefinition> Definitions => _definitions;

        /// <summary>Product of every upgrade's light boost at its current level (1 = nothing bought).</summary>
        public float LightBoost
        {
            get
            {
                float boost = 1f;
                for (int i = 0; i < _definitions.Length; i++)
                {
                    boost *= _definitions[i].LightBoostAt(_levels[i]);
                }

                return boost;
            }
        }

        public UpgradeDefinition Find(string id)
        {
            int index = IndexOf(id);
            return index >= 0 ? _definitions[index] : null;
        }

        /// <summary>Levels bought of <paramref name="id"/> (0 when none or unknown).</summary>
        public int LevelOf(string id)
        {
            int index = IndexOf(id);
            return index >= 0 ? _levels[index] : 0;
        }

        public bool TryGetOffer(string id, out UpgradeOffer offer)
        {
            int index = IndexOf(id);
            if (index < 0)
            {
                offer = default;
                return false;
            }

            UpgradeDefinition definition = _definitions[index];
            int level = _levels[index];
            bool canAfford = level < definition.MaxLevel && _wallet.CanAfford(definition.Levels[level].Cost);
            offer = new UpgradeOffer(definition, level, canAfford);
            return true;
        }

        public PurchaseResult Purchase(string id)
        {
            int index = IndexOf(id);
            if (index < 0)
            {
                return PurchaseResult.Unknown;
            }

            UpgradeDefinition definition = _definitions[index];
            int level = _levels[index];
            if (level >= definition.MaxLevel)
            {
                return PurchaseResult.Maxed;
            }

            if (!_wallet.TrySpend(definition.Levels[level].Cost))
            {
                return PurchaseResult.CannotAfford;
            }

            _levels[index] = level + 1;
            GrantAbilities(index);
            _events.Publish(new UpgradePurchased(definition.Id, level + 1));
            PublishSignal(index);
            return PurchaseResult.Purchased;
        }

        /// <summary>Announces the current signal radius of every radio upgrade (at boot, after a load).</summary>
        public void PublishSignals()
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                PublishSignal(i);
            }
        }

        public UpgradesSaveData Capture()
        {
            var data = new UpgradesSaveData { upgrades = new UpgradeSaveData[_definitions.Length] };
            for (int i = 0; i < _definitions.Length; i++)
            {
                data.upgrades[i] = new UpgradeSaveData { id = _definitions[i].Id, level = _levels[i] };
            }

            return data;
        }

        /// <summary>Applies saved levels by id (clamped; unknown ids ignored) without purchase events.</summary>
        public void Restore(UpgradesSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            foreach (UpgradeSaveData saved in data.upgrades)
            {
                int index = saved != null ? IndexOf(saved.id) : -1;
                if (index >= 0)
                {
                    _levels[index] = Math.Max(0, Math.Min(saved.level, _definitions[index].MaxLevel));
                    GrantAbilities(index);
                }
            }

            PublishSignals();
        }

        /// <summary>Grants every ability unlocked by the bought levels of upgrade <paramref name="index"/>.</summary>
        private void GrantAbilities(int index)
        {
            UpgradeDefinition definition = _definitions[index];
            for (int level = 0; level < _levels[index]; level++)
            {
                UpgradeLevel bought = definition.Levels[level];
                if (bought.GrantsAbility)
                {
                    _abilities.Grant(bought.Ability);
                }
            }
        }

        private void PublishSignal(int index)
        {
            UpgradeDefinition definition = _definitions[index];
            float radius = definition.SignalRadiusAt(_levels[index]);
            if (radius > 0f)
            {
                _events.Publish(new SignalRadiusChanged(radius));
            }
        }

        private int IndexOf(string id)
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                if (string.Equals(_definitions[i].Id, id, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
