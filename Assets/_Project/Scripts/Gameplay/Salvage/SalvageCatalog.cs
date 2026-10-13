using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The salvage in the world (docs/features/M3-13): the five wreck sites, the derelicts left on the routes (0.5.5
    /// §1.7), Art's hull fragments for Kestrel-3's scrap field (0.5.5 §2, set on the World's lane slots at boot) and
    /// the bundle each material folds into on its way to 07. Written by the Gameplay/Content builder; runtime code only
    /// reads it.
    /// </summary>
    public sealed class SalvageCatalog : ScriptableObject
    {
        [Tooltip("The sites, in a fixed order (saves and the sonar keep it).")]
        [SerializeField] private SalvageSiteEntry[] _sites = Array.Empty<SalvageSiteEntry>();

        [Tooltip("The derelicts on the World's micro-site anchors (0.5.5 §1.7), in a fixed order (saves keep it): " +
                 "no heart, no crew crate, and only the pieces Art gives them.")]
        [SerializeField] private SalvageSiteEntry[] _derelicts = Array.Empty<SalvageSiteEntry>();

        [Tooltip("Art's hull fragments for Kestrel-3's scrap field (Generated/Art/Props/HullFragment_<n>.prefab), " +
                 "indexed by the World's slot variant: each slot's piece to cut (Metal).")]
        [SerializeField] private GameObject[] _scrapFragments = Array.Empty<GameObject>();

        [Tooltip("Each hull fragment's seam (its number is its variant), written by the content builder.")]
        [SerializeField] private PieceCutLine[] _scrapCutLines = Array.Empty<PieceCutLine>();

        [Tooltip("The size of each hull fragment in the scrap field (docs/features/M3-26 §4.2).")]
        [SerializeField] private PieceSize _scrapSize = PieceSize.Small;

        [Tooltip("Bundle a cut metal piece folds into (Generated/Art/Pickups/Material_Metal.prefab).")]
        [SerializeField] private GameObject _metalBundle;

        [Tooltip("Bundle a cut wiring piece folds into (Generated/Art/Pickups/Material_Wiring.prefab).")]
        [SerializeField] private GameObject _wiringBundle;

        [Tooltip("Bundle a cut optics piece folds into (Generated/Art/Pickups/Material_Optics.prefab).")]
        [SerializeField] private GameObject _opticsBundle;

        public IReadOnlyList<SalvageSiteEntry> Sites => _sites;

        public IReadOnlyList<SalvageSiteEntry> Derelicts => _derelicts;

        /// <summary>The scrap field's piece size.</summary>
        public PieceSize ScrapSize => _scrapSize;

        /// <summary>
        /// Art's hull fragment for slot variant <paramref name="variant"/>, or null when there is none.
        /// </summary>
        public GameObject ScrapFragment(int variant)
        {
            return variant >= 0 && variant < _scrapFragments.Length ? _scrapFragments[variant] : null;
        }

        /// <summary>The seam of hull fragment <paramref name="variant"/> (its CutPoint's space).</summary>
        public Vector3[] ScrapCutLine(int variant)
        {
            for (int i = 0; i < _scrapCutLines.Length; i++)
            {
                if (_scrapCutLines[i].Number == variant && _scrapCutLines[i].IsValid)
                {
                    return _scrapCutLines[i].Points;
                }
            }

            return null;
        }

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

        /// <summary>
        /// Null when every entry is complete and ids are unique across sites and derelicts, else the first problem.
        /// </summary>
        public string Validate()
        {
            if (_sites.Length == 0)
            {
                return "has no sites";
            }

            string problem = ValidateList(_sites, false, "site") ?? ValidateList(_derelicts, true, "derelict");
            if (problem != null)
            {
                return problem;
            }

            for (int i = 0; i < _derelicts.Length; i++)
            {
                for (int j = 0; j < _sites.Length; j++)
                {
                    if (string.Equals(_sites[j].Id, _derelicts[i].Id, StringComparison.Ordinal))
                    {
                        return $"'{_derelicts[i].Id}' is both a site and a derelict";
                    }
                }
            }

            if (_metalBundle == null || _wiringBundle == null || _opticsBundle == null)
            {
                return "is missing a material bundle prefab (Generated/Art/Pickups/Material_*)";
            }

            if (_scrapFragments.Length == 0)
            {
                return "has no hull fragments for Kestrel-3's scrap field (Generated/Art/Props/HullFragment_*)";
            }

            for (int i = 0; i < _scrapFragments.Length; i++)
            {
                if (_scrapFragments[i] == null || ScrapCutLine(i) == null)
                {
                    return $"has no hull fragment {i} with its seam (run Gameplay/Content)";
                }
            }

            return null;
        }

        private static string ValidateList(SalvageSiteEntry[] entries, bool derelicts, string noun)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                string problem = entries[i] == null ? "is empty" : entries[i].Validate(derelicts);
                if (problem != null)
                {
                    return $"{noun} {i} {problem}";
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(entries[j].Id, entries[i].Id, StringComparison.Ordinal))
                    {
                        return $"{noun} '{entries[i].Id}' appears twice";
                    }
                }
            }

            return null;
        }

        internal void Populate(SalvageSiteEntry[] sites, SalvageSiteEntry[] derelicts, GameObject metalBundle,
            GameObject wiringBundle, GameObject opticsBundle, GameObject[] scrapFragments, PieceCutLine[] scrapCutLines,
            PieceSize scrapSize)
        {
            _scrapFragments = scrapFragments ?? throw new ArgumentNullException(nameof(scrapFragments));
            _scrapCutLines = scrapCutLines ?? throw new ArgumentNullException(nameof(scrapCutLines));
            _scrapSize = scrapSize;
            _sites = sites ?? throw new ArgumentNullException(nameof(sites));
            _derelicts = derelicts ?? throw new ArgumentNullException(nameof(derelicts));
            _metalBundle = metalBundle;
            _wiringBundle = wiringBundle;
            _opticsBundle = opticsBundle;
        }
    }
}
