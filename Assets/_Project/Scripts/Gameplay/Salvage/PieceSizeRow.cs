using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>One row of the size table (docs/features/M3-26 §4.1): a piece size's segments, time and yield.</summary>
    [Serializable]
    public struct PieceSizeRow
    {
        [Tooltip("Segments the beam cuts in turn, sliding from one to the next without a new charge.")]
        [Range(1, 3)] [SerializeField] private int _segments;

        [Tooltip("Seconds of cutting per segment at tier 0 (the cutter's speed divides it).")]
        [Range(0.5f, 6f)] [SerializeField] private float _segmentSeconds;

        [Tooltip("Units of material the piece yields.")]
        [Range(1, 12)] [SerializeField] private int _units;

        public PieceSizeRow(int segments, float segmentSeconds, int units)
        {
            _segments = segments;
            _segmentSeconds = segmentSeconds;
            _units = units;
        }

        public int Segments => _segments;

        public float SegmentSeconds => _segmentSeconds;

        public int Units => _units;
    }
}
