using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Every databank entry (docs/design/databank.md §2), written by Gameplay's content builder from
    /// <c>DatabankRecipes</c> (Assets/_Project/Data/Content/DatabankCatalogue.asset).
    /// </summary>
    [CreateAssetMenu(fileName = "DatabankCatalogue", menuName = "MoonProject/Gameplay/Databank Catalogue")]
    public sealed class DatabankCatalogue : ScriptableObject
    {
        [SerializeField] private DatabankEntry[] _entries = Array.Empty<DatabankEntry>();

        public int Count => _entries.Length;

        public DatabankEntry EntryAt(int index)
        {
            return _entries[index];
        }

        public bool TryGet(string id, out DatabankEntry entry)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (string.Equals(_entries[i].Id, id, StringComparison.Ordinal))
                {
                    entry = _entries[i];
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public void Populate(DatabankEntry[] entries)
        {
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        /// <summary>Null when every entry is sound and ids are unique, else what breaks the contract.</summary>
        public string Validate()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _entries.Length; i++)
            {
                DatabankEntry entry = _entries[i];
                if (entry == null)
                {
                    return $"entry {i} is null";
                }

                string problem = entry.Validate();
                if (problem != null)
                {
                    return problem;
                }

                if (!ids.Add(entry.Id))
                {
                    return $"'{entry.Id}' is listed twice";
                }
            }

            return null;
        }
    }
}
