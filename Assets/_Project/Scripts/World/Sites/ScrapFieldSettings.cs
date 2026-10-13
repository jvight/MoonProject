using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Kestrel-3's trail as a scrap field (0.5.5 §2): hull fragments the impact tore off (Art's HullFragment variants,
    /// modelled half-buried), lying along the furrow between its far end and the crater. Scenery, never loot: nothing
    /// glows and nothing is picked up; Gameplay's cuttable fragments lie in the clear lane down the furrow's middle.
    /// </summary>
    [Serializable]
    public sealed class ScrapFieldSettings
    {
        [Tooltip("Hull fragments along the furrow.")]
        [Range(0, 40)]
        [SerializeField] private int _pieces = 10;

        [Tooltip("Half width of the clear lane down the furrow's middle, metres: 07's way through and the cuttable " +
            "fragments' place.")]
        [Range(0f, 10f)]
        [SerializeField] private float _laneHalfWidth = 2.5f;

        [Tooltip("How far beyond the lane the fragments lie, metres.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _spread = 5f;

        [Tooltip("Span of the furrow the fragments cover, as shares from its far end (0) to the crater's rim (1).")]
        [SerializeField] private Vector2 _span = new Vector2(0.05f, 0.95f);

        public int Pieces => _pieces;
        public float LaneHalfWidth => _laneHalfWidth;
        public float Spread => _spread;
        public Vector2 Span => _span;
    }
}
