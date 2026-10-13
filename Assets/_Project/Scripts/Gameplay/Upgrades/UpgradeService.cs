using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Upgrade levels and their effects. A purchase spends the level's recipe from 07's materials, raises the level and
    /// publishes <see cref="UpgradePurchased"/>; a radio upgrade also publishes <see cref="SignalRadiusChanged"/>. A
    /// level whose ability comes with a part (0.5.5 §3.5) grants nothing itself: the bay crafts the part and the
    /// ability follows it once fitted (<see cref="RoverEquipment"/>). Any other ability is granted through
    /// <see cref="IRoverAbilities"/> before the event, so listeners already see it. Restoring a save applies levels
    /// silently (no purchase event): an older save's owned parts load fitted, other abilities are granted again, and
    /// the signal radius is republished.
    /// </summary>
    public sealed class UpgradeService
    {
        private readonly EventBus _events;
        private readonly MaterialStock _stock;
        private readonly IRoverAbilities _abilities;
        private readonly RoverEquipment _equipment;
        private readonly EquipmentTable _parts;
        private readonly UpgradeDefinition[] _definitions;
        private readonly int[] _levels;

        /// <param name="equipment">07's equipment: an older save's owned parts are laid in fitted.</param>
        /// <param name="parts">Which abilities come with a part.</param>
        public UpgradeService(EventBus events, MaterialStock stock, IRoverAbilities abilities,
            RoverEquipment equipment, EquipmentTable parts, IReadOnlyList<UpgradeDefinition> definitions)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            _parts = parts ?? throw new ArgumentNullException(nameof(parts));
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

        /// <summary>
        /// How far the radio tower's clear signal reaches now (m): the widest radio upgrade's radius at its level (0
        /// when none is a radio upgrade). Home's circle in the station's reach.
        /// </summary>
        public float SignalRadius
        {
            get
            {
                float radius = 0f;
                for (int i = 0; i < _definitions.Length; i++)
                {
                    radius = Mathf.Max(radius, _definitions[i].SignalRadiusAt(_levels[i]));
                }

                return radius;
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
            bool canAfford = level < definition.MaxLevel && _stock.Has(definition.Levels[level].Recipe);
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

            if (!_stock.TrySpend(definition.Levels[level].Recipe))
            {
                return PurchaseResult.CannotAfford;
            }

            _levels[index] = level + 1;
            GrantAbilities(index, false);
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
                    GrantAbilities(index, true);
                }
            }

            PublishSignals();
        }

        /// <summary>
        /// The part <paramref name="level"/> (1-based) of upgrade <paramref name="id"/> brings, when its ability comes
        /// with one: the bay crafts it.
        /// </summary>
        public bool TryGetPart(string id, int level, out RoverKitPiece part)
        {
            int index = IndexOf(id);
            part = default;
            if (index < 0 || level < 1 || level > _definitions[index].MaxLevel)
            {
                return false;
            }

            UpgradeLevel bought = _definitions[index].Levels[level - 1];
            if (!bought.GrantsAbility || !_parts.TryFindByAbility(bought.Ability, out EquipmentPart entry))
            {
                return false;
            }

            part = entry.Piece;
            return true;
        }

        /// <summary>
        /// The recipe of the bought level that brought <paramref name="part"/>; false when no bought level did.
        /// </summary>
        public bool TryGetRecipeOf(RoverKitPiece part, out Recipe recipe)
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                for (int level = 1; level <= _levels[i]; level++)
                {
                    if (TryGetPart(_definitions[i].Id, level, out RoverKitPiece brought) && brought == part)
                    {
                        recipe = _definitions[i].Levels[level - 1].Recipe;
                        return true;
                    }
                }
            }

            recipe = default;
            return false;
        }

        /// <summary>
        /// The parts upgrade <paramref name="id"/> brings are known from now on (its blueprint decoded): 07's
        /// equipment screen names their sockets (0.5.5 §3.5).
        /// </summary>
        public void MarkPartsKnown(string id)
        {
            int index = IndexOf(id);
            for (int level = 1; index >= 0 && level <= _definitions[index].MaxLevel; level++)
            {
                if (TryGetPart(id, level, out RoverKitPiece part))
                {
                    _equipment.MarkKnown(part);
                }
            }
        }

        /// <summary>Every pinned blueprint's parts are known (after a load).</summary>
        public void MarkPinnedPartsKnown(IBlueprintBoard board)
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                if (board.IsPinned(_definitions[i].Id))
                {
                    MarkPartsKnown(_definitions[i].Id);
                }
            }
        }

        /// <summary>
        /// After a load, each bought part that is neither fitted nor waiting in the row goes back into the row (a game
        /// closed between a purchase and its craft), so a paid part is never lost.
        /// </summary>
        public void ReturnUnplacedParts()
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                for (int level = 1; level <= _levels[i]; level++)
                {
                    if (TryGetPart(_definitions[i].Id, level, out RoverKitPiece part))
                    {
                        _equipment.ReturnUnplaced(part);
                    }
                }
            }
        }

        /// <summary>
        /// Grants every ability unlocked by the bought levels of upgrade <paramref name="index"/> that comes without a
        /// part. A part's ability follows the part: on a <paramref name="load"/> an older save's part is laid in
        /// fitted; a purchase leaves it to the bay's craft.
        /// </summary>
        private void GrantAbilities(int index, bool load)
        {
            UpgradeDefinition definition = _definitions[index];
            for (int level = 0; level < _levels[index]; level++)
            {
                UpgradeLevel bought = definition.Levels[level];
                if (!bought.GrantsAbility)
                {
                    continue;
                }

                if (!_parts.TryFindByAbility(bought.Ability, out EquipmentPart part))
                {
                    _abilities.Grant(bought.Ability);
                }
                else if (load)
                {
                    _equipment.RestoreOwned(part.Piece);
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
