using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// A settings tab's CRT terminal frame (Variant C): a beveled rectangle with chamfered corners, its fill and edge
    /// from USS (--tab-color, --tab-edge, --tab-edge-width), so the selected tab changes by class alone.
    /// </summary>
    internal sealed class TabPainter : StylePainter
    {
        private const float Chamfer = 6f;

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

            float cut = Mathf.Min(Chamfer, Mathf.Min(rect.width, rect.height) * 0.25f);
            Painter2D painter = context.painter2D;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin + cut, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax - cut, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin + cut));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax - cut));
            painter.LineTo(new Vector2(rect.xMax - cut, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin + cut, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax - cut));
            painter.LineTo(new Vector2(rect.xMin, rect.yMin + cut));
            painter.ClosePath();
            painter.fillColor = _tabColor;
            painter.Fill();
            painter.lineWidth = _edgeWidth;
            painter.strokeColor = _tabEdge;
            painter.Stroke();
        }
    }
}
