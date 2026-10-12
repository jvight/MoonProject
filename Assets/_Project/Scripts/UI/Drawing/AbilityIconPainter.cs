using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// An ability slot's pictogram, drawn as soft strokes in a unit square sized to its element: the tether's ring with
    /// its line hanging down, the listening mast with two arcs, the leap's double chevron over the ground, the cradle's
    /// basket, the headlamp's rays, the coils' bolt, or a padlock. Never a letter.
    /// USS: --icon-color.
    /// </summary>
    internal sealed class AbilityIconPainter : StylePainter
    {
        private const float StrokeWidth = 0.1f;

        private static readonly CustomStyleProperty<Color> IconColorProperty =
            new CustomStyleProperty<Color>("--icon-color");

        private Color _iconColor = Color.clear;
        private AbilityIcon _icon;
        private Rect _rect;
        private float _size;

        public AbilityIconPainter(VisualElement element, AbilityIcon icon) : base(element)
        {
            _icon = icon;
        }

        /// <summary>Changes the icon (a slot unlocking); repaints.</summary>
        public void Show(AbilityIcon icon)
        {
            if (icon != _icon)
            {
                _icon = icon;
                Repaint();
            }
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(IconColorProperty, out _iconColor);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            _rect = Element.contentRect;
            _size = Mathf.Min(_rect.width, _rect.height);
            if (_size <= 0f)
            {
                return;
            }

            Painter2D p = context.painter2D;
            p.lineWidth = _size * StrokeWidth;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            p.strokeColor = _iconColor;
            switch (_icon)
            {
                case AbilityIcon.Tether:
                    Arc(p, 0.5f, 0.28f, 0.17f, 0f, 360f);
                    Line(p, 0.5f, 0.45f, 0.5f, 0.9f);
                    break;
                case AbilityIcon.Listen:
                    Line(p, 0.34f, 0.18f, 0.34f, 0.9f);
                    Line(p, 0.18f, 0.9f, 0.5f, 0.9f);
                    Arc(p, 0.34f, 0.3f, 0.2f, -55f, 55f);
                    Arc(p, 0.34f, 0.3f, 0.38f, -50f, 50f);
                    break;
                case AbilityIcon.Leap:
                    Polyline(p, 0.24f, 0.48f, 0.5f, 0.24f, 0.76f, 0.48f);
                    Polyline(p, 0.24f, 0.7f, 0.5f, 0.46f, 0.76f, 0.7f);
                    Line(p, 0.18f, 0.88f, 0.82f, 0.88f);
                    break;
                case AbilityIcon.Cradle:
                    Polyline(p, 0.14f, 0.38f, 0.26f, 0.8f, 0.74f, 0.8f, 0.86f, 0.38f);
                    Line(p, 0.1f, 0.38f, 0.9f, 0.38f);
                    break;
                case AbilityIcon.Headlamp:
                    p.BeginPath();
                    p.Arc(At(0.36f, 0.5f), _size * 0.18f, Angle.Degrees(0f), Angle.Degrees(360f));
                    p.Stroke();
                    Line(p, 0.64f, 0.5f, 0.92f, 0.5f);
                    Line(p, 0.62f, 0.3f, 0.86f, 0.18f);
                    Line(p, 0.62f, 0.7f, 0.86f, 0.82f);
                    break;
                case AbilityIcon.Boost:
                    Polyline(p, 0.58f, 0.08f, 0.32f, 0.54f, 0.6f, 0.5f, 0.42f, 0.92f);
                    break;
                default:
                    Arc(p, 0.5f, 0.44f, 0.2f, 180f, 360f);
                    Polyline(p, 0.24f, 0.46f, 0.76f, 0.46f, 0.76f, 0.88f, 0.24f, 0.88f, 0.24f, 0.46f);
                    break;
            }
        }

        private Vector2 At(float x, float y)
        {
            return new Vector2(_rect.center.x + (x - 0.5f) * _size, _rect.center.y + (y - 0.5f) * _size);
        }

        private void Line(Painter2D p, float x0, float y0, float x1, float y1)
        {
            p.BeginPath();
            p.MoveTo(At(x0, y0));
            p.LineTo(At(x1, y1));
            p.Stroke();
        }

        private void Arc(Painter2D p, float x, float y, float radius, float from, float to)
        {
            p.BeginPath();
            p.Arc(At(x, y), _size * radius, Angle.Degrees(from), Angle.Degrees(to));
            p.Stroke();
        }

        private void Polyline(Painter2D p, params float[] points)
        {
            p.BeginPath();
            p.MoveTo(At(points[0], points[1]));
            for (int i = 2; i < points.Length; i += 2)
            {
                p.LineTo(At(points[i], points[i + 1]));
            }

            p.Stroke();
        }
    }
}
