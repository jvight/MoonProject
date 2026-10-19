using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Traces the maker's mark (<see cref="MakerLinework"/>) on 07's face as <see cref="BootFace"/> says: each stroke
    /// drawn as far as the beam has got, a bright head where the beam is, a soft phosphor halo, and a little extra glow
    /// on strokes the beam has just left. Colour and widths come from USS (--trace-color, --trace-halo-scale).
    /// </summary>
    internal sealed class LogoTracePainter : StylePainter
    {
        private static readonly CustomStyleProperty<Color> ColorProperty = new CustomStyleProperty<Color>("--trace-color");
        private static readonly CustomStyleProperty<float> HaloProperty = new CustomStyleProperty<float>("--trace-halo-scale");

        private const float HaloAlpha = 0.14f;
        private const float HaloGlowAlpha = 0.3f;
        private const float CoreAlpha = 0.82f;
        private const float CoreGlowAlpha = 0.18f;
        private const float BeamScale = 1.7f;
        private const float MinCoreWidth = 1.5f;

        private readonly MakerLinework _linework;
        private readonly BootFace _face;
        private Color _color = new Color(1f, 0.878f, 0.596f);
        private float _haloScale = 4f;

        public LogoTracePainter(VisualElement element, MakerLinework linework, BootFace face) : base(element)
        {
            _linework = linework ?? throw new ArgumentNullException(nameof(linework));
            _face = face ?? throw new ArgumentNullException(nameof(face));
        }

        /// <summary>Redraws the mark (call when the face's boot moved on).</summary>
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

            if (style.TryGetValue(HaloProperty, out float halo))
            {
                _haloScale = halo;
            }
        }

        protected override void Paint(MeshGenerationContext context)
        {
            float alpha = _face.LogoAlpha;
            Rect rect = Element.contentRect;
            float side = Mathf.Min(rect.width, rect.height);
            if (alpha <= 0f || side <= 0f)
            {
                return;
            }

            var origin = new Vector2((rect.width - side) * 0.5f, (rect.height - side) * 0.5f);
            float core = Mathf.Max(MinCoreWidth, _linework.StrokeWidth * side);
            Painter2D painter = context.painter2D;
            painter.lineJoin = LineJoin.Round;
            painter.lineCap = LineCap.Round;
            for (int stroke = 0; stroke < _linework.StrokeCount; stroke++)
            {
                float progress = _face.StrokeProgress(stroke);
                if (progress <= 0f)
                {
                    continue;
                }

                float glow = _face.StrokeGlow(stroke);
                float reach = progress * _linework.Length(stroke);
                Trace(painter, stroke, reach, origin, side, core * _haloScale,
                    Tint(alpha * (HaloAlpha + HaloGlowAlpha * glow)), out _);
                Trace(painter, stroke, reach, origin, side, core, Tint(alpha * (CoreAlpha + CoreGlowAlpha * glow)),
                    out Vector2 tip);
                if (progress < 1f)
                {
                    painter.fillColor = Tint(alpha);
                    painter.BeginPath();
                    painter.Arc(tip, core * BeamScale, 0f, 360f);
                    painter.Fill();
                }
            }
        }

        private Color Tint(float alpha)
        {
            return new Color(_color.r, _color.g, _color.b, _color.a * Mathf.Clamp01(alpha));
        }

        /// <summary>Strokes <paramref name="stroke"/> as far as <paramref name="reach"/> along it; <paramref name="tip"/> is where it stops.</summary>
        private void Trace(Painter2D painter, int stroke, float reach, Vector2 origin, float side, float width,
            Color color, out Vector2 tip)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            Vector2 previous = _linework.Point(stroke, 0);
            tip = origin + previous * side;
            painter.MoveTo(tip);
            int count = _linework.PointCount(stroke);
            float travelled = 0f;
            for (int i = 1; i < count; i++)
            {
                Vector2 next = _linework.Point(stroke, i);
                float segment = Vector2.Distance(previous, next);
                bool last = travelled + segment >= reach;
                Vector2 end = last && segment > 1e-6f
                    ? Vector2.Lerp(previous, next, (reach - travelled) / segment)
                    : next;
                tip = origin + end * side;
                painter.LineTo(tip);
                if (last)
                {
                    break;
                }

                travelled += segment;
                previous = next;
            }

            painter.Stroke();
        }
    }
}
