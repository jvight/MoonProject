using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// A soft ring with a faint halo and a tiny centre dot, sized to its element. USS: --ring-color, --ring-width,
    /// --glow-color, --glow-width, --dot-color, --dot-radius.
    /// </summary>
    internal sealed class ReticlePainter : StylePainter
    {
        private static readonly CustomStyleProperty<Color> RingColorProperty =
            new CustomStyleProperty<Color>("--ring-color");
        private static readonly CustomStyleProperty<float> RingWidthProperty =
            new CustomStyleProperty<float>("--ring-width");
        private static readonly CustomStyleProperty<Color> GlowColorProperty =
            new CustomStyleProperty<Color>("--glow-color");
        private static readonly CustomStyleProperty<float> GlowWidthProperty =
            new CustomStyleProperty<float>("--glow-width");
        private static readonly CustomStyleProperty<Color> DotColorProperty =
            new CustomStyleProperty<Color>("--dot-color");
        private static readonly CustomStyleProperty<float> DotRadiusProperty =
            new CustomStyleProperty<float>("--dot-radius");

        private Color _ringColor = Color.clear;
        private float _ringWidth;
        private Color _glowColor = Color.clear;
        private float _glowWidth;
        private Color _dotColor = Color.clear;
        private float _dotRadius;

        public ReticlePainter(VisualElement element) : base(element)
        {
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(RingColorProperty, out _ringColor);
            style.TryGetValue(RingWidthProperty, out _ringWidth);
            style.TryGetValue(GlowColorProperty, out _glowColor);
            style.TryGetValue(GlowWidthProperty, out _glowWidth);
            style.TryGetValue(DotColorProperty, out _dotColor);
            style.TryGetValue(DotRadiusProperty, out _dotRadius);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f - Mathf.Max(_ringWidth, _glowWidth) * 0.5f;
            if (radius <= 0f)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            if (_glowWidth > 0f && _glowColor.a > 0f)
            {
                Shapes.Ring(painter, rect.center, radius, _glowWidth, _glowColor);
            }

            if (_ringWidth > 0f && _ringColor.a > 0f)
            {
                Shapes.Ring(painter, rect.center, radius, _ringWidth, _ringColor);
            }

            if (_dotRadius > 0f && _dotColor.a > 0f)
            {
                Shapes.Disc(painter, rect.center, _dotRadius, _dotColor);
            }
        }
    }
}
