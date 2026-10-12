using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What each material is (docs/features/M3-27 §8, <see cref="IMaterialCatalog"/>): its family, how many units one
    /// slot of 07's hold or home's shelves holds, and whether it can be gathered in this version. Written by the
    /// content builder from <c>SalvageEconomy</c>; the localization keys follow the material's id
    /// ("material.metal", "material.metal.job", "material.metal.source") and are built once.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Gameplay/Material Catalog", fileName = "MaterialCatalog")]
    public sealed class MaterialCatalog : ScriptableObject, IMaterialCatalog
    {
        private const string KeyPrefix = "material.";

        [Tooltip("One entry per material that exists in this version; at most one per material.")]
        [SerializeField] private MaterialEntry[] _entries = Array.Empty<MaterialEntry>();

        private readonly int[] _index = new int[SalvageMaterials.Count];
        private readonly string[] _names = new string[SalvageMaterials.Count];
        private readonly string[] _jobs = new string[SalvageMaterials.Count];
        private readonly string[] _sources = new string[SalvageMaterials.Count];
        private bool _built;

        /// <summary>Entries in catalog order (tests and the economy table).</summary>
        public int Count => _entries.Length;

        public MaterialEntry EntryAt(int index)
        {
            return _entries[index];
        }

        public bool Contains(SalvageMaterial material)
        {
            return IndexOf(material) >= 0;
        }

        /// <summary>True when 07 can gather <paramref name="material"/> in this version (not only see it).</summary>
        public bool IsHarvestable(SalvageMaterial material)
        {
            return Entry(material).Harvestable;
        }

        public MaterialFamily FamilyOf(SalvageMaterial material)
        {
            return Entry(material).Family;
        }

        public int StackSize(SalvageMaterial material)
        {
            return Entry(material).StackSize;
        }

        public string NameKey(SalvageMaterial material)
        {
            Entry(material);
            return _names[(int)material];
        }

        public string JobKey(SalvageMaterial material)
        {
            Entry(material);
            return _jobs[(int)material];
        }

        public string SourceKey(SalvageMaterial material)
        {
            Entry(material);
            return _sources[(int)material];
        }

        /// <summary>Writes the catalog (the content builder).</summary>
        public void Populate(MaterialEntry[] entries)
        {
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
            _built = false;
        }

        /// <summary>The first problem with the catalog's content, or null when it is sound.</summary>
        public string Validate()
        {
            var seen = new bool[SalvageMaterials.Count];
            for (int i = 0; i < _entries.Length; i++)
            {
                MaterialEntry entry = _entries[i];
                if (entry == null)
                {
                    return $"entry {i} is empty";
                }

                int m = (int)entry.Material;
                if (m < 0 || m >= SalvageMaterials.Count)
                {
                    return $"entry {i} names an unknown material {m}";
                }

                if (seen[m])
                {
                    return $"{entry.Material} is listed twice";
                }

                seen[m] = true;
                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    return $"{entry.Material} has no id";
                }
            }

            return null;
        }

        private void OnValidate()
        {
            _built = false;
            string problem = Validate();
            if (problem != null)
            {
                Debug.LogError($"{nameof(MaterialCatalog)}: {problem}.", this);
            }
        }

        private MaterialEntry Entry(SalvageMaterial material)
        {
            int index = IndexOf(material);
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(material), material,
                    "This material is not in the catalog (this version does not have it).");
            }

            return _entries[index];
        }

        private int IndexOf(SalvageMaterial material)
        {
            int m = (int)material;
            if (m < 0 || m >= SalvageMaterials.Count)
            {
                return -1;
            }

            Build();
            return _index[m];
        }

        /// <summary>Indexes the entries and builds their keys once (and again after an edit).</summary>
        private void Build()
        {
            if (_built)
            {
                return;
            }

            for (int m = 0; m < SalvageMaterials.Count; m++)
            {
                _index[m] = -1;
            }

            for (int i = 0; i < _entries.Length; i++)
            {
                MaterialEntry entry = _entries[i];
                int m = (int)entry.Material;
                _index[m] = i;
                _names[m] = KeyPrefix + entry.Id;
                _jobs[m] = KeyPrefix + entry.Id + ".job";
                _sources[m] = KeyPrefix + entry.Id + ".source";
            }

            _built = true;
        }
    }
}
