using System;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// What the station 07 is parked at still sells, in the station's own order (<see cref="IUpgradeShop"/>'s listing
    /// with the fully bought ones left out), each with its next level, and which one is picked (pure logic,
    /// EditMode-tested). The pick stays on the same upgrade when the list changes around it; when the picked one is
    /// bought out, it moves to the one that took its place. Stepping wraps around. Allocation-free once its buffers
    /// have grown to the station's size.
    /// </summary>
    internal sealed class BenchChoice
    {
        private UpgradeDefinition[] _choices = Array.Empty<UpgradeDefinition>();
        private int[] _levels = Array.Empty<int>();
        private UpgradeDefinition[] _nextChoices = Array.Empty<UpgradeDefinition>();
        private int[] _nextLevels = Array.Empty<int>();

        /// <summary>How many upgrades are on offer.</summary>
        public int Count { get; private set; }

        /// <summary>The picked choice's index, or -1 when nothing is on offer.</summary>
        public int Selected { get; private set; } = -1;

        /// <summary>Changes whenever the list or a choice's level changes (views rebuild their rows then).</summary>
        public int Version { get; private set; }

        /// <summary>The picked upgrade, or null when nothing is on offer.</summary>
        public UpgradeDefinition Current => Selected >= 0 ? _choices[Selected] : null;

        /// <summary>Choice <paramref name="index"/>.</summary>
        public UpgradeDefinition At(int index)
        {
            CheckIndex(index);
            return _choices[index];
        }

        /// <summary>The level choice <paramref name="index"/> would be bought to (1-based).</summary>
        public int NextLevelAt(int index)
        {
            CheckIndex(index);
            return _levels[index] + 1;
        }

        /// <summary>What choice <paramref name="index"/>'s next level costs.</summary>
        public Recipe CostAt(int index)
        {
            CheckIndex(index);
            return _choices[index].Levels[_levels[index]].Recipe;
        }

        /// <summary>Re-reads what <paramref name="shop"/> offers. Returns true when anything changed.</summary>
        public bool Refresh(IUpgradeShop shop)
        {
            if (shop == null)
            {
                throw new ArgumentNullException(nameof(shop));
            }

            int total = shop.StationUpgradeCount;
            Reserve(total);
            int count = 0;
            bool changed = false;
            for (int i = 0; i < total; i++)
            {
                UpgradeDefinition upgrade = shop.StationUpgradeAt(i);
                if (!shop.TryGetOffer(upgrade.Id, out UpgradeOffer offer) || offer.IsMaxed)
                {
                    continue;
                }

                changed |= count >= Count || _choices[count] != upgrade || _levels[count] != offer.CurrentLevel;
                _nextChoices[count] = upgrade;
                _nextLevels[count] = offer.CurrentLevel;
                count++;
            }

            if (!changed && count == Count)
            {
                return false;
            }

            UpgradeDefinition picked = Current;
            int pickedIndex = Selected;
            Swap();
            for (int i = count; i < _choices.Length; i++)
            {
                _choices[i] = null;
            }

            Count = count;
            Selected = count == 0 ? -1 : Math.Min(Math.Max(pickedIndex, 0), count - 1);
            for (int i = 0; i < count; i++)
            {
                if (_choices[i] == picked)
                {
                    Selected = i;
                    break;
                }
            }

            Version++;
            return true;
        }

        /// <summary>Moves the pick <paramref name="direction"/> places, wrapping. True when it moved.</summary>
        public bool Step(int direction)
        {
            if (Count < 2 || direction == 0)
            {
                return false;
            }

            Selected = ((Selected + direction) % Count + Count) % Count;
            return true;
        }

        /// <summary>Forgets the list and the pick (07 left the pad: next time starts from the first choice).</summary>
        public void Clear()
        {
            if (Count == 0 && Selected < 0)
            {
                return;
            }

            for (int i = 0; i < Count; i++)
            {
                _choices[i] = null;
            }

            Count = 0;
            Selected = -1;
            Version++;
        }

        private void Reserve(int total)
        {
            if (_choices.Length >= total)
            {
                return;
            }

            Array.Resize(ref _choices, total);
            Array.Resize(ref _levels, total);
            Array.Resize(ref _nextChoices, total);
            Array.Resize(ref _nextLevels, total);
        }

        private void Swap()
        {
            UpgradeDefinition[] choices = _choices;
            _choices = _nextChoices;
            _nextChoices = choices;
            int[] levels = _levels;
            _levels = _nextLevels;
            _nextLevels = levels;
        }

        private void CheckIndex(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"{Count} choices are on offer.");
            }
        }
    }
}
