using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Paints <see cref="CoverVignette"/> over the whole screen: the tube's dark (USS --cover-color) everywhere outside an
    /// ellipse of the screen's own shape, with a soft edge drawn as bands of rising cover, the ellipse shrinking to
    /// nothing as the cover closes.
    /// </summary>
    internal sealed class CoverVignettePainter : StylePainter
    {
        private static readonly CustomStyleProperty<Color> ColorProperty = new CustomStyleProperty<Color>("--cover-color");

        private const int Bands = 10;
        private const int Segments = 48;

        /// <summary>The screen's corner, in units of its half width and half height.</summary>
        private static readonly float HalfDiagonal = Mathf.Sqrt(2f);

        private readonly CoverVignette _cover;
        private readonly BootFaceSettings _settings;
        private Color _color = new Color(0.071f, 0.047f, 0.024f);

        public CoverVignettePainter(VisualElement element, CoverVignette cover, BootFaceSettings settings)
            : base(element)
        {
            _cover = cover ?? throw new ArgumentNullException(nameof(cover));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Refresh()
        {
            Repaint();
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            if (style.TryGetValue(ColorProperty, out Color color))
            {
                _color = color;
            }
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            float cover = _cover.Cover01;
            if (cover <= 0f || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            float soft = _settings.CoverSoftness * HalfDiagonal;
            float inner = (1f - cover) * (HalfDiagonal + soft) - soft;
            var centre = new Vector2(rect.width * 0.5f, rect.height * 0.5f);
            var half = new Vector2(rect.width * 0.5f, rect.height * 0.5f);
            for (int band = 0; band < Bands; band++)
            {
                float from = inner + soft * band / Bands;
                float to = from + soft / Bands;
                bool outermost = band == Bands - 1;
                if (!outermost && to <= 0f)
                {
                    continue;
                }

                float alpha = (band + 1f) / Bands;
                painter.fillColor = new Color(_color.r, _color.g, _color.b, _color.a * alpha);
                painter.BeginPath();
                if (outermost)
                {
                    painter.MoveTo(Vector2.zero);
                    painter.LineTo(new Vector2(rect.width, 0f));
                    painter.LineTo(new Vector2(rect.width, rect.height));
                    painter.LineTo(new Vector2(0f, rect.height));
                    painter.ClosePath();
                }
                else
                {
                    Ellipse(painter, centre, half, to);
                }

                if (from > 0f)
                {
                    Ellipse(painter, centre, half, from);
                }

                painter.Fill(FillRule.OddEven);
            }
        }

        /// <summary>Adds a closed ellipse of the screen's shape, <paramref name="radius"/> half-extents across.</summary>
        private static void Ellipse(Painter2D painter, Vector2 centre, Vector2 half, float radius)
        {
            for (int i = 0; i <= Segments; i++)
            {
                float angle = 2f * Mathf.PI * i / Segments;
                var point = new Vector2(centre.x + Mathf.Cos(angle) * radius * half.x,
                    centre.y + Mathf.Sin(angle) * radius * half.y);
                if (i == 0)
                {
                    painter.MoveTo(point);
                }
                else
                {
                    painter.LineTo(point);
                }
            }

            painter.ClosePath();
        }
    }
}
