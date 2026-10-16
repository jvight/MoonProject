using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Tracks which buildable base structures 07 has learned (0.5.7 slice 6, BUILD-MODE). A new game knows nothing;
    /// callers unlock structures via <see cref="Learn"/>, which publishes <see cref="StructureLearned"/>, and
    /// <see cref="PlacedBase"/> persists the known ids in <see cref="BuildingSaveData.known"/>.
    /// </summary>
    public sealed class KnownStructures
    {
        private readonly StructureCatalog _catalog;
        private readonly EventBus _events;
        private readonly List<string> _known = new List<string>();

        public KnownStructures(EventBus events = null)
            : this(null, events)
        {
        }

        public KnownStructures(StructureCatalog catalog, EventBus events = null)
        {
            _catalog = catalog;
            _events = events;
        }

        public int Count => _known.Count;

        public IReadOnlyList<string> All => _known;

        /// <summary>True when 07 has learned structure <paramref name="id"/>.</summary>
        public bool IsKnown(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            int count = _known.Count;
            for (int i = 0; i < count; i++)
            {
                if (string.Equals(_known[i], id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Marks structure <paramref name="id"/> as known and publishes <see cref="StructureLearned"/> when newly
        /// learned. Returns false when <paramref name="id"/> is empty, unknown to the catalog, or already known.
        /// </summary>
        public bool Learn(string id, bool restored = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            if (_catalog != null && !_catalog.TryGet(id, out _))
            {
                Debug.LogError($"{nameof(KnownStructures)}: cannot learn unknown structure id '{id}'.");
                return false;
            }

            if (IsKnown(id))
            {
                return false;
            }

            _known.Add(id);
            _events?.Publish(new StructureLearned(id, restored));
            return true;
        }

        /// <summary>Forgets all learned structures (used before restoring a save or starting a new game).</summary>
        public void Clear()
        {
            _known.Clear();
        }

        /// <summary>Captures the known structure ids for <see cref="BuildingSaveData.known"/>.</summary>
        public string[] Capture()
        {
            return _known.Count == 0 ? Array.Empty<string>() : _known.ToArray();
        }

        /// <summary>
        /// Restores known structure ids from <see cref="BuildingSaveData.known"/> and publishes
        /// <see cref="StructureLearned"/> with <c>Restored = true</c> for each valid id.
        /// </summary>
        public void Restore(string[] knownIds)
        {
            _known.Clear();
            if (knownIds == null)
            {
                return;
            }

            for (int i = 0; i < knownIds.Length; i++)
            {
                string id = knownIds[i];
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (_catalog != null && !_catalog.TryGet(id, out _))
                {
                    Debug.LogError(
                        $"{nameof(KnownStructures)}: unknown structure id '{id}' at save index {i}; skipping entry.");
                    continue;
                }

                if (IsKnown(id))
                {
                    continue;
                }

                _known.Add(id);
                _events?.Publish(new StructureLearned(id, true));
            }
        }
    }
}
