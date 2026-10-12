using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The battery dial's ring: one arc segment per cell clockwise from twelve o'clock, a faint track under each, the
    /// charged share lit, and one segment breathing (the cell in use). Repaints only when what it shows moves. USS:
    /// --track-color, --fill-color, --low-color, --ring-width.
    /// </summary>
    internal sealed class CellRingPainter : StylePainter
    {
        // Wide enough that four or six cells stay countable at 720p.
        private const float GapDegrees = 18f;
        private const float RepaintEpsilon = 0.004f;

        private static readonly CustomStyleProperty<Color> TrackColorProperty =
            new CustomStyleProperty<Color>("--track-color");

        private static readonly CustomStyleProperty<Color> FillColorProperty =
            new CustomStyleProperty<Color>("--fill-color");

        private static readonly CustomStyleProperty<Color> LowColorProperty =
            new CustomStyleProperty<Color>("--low-color");

        private static readonly CustomStyleProperty<float> RingWidthProperty =
            new CustomStyleProperty<float>("--ring-width");

        private Color _trackColor = Color.clear;
        private Color _fillColor = Color.clear;
        private Color _lowColor = Color.clear;
        private float _ringWidth;
        private int _cells;
        private float _charge;
        private int _breathing = -1;
        private float _breath = 1f;
        private bool _low;

        public CellRingPainter(VisualElement element) : base(element)
        {
        }

        /// <summary>What the ring shows; it repaints only when something visible moved.</summary>
        public void Set(int cells, float charge, int breathing, float breath, bool low)
        {
            if (cells == _cells && breathing == _breathing && low == _low &&
                Mathf.Abs(charge - _charge) < RepaintEpsilon && Mathf.Abs(breath - _breath) < RepaintEpsilon)
            {
                return;
            }

            _cells = cells;
            _charge = charge;
            _breathing = breathing;
            _breath = breath;
            _low = low;
            Repaint();
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(TrackColorProperty, out _trackColor);
            style.TryGetValue(FillColorProperty, out _fillColor);
            style.TryGetValue(LowColorProperty, out _lowColor);
            style.TryGetValue(RingWidthProperty, out _ringWidth);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f - _ringWidth * 0.5f;
            if (radius <= 0f || _ringWidth <= 0f || _cells <= 0)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            painter.lineWidth = _ringWidth;
            painter.lineCap = LineCap.Butt;
            float span = 360f / _cells;
            for (int i = 0; i < _cells; i++)
            {
                float start = -90f + i * span + GapDegrees * 0.5f;
                float end = start + span - GapDegrees;
                Segment(painter, rect.center, radius, start, end, _trackColor);
                float share = Mathf.Clamp01(_charge - i);
                if (share <= 0f)
                {
                    continue;
                }

                Color lit = _low ? _lowColor : _fillColor;
                if (i == _breathing)
                {
                    lit.a *= _breath;
                }

                Segment(painter, rect.center, radius, start, start + (end - start) * share, lit);
            }
        }

        private static void Segment(Painter2D painter, Vector2 centre, float radius, float from, float to, Color color)
        {
            painter.strokeColor = color;
            painter.BeginPath();
            painter.Arc(centre, radius, Angle.Degrees(from), Angle.Degrees(to));
            painter.Stroke();
        }
    }
}
