using System;
using UnityEngine;

namespace MoonProject.Core.Map
{
    /// <summary>One polyline of the <see cref="MapSheet"/>: a run of its points.</summary>
    [Serializable]
    public struct MapLine
    {
        [SerializeField] private MapLineKind _kind;
        [SerializeField] private int _start;
        [SerializeField] private int _count;
        [SerializeField] private bool _closed;
        [SerializeField] private float _height;

        public MapLine(MapLineKind kind, int start, int count, bool closed, float height)
        {
            _kind = kind;
            _start = start;
            _count = count;
            _closed = closed;
            _height = height;
        }

        public MapLineKind Kind => _kind;

        /// <summary>Index of the line's first point in <see cref="MapSheet.GetPoint"/>.</summary>
        public int Start => _start;

        /// <summary>Number of points, at least two.</summary>
        public int Count => _count;

        /// <summary>The last point joins back to the first.</summary>
        public bool Closed => _closed;

        /// <summary>Surface height in metres (a contour's level; the mean height along other lines).</summary>
        public float Height => _height;
    }
}
