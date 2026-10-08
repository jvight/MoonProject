using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// An upgrade crafted from materials: its id, the station that sells it, its state before any purchase (level 0)
    /// and its levels (each with its recipe) in order. Its name lives in the localization tables under
    /// "upgrade.&lt;id&gt;.name". Written by the Gameplay/Content builder; runtime code only reads it.
    /// </summary>
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [Tooltip("Stable id (saves and the localization keys upgrade.<id>.* use it), e.g. radio_tower.")]
        [SerializeField] private string _id = string.Empty;

        [Tooltip("Where it is sold: the radio tower's pad or Kenji's Rover Bay.")]
        [SerializeField] private UpgradeStationKind _station;

        [Tooltip("Clear radio signal radius (m) before any level is bought; 0 = not a radio upgrade.")]
        [Range(0f, 1000f)] [SerializeField] private float _baseSignalRadius;

        [Tooltip("The levels, cheapest first.")]
        [SerializeField] private UpgradeLevel[] _levels = Array.Empty<UpgradeLevel>();

        public string Id => _id;

        public UpgradeStationKind Station => _station;

        public float BaseSignalRadius => _baseSignalRadius;

        public IReadOnlyList<UpgradeLevel> Levels => _levels;

        public int MaxLevel => _levels.Length;

        /// <summary>Signal radius at <paramref name="level"/> (0 = before any purchase).</summary>
        public float SignalRadiusAt(int level)
        {
            return level <= 0 ? _baseSignalRadius : _levels[Mathf.Min(level, _levels.Length) - 1].SignalRadius;
        }

        /// <summary>Base light boost at <paramref name="level"/> (1 before any purchase).</summary>
        public float LightBoostAt(int level)
        {
            return level <= 0 ? 1f : _levels[Mathf.Min(level, _levels.Length) - 1].LightBoost;
        }

        /// <summary>Null when the definition is complete, else the first problem.</summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                return "has no id";
            }

            if (_levels.Length == 0)
            {
                return $"'{_id}' has no levels";
            }

            for (int i = 0; i < _levels.Length; i++)
            {
                if (_levels[i] == null || _levels[i].Recipe.IsFree)
                {
                    return $"'{_id}' level {i + 1} needs a recipe";
                }
            }

            return null;
        }

        internal void Populate(string id, UpgradeStationKind station, float baseSignalRadius, UpgradeLevel[] levels)
        {
            _id = id;
            _station = station;
            _baseSignalRadius = baseSignalRadius;
            _levels = levels ?? throw new ArgumentNullException(nameof(levels));
        }
    }
}
