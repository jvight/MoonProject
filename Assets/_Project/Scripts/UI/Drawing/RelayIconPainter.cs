using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The relay icon: a slim mast with its lamp lit on top and two arcs of signal either side, sized to its element.
    /// USS: --icon-color (the lamp and the signal), --mast-color (the mast).
    /// </summary>
    internal sealed class RelayIconPainter : StylePainter
    {
        private const float LampHeight = 0.36f;
        private const float LampRadius = 0.12f;
        private const float InnerArc = 0.26f;
        private const float OuterArc = 0.42f;
        private const float ArcSweep = 50f;
        private const float StrokeWidth = 0.08f;
        private const float OuterAlpha = 0.5f;

        private static readonly CustomStyleProperty<Color> IconColorProperty =
            new CustomStyleProperty<Color>("--icon-color");
        private static readonly CustomStyleProperty<Color> MastColorProperty =
            new CustomStyleProperty<Color>("--mast-color");

        private Color _iconColor = Color.clear;
        private Color _mastColor = Color.clear;

        public RelayIconPainter(VisualElement element) : base(element)
        {
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(IconColorProperty, out _iconColor);
            style.TryGetValue(MastColorProperty, out _mastColor);
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
            var lamp = new Vector2(rect.center.x, rect.yMin + rect.height * LampHeight);
            painter.lineWidth = size * StrokeWidth;
            painter.lineCap = LineCap.Round;
            painter.strokeColor = _mastColor;
            painter.BeginPath();
            painter.MoveTo(lamp);
            painter.LineTo(new Vector2(lamp.x, rect.yMax));
            painter.Stroke();

            Color outer = _iconColor;
            outer.a *= OuterAlpha;
            Signal(painter, lamp, size * InnerArc, _iconColor);
            Signal(painter, lamp, size * OuterArc, outer);
            Shapes.Disc(painter, lamp, size * LampRadius, _iconColor);
        }

        /// <summary>A pair of arcs around <paramref name="lamp"/>, one to each side.</summary>
        private static void Signal(Painter2D painter, Vector2 lamp, float radius, Color color)
        {
            painter.strokeColor = color;
            painter.BeginPath();
            painter.Arc(lamp, radius, Angle.Degrees(-ArcSweep * 0.5f), Angle.Degrees(ArcSweep * 0.5f));
            painter.Stroke();
            painter.BeginPath();
            painter.Arc(lamp, radius, Angle.Degrees(180f - ArcSweep * 0.5f), Angle.Degrees(180f + ArcSweep * 0.5f));
            painter.Stroke();
        }
    }
}
