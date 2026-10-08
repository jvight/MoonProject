using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The salvage in the world (docs/features/M3-13): the five wreck sites, the bundle each material folds into on
    /// its way to 07, and the loose bits along Kestrel-3's debris trail. Written by the Gameplay/Content builder;
    /// runtime code only reads it.
    /// </summary>
    public sealed class SalvageCatalog : ScriptableObject
    {
        [Tooltip("The sites, in a fixed order (saves and the sonar keep it).")]
        [SerializeField] private SalvageSiteEntry[] _sites = Array.Empty<SalvageSiteEntry>();

        [Tooltip("Bundle a cut metal piece folds into (Generated/Art/Pickups/Material_Metal.prefab).")]
        [SerializeField] private GameObject _metalBundle;

        [Tooltip("Bundle a cut wiring piece folds into (Generated/Art/Pickups/Material_Wiring.prefab).")]
        [SerializeField] private GameObject _wiringBundle;

        [Tooltip("Bundle a cut optics piece folds into (Generated/Art/Pickups/Material_Optics.prefab).")]
        [SerializeField] private GameObject _opticsBundle;

        [Tooltip("The loose bits along the Kestrel trail (WorldAnchorIds.KestrelTrail), in a fixed order.")]
        [SerializeField] private SalvageTrailBit[] _trail = Array.Empty<SalvageTrailBit>();

        public IReadOnlyList<SalvageSiteEntry> Sites => _sites;

        public IReadOnlyList<SalvageTrailBit> Trail => _trail;

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

        /// <summary>Null when every entry is complete and site ids are unique, else the first problem.</summary>
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
                    if (string.Equals(_sites[j].AnchorId, _sites[i].AnchorId, StringComparison.Ordinal))
                    {
                        return $"site '{_sites[i].AnchorId}' appears twice";
                    }
                }
            }

            if (_metalBundle == null || _wiringBundle == null || _opticsBundle == null)
            {
                return "is missing a material bundle prefab (Generated/Art/Pickups/Material_*)";
            }

            for (int i = 0; i < _trail.Length; i++)
            {
                string problem = _trail[i] == null ? "is empty" : _trail[i].Validate();
                if (problem != null)
                {
                    return $"trail bit {i} {problem}";
                }
            }

            return null;
        }

        internal void Populate(SalvageSiteEntry[] sites, GameObject metalBundle, GameObject wiringBundle,
            GameObject opticsBundle, SalvageTrailBit[] trail)
        {
            _sites = sites ?? throw new ArgumentNullException(nameof(sites));
            _metalBundle = metalBundle;
            _wiringBundle = wiringBundle;
            _opticsBundle = opticsBundle;
            _trail = trail ?? throw new ArgumentNullException(nameof(trail));
        }
    }
}
