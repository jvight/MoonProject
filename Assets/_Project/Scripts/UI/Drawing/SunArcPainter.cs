using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The Sun's arc: a quarter of a dome from the apex down to the rim on the right, the part of the Sun still above
    /// the rim lit, with the Sun a small disc at its end. Repaints only when the fraction moves. USS: --track-color,
    /// --fill-color, --ring-width.
    /// </summary>
    internal sealed class SunArcPainter : StylePainter
    {
        private const float RepaintEpsilon = 0.004f;
        private const float QuarterDegrees = 90f;
        private const float SunDiscWidths = 0.9f;

        private static readonly CustomStyleProperty<Color> TrackColorProperty =
            new CustomStyleProperty<Color>("--track-color");
        private static readonly CustomStyleProperty<Color> FillColorProperty =
            new CustomStyleProperty<Color>("--fill-color");
        private static readonly CustomStyleProperty<float> RingWidthProperty =
            new CustomStyleProperty<float>("--ring-width");

        private Color _trackColor = Color.clear;
        private Color _fillColor = Color.clear;
        private float _ringWidth;
        private float _fraction;

        public SunArcPainter(VisualElement element) : base(element)
        {
        }

        /// <summary>0..1 of the Sun still above the rim.</summary>
        public float Fraction
        {
            get => _fraction;
            set
            {
                float clamped = Mathf.Clamp01(value);
                bool reachesEnd = clamped <= 0f || clamped >= 1f;
                if (clamped == _fraction || (!reachesEnd && Mathf.Abs(clamped - _fraction) < RepaintEpsilon))
                {
                    return;
                }

                _fraction = clamped;
                Repaint();
            }
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(TrackColorProperty, out _trackColor);
            style.TryGetValue(FillColorProperty, out _fillColor);
            style.TryGetValue(RingWidthProperty, out _ringWidth);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            float radius = Mathf.Min(rect.width, rect.height) - _ringWidth;
            if (radius <= 0f || _ringWidth <= 0f)
            {
                return;
            }

            // The dome's centre is the rim's left end at the bottom of the box; bearings run clockwise from twelve.
            var centre = new Vector2(rect.xMin + _ringWidth * 0.5f, rect.yMax - _ringWidth * 0.5f);
            Painter2D painter = context.painter2D;
            Shapes.ArcBetween(painter, centre, radius, _ringWidth, 0f, QuarterDegrees, _trackColor);
            if (_fraction <= 0f)
            {
                return;
            }

            float sunBearing = QuarterDegrees * (1f - _fraction);
            Shapes.ArcBetween(painter, centre, radius, _ringWidth, sunBearing, QuarterDegrees, _fillColor);
            float radians = sunBearing * Mathf.Deg2Rad;
            var sun = new Vector2(centre.x + radius * Mathf.Sin(radians), centre.y - radius * Mathf.Cos(radians));
            Shapes.Disc(painter, sun, _ringWidth * SunDiscWidths, _fillColor);
        }
    }
}
