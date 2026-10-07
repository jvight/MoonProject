using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Every crew log cache in the world, in a fixed order (written by the Gameplay/Content builder).
    /// </summary>
    public sealed class LogCacheCatalog : ScriptableObject
    {
        [Tooltip("The cache definitions; order is stable.")]
        [SerializeField] private LogCacheDefinition[] _caches = Array.Empty<LogCacheDefinition>();

        public IReadOnlyList<LogCacheDefinition> Caches => _caches;

        /// <summary>Null when every definition is complete and log ids are unique, else the first problem.</summary>
        public string Validate()
        {
            for (int i = 0; i < _caches.Length; i++)
            {
                if (_caches[i] == null)
                {
                    return $"entry {i} is empty";
                }

                string problem = _caches[i].Validate();
                if (problem != null)
                {
                    return $"cache {i} {problem}";
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(_caches[j].LogId, _caches[i].LogId, StringComparison.Ordinal))
                    {
                        return $"log id '{_caches[i].LogId}' appears twice";
                    }
                }
            }

            return null;
        }

        internal void Populate(LogCacheDefinition[] caches)
        {
            _caches = caches ?? throw new ArgumentNullException(nameof(caches));
        }
    }
}
