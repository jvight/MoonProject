using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>One kind of scrap: its Art prefab, what it is worth and how common it is.</summary>
    [Serializable]
    public sealed class ScrapVariant
    {
        [Tooltip("Art prefab (Generated/Art/Scrap/Scrap_*.prefab): meshes only, pivot at the centre of mass.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Scrap this piece adds to the wallet.")]
        [Range(1, 20)] [SerializeField] private int _value = 1;

        [Tooltip("Relative chance of this variant when a piece is placed.")]
        [Range(0.01f, 10f)] [SerializeField] private float _weight = 1f;

        public ScrapVariant(GameObject prefab, int value, float weight)
        {
            _prefab = prefab;
            _value = value;
            _weight = weight;
        }

        public GameObject Prefab => _prefab;

        public int Value => _value;

        public float Weight => _weight;
    }
}
