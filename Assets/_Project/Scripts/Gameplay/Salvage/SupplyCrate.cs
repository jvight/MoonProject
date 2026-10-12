using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A salvage site's crew supply crate (docs/features/M3-25 T4): sealed beside its heart, opened by one beam cut,
    /// holding a stack of the site's signature material and a packing note in the crew's hand.
    /// </summary>
    [Serializable]
    public struct SupplyCrate
    {
        [Tooltip("Whose hand packed it (a crew id: kenji, ines, ro, theo).")]
        [SerializeField] private string _crewId;

        [Tooltip("The site's signature material inside.")]
        [SerializeField] private SalvageMaterial _material;

        [Tooltip("Units of it.")]
        [Range(1, 12)] [SerializeField] private int _units;

        public SupplyCrate(string crewId, SalvageMaterial material, int units)
        {
            _crewId = crewId;
            _material = material;
            _units = units;
        }

        public string CrewId => _crewId;

        public SalvageMaterial Material => _material;

        public int Units => _units;
    }
}
