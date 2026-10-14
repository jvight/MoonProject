using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// A partially recovered kit blueprint on Kenji's board (0.5.5 slice D2): when <c>0 &lt; Pieces &lt; SetSize</c>,
    /// draws <see cref="Pieces"/> of <see cref="SetSize"/> torn paper strips pinned to the node's spot, with a jagged
    /// tear on the unfinished edge and faint pencil lines inside each piece. Never a number badge; once the set
    /// completes, <see cref="Pieces"/> resets to 0 and the full sheet label takes over.
    /// </summary>
    internal sealed class TornSheetPainter : StylePainter
    {
        private const float TearJaggedness = 0.08f;
        private const float SeamGap = 1.5f;
        private const float StrokeWidth = 1.4f;
        private const float SketchAlpha = 0.55f;
        private const float PinRadius = 1.8f;

        private static readonly CustomStyleProperty<Color> SheetColorProperty =
            new CustomStyleProperty<Color>("--sheet-color");

        private static readonly CustomStyleProperty<Color> SheetFillProperty =
            new CustomStyleProperty<Color>("--sheet-fill");

        private Color _stroke = new Color(0.96f, 0.9f, 0.78f, 0.85f);
        private Color _fill = new Color(0.96f, 0.9f, 0.78f, 0.14f);
        private int _pieces;
        private int _setSize;

        public TornSheetPainter(VisualElement element) : base(element)
        {
        }

        /// <summary>Scanned fragment pieces currently shown on this torn sheet (0 when hidden or complete).</summary>
        public int Pieces => _pieces;

        /// <summary>Total fragments in the blueprint set (0 when hidden or complete).</summary>
        public int SetSize => _setSize;

        /// <summary>Updates the torn sheet's piece count and marks the element for repaint only when changed.</summary>
        public void Set(int pieces, int setSize)
        {
            if (_pieces == pieces && _setSize == setSize)
            {
                return;
            }

            _pieces = pieces;
            _setSize = setSize;
            Repaint();
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            if (style.TryGetValue(SheetColorProperty, out Color stroke))
            {
                _stroke = stroke;
            }

            if (style.TryGetValue(SheetFillProperty, out Color fill))
            {
                _fill = fill;
            }
        }

        protected override void Paint(MeshGenerationContext context)
        {
            if (_pieces <= 0 || _setSize <= 0 || _pieces >= _setSize)
            {
                return;
            }

            Rect rect = Element.contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            painter.lineJoin = LineJoin.Round;
            painter.lineCap = LineCap.Round;
            float stripWidth = rect.width / _setSize;
            float tear = rect.width * TearJaggedness;
            float top = rect.yMin + 1f;
            float bottom = rect.yMax - 1f;
            float height = bottom - top;

            for (int p = 0; p < _pieces; p++)
            {
                float left = rect.xMin + p * stripWidth + (p > 0 ? SeamGap * 0.5f : 1f);
                float right = rect.xMin + (p + 1) * stripWidth - SeamGap * 0.5f;
                bool tornRight = p == _pieces - 1;

                painter.BeginPath();
                painter.MoveTo(new Vector2(left, top));
                painter.LineTo(new Vector2(right, top));
                if (tornRight)
                {
                    painter.LineTo(new Vector2(right - tear, top + height * 0.33f));
                    painter.LineTo(new Vector2(right + tear * 0.5f, top + height * 0.66f));
                }

                painter.LineTo(new Vector2(right, bottom));
                painter.LineTo(new Vector2(left, bottom));
                painter.ClosePath();
                painter.fillColor = _fill;
                painter.Fill();
                painter.lineWidth = StrokeWidth;
                painter.strokeColor = _stroke;
                painter.Stroke();

                Color sketch = _stroke;
                sketch.a *= SketchAlpha;
                painter.strokeColor = sketch;
                painter.lineWidth = 1f;
                float lineLeft = left + 2.5f;
                float lineRight = Mathf.Max(lineLeft + 2f, right - tear - 2f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(lineLeft, top + height * 0.42f));
                painter.LineTo(new Vector2(lineRight, top + height * 0.42f));
                painter.MoveTo(new Vector2(lineLeft, top + height * 0.68f));
                painter.LineTo(new Vector2(lineRight, top + height * 0.68f));
                painter.Stroke();
            }

            Shapes.Disc(painter, new Vector2(rect.xMin + stripWidth * 0.5f, top + 3f), PinRadius, _stroke);
        }
    }
}
