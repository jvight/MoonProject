using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Anti-aliased vector shapes the UI paints with <see cref="Painter2D"/> instead of textures: soft shadows, rings
    /// and arcs. Stateless helpers; painters call them from generateVisualContent, which runs only when an element is
    /// marked dirty, never every frame.
    /// </summary>
    internal static class Shapes
    {
        /// <summary>Adds a closed rounded rectangle to the current path.</summary>
        public static void RoundedRect(Painter2D painter, Rect rect, float radius)
        {
            float r = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f);
            painter.MoveTo(new Vector2(rect.xMin + r, rect.yMin));
            painter.ArcTo(new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMax), r);
            painter.ArcTo(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax), r);
            painter.ArcTo(new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMin, rect.yMin), r);
            painter.ArcTo(new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin), r);
            painter.ClosePath();
        }

        /// <summary>
        /// A soft drop shadow: <paramref name="layers"/> stacked rounded rectangles, each a little larger and fainter,
        /// so the edge fades out over <paramref name="spread"/> pixels like a blur.
        /// </summary>
        public static void SoftShadow(Painter2D painter, Rect rect, float radius, float spread, Vector2 offset,
            Color color, int layers)
        {
            if (layers <= 0 || color.a <= 0f)
            {
                return;
            }

            Color layerColor = color;
            layerColor.a = color.a / layers;
            painter.fillColor = layerColor;
            for (int i = 0; i < layers; i++)
            {
                float grow = spread * (i + 1) / layers;
                Rect layer = new Rect(rect.x + offset.x - grow * 0.5f, rect.y + offset.y - grow * 0.5f,
                    rect.width + grow, rect.height + grow);
                painter.BeginPath();
                RoundedRect(painter, layer, radius + grow * 0.5f);
                painter.Fill();
            }
        }

        /// <summary>A full circle stroked around <paramref name="center"/>.</summary>
        public static void Ring(Painter2D painter, Vector2 center, float radius, float width, Color color)
        {
            painter.lineWidth = width;
            painter.strokeColor = color;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.ClosePath();
            painter.Stroke();
        }

        /// <summary>
        /// An arc from twelve o'clock, clockwise, covering <paramref name="fraction"/> (0..1) of the circle.
        /// </summary>
        public static void Arc(Painter2D painter, Vector2 center, float radius, float width, float fraction,
            Color color)
        {
            if (fraction <= 0f)
            {
                return;
            }

            painter.lineWidth = width;
            painter.strokeColor = color;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(-90f), Angle.Degrees(-90f + 360f * Mathf.Min(1f, fraction)));
            painter.Stroke();
        }

        /// <summary>A filled disc.</summary>
        public static void Disc(Painter2D painter, Vector2 center, float radius, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.ClosePath();
            painter.Fill();
        }

        /// <summary>A hexagonal nut (the scrap icon): six flat sides with a round hole.</summary>
        public static void Nut(Painter2D painter, Vector2 center, float radius, Color body, Color hole)
        {
            painter.fillColor = body;
            painter.BeginPath();
            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (60f * i + 30f);
                var corner = new Vector2(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius);
                if (i == 0)
                {
                    painter.MoveTo(corner);
                }
                else
                {
                    painter.LineTo(corner);
                }
            }

            painter.ClosePath();
            painter.Fill();
            Disc(painter, center, radius * 0.42f, hole);
        }
    }
}
