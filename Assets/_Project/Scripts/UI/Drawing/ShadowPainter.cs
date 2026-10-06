using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// A soft drop shadow behind a panel (UI Toolkit has no box-shadow). Put it on an absolutely positioned element
    /// that fills the panel's wrapper, before the panel itself. USS: --shadow-color, --shadow-radius, --shadow-spread,
    /// --shadow-offset (px downwards), --shadow-layers.
    /// </summary>
    internal sealed class ShadowPainter : StylePainter
    {
        private static readonly CustomStyleProperty<Color> ColorProperty =
            new CustomStyleProperty<Color>("--shadow-color");
        private static readonly CustomStyleProperty<float> RadiusProperty =
            new CustomStyleProperty<float>("--shadow-radius");
        private static readonly CustomStyleProperty<float> SpreadProperty =
            new CustomStyleProperty<float>("--shadow-spread");
        private static readonly CustomStyleProperty<float> OffsetProperty =
            new CustomStyleProperty<float>("--shadow-offset");
        private static readonly CustomStyleProperty<int> LayersProperty =
            new CustomStyleProperty<int>("--shadow-layers");

        private Color _color = Color.clear;
        private float _radius;
        private float _spread;
        private float _offset;
        private int _layers;

        public ShadowPainter(VisualElement element) : base(element)
        {
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(ColorProperty, out _color);
            style.TryGetValue(RadiusProperty, out _radius);
            style.TryGetValue(SpreadProperty, out _spread);
            style.TryGetValue(OffsetProperty, out _offset);
            style.TryGetValue(LayersProperty, out _layers);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Shapes.SoftShadow(context.painter2D, rect, _radius, _spread, new Vector2(0f, _offset), _color, _layers);
        }
    }
}
