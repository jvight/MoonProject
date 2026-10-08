using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What a piece of kit or station work costs in salvage (docs/features/M3-13): so much metal, wiring and optics.
    /// Serialised inside upgrade levels and the relay tuning.
    /// </summary>
    [Serializable]
    public struct Recipe
    {
        [Tooltip("Metal it takes: plates, struts, hull pieces.")]
        [Min(0)] [SerializeField] private int _metal;

        [Tooltip("Wiring it takes: cable bundles, circuit boards, junction boxes.")]
        [Min(0)] [SerializeField] private int _wiring;

        [Tooltip("Optics it takes: solar cells, lenses, dish panels.")]
        [Min(0)] [SerializeField] private int _optics;

        public Recipe(int metal, int wiring, int optics)
        {
            if (metal < 0 || wiring < 0 || optics < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(metal), "A recipe never takes a negative amount.");
            }

            _metal = metal;
            _wiring = wiring;
            _optics = optics;
        }

        public int Metal => _metal;

        public int Wiring => _wiring;

        public int Optics => _optics;

        /// <summary>Units of every material together.</summary>
        public int Total => _metal + _wiring + _optics;

        public bool IsFree => Total == 0;

        public int Of(SalvageMaterial material)
        {
            switch (material)
            {
                case SalvageMaterial.Metal:
                    return _metal;
                case SalvageMaterial.Wiring:
                    return _wiring;
                case SalvageMaterial.Optics:
                    return _optics;
                default:
                    throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown salvage material.");
            }
        }

        /// <summary>This recipe and <paramref name="other"/> together.</summary>
        public Recipe Plus(Recipe other)
        {
            return new Recipe(_metal + other._metal, _wiring + other._wiring, _optics + other._optics);
        }

        public override string ToString()
        {
            return $"{_metal} metal + {_wiring} wiring + {_optics} optics";
        }
    }
}
