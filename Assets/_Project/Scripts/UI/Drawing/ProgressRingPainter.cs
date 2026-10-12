using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// A ring that fills clockwise from twelve o'clock (the hold-to-confirm ring), with an optional thinner charge arc
    /// inside it (the cut's charge-up). Repaints only when either moves. USS: --track-color, --fill-color,
    /// --charge-color, --ring-width.
    /// </summary>
    internal sealed class ProgressRingPainter : StylePainter
    {
        private const float RepaintEpsilon = 0.002f;
        private const float ChargeInset = 2.6f;
        private const float ChargeThickness = 0.7f;

        private static readonly CustomStyleProperty<Color> TrackColorProperty =
            new CustomStyleProperty<Color>("--track-color");
        private static readonly CustomStyleProperty<Color> FillColorProperty =
            new CustomStyleProperty<Color>("--fill-color");
        private static readonly CustomStyleProperty<Color> ChargeColorProperty =
            new CustomStyleProperty<Color>("--charge-color");

        private static readonly CustomStyleProperty<float> RingWidthProperty =
            new CustomStyleProperty<float>("--ring-width");

        private Color _trackColor = Color.clear;
        private Color _fillColor = Color.clear;
        private Color _chargeColor = Color.clear;
        private float _charge;
        private float _ringWidth;
        private float _progress;

        public ProgressRingPainter(VisualElement element) : base(element)
        {
        }

        /// <summary>0..1 of the ring filled.</summary>
        public float Progress
        {
            get => _progress;
            set
            {
                float clamped = Mathf.Clamp01(value);
                bool reachesEnd = clamped <= 0f || clamped >= 1f;
                if (clamped == _progress || (!reachesEnd && Mathf.Abs(clamped - _progress) < RepaintEpsilon))
                {
                    return;
                }

                _progress = clamped;
                Repaint();
            }
        }

        /// <summary>0..1 of the inner charge arc filled (0 draws nothing).</summary>
        public float Charge
        {
            get => _charge;
            set
            {
                float clamped = Mathf.Clamp01(value);
                bool reachesEnd = clamped <= 0f || clamped >= 1f;
                if (clamped == _charge || (!reachesEnd && Mathf.Abs(clamped - _charge) < RepaintEpsilon))
                {
                    return;
                }

                _charge = clamped;
                Repaint();
            }
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(ChargeColorProperty, out _chargeColor);
            style.TryGetValue(TrackColorProperty, out _trackColor);
            style.TryGetValue(FillColorProperty, out _fillColor);
            style.TryGetValue(RingWidthProperty, out _ringWidth);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f - _ringWidth * 0.5f;
            if (radius <= 0f || _ringWidth <= 0f)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            Shapes.Ring(painter, rect.center, radius, _ringWidth, _trackColor);
            Shapes.Arc(painter, rect.center, radius, _ringWidth, _progress, _fillColor);
            if (_charge > 0f)
            {
                float inner = radius - _ringWidth * ChargeInset;
                if (inner > 0f)
                {
                    Shapes.Arc(painter, rect.center, inner, _ringWidth * ChargeThickness, _charge, _chargeColor);
                }
            }
        }
    }
}
