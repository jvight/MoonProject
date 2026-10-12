using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>Where a painted arrow points.</summary>
    internal enum ArrowDirection
    {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3,
    }

    /// <summary>
    /// A small filled arrow, painted because the fonts have none (the lab's gamepad hints: the stick and the way to push
    /// it). Its colour comes from USS (--arrow-color); it fills its element.
    /// </summary>
    internal sealed class ArrowPainter : StylePainter
    {
        private static readonly CustomStyleProperty<Color> ArrowColorProperty =
            new CustomStyleProperty<Color>("--arrow-color");

        private readonly ArrowDirection _direction;
        private Color _color = Color.clear;

        public ArrowPainter(VisualElement element, ArrowDirection direction) : base(element)
        {
            _direction = direction;
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(ArrowColorProperty, out _color);
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
            switch (_direction)
            {
                case ArrowDirection.Left:
                    painter.MoveTo(new Vector2(rect.xMin, rect.center.y));
                    painter.LineTo(new Vector2(rect.xMax, rect.yMin));
                    painter.LineTo(new Vector2(rect.xMax, rect.yMax));
                    break;
                case ArrowDirection.Right:
                    painter.MoveTo(new Vector2(rect.xMax, rect.center.y));
                    painter.LineTo(new Vector2(rect.xMin, rect.yMin));
                    painter.LineTo(new Vector2(rect.xMin, rect.yMax));
                    break;
                case ArrowDirection.Up:
                    painter.MoveTo(new Vector2(rect.center.x, rect.yMin));
                    painter.LineTo(new Vector2(rect.xMax, rect.yMax));
                    painter.LineTo(new Vector2(rect.xMin, rect.yMax));
                    break;
                default:
                    painter.MoveTo(new Vector2(rect.center.x, rect.yMax));
                    painter.LineTo(new Vector2(rect.xMax, rect.yMin));
                    painter.LineTo(new Vector2(rect.xMin, rect.yMin));
                    break;
            }

            painter.ClosePath();
            painter.Fill();
        }
    }
}
