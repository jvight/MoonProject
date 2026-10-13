using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Kestrel-3's trail as a scrap field (0.5.5 §2): hull plates the impact tore off, half-buried along the furrow
    /// between its far end and the crater. Scenery, never loot: nothing glows and nothing is picked up; Gameplay's
    /// cuttable fragments lie in the clear lane down the furrow's middle.
    /// </summary>
    [Serializable]
    public sealed class ScrapFieldSettings
    {
        [Tooltip("Hull plates half-buried along the furrow.")]
        [Range(0, 40)]
        [SerializeField] private int _plates = 10;

        [Tooltip("Plate length range, metres (x = min, y = max).")]
        [SerializeField] private Vector2 _length = new Vector2(1.8f, 3.6f);

        [Tooltip("Plate width as a share of its length (x = min, y = max).")]
        [SerializeField] private Vector2 _widthShare = new Vector2(0.45f, 0.8f);

        [Tooltip("Plate thickness, metres.")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _thickness = 0.09f;

        [Tooltip("How steeply the plates stick out of the dust, degrees (x = min, y = max).")]
        [SerializeField] private Vector2 _tilt = new Vector2(30f, 62f);

        [Tooltip("Share of each plate's width sunk into the dust (0..1): half-buried, never resting on top.")]
        [Range(0f, 0.9f)]
        [SerializeField] private float _sink = 0.45f;

        [Tooltip("Half width of the clear lane down the furrow's middle, metres: 07's way through and the cuttable " +
            "fragments' place.")]
        [Range(0f, 10f)]
        [SerializeField] private float _laneHalfWidth = 2.5f;

        [Tooltip("How far beyond the lane the plates lie, metres.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _spread = 5f;

        [Tooltip("Span of the furrow the plates cover, as shares from its far end (0) to the crater's rim (1).")]
        [SerializeField] private Vector2 _span = new Vector2(0.05f, 0.95f);

        public int Plates => _plates;
        public Vector2 Length => _length;
        public Vector2 WidthShare => _widthShare;
        public float Thickness => _thickness;
        public Vector2 Tilt => _tilt;
        public float Sink => _sink;
        public float LaneHalfWidth => _laneHalfWidth;
        public float Spread => _spread;
        public Vector2 Span => _span;
    }
}
