using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Every cassette in the game's content, in a fixed order (written by the Gameplay/Content builder); its count is
    /// the radio program's tape total.
    /// </summary>
    public sealed class CassetteCatalog : ScriptableObject
    {
        [Tooltip("The cassette definitions; order is stable.")]
        [SerializeField] private CassetteDefinition[] _cassettes = Array.Empty<CassetteDefinition>();

        public IReadOnlyList<CassetteDefinition> Cassettes => _cassettes;

        /// <summary>Every cassette id, in catalog order (for the radio program).</summary>
        public string[] Ids()
        {
            var ids = new string[_cassettes.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = _cassettes[i].Id;
            }

            return ids;
        }

        /// <summary>Null when every definition is complete and ids are unique, else the first problem.</summary>
        public string Validate()
        {
            for (int i = 0; i < _cassettes.Length; i++)
            {
                if (_cassettes[i] == null)
                {
                    return $"entry {i} is empty";
                }

                string problem = _cassettes[i].Validate();
                if (problem != null)
                {
                    return $"cassette {i} {problem}";
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(_cassettes[j].Id, _cassettes[i].Id, StringComparison.Ordinal))
                    {
                        return $"cassette id '{_cassettes[i].Id}' appears twice";
                    }
                }
            }

            return null;
        }

        internal void Populate(CassetteDefinition[] cassettes)
        {
            _cassettes = cassettes ?? throw new ArgumentNullException(nameof(cassettes));
        }
    }
}
