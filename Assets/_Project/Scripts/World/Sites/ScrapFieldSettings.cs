using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Kestrel-3's trail as a scrap field (0.5.5 §2): hull fragments the impact tore off (Art's HullFragment variants),
    /// sunk deep in the dust along the furrow, darker and dustier than the cuttable pieces. Scenery, never loot:
    /// nothing glows, nothing is cut or picked up; Gameplay's 4 cuttable fragments (with their glinting seams) lie in
    /// the clear lane down the furrow's middle.
    /// </summary>
    [Serializable]
    public sealed class ScrapFieldSettings
    {
        [Tooltip("Hull fragments along the furrow.")]
        [Range(0, 40)]
        [SerializeField] private int _pieces = 14;

        [Tooltip("Half width of the clear lane down the furrow's middle, metres: 07's way through and the cuttable " +
            "fragments' place.")]
        [Range(0f, 10f)]
        [SerializeField] private float _laneHalfWidth = 2.5f;

        [Tooltip("How far beyond the lane the scenery fragments lie, metres.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _spread = 6f;

        [Tooltip("Open ground kept between the lane's edge and the nearest fragment, metres.")]
        [Range(0f, 5f)]
        [SerializeField] private float _laneMargin = 1.2f;

        [Tooltip("Span of the furrow the fragments cover, as shares from its far end (0) to the crater's rim (1).")]
        [SerializeField] private Vector2 _span = new Vector2(0.05f, 0.95f);

        [Tooltip("Big curved hull skins among the scenery (Art's variant 0), the wreck's anchors near the impact end.")]
        [Range(0, 8)]
        [SerializeField] private int _anchors = 3;

        [Tooltip("Scale of an anchor skin (x = min, y = max).")]
        [SerializeField] private Vector2 _anchorScale = new Vector2(2f, 2.5f);

        [Tooltip("Scale of the other scenery fragments (x = min, y = max).")]
        [SerializeField] private Vector2 _scale = new Vector2(1f, 1.6f);

        [Tooltip("How the fragments crowd the impact end (the furrow's far end): a fragment's place along the furrow " +
            "is a uniform share raised to this power, so above 1 most lie near where Kestrel-3 first hit.")]
        [Range(1f, 4f)]
        [SerializeField] private float _crowding = 1.8f;

        [Tooltip("How far each scenery fragment sinks below its dust line at scale 1, metres: about two thirds " +
            "buried (it sinks in proportion to its scale).")]
        [Range(0f, 2f)]
        [SerializeField] private float _sink = 0.45f;

        [Tooltip("Extra tilt of a scenery fragment beyond Art's modelled lean, degrees (x = min, y = max).")]
        [SerializeField] private Vector2 _tilt = new Vector2(12f, 28f);

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

        [Tooltip("Dust tint multiplied into the scenery fragments' colour: darker and dustier than the cuttable ones.")]
        [SerializeField] private Color _dust = new Color(0.7f, 0.68f, 0.74f, 1f);

        public int Pieces => _pieces;
        public float LaneHalfWidth => _laneHalfWidth;
        public float Spread => _spread;
        public float LaneMargin => _laneMargin;
        public Vector2 Span => _span;
        public float Sink => _sink;
        public int Anchors => _anchors;
        public Vector2 AnchorScale => _anchorScale;
        public Vector2 Scale => _scale;
        public float Crowding => _crowding;
        public Vector2 Tilt => _tilt;
        public Color Dust => _dust;
        public int Cuttables => _cuttables;
        public Vector2 CuttableSpan => _cuttableSpan;
        public float CuttableOffset => _cuttableOffset;
        public float CuttableSink => _cuttableSink;
        public Vector2 CuttableTilt => _cuttableTilt;
    }
}
