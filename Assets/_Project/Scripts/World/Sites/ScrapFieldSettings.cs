using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Kestrel-3's trail as a scrap field (0.5.5 §2): hull fragments the impact tore off (Art's HullFragment variants),
    /// sunk deep in the dust along the furrow, darker and dustier than the cuttable pieces. Scenery, never loot:
    /// nothing glows, nothing is cut or picked up; Gameplay's 4 cuttable fragments (with their glinting seams) lie in
    /// the clear lane down the furrow's middle. Two flatter pieces at the lane's edge host the Warm Headlamp's scan
    /// fragments.
    /// </summary>
    [Serializable]
    public sealed class ScrapFieldSettings
    {
        [Tooltip("Hull fragments along the furrow, the hosts included.")]
        [Range(0, 40)]
        [SerializeField] private int _pieces = 6;

        [Tooltip("Of them, the pieces at the lane's edge that host the Warm Headlamp's scan fragments.")]
        [Range(0, 4)]
        [SerializeField] private int _hosts = 2;

        [Tooltip("Half width of the clear lane down the furrow's middle, metres: 07's way through and the cuttable " +
            "fragments' place.")]
        [Range(0f, 10f)]
        [SerializeField] private float _laneHalfWidth = 2.5f;

        [Tooltip("How far beyond the lane the scenery fragments lie, metres.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _spread = 6f;

        [Tooltip("How far beyond the lane's edge a host stands, metres: close enough for 07 to reach its socket.")]
        [Range(0f, 5f)]
        [SerializeField] private float _hostOffset = 1.2f;

        [Tooltip("Span of the furrow the fragments cover, as shares from its far end (0) to the crater's rim (1).")]
        [SerializeField] private Vector2 _span = new Vector2(0.05f, 0.95f);

        [Tooltip("How far each scenery fragment sinks below its dust line, metres: about two thirds buried.")]
        [Range(0f, 2f)]
        [SerializeField] private float _sink = 0.45f;

        [Tooltip("Extra tilt of a scenery fragment beyond Art's modelled lean, degrees (x = min, y = max).")]
        [SerializeField] private Vector2 _tilt = new Vector2(12f, 28f);

        [Tooltip("Dust tint multiplied into the scenery fragments' colour: darker and dustier than the cuttable ones.")]
        [SerializeField] private Color _dust = new Color(0.7f, 0.68f, 0.74f, 1f);

        public int Pieces => _pieces;
        public int Hosts => _hosts;
        public float LaneHalfWidth => _laneHalfWidth;
        public float Spread => _spread;
        public float HostOffset => _hostOffset;
        public Vector2 Span => _span;
        public float Sink => _sink;
        public Vector2 Tilt => _tilt;
        public Color Dust => _dust;
    }
}
