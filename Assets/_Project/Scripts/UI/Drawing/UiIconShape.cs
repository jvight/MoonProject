using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// One parsed SVG path layer (<c>fill</c>, <c>primary</c>, or <c>detail</c>) inside a <see cref="UiIconShape"/>,
    /// using <see cref="WordmarkCommand"/> and float parameters in 0..64 SVG viewBox units.
    /// </summary>
    [Serializable]
    public struct UiIconPath
    {
        [SerializeField] private WordmarkCommand[] _commands;
        [SerializeField] private float[] _values;
        [SerializeField] private float _strokeWidth;
        [SerializeField] private bool _filled;

        public UiIconPath(WordmarkCommand[] commands, float[] values, float strokeWidth, bool filled)
        {
            _commands = commands ?? Array.Empty<WordmarkCommand>();
            _values = values ?? Array.Empty<float>();
            _strokeWidth = strokeWidth;
            _filled = filled;
        }

        public bool IsEmpty => _commands == null || _commands.Length == 0;

        public float StrokeWidth => _strokeWidth;

        public bool IsFilled => _filled;

        public WordmarkCommand[] Commands => _commands;

        public float[] Values => _values;

        public string Validate()
        {
            if (_commands == null || _values == null)
            {
                return "has null arrays";
            }

            if (_commands.Length == 0)
            {
                return _values.Length == 0 ? null : "has values with no commands";
            }

            int needed = 0;
            for (int i = 0; i < _commands.Length; i++)
            {
                needed += WordmarkShape.ValueCount(_commands[i]);
            }

            return needed == _values.Length ? null : "has values that do not match its commands";
        }

        internal void Draw(Painter2D painter, Vector2 origin, float scale, Color32 strokeColor, Color32 fillColor)
        {
            if (IsEmpty)
            {
                return;
            }

            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.lineWidth = Mathf.Max(_strokeWidth >= 2f ? 1.2f : 1.0f, _strokeWidth * scale);
            painter.strokeColor = strokeColor;
            if (_filled)
            {
                painter.fillColor = fillColor;
            }

            bool open = false;
            int v = 0;
            for (int i = 0; i < _commands.Length; i++)
            {
                switch (_commands[i])
                {
                    case WordmarkCommand.MoveTo:
                        if (open)
                        {
                            if (_filled)
                            {
                                painter.Fill();
                            }

                            painter.Stroke();
                        }

                        painter.BeginPath();
                        open = true;
                        painter.MoveTo(origin + new Vector2(_values[v], _values[v + 1]) * scale);
                        v += 2;
                        break;
                    case WordmarkCommand.LineTo:
                        painter.LineTo(origin + new Vector2(_values[v], _values[v + 1]) * scale);
                        v += 2;
                        break;
                    case WordmarkCommand.CubicTo:
                        painter.BezierCurveTo(
                            origin + new Vector2(_values[v], _values[v + 1]) * scale,
                            origin + new Vector2(_values[v + 2], _values[v + 3]) * scale,
                            origin + new Vector2(_values[v + 4], _values[v + 5]) * scale);
                        v += 6;
                        break;
                    case WordmarkCommand.ArcTo:
                        painter.Arc(
                            origin + new Vector2(_values[v], _values[v + 1]) * scale,
                            _values[v + 2] * scale,
                            Angle.Degrees(_values[v + 3]),
                            Angle.Degrees(_values[v + 4]),
                            _values[v + 5] > 0.5f ? ArcDirection.Clockwise : ArcDirection.CounterClockwise);
                        v += 6;
                        break;
                    case WordmarkCommand.Close:
                        painter.ClosePath();
                        break;
                }
            }

            if (open)
            {
                if (_filled)
                {
                    painter.Fill();
                }

                painter.Stroke();
            }
        }

        /// <summary>
        /// Fills every subpath as one shape with the even-odd rule, so a subpath inside another cuts a hole in it (a
        /// silhouette's windows and gaps), and strokes nothing.
        /// </summary>
        internal void FillEvenOdd(Painter2D painter, Vector2 origin, float scale, Color32 color)
        {
            if (IsEmpty || color.a == 0)
            {
                return;
            }

            painter.fillColor = color;
            painter.BeginPath();
            int v = 0;
            for (int i = 0; i < _commands.Length; i++)
            {
                switch (_commands[i])
                {
                    case WordmarkCommand.MoveTo:
                        painter.MoveTo(origin + new Vector2(_values[v], _values[v + 1]) * scale);
                        v += 2;
                        break;
                    case WordmarkCommand.LineTo:
                        painter.LineTo(origin + new Vector2(_values[v], _values[v + 1]) * scale);
                        v += 2;
                        break;
                    case WordmarkCommand.CubicTo:
                        painter.BezierCurveTo(
                            origin + new Vector2(_values[v], _values[v + 1]) * scale,
                            origin + new Vector2(_values[v + 2], _values[v + 3]) * scale,
                            origin + new Vector2(_values[v + 4], _values[v + 5]) * scale);
                        v += 6;
                        break;
                    case WordmarkCommand.ArcTo:
                        painter.Arc(
                            origin + new Vector2(_values[v], _values[v + 1]) * scale,
                            _values[v + 2] * scale,
                            Angle.Degrees(_values[v + 3]),
                            Angle.Degrees(_values[v + 4]),
                            _values[v + 5] > 0.5f ? ArcDirection.Clockwise : ArcDirection.CounterClockwise);
                        v += 6;
                        break;
                    case WordmarkCommand.Close:
                        painter.ClosePath();
                        break;
                }
            }

            painter.Fill(FillRule.OddEven);
        }
    }

    /// <summary>
    /// One filled tone of a two-tone icon: a silhouette filled with the even-odd rule, drawn in the icon's tint at
    /// <see cref="Opacity"/> (Argon's face icons: the body at 1, its second tone at 0.45).
    /// </summary>
    [Serializable]
    public struct UiIconTone
    {
        [SerializeField] private UiIconPath _path;
        [SerializeField] private float _opacity;

        public UiIconTone(UiIconPath path, float opacity)
        {
            _path = path;
            _opacity = Mathf.Clamp01(opacity);
        }

        public UiIconPath Path => _path;

        /// <summary>The share of the tint's alpha this tone is drawn at (0..1).</summary>
        public float Opacity => _opacity;
    }

    /// <summary>
    /// One 64x64 vector icon parsed from an SVG: filled tones drawn first (two-tone silhouettes), then an optional
    /// accent fill path, a primary stroke path and an optional detail stroke path (the line icons). An icon has tones,
    /// a primary stroke, or both.
    /// </summary>
    [Serializable]
    public struct UiIconShape
    {
        public const float ViewBoxSize = 64f;

        [SerializeField] private string _id;
        [SerializeField] private UiIconPath _fill;
        [SerializeField] private UiIconPath _primary;
        [SerializeField] private UiIconPath _detail;
        [SerializeField] private UiIconTone[] _tones;

        public UiIconShape(string id, UiIconPath fill, UiIconPath primary, UiIconPath detail)
            : this(id, fill, primary, detail, Array.Empty<UiIconTone>())
        {
        }

        public UiIconShape(string id, UiIconPath fill, UiIconPath primary, UiIconPath detail, UiIconTone[] tones)
        {
            _id = id ?? throw new ArgumentNullException(nameof(id));
            _fill = fill;
            _primary = primary;
            _detail = detail;
            _tones = tones ?? throw new ArgumentNullException(nameof(tones));
        }

        /// <summary>The canonical icon id (for example <c>item_metal</c> or <c>kit_wing</c>).</summary>
        public string Id => _id;

        public UiIconPath Fill => _fill;

        public UiIconPath Primary => _primary;

        public UiIconPath Detail => _detail;

        /// <summary>The filled tones, back to front (none for a line icon).</summary>
        public UiIconTone[] Tones => _tones ?? Array.Empty<UiIconTone>();

        public string Validate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                return "has an empty id";
            }

            UiIconTone[] tones = Tones;
            if (_primary.IsEmpty && tones.Length == 0)
            {
                return $"icon '{_id}' has neither a primary stroke path nor a filled tone";
            }

            for (int i = 0; i < tones.Length; i++)
            {
                string toneProblem = tones[i].Path.IsEmpty ? "is empty" : tones[i].Path.Validate();
                if (toneProblem != null)
                {
                    return $"icon '{_id}' tone {i} {toneProblem}";
                }
            }

            string fillProblem = _fill.Validate();
            if (fillProblem != null)
            {
                return $"icon '{_id}' fill path {fillProblem}";
            }

            string primaryProblem = _primary.Validate();
            if (primaryProblem != null)
            {
                return $"icon '{_id}' primary path {primaryProblem}";
            }

            string detailProblem = _detail.Validate();
            return detailProblem != null ? $"icon '{_id}' detail path {detailProblem}" : null;
        }
    }
}
