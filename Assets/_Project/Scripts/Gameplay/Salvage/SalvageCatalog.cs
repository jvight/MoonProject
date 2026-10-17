using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The salvage in the world (docs/features/M3-13, opening-v2 §5: the Kestrel impact is the opening's optional
    /// wreck) and the bundle each material folds into on its way to 07. Written by the Gameplay/Content builder;
    /// runtime code only reads it.
    /// </summary>
    public sealed class SalvageCatalog : ScriptableObject
    {
        [Tooltip("The sites, in a fixed order (saves keep it).")]
        [SerializeField] private SalvageSiteEntry[] _sites = Array.Empty<SalvageSiteEntry>();

        [Tooltip("Bundle a cut metal piece folds into (Generated/Art/Pickups/Material_Metal.prefab).")]
        [SerializeField] private GameObject _metalBundle;

        [Tooltip("Bundle a cut wiring piece folds into (Generated/Art/Pickups/Material_Wiring.prefab).")]
        [SerializeField] private GameObject _wiringBundle;

        [Tooltip("Bundle a cut optics piece folds into (Generated/Art/Pickups/Material_Optics.prefab).")]
        [SerializeField] private GameObject _opticsBundle;

        public IReadOnlyList<SalvageSiteEntry> Sites => _sites;

        /// <summary>The bundle <paramref name="material"/> folds into.</summary>
        public GameObject Bundle(SalvageMaterial material)
        {
            switch (material)
            {
                case SalvageMaterial.Metal:
                    return _metalBundle;
                case SalvageMaterial.Wiring:
                    return _wiringBundle;
                case SalvageMaterial.Optics:
                    return _opticsBundle;
                default:
                    throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown material.");
            }
        }

        /// <summary>Null when every entry is complete and ids are unique, else the first problem.</summary>
        public string Validate()
        {
            if (_sites.Length == 0)
            {
                return "has no sites";
            }

            for (int i = 0; i < _sites.Length; i++)
            {
                string problem = _sites[i] == null ? "is empty" : _sites[i].Validate();
                if (problem != null)
                {
                    return $"site {i} {problem}";
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(_sites[j].Id, _sites[i].Id, StringComparison.Ordinal))
                    {
                        return $"site '{_sites[i].Id}' appears twice";
                    }
                }
            }

            return _metalBundle == null || _wiringBundle == null || _opticsBundle == null
                ? "is missing a material bundle prefab (Generated/Art/Pickups/Material_*)"
                : null;
        }

        internal void Populate(SalvageSiteEntry[] sites, GameObject metalBundle, GameObject wiringBundle,
            GameObject opticsBundle)
        {
            _sites = sites ?? throw new ArgumentNullException(nameof(sites));
            _metalBundle = metalBundle;
            _wiringBundle = wiringBundle;
            _opticsBundle = opticsBundle;
        }
    }
}
