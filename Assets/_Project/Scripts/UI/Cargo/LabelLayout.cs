using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// Where the socket names sit on the 07 tab's portrait so none overlaps another (pure, EditMode-tested): sockets in
    /// the left half of the picture get their name in a column down the left edge, the others in a column down the
    /// right edge, each name as near its own height as the names above it allow. The sockets themselves stay on their
    /// true points; a short leader line joins each to its name. Allocation-free.
    /// </summary>
    internal static class LabelLayout
    {
        /// <summary>
        /// Fills <paramref name="centreY"/> and <paramref name="right"/> for every index of
        /// <paramref name="points"/> that <paramref name="wants"/> a name.
        /// </summary>
        /// <param name="points">True socket points in pixels of the portrait, origin top left.</param>
        /// <param name="wants">Which sockets carry a name.</param>
        /// <param name="width">Portrait width in pixels.</param>
        /// <param name="height">Portrait height in pixels.</param>
        /// <param name="labelHeight">Height of one name in pixels.</param>
        /// <param name="gap">Pixels kept between two names in a column.</param>
        /// <param name="centreY">Out: each name's vertical centre.</param>
        /// <param name="right">Out: true when the name sits in the right column.</param>
        public static void Place(Vector2[] points, bool[] wants, float width, float height, float labelHeight,
            float gap, float[] centreY, bool[] right)
        {
            if (points.Length != wants.Length || points.Length != centreY.Length || points.Length != right.Length)
            {
                throw new ArgumentException("One entry per socket.");
            }

            for (int i = 0; i < points.Length; i++)
            {
                right[i] = points[i].x >= width * 0.5f;
            }

            PlaceColumn(points, wants, right, false, height, labelHeight, gap, centreY);
            PlaceColumn(points, wants, right, true, height, labelHeight, gap, centreY);
        }

        private static void PlaceColumn(Vector2[] points, bool[] wants, bool[] right, bool rightColumn, float height,
            float labelHeight, float gap, float[] centreY)
        {
            Span<int> order = stackalloc int[points.Length];
            int count = 0;
            for (int i = 0; i < points.Length; i++)
            {
                if (!wants[i] || right[i] != rightColumn)
                {
                    continue;
                }

                int at = count++;
                while (at > 0 && points[order[at - 1]].y > points[i].y)
                {
                    order[at] = order[at - 1];
                    at--;
                }

                order[at] = i;
            }

            float half = labelHeight * 0.5f;
            float pitch = labelHeight + gap;
            for (int k = 0; k < count; k++)
            {
                float y = Mathf.Clamp(points[order[k]].y, half, height - half);
                centreY[order[k]] = k == 0 ? y : Mathf.Max(y, centreY[order[k - 1]] + pitch);
            }

            // Names pushed past the bottom edge come back up, keeping their order and spacing.
            for (int k = count - 1; k >= 0; k--)
            {
                float limit = k == count - 1 ? height - half : centreY[order[k + 1]] - pitch;
                centreY[order[k]] = Mathf.Min(centreY[order[k]], limit);
            }
        }
    }
}
