using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Parses the SVG path data the wordmark uses into <see cref="WordmarkCommand"/>s: absolute M, C, A (circular,
    /// unrotated) and Z, with C and A repeatable. Arcs are resolved from SVG's endpoint form to a centre, a radius and
    /// two angles (degrees, y down) so Painter2D can draw them. Anything else is a format error, never skipped.
    /// </summary>
    public static class SvgPathParser
    {
        private const float CircularTolerance = 0.001f;

        /// <summary>Appends the commands and values of <paramref name="data"/> to the two lists.</summary>
        public static void Parse(string data, List<WordmarkCommand> commands, List<float> values)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var reader = new Reader(data);
            char command = '\0';
            Vector2 current = Vector2.zero;
            Vector2 start = Vector2.zero;
            while (reader.SkipSeparators())
            {
                if (char.IsLetter(reader.Peek))
                {
                    command = reader.Next();
                }
                else if (command != 'C' && command != 'A')
                {
                    throw new FormatException($"SVG path: a number with no command at {reader.Position}.");
                }

                switch (command)
                {
                    case 'M':
                        current = start = reader.Point();
                        Add(commands, values, WordmarkCommand.MoveTo, current.x, current.y);
                        command = '\0';
                        break;
                    case 'C':
                        Vector2 first = reader.Point();
                        Vector2 second = reader.Point();
                        current = reader.Point();
                        Add(commands, values, WordmarkCommand.CubicTo, first.x, first.y, second.x, second.y,
                            current.x, current.y);
                        break;
                    case 'A':
                        current = Arc(reader, current, commands, values);
                        break;
                    case 'Z':
                        commands.Add(WordmarkCommand.Close);
                        current = start;
                        command = '\0';
                        break;
                    default:
                        throw new FormatException($"SVG path: command '{command}' is not supported.");
                }
            }
        }

        private static Vector2 Arc(Reader reader, Vector2 from, List<WordmarkCommand> commands, List<float> values)
        {
            float rx = reader.Number();
            float ry = reader.Number();
            float rotation = reader.Number();
            bool large = reader.Number() != 0f;
            bool sweep = reader.Number() != 0f;
            Vector2 to = reader.Point();
            if (Mathf.Abs(rx - ry) > CircularTolerance || Mathf.Abs(rotation) > CircularTolerance)
            {
                throw new FormatException("SVG path: only circular, unrotated arcs are supported.");
            }

            // SVG 1.1 implementation notes, F.6.5 (endpoint to centre), for a circle with no rotation.
            Vector2 half = (from - to) * 0.5f;
            float radius = Mathf.Max(rx, half.magnitude);
            float reach = Mathf.Sqrt(Mathf.Max(0f, radius * radius - half.sqrMagnitude) / half.sqrMagnitude);
            float sign = large != sweep ? 1f : -1f;
            Vector2 centre = new Vector2(half.y, -half.x) * (sign * reach) + (from + to) * 0.5f;
            float startAngle = Mathf.Atan2(from.y - centre.y, from.x - centre.x) * Mathf.Rad2Deg;
            float delta = Mathf.Atan2(to.y - centre.y, to.x - centre.x) * Mathf.Rad2Deg - startAngle;
            if (sweep && delta < 0f)
            {
                delta += 360f;
            }
            else if (!sweep && delta > 0f)
            {
                delta -= 360f;
            }

            Add(commands, values, WordmarkCommand.ArcTo, centre.x, centre.y, radius, startAngle, startAngle + delta,
                sweep ? 1f : 0f);
            return to;
        }

        private static void Add(List<WordmarkCommand> commands, List<float> values, WordmarkCommand command,
            params float[] numbers)
        {
            commands.Add(command);
            values.AddRange(numbers);
        }

        private sealed class Reader
        {
            private readonly string _text;

            public Reader(string text)
            {
                _text = text;
            }

            public int Position { get; private set; }

            public char Peek => _text[Position];

            public char Next()
            {
                return _text[Position++];
            }

            /// <summary>Skips spaces and commas; false at the end.</summary>
            public bool SkipSeparators()
            {
                while (Position < _text.Length && (char.IsWhiteSpace(_text[Position]) || _text[Position] == ','))
                {
                    Position++;
                }

                return Position < _text.Length;
            }

            public Vector2 Point()
            {
                float x = Number();
                return new Vector2(x, Number());
            }

            public float Number()
            {
                if (!SkipSeparators())
                {
                    throw new FormatException("SVG path: ended in the middle of a command.");
                }

                int begin = Position;
                if (Peek == '-' || Peek == '+')
                {
                    Position++;
                }

                bool dot = false;
                while (Position < _text.Length)
                {
                    char c = _text[Position];
                    if (char.IsDigit(c))
                    {
                        Position++;
                    }
                    else if (c == '.' && !dot)
                    {
                        dot = true;
                        Position++;
                    }
                    else if ((c == 'e' || c == 'E') && Position > begin)
                    {
                        Position++;
                        if (Position < _text.Length && (_text[Position] == '-' || _text[Position] == '+'))
                        {
                            Position++;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                string token = _text.Substring(begin, Position - begin);
                if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    throw new FormatException($"SVG path: '{token}' at {begin} is not a number.");
                }

                return value;
            }
        }
    }
}
