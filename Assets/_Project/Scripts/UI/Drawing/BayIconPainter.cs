using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The small bay icon on a socket whose part is fitted at the bay: a turntable seen from the side (a flat oval
    /// under a short post), painted so no sentence has to say it. Its colour comes from USS (--bay-icon-color).
    /// </summary>
    internal sealed class BayIconPainter : StylePainter
    {
        private const float StrokeShare = 0.1f;
        private const float OvalRadiusX = 0.46f;
        private const float OvalRadiusY = 0.14f;
        private const float OvalCentreY = 0.78f;
        private const float PostTop = 0.18f;
        private const float PostBottom = OvalCentreY - 0.06f;
        private const float PostHalfWidth = 0.12f;
        private const int OvalSteps = 20;

        private static readonly CustomStyleProperty<Color> ColorProperty =
            new CustomStyleProperty<Color>("--bay-icon-color");

        private Color _color = Color.clear;

        public BayIconPainter(VisualElement element) : base(element)
        {
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(ColorProperty, out _color);
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
            Vector2 centre = origin + new Vector2(0.5f, OvalCentreY) * size;
            painter.BeginPath();
            for (int i = 0; i <= OvalSteps; i++)
            {
                float angle = i * Mathf.PI * 2f / OvalSteps;
                Vector2 point = centre + new Vector2(Mathf.Cos(angle) * OvalRadiusX, Mathf.Sin(angle) * OvalRadiusY) *
                                size;
                if (i == 0)
                {
                    painter.MoveTo(point);
                }
                else
                {
                    painter.LineTo(point);
                }
            }

            painter.Stroke();
            painter.BeginPath();
            painter.MoveTo(origin + new Vector2(0.5f - PostHalfWidth, PostBottom) * size);
            painter.LineTo(origin + new Vector2(0.5f - PostHalfWidth, PostTop) * size);
            painter.LineTo(origin + new Vector2(0.5f + PostHalfWidth, PostTop) * size);
            painter.LineTo(origin + new Vector2(0.5f + PostHalfWidth, PostBottom) * size);
            painter.Stroke();
        }
    }
}
