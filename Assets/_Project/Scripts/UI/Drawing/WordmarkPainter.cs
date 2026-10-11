using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Draws the <see cref="WordmarkShape"/> to fill its element's width, keeping the element's height at the
    /// shape's aspect. A very soft warm glow is laid first as wide, faint round strokes, then each path is filled and
    /// softened with its round-joined stroke in the ink colour. USS: --ink-color, --glow-color, --glow-width.
    /// </summary>
    internal sealed class WordmarkPainter : StylePainter
    {
        private const float HeightEpsilon = 0.5f;
        private const int GlowPasses = 3;

        private static readonly CustomStyleProperty<Color> InkColorProperty = new CustomStyleProperty<Color>("--ink-color");
        private static readonly CustomStyleProperty<Color> GlowColorProperty =
            new CustomStyleProperty<Color>("--glow-color");
        private static readonly CustomStyleProperty<float> GlowWidthProperty =
            new CustomStyleProperty<float>("--glow-width");

        private readonly WordmarkShape _shape;
        private Color _ink = Color.clear;
        private Color _glow = Color.clear;
        private float _glowWidth;

        public WordmarkPainter(VisualElement element, WordmarkShape shape) : base(element)
        {
            _shape = shape != null ? shape : throw new ArgumentNullException(nameof(shape));
            element.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(InkColorProperty, out _ink);
            style.TryGetValue(GlowColorProperty, out _glow);
            style.TryGetValue(GlowWidthProperty, out _glowWidth);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            if (rect.width <= 0f)
            {
                return;
            }

            float scale = rect.width / _shape.Size.x;
            Painter2D painter = context.painter2D;
            painter.lineJoin = LineJoin.Round;
            painter.lineCap = LineCap.Round;
            for (int pass = GlowPasses; pass >= 1 && _glowWidth > 0f; pass--)
            {
                painter.strokeColor = _glow;
                painter.lineWidth = _glowWidth * pass / GlowPasses;
                for (int path = 0; path < _shape.PathCount; path++)
                {
                    painter.BeginPath();
                    _shape.Trace(painter, path, rect.min, scale);
                    painter.Stroke();
                }
            }

            painter.fillColor = _ink;
            painter.strokeColor = _ink;
            painter.lineWidth = _shape.Soften * scale;
            for (int path = 0; path < _shape.PathCount; path++)
            {
                painter.BeginPath();
                _shape.Trace(painter, path, rect.min, scale);
                painter.Fill(_shape.IsEvenOdd(path) ? FillRule.OddEven : FillRule.NonZero);
                painter.Stroke();
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            float height = evt.newRect.width * _shape.Size.y / _shape.Size.x;
            if (Mathf.Abs(Element.resolvedStyle.height - height) > HeightEpsilon)
            {
                Element.style.height = height;
            }
        }
    }
}
