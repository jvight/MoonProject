using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One loose bit of Kestrel-3's debris trail: Art's "Debris_&lt;Material&gt;_&lt;n&gt;" prefab, what it yields and
    /// how far down the trail it lies (metres along the trail anchor's forward from its home-side end).
    /// </summary>
    [Serializable]
    public sealed class SalvageTrailBit
    {
        [Tooltip("Art's debris prefab (Generated/Art/Sites/Debris_<Material>_<n>.prefab).")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("What it folds into when 07 drives through it.")]
        [SerializeField] private SalvageMaterial _material;

        [Tooltip("Metres from the trail anchor along its forward (toward the crater).")]
        [Min(0f)] [SerializeField] private float _distance;

        public SalvageTrailBit(GameObject prefab, SalvageMaterial material, float distance)
        {
            _prefab = prefab;
            _material = material;
            _distance = distance;
        }

        public GameObject Prefab => _prefab;

        public SalvageMaterial Material => _material;

        public float Distance => _distance;

        /// <summary>Null when the entry is complete, else what is wrong with it.</summary>
        public string Validate()
        {
            if (_prefab == null)
            {
                return "has no prefab (Generated/Art/Sites/Debris_*)";
            }

            return SalvagePieceName.TryParseDebris(_prefab.name, out SalvageMaterial named) && named == _material
                ? null
                : $"'{_prefab.name}' is not a {_material} debris prefab";
        }
    }
}
