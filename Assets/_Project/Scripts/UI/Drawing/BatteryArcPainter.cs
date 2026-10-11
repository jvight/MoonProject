using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// 07's battery as a thin segmented arc high in the middle of the HUD (docs/features/M3-20 "How 07 shows it"):
    /// one segment per cell, left to right, bowed over the "next" line; full cells in the fill colour, the cell being
    /// drawn filled by what is left of it, empty cells as a faint track. Repaints only when a cell or a visible share
    /// of one changes. USS: --track-color, --fill-color, --arc-width, --arc-span (degrees), --segment-gap (degrees).
    /// </summary>
    internal sealed class BatteryArcPainter : StylePainter
    {
        /// <summary>A charge change smaller than this share of a cell is not worth a repaint.</summary>
        private const float RepaintStep = 0.01f;

        private static readonly CustomStyleProperty<Color> TrackColorProperty =
            new CustomStyleProperty<Color>("--track-color");
        private static readonly CustomStyleProperty<Color> FillColorProperty =
            new CustomStyleProperty<Color>("--fill-color");
        private static readonly CustomStyleProperty<float> ArcWidthProperty =
            new CustomStyleProperty<float>("--arc-width");
        private static readonly CustomStyleProperty<float> ArcSpanProperty =
            new CustomStyleProperty<float>("--arc-span");
        private static readonly CustomStyleProperty<float> SegmentGapProperty =
            new CustomStyleProperty<float>("--segment-gap");

        private Color _trackColor = Color.clear;
        private Color _fillColor = Color.clear;
        private float _arcWidth;
        private float _arcSpan;
        private float _segmentGap;
        private int _cells;
        private float _charge;

        public BatteryArcPainter(VisualElement element) : base(element)
        {
        }

        public int Cells => _cells;

        public float Charge => _charge;

        /// <summary>Shows <paramref name="charge"/> cells of a <paramref name="cells"/>-cell battery.</summary>
        public void Show(int cells, float charge)
        {
            charge = Mathf.Clamp(charge, 0f, cells);
            if (cells == _cells && Mathf.Abs(charge - _charge) < RepaintStep &&
                (charge > 0f || _charge <= 0f) && (charge < cells || _charge >= cells))
            {
                return;
            }

            _cells = cells;
            _charge = charge;
            Repaint();
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(TrackColorProperty, out _trackColor);
            style.TryGetValue(FillColorProperty, out _fillColor);
            style.TryGetValue(ArcWidthProperty, out _arcWidth);
            style.TryGetValue(ArcSpanProperty, out _arcSpan);
            style.TryGetValue(SegmentGapProperty, out _segmentGap);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            float halfSpan = _arcSpan * 0.5f;
            float segment = (_arcSpan - _segmentGap * (_cells - 1)) / Mathf.Max(1, _cells);
            if (_cells <= 0 || _arcWidth <= 0f || halfSpan <= 0f || segment <= 0f || rect.width <= _arcWidth)
            {
                return;
            }

            float radius = (rect.width - _arcWidth) * 0.5f / Mathf.Sin(halfSpan * Mathf.Deg2Rad);
            var center = new Vector2(rect.center.x, rect.yMin + _arcWidth * 0.5f + radius);
            Painter2D painter = context.painter2D;
            for (int i = 0; i < _cells; i++)
            {
                float from = -halfSpan + i * (segment + _segmentGap);
                Shapes.ArcBetween(painter, center, radius, _arcWidth, from, from + segment, _trackColor);
                float left = Mathf.Clamp01(_charge - i);
                Shapes.ArcBetween(painter, center, radius, _arcWidth, from, from + segment * left, _fillColor);
            }
        }
    }
}
