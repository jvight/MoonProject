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

        /// <summary>
        /// A cassette tape filling <paramref name="rect"/>: a rounded shell with a window showing its two reels.
        /// </summary>
        public static void Cassette(Painter2D painter, Rect rect, Color shell, Color window)
        {
            float height = rect.height;
            painter.fillColor = shell;
            painter.BeginPath();
            RoundedRect(painter, rect, height * 0.2f);
            painter.Fill();

            var glass = new Rect(rect.x + rect.width * 0.17f, rect.y + height * 0.24f, rect.width * 0.66f,
                height * 0.4f);
            painter.fillColor = window;
            painter.BeginPath();
            RoundedRect(painter, glass, glass.height * 0.5f);
            painter.Fill();

            float reel = glass.height * 0.3f;
            float offset = glass.width * 0.27f;
            Disc(painter, new Vector2(glass.center.x - offset, glass.center.y), reel, shell);
            Disc(painter, new Vector2(glass.center.x + offset, glass.center.y), reel, shell);
        }
    }
}
