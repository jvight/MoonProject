using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Catalog of all buildable base structures (0.5.7). Provides lookup by structure id and whole-catalog validation
    /// (reporting how many definitions still have art pending).
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Gameplay/Structure Catalog", fileName = "StructureCatalog")]
    public sealed class StructureCatalog : ScriptableObject
    {
        [Tooltip("All buildable structure definitions in deterministic catalog order.")]
        [SerializeField] private StructureDefinition[] _structures = Array.Empty<StructureDefinition>();

        public IReadOnlyList<StructureDefinition> Structures => _structures;

        public int Count => _structures != null ? _structures.Length : 0;

        public StructureDefinition StructureAt(int index)
        {
            return _structures[index];
        }

        /// <summary>Finds a structure definition by <paramref name="id"/>, or returns null when unknown.</summary>
        public StructureDefinition Find(string id)
        {
            return TryGet(id, out StructureDefinition definition) ? definition : null;
        }

        /// <summary>True when a structure with <paramref name="id"/> exists in this catalog.</summary>
        public bool TryGet(string id, out StructureDefinition definition)
        {
            if (!string.IsNullOrEmpty(id) && _structures != null)
            {
                for (int i = 0; i < _structures.Length; i++)
                {
                    StructureDefinition candidate = _structures[i];
                    if (candidate != null && string.Equals(candidate.Id, id, StringComparison.Ordinal))
                    {
                        definition = candidate;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }

        /// <summary>Writes the structure array into the catalog.</summary>
        public void Populate(StructureDefinition[] structures)
        {
            _structures = structures ?? throw new ArgumentNullException(nameof(structures));
        }

        /// <summary>Null when every definition in the catalog is valid and ids are unique, else the first problem.</summary>
        public string Validate()
        {
            return Validate(out _);
        }

        /// <summary>
        /// Null when every definition in the catalog is valid and ids are unique, else the first problem. Reports via
        /// <paramref name="artPendingCount"/> how many valid definitions have <see cref="StructureDefinition.ArtPending"/>
        /// set while awaiting art prefabs.
        /// </summary>
        public string Validate(out int artPendingCount)
        {
            artPendingCount = 0;
            if (_structures == null || _structures.Length == 0)
            {
                return "has no structures";
            }

            for (int i = 0; i < _structures.Length; i++)
            {
                StructureDefinition current = _structures[i];
                if (current == null)
                {
                    return $"entry {i} is empty";
                }

                string problem = current.Validate(out bool artPending);
                if (problem != null)
                {
                    return $"structure {i} {problem}";
                }

                if (artPending)
                {
                    artPendingCount++;
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(_structures[j].Id, current.Id, StringComparison.Ordinal))
                    {
                        return $"structure id '{current.Id}' appears twice";
                    }
                }
            }

            return null;
        }
    }
}
