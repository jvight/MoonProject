using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>One material in the <see cref="MaterialCatalog"/>.</summary>
    [Serializable]
    public sealed class MaterialEntry
    {
        [Tooltip("The material (its saved value).")]
        [SerializeField] private SalvageMaterial _material;

        [Tooltip("Stable id, the root of its localization keys (\"metal\" for material.metal).")]
        [SerializeField] private string _id = string.Empty;

        [Tooltip("Where it comes from: salvage, the Moon itself, or refined at home.")]
        [SerializeField] private MaterialFamily _family;

        [Tooltip("Units one slot of 07's hold or a home shelf holds (4 for salvage bundles, 6 for ores).")]
        [Range(1, 12)] [SerializeField] private int _stackSize = 4;

        [Tooltip("True when 07 can gather it in this version; false while it is only seen (the orange vent field).")]
        [SerializeField] private bool _harvestable = true;

        public MaterialEntry(SalvageMaterial material, string id, MaterialFamily family, int stackSize,
            bool harvestable)
        {
            _material = material;
            _id = id;
            _family = family;
            _stackSize = stackSize;
            _harvestable = harvestable;
        }

        public SalvageMaterial Material => _material;

        public string Id => _id;

        public MaterialFamily Family => _family;

        public int StackSize => _stackSize;

        public bool Harvestable => _harvestable;
    }
}
