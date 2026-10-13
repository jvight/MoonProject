using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The thin leader lines of the 07 tab: one short line from each socket's true point on the portrait to its name.
    /// The page writes the lines each frame (07 bobs) and the painter draws what it holds. Colour from USS
    /// (--leader-color).
    /// </summary>
    internal sealed class LeaderPainter : StylePainter
    {
        private const float LineWidth = 1.5f;

        private static readonly CustomStyleProperty<Color> ColorProperty =
            new CustomStyleProperty<Color>("--leader-color");

        private readonly Vector2[] _from;
        private readonly Vector2[] _to;
        private int _count;
        private Color _color = Color.clear;

        public LeaderPainter(VisualElement element, int capacity) : base(element)
        {
            _from = new Vector2[capacity];
            _to = new Vector2[capacity];
        }

        /// <summary>Starts a new set of lines.</summary>
        public void Clear()
        {
            _count = 0;
        }

        /// <summary>Adds a line from <paramref name="from"/> to <paramref name="to"/> (pixels of the element).</summary>
        public void Add(Vector2 from, Vector2 to)
        {
            _from[_count] = from;
            _to[_count] = to;
            _count++;
        }

        /// <summary>Draws the lines as they were last written.</summary>
        public void Apply()
        {
            Element.MarkDirtyRepaint();
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(ColorProperty, out _color);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            if (_count == 0)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            painter.lineWidth = LineWidth;
            painter.lineCap = LineCap.Round;
            painter.strokeColor = _color;
            for (int i = 0; i < _count; i++)
            {
                painter.BeginPath();
                painter.MoveTo(_from[i]);
                painter.LineTo(_to[i]);
                painter.Stroke();
            }
        }
    }
}
