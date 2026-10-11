using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The Lunarion wordmark (docs/features/M3-09) as filled vector paths in SVG units (y down), parsed at build time
    /// from the approved SVG (UI/Wordmark builder) and traced into a Painter2D by <see cref="WordmarkPainter"/>. Each
    /// path is filled even-odd or non-zero, then stroked with round joins <see cref="Soften"/> wide, as the SVG is.
    /// </summary>
    public sealed class WordmarkShape : ScriptableObject
    {
        [SerializeField] private Vector2 _size;
        [SerializeField] private float _soften;
        [SerializeField] private WordmarkCommand[] _commands = Array.Empty<WordmarkCommand>();
        [SerializeField] private float[] _values = Array.Empty<float>();
        [SerializeField] private int[] _pathStarts = Array.Empty<int>();
        [SerializeField] private bool[] _pathEvenOdd = Array.Empty<bool>();

        /// <summary>The SVG's viewBox size.</summary>
        public Vector2 Size => _size;

        /// <summary>Width of the round-joined stroke in the ink colour that softens every corner (SVG units).</summary>
        public float Soften => _soften;

        public int PathCount => _pathStarts.Length;

        public bool IsEvenOdd(int path) => _pathEvenOdd[path];

        internal void Set(Vector2 size, float soften, WordmarkCommand[] commands, float[] values, int[] pathStarts,
            bool[] pathEvenOdd)
        {
            _size = size;
            _soften = soften;
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
            _values = values ?? throw new ArgumentNullException(nameof(values));
            _pathStarts = pathStarts ?? throw new ArgumentNullException(nameof(pathStarts));
            _pathEvenOdd = pathEvenOdd ?? throw new ArgumentNullException(nameof(pathEvenOdd));
        }

        /// <summary>Null when the shape can be drawn, otherwise what is wrong with it.</summary>
        public string Validate()
        {
            if (_size.x <= 0f || _size.y <= 0f)
            {
                return "has no size";
            }

            if (_pathStarts.Length == 0 || _pathStarts.Length != _pathEvenOdd.Length)
            {
                return "has no paths";
            }

            int needed = 0;
            foreach (WordmarkCommand command in _commands)
            {
                needed += ValueCount(command);
            }

            return needed == _values.Length ? null : "has values that do not match its commands";
        }

        /// <summary>How many values a command reads.</summary>
        public static int ValueCount(WordmarkCommand command)
        {
            switch (command)
            {
                case WordmarkCommand.MoveTo:
                    return 2;
                case WordmarkCommand.CubicTo:
                case WordmarkCommand.ArcTo:
                    return 6;
                default:
                    return 0;
            }
        }

        /// <summary>Adds one path to the painter's current path, mapped by origin + point * scale.</summary>
        internal void Trace(Painter2D painter, int path, Vector2 origin, float scale)
        {
            int first = 0;
            for (int i = 0; i < _pathStarts[path]; i++)
            {
                first += ValueCount(_commands[i]);
            }

            int end = path + 1 < _pathStarts.Length ? _pathStarts[path + 1] : _commands.Length;
            int v = first;
            for (int i = _pathStarts[path]; i < end; i++)
            {
                switch (_commands[i])
                {
                    case WordmarkCommand.MoveTo:
                        painter.MoveTo(origin + Point(v) * scale);
                        break;
                    case WordmarkCommand.CubicTo:
                        painter.BezierCurveTo(origin + Point(v) * scale, origin + Point(v + 2) * scale,
                            origin + Point(v + 4) * scale);
                        break;
                    case WordmarkCommand.ArcTo:
                        painter.Arc(origin + Point(v) * scale, _values[v + 2] * scale,
                            Angle.Degrees(_values[v + 3]), Angle.Degrees(_values[v + 4]),
                            _values[v + 5] > 0.5f ? ArcDirection.Clockwise : ArcDirection.CounterClockwise);
                        break;
                    default:
                        painter.ClosePath();
                        break;
                }

                v += ValueCount(_commands[i]);
            }
        }

        private Vector2 Point(int value)
        {
            return new Vector2(_values[value], _values[value + 1]);
        }
    }
}
