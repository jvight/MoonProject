using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The cassette icon: a small tape with two reels, sized to its element. USS: --icon-color (the shell and
    /// reels), --icon-hole-color (the window).
    /// </summary>
    internal sealed class CassetteIconPainter : StylePainter
    {
        private static readonly CustomStyleProperty<Color> IconColorProperty =
            new CustomStyleProperty<Color>("--icon-color");
        private static readonly CustomStyleProperty<Color> HoleColorProperty =
            new CustomStyleProperty<Color>("--icon-hole-color");

        private Color _iconColor = Color.clear;
        private Color _holeColor = Color.clear;

        public CassetteIconPainter(VisualElement element) : base(element)
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
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Shapes.Cassette(context.painter2D, rect, _iconColor, _holeColor);
        }
    }
}
