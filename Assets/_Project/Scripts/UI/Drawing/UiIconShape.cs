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
    }

    /// <summary>
    /// One 64x64 vector icon parsed from <c>Assets/_Project/UI/Icons/&lt;id&gt;.svg</c>: an optional accent fill path,
    /// a 2.2px primary stroke path, and an optional 1.5px detail stroke path.
    /// </summary>
    [Serializable]
    public struct UiIconShape
    {
        public const float ViewBoxSize = 64f;

        [SerializeField] private string _id;
        [SerializeField] private UiIconPath _fill;
        [SerializeField] private UiIconPath _primary;
        [SerializeField] private UiIconPath _detail;

        public UiIconShape(string id, UiIconPath fill, UiIconPath primary, UiIconPath detail)
        {
            _id = id ?? throw new ArgumentNullException(nameof(id));
            _fill = fill;
            _primary = primary;
            _detail = detail;
        }

        /// <summary>The canonical icon id (for example <c>item_metal</c> or <c>kit_wing</c>).</summary>
        public string Id => _id;

        public UiIconPath Fill => _fill;

        public UiIconPath Primary => _primary;

        public UiIconPath Detail => _detail;

        public string Validate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                return "has an empty id";
            }

            if (_primary.IsEmpty)
            {
                return $"icon '{_id}' has no primary stroke path";
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
