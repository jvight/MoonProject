using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Upgrade definitions and a small shop made in the test (whatever the shipped content says), destroyed by
    /// <see cref="Dispose"/>.
    /// </summary>
    internal sealed class TestUpgrades : IDisposable, IUpgradeShop, IMaterialStock
    {
        private readonly List<UpgradeDefinition> _made = new List<UpgradeDefinition>();
        private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();

        /// <summary>What the parked station sells, in its order (empty: not parked).</summary>
        public UpgradeDefinition[] Catalog { get; set; } = Array.Empty<UpgradeDefinition>();

        public int Metal { get; set; }

        public int Wiring { get; set; }

        public int Optics { get; set; }

        public int Total => Metal + Wiring + Optics;

        public bool IsAtStation => Catalog.Length > 0;

        public UpgradeDefinition StationUpgrade => Catalog.Length > 0 ? Catalog[0] : null;

        public int StationUpgradeCount => Catalog.Length;

        /// <summary>A piece of bench kit: one level costing <paramref name="cost"/>, granting an ability.</summary>
        public UpgradeDefinition Kit(string id, Recipe cost)
        {
            return Make(id, UpgradeStationKind.Workshop, true, cost);
        }

        /// <summary>A radio-tower style upgrade: one level per recipe, none granting an ability.</summary>
        public UpgradeDefinition Tower(string id, params Recipe[] costs)
        {
            return Make(id, UpgradeStationKind.RadioTower, false, costs);
        }

        public UpgradeDefinition StationUpgradeAt(int index)
        {
            return Catalog[index];
        }

        public int LevelOf(string upgradeId)
        {
            return _levels.TryGetValue(upgradeId, out int level) ? level : 0;
        }

        public bool TryGetOffer(string upgradeId, out UpgradeOffer offer)
        {
            for (int i = 0; i < Catalog.Length; i++)
            {
                UpgradeDefinition upgrade = Catalog[i];
                if (upgrade.Id == upgradeId)
                {
                    int level = LevelOf(upgradeId);
                    bool maxed = level >= upgrade.MaxLevel;
                    offer = new UpgradeOffer(upgrade, level, !maxed && Has(upgrade.Levels[level].Recipe));
                    return true;
                }
            }

            offer = default;
            return false;
        }

        public PurchaseResult Purchase(string upgradeId)
        {
            _levels[upgradeId] = LevelOf(upgradeId) + 1;
            return PurchaseResult.Purchased;
        }

        public int Of(SalvageMaterial material)
        {
            return Materials.Of(this, material);
        }

        public bool Has(Recipe recipe)
        {
            return Metal >= recipe.Metal && Wiring >= recipe.Wiring && Optics >= recipe.Optics;
        }

        public void Dispose()
        {
            foreach (UpgradeDefinition upgrade in _made)
            {
                UnityEngine.Object.DestroyImmediate(upgrade);
            }

            _made.Clear();
        }

        private UpgradeDefinition Make(string id, UpgradeStationKind station, bool grantsAbility, params Recipe[] costs)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            var serialized = new SerializedObject(upgrade);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_station").intValue = (int)station;
            SerializedProperty levels = serialized.FindProperty("_levels");
            levels.arraySize = costs.Length;
            for (int i = 0; i < costs.Length; i++)
            {
                SerializedProperty level = levels.GetArrayElementAtIndex(i);
                SerializedProperty recipe = level.FindPropertyRelative("_recipe");
                recipe.FindPropertyRelative("_metal").intValue = costs[i].Metal;
                recipe.FindPropertyRelative("_wiring").intValue = costs[i].Wiring;
                recipe.FindPropertyRelative("_optics").intValue = costs[i].Optics;
                level.FindPropertyRelative("_grantsAbility").boolValue = grantsAbility;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            _made.Add(upgrade);
            return upgrade;
        }
    }
}
