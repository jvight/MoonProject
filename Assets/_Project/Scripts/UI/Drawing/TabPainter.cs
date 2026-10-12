using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// A settings tab's skewed glass (Moonlight Glass, Subnautica's tab buttons): a parallelogram leaning forward, its
    /// fill and edge from USS (--tab-color, --tab-edge, --tab-edge-width), so the selected tab changes by class alone.
    /// </summary>
    internal sealed class TabPainter : StylePainter
    {
        private const float SkewDegrees = 18f;

        private static readonly CustomStyleProperty<Color> TabColorProperty =
            new CustomStyleProperty<Color>("--tab-color");

        private static readonly CustomStyleProperty<Color> TabEdgeProperty =
            new CustomStyleProperty<Color>("--tab-edge");

        private static readonly CustomStyleProperty<float> EdgeWidthProperty =
            new CustomStyleProperty<float>("--tab-edge-width");

        private Color _tabColor = Color.clear;
        private Color _tabEdge = Color.clear;
        private float _edgeWidth = 1f;

        public TabPainter(VisualElement element) : base(element)
        {
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(TabColorProperty, out _tabColor);
            style.TryGetValue(TabEdgeProperty, out _tabEdge);
            style.TryGetValue(EdgeWidthProperty, out _edgeWidth);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = new Rect(Vector2.zero, Element.localBound.size);
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float lean = rect.height * Mathf.Tan(SkewDegrees * Mathf.Deg2Rad);
            Painter2D painter = context.painter2D;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin + lean, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax - lean, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.fillColor = _tabColor;
            painter.Fill();
            painter.lineWidth = _edgeWidth;
            painter.strokeColor = _tabEdge;
            painter.Stroke();
        }
    }
}
