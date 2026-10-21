using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The small caret on a chip that rides on 07 because its object is off screen (opening v3 guidance audit, G3): a
    /// soft triangle on the chip's edge pointing the way to the object. It turns round the chip's centre, so it always
    /// sits where the chip's edge faces the object. USS: .prompt-caret (the pivot), .prompt-caret__tip (--caret-color).
    /// </summary>
    internal sealed class PromptCaret
    {
        public const string PivotClass = "prompt-caret";
        public const string TipClass = "prompt-caret__tip";

        // Pixels the caret's tip clears the chip's edge by.
        private const float EdgeGap = 6f;

        private readonly VisualElement _pivot;
        private readonly VisualElement _tip;
        private bool _shown = true;

        public PromptCaret(VisualElement chip)
        {
            if (chip == null)
            {
                throw new ArgumentNullException(nameof(chip));
            }

            _pivot = new VisualElement { name = "prompt-caret", pickingMode = PickingMode.Ignore };
            _pivot.AddToClassList(PivotClass);
            _tip = new VisualElement { name = "prompt-caret-tip", pickingMode = PickingMode.Ignore };
            _tip.AddToClassList(TipClass);
            _pivot.Add(_tip);
            chip.Add(_pivot);
            new CaretPainter(_tip);
            Hide();
        }

        public bool IsShown => _shown;

        /// <summary>The way the caret points, in degrees on screen (0 to the right, 90 down), while shown (tests).</summary>
        public float Angle { get; private set; }

        /// <summary>Points from a chip of <paramref name="size"/> along <paramref name="direction"/> (panel space).</summary>
        public void Point(Vector2 size, Vector2 direction)
        {
            if (direction.sqrMagnitude < 1e-6f || float.IsNaN(size.x) || float.IsNaN(size.y))
            {
                Hide();
                return;
            }

            Vector2 way = direction.normalized;
            float half = Mathf.Min(Mathf.Abs(way.x) > 1e-4f ? size.x * 0.5f / Mathf.Abs(way.x) : float.MaxValue,
                Mathf.Abs(way.y) > 1e-4f ? size.y * 0.5f / Mathf.Abs(way.y) : float.MaxValue);
            Angle = Mathf.Atan2(way.y, way.x) * Mathf.Rad2Deg;
            _pivot.style.rotate = new Rotate(Angle);
            _tip.style.translate = new Translate(half + EdgeGap, 0f);
            if (!_shown)
            {
                _shown = true;
                _pivot.style.display = DisplayStyle.Flex;
            }
        }

        public void Hide()
        {
            if (!_shown)
            {
                return;
            }

            _shown = false;
            _pivot.style.display = DisplayStyle.None;
        }

        /// <summary>A small triangle pointing right, in --caret-color.</summary>
        private sealed class CaretPainter : StylePainter
        {
            private static readonly CustomStyleProperty<Color> ColorProperty =
                new CustomStyleProperty<Color>("--caret-color");

            private Color _color = Color.clear;

            public CaretPainter(VisualElement element) : base(element)
            {
            }

            protected override void ReadStyle(ICustomStyle style)
            {
                style.TryGetValue(ColorProperty, out _color);
            }

            protected override void Paint(MeshGenerationContext context)
            {
                Rect rect = Element.contentRect;
                if (rect.width <= 0f || rect.height <= 0f)
                {
                    return;
                }

                Painter2D painter = context.painter2D;
                painter.fillColor = _color;
                painter.BeginPath();
                painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
                painter.LineTo(new Vector2(rect.xMax, rect.center.y));
                painter.LineTo(new Vector2(rect.xMin, rect.yMax));
                painter.ClosePath();
                painter.Fill();
            }
        }
    }
}
