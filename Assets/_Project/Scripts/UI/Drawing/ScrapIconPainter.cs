using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The scrap icon: a small glowing hex nut, sized to its element. USS: --icon-color, --icon-hole-color.
    /// </summary>
    internal sealed class ScrapIconPainter : StylePainter
    {
        private static readonly CustomStyleProperty<Color> IconColorProperty =
            new CustomStyleProperty<Color>("--icon-color");
        private static readonly CustomStyleProperty<Color> HoleColorProperty =
            new CustomStyleProperty<Color>("--icon-hole-color");

        private Color _iconColor = Color.clear;
        private Color _holeColor = Color.clear;

        public ScrapIconPainter(VisualElement element) : base(element)
        {
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(IconColorProperty, out _iconColor);
            style.TryGetValue(HoleColorProperty, out _holeColor);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            if (radius <= 0f)
            {
                return;
            }

            Shapes.Nut(context.painter2D, rect.center, radius, _iconColor, _holeColor);
        }
    }
}
