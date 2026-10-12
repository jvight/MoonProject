using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// A small check mark drawn as two soft strokes, sized to its element (Be Vietnam Pro has no check glyph). USS:
    /// --tick-color.
    /// </summary>
    internal sealed class CheckPainter : StylePainter
    {
        private const float StrokeWidth = 0.14f;
        private static readonly Vector2 Start = new Vector2(0.12f, 0.54f);
        private static readonly Vector2 Elbow = new Vector2(0.4f, 0.8f);
        private static readonly Vector2 End = new Vector2(0.9f, 0.2f);

        private static readonly CustomStyleProperty<Color> TickColorProperty =
            new CustomStyleProperty<Color>("--tick-color");

        private Color _tickColor = Color.clear;

        public CheckPainter(VisualElement element) : base(element)
        {
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(TickColorProperty, out _tickColor);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            float size = Mathf.Min(rect.width, rect.height);
            if (size <= 0f)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            painter.lineWidth = size * StrokeWidth;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.strokeColor = _tickColor;
            painter.BeginPath();
            painter.MoveTo(At(rect, size, Start));
            painter.LineTo(At(rect, size, Elbow));
            painter.LineTo(At(rect, size, End));
            painter.Stroke();
        }

        private static Vector2 At(Rect rect, float size, Vector2 unit)
        {
            return new Vector2(rect.center.x + (unit.x - 0.5f) * size, rect.center.y + (unit.y - 0.5f) * size);
        }
    }
}
