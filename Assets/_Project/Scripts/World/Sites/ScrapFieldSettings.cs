using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Kestrel-3's trail (0.5.5 §2): the slots down the furrow's clear lane where Gameplay stands its cuttable hull
    /// fragments (Art's HullFragment variants), and the dust tint over them.
    /// </summary>
    [Serializable]
    public sealed class ScrapFieldSettings
    {
        [Tooltip("Half width of the clear lane down the furrow's middle, metres: 07's way through and the cuttable " +
            "fragments' place.")]
        [Range(0f, 10f)]
        [SerializeField] private float _laneHalfWidth = 2.5f;

        [Header("Cuttable slots (Gameplay's salvage pieces)")]
        [Tooltip("Slots in the clear lane where Gameplay places Kestrel-3's cuttable fragments (IScrapField); World " +
            "puts nothing there.")]
        [Range(0, 8)]
        [SerializeField] private int _cuttables = 4;

        [Tooltip("Span of the furrow the cuttable slots cover, as shares from its far end (0) to the crater's rim " +
            "(1): spaced evenly, so 07 finds them one after another down the lane.")]
        [SerializeField] private Vector2 _cuttableSpan = new Vector2(0.15f, 0.85f);

        [Tooltip("Furthest a cuttable slot lies from the lane's middle, metres: inside the lane, never by the " +
            "scenery.")]
        [Range(0f, 5f)]
        [SerializeField] private float _cuttableOffset = 1f;

        [Tooltip("How far a cuttable fragment sinks below its dust line, metres: shallow, so its seam shows.")]
        [Range(0f, 1f)]
        [SerializeField] private float _cuttableSink = 0.15f;

        [Tooltip("Extra tilt of a cuttable fragment beyond Art's modelled lean, degrees (x = min, y = max): gentler " +
            "than the scenery's.")]
        [SerializeField] private Vector2 _cuttableTilt = new Vector2(4f, 10f);

        [Tooltip("Dust tint multiplied into the cuttable fragments' colour (IScrapField.Dust).")]
        [SerializeField] private Color _dust = new Color(0.7f, 0.68f, 0.74f, 1f);

        public float LaneHalfWidth => _laneHalfWidth;
        public Color Dust => _dust;
        public int Cuttables => _cuttables;
        public Vector2 CuttableSpan => _cuttableSpan;
        public float CuttableOffset => _cuttableOffset;
        public float CuttableSink => _cuttableSink;
        public Vector2 CuttableTilt => _cuttableTilt;
    }
}
