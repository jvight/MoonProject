using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>Every relic in the game, in a fixed order (written by the Gameplay/Content builder).</summary>
    public sealed class RelicCatalog : ScriptableObject
    {
        [Tooltip("The relic definitions; order is stable (it drives deterministic site placement).")]
        [SerializeField] private RelicDefinition[] _relics = Array.Empty<RelicDefinition>();

        public IReadOnlyList<RelicDefinition> Relics => _relics;

        /// <summary>Null when every definition is complete and ids are unique, else the first problem.</summary>
        public string Validate()
        {
            if (_relics.Length == 0)
            {
                return "has no relics";
            }

            for (int i = 0; i < _relics.Length; i++)
            {
                if (_relics[i] == null)
                {
                    return $"entry {i} is empty";
                }

                string problem = _relics[i].Validate();
                if (problem != null)
                {
                    return $"relic {i} {problem}";
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(_relics[j].Id, _relics[i].Id, StringComparison.Ordinal))
                    {
                        return $"relic id '{_relics[i].Id}' appears twice";
                    }
                }
            }

            return null;
        }

        internal void Populate(RelicDefinition[] relics)
        {
            _relics = relics ?? throw new ArgumentNullException(nameof(relics));
        }
    }
}
