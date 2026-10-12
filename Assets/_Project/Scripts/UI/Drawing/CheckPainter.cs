using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// A check mark, painted because the fonts have no ✓ (a recipe row's "this one is covered"). Its colour comes from
    /// USS (--check-color) and its stroke scales with the element.
    /// </summary>
    internal sealed class CheckPainter : StylePainter
    {
        private const float StrokeShare = 0.16f;
        private static readonly Vector2 Start = new Vector2(0.12f, 0.54f);
        private static readonly Vector2 Elbow = new Vector2(0.4f, 0.8f);
        private static readonly Vector2 End = new Vector2(0.9f, 0.2f);

        private static readonly CustomStyleProperty<Color> CheckColorProperty =
            new CustomStyleProperty<Color>("--check-color");

        private Color _color = Color.clear;

        public CheckPainter(VisualElement element) : base(element)
        {
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(CheckColorProperty, out _color);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            float size = Mathf.Min(rect.width, rect.height);
            if (size <= 0f)
            {
                return;
            }

            Vector2 origin = rect.center - new Vector2(size, size) * 0.5f;
            Painter2D painter = context.painter2D;
            painter.lineWidth = size * StrokeShare;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.strokeColor = _color;
            painter.BeginPath();
            painter.MoveTo(origin + Start * size);
            painter.LineTo(origin + Elbow * size);
            painter.LineTo(origin + End * size);
            painter.Stroke();
        }
    }
}
