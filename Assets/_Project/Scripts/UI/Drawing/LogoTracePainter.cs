using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Traces the maker's mark (<see cref="MakerLinework"/>) on 07's face as <see cref="BootFace"/> says, like a vector
    /// tube's beam: each stroke drawn as far as the beam has got, a bright head where the beam is with a short
    /// brighter phosphor trail behind it and a barely-there deflection wobble, a soft phosphor halo, and a little extra
    /// glow on strokes the beam has just left. Colour and widths come from USS (--trace-color, --trace-halo-scale).
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
        private readonly BootFaceSettings _settings;
        private Color _color = new Color(1f, 0.878f, 0.596f);
        private float _haloScale = 4f;

        public LogoTracePainter(VisualElement element, BootFaceSettings settings, BootFace face) : base(element)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _linework = settings.Linework != null
                ? settings.Linework
                : throw new ArgumentException("The boot face has no maker's mark.", nameof(settings));
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
                Trace(painter, stroke, 0f, reach, origin, side, core * _haloScale,
                    Tint(alpha * (HaloAlpha + HaloGlowAlpha * glow)), out _, out _);
                Trace(painter, stroke, 0f, reach, origin, side, core,
                    Tint(alpha * (CoreAlpha + CoreGlowAlpha * glow)), out Vector2 tip, out Vector2 heading);
                if (progress < 1f)
                {
                    // The phosphor just behind the beam is still bright: a short trail ahead of the settled line.
                    Trace(painter, stroke, reach - _settings.TrailShare, reach, origin, side, core,
                        Tint(alpha * (CoreAlpha + _settings.TrailBoost)), out _, out _);
                    float wobble = Mathf.Sin(_face.Clock * _settings.BeamWobbleHertz * 2f * Mathf.PI) *
                                   _settings.BeamWobble * side;
                    Vector2 across = new Vector2(-heading.y, heading.x);
                    painter.fillColor = Tint(alpha);
                    painter.BeginPath();
                    painter.Arc(tip + across * wobble, core * BeamScale, 0f, 360f);
                    painter.Fill();
                }
            }
        }

        private Color Tint(float alpha)
        {
            return new Color(_color.r, _color.g, _color.b, _color.a * Mathf.Clamp01(alpha));
        }

        /// <summary>
        /// Strokes <paramref name="stroke"/> from <paramref name="from"/> to <paramref name="reach"/> along it;
        /// <paramref name="tip"/> is where it stops and <paramref name="heading"/> the way the beam was going there.
        /// </summary>
        private void Trace(Painter2D painter, int stroke, float from, float reach, Vector2 origin, float side,
            float width, Color color, out Vector2 tip, out Vector2 heading)
        {
            from = Mathf.Max(0f, from);
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            Vector2 previous = _linework.Point(stroke, 0);
            tip = origin + previous * side;
            heading = Vector2.right;
            bool started = from <= 0f;
            if (started)
            {
                painter.MoveTo(tip);
            }

            int count = _linework.PointCount(stroke);
            float travelled = 0f;
            for (int i = 1; i < count; i++)
            {
                Vector2 next = _linework.Point(stroke, i);
                float segment = Vector2.Distance(previous, next);
                if (segment > 1e-6f)
                {
                    heading = (next - previous) / segment;
                }

                if (!started && travelled + segment >= from)
                {
                    Vector2 begin = segment > 1e-6f
                        ? Vector2.Lerp(previous, next, (from - travelled) / segment)
                        : previous;
                    painter.MoveTo(origin + begin * side);
                    started = true;
                }

                bool last = travelled + segment >= reach;
                Vector2 end = last && segment > 1e-6f
                    ? Vector2.Lerp(previous, next, (reach - travelled) / segment)
                    : next;
                tip = origin + end * side;
                if (started)
                {
                    painter.LineTo(tip);
                }

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
