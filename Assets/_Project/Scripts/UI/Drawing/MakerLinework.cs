using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The maker's monoline mark 07's face draws as it boots (Kessler–Arai), as polylines in a 0..1 square (origin top
    /// left, y down), built from Art's CRT linework by the UI/MakerLinework builder and traced by
    /// <see cref="LogoTracePainter"/> stroke by stroke. Each stroke keeps its length so a beam can trace part of it.
    /// </summary>
    public sealed class MakerLinework : ScriptableObject
    {
        [SerializeField] private Vector2[] _points = Array.Empty<Vector2>();
        [SerializeField] private int[] _strokeStarts = Array.Empty<int>();
        [SerializeField] private bool[] _strokeClosed = Array.Empty<bool>();
        [SerializeField] private float _strokeWidth;

        public int StrokeCount => _strokeStarts.Length;

        /// <summary>The stroke's width as a share of the square's side.</summary>
        public float StrokeWidth => _strokeWidth;

        /// <summary>Points of stroke <paramref name="stroke"/>, its closing point included when it is closed.</summary>
        public int PointCount(int stroke)
        {
            int end = stroke + 1 < _strokeStarts.Length ? _strokeStarts[stroke + 1] : _points.Length;
            return end - _strokeStarts[stroke] + (_strokeClosed[stroke] ? 1 : 0);
        }

        /// <summary>Point <paramref name="index"/> of stroke <paramref name="stroke"/> (a closed stroke ends where it began).</summary>
        public Vector2 Point(int stroke, int index)
        {
            int start = _strokeStarts[stroke];
            int end = stroke + 1 < _strokeStarts.Length ? _strokeStarts[stroke + 1] : _points.Length;
            return _points[start + (index < end - start ? index : 0)];
        }

        /// <summary>Length of stroke <paramref name="stroke"/> in square units.</summary>
        public float Length(int stroke)
        {
            float length = 0f;
            int count = PointCount(stroke);
            for (int i = 1; i < count; i++)
            {
                length += Vector2.Distance(Point(stroke, i - 1), Point(stroke, i));
            }

            return length;
        }

        internal void Set(Vector2[] points, int[] strokeStarts, bool[] strokeClosed, float strokeWidth)
        {
            _points = points ?? throw new ArgumentNullException(nameof(points));
            _strokeStarts = strokeStarts ?? throw new ArgumentNullException(nameof(strokeStarts));
            _strokeClosed = strokeClosed ?? throw new ArgumentNullException(nameof(strokeClosed));
            _strokeWidth = strokeWidth;
        }

        /// <summary>Null when the mark can be drawn, otherwise what is wrong with it.</summary>
        public string Validate()
        {
            if (_strokeStarts.Length == 0 || _strokeStarts.Length != _strokeClosed.Length)
            {
                return "has no strokes";
            }

            for (int i = 0; i < _strokeStarts.Length; i++)
            {
                int end = i + 1 < _strokeStarts.Length ? _strokeStarts[i + 1] : _points.Length;
                if (end - _strokeStarts[i] < 2)
                {
                    return $"has a stroke ({i}) with fewer than two points";
                }
            }

            return _strokeWidth > 0f ? null : "has no stroke width";
        }
    }
}
