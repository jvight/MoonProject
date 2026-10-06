using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>The scrap kinds scattered on the moon (written by the Gameplay/Content builder).</summary>
    public sealed class ScrapCatalog : ScriptableObject
    {
        [Tooltip("Scrap variants; order is stable (saved fields and seeded placement depend on it).")]
        [SerializeField] private ScrapVariant[] _variants = Array.Empty<ScrapVariant>();

        public IReadOnlyList<ScrapVariant> Variants => _variants;

        /// <summary>Null when every variant has a prefab, a value and a weight, else the first problem.</summary>
        public string Validate()
        {
            if (_variants.Length == 0)
            {
                return "has no variants";
            }

            for (int i = 0; i < _variants.Length; i++)
            {
                ScrapVariant variant = _variants[i];
                if (variant == null || variant.Prefab == null)
                {
                    return $"variant {i} has no prefab (Generated/Art/Scrap/Scrap_*.prefab)";
                }

                if (variant.Value <= 0 || variant.Weight <= 0f)
                {
                    return $"variant {i} ({variant.Prefab.name}) needs a positive value and weight";
                }
            }

            return null;
        }

        internal void Populate(ScrapVariant[] variants)
        {
            _variants = variants ?? throw new ArgumentNullException(nameof(variants));
        }
    }
}
