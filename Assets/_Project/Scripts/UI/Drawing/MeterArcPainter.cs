using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The home meter's arc (Variant C cyan telemetry gauge): a shallow arc over the distance with a faint
    /// track, a few ticks and a lit share. Repaints only when the share moves. USS: --track-color, --fill-color,
    /// --tick-color, --ring-width.
    /// </summary>
    internal sealed class MeterArcPainter : StylePainter
    {
        private const float From = 205f;
        private const float To = 335f;
        private const int Ticks = 5;
        private const float TickLength = 0.12f;
        private const float RepaintEpsilon = 0.004f;

        private static readonly CustomStyleProperty<Color> TrackColorProperty =
            new CustomStyleProperty<Color>("--track-color");

        private static readonly CustomStyleProperty<Color> FillColorProperty =
            new CustomStyleProperty<Color>("--fill-color");

        private static readonly CustomStyleProperty<Color> TickColorProperty =
            new CustomStyleProperty<Color>("--tick-color");

        private static readonly CustomStyleProperty<float> RingWidthProperty =
            new CustomStyleProperty<float>("--ring-width");

        private Color _trackColor = Color.clear;
        private Color _fillColor = Color.clear;
        private Color _tickColor = Color.clear;
        private float _ringWidth;
        private float _fill;

        public MeterArcPainter(VisualElement element) : base(element)
        {
        }

        /// <summary>0..1 of the arc lit.</summary>
        public float Fill
        {
            get => _fill;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Abs(clamped - _fill) < RepaintEpsilon && !(clamped <= 0f && _fill > 0f))
                {
                    return;
                }

                _fill = clamped;
                Repaint();
            }
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(TrackColorProperty, out _trackColor);
            style.TryGetValue(FillColorProperty, out _fillColor);
            style.TryGetValue(TickColorProperty, out _tickColor);
            style.TryGetValue(RingWidthProperty, out _ringWidth);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            float radius = rect.width * 0.5f;
            if (radius <= 0f || _ringWidth <= 0f)
            {
                return;
            }

            var centre = new Vector2(rect.center.x, rect.yMax + radius * 0.55f);
            Painter2D painter = context.painter2D;
            painter.lineCap = LineCap.Round;
            painter.lineWidth = _ringWidth;
            Arc(painter, centre, radius, From, To, _trackColor);
            if (_fill > 0f)
            {
                Arc(painter, centre, radius, From, From + (To - From) * _fill, _fillColor);
            }

            painter.lineWidth = _ringWidth * 0.5f;
            painter.strokeColor = _tickColor;
            for (int i = 0; i < Ticks; i++)
            {
                float angle = (From + (To - From) * i / (Ticks - 1)) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                painter.BeginPath();
                painter.MoveTo(centre + direction * radius * (1f + TickLength));
                painter.LineTo(centre + direction * radius * (1f + TickLength * 2f));
                painter.Stroke();
            }
        }

        private static void Arc(Painter2D painter, Vector2 centre, float radius, float from, float to, Color color)
        {
            painter.strokeColor = color;
            painter.BeginPath();
            painter.Arc(centre, radius, Angle.Degrees(from), Angle.Degrees(to));
            painter.Stroke();
        }
    }
}
