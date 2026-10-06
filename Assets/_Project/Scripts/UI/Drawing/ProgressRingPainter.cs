using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// A ring that fills clockwise from twelve o'clock (the hold-to-confirm ring). Repaints only when the progress
    /// moves. USS: --track-color, --fill-color, --ring-width.
    /// </summary>
    internal sealed class ProgressRingPainter : StylePainter
    {
        private const float RepaintEpsilon = 0.002f;

        private static readonly CustomStyleProperty<Color> TrackColorProperty =
            new CustomStyleProperty<Color>("--track-color");
        private static readonly CustomStyleProperty<Color> FillColorProperty =
            new CustomStyleProperty<Color>("--fill-color");
        private static readonly CustomStyleProperty<float> RingWidthProperty =
            new CustomStyleProperty<float>("--ring-width");

        private Color _trackColor = Color.clear;
        private Color _fillColor = Color.clear;
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

        protected override void ReadStyle(ICustomStyle style)
        {
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
        }
    }
}
